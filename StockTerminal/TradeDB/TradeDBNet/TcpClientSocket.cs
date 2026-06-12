using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Utils;
using Logger;
using StockTerminal;

namespace TradeDB.Net
{
    public class TcpClientSocket
    {
        /*
        private class BufferPool
        {
            public class BufferItem
            {
                public readonly byte[] Buffer;
                public readonly TcpClientSocket TcpSocket;

                public BufferItem(TcpClientSocket TcpSocket, int Length)
                {
                    this.TcpSocket = TcpSocket;
                    Buffer = new byte[Length];
                }
            }

            private readonly object PoolMutex = new object();
            private readonly Stack<BufferItem> Pool = new Stack<BufferItem>(100);

            private int pMinimumBufferLength = 10000;
            private TcpClientSocket pTcpSocket = null;

            public BufferPool(TcpClientSocket TcpSocket, int MinimumBufferLength)
            {
                this.pTcpSocket = TcpSocket;
                this.MinimumBufferLength = MinimumBufferLength;
            }

            public TcpClientSocket TcpSocket
            {
                get { return pTcpSocket; }
            }

            public int MinimumBufferLength
            {
                get { return pMinimumBufferLength; }
                set { pMinimumBufferLength = value > 0 ? value : 1; }
            }

            public BufferItem Get(int Length)
            {
                BufferItem item = null;

                if (Length < pMinimumBufferLength) Length = pMinimumBufferLength;

                lock (PoolMutex)
                {
                    if (Pool.Count > 0)
                    {
                        item = Pool.Pop();
                        if (item != null && item.Buffer.Length < Length) item = null;
                    }
                }

                if (item == null)
                    item = new BufferItem(pTcpSocket, Length);

                return item;
            }

            public void Put(BufferItem Item)
            {
                if (Item != null)
                {
                    lock (PoolMutex)
                    {
                        Pool.Push(Item);
                    }
                }
            }
        }
         */

        public delegate void CompleteDelgate();
        public delegate void DataArrivalDelegate(MessageRaw MsgRaw);

        public CompleteDelgate ConnectComplete = null;
        public CompleteDelgate SendComplete = null;
        public DataArrivalDelegate DataArrival = null;

        public volatile bool MustCloseBeforeAllowConnect = false;

        private Socket TheSocket = null;
        private IPEndPoint pRemoteEP = null;

        //private readonly BufferPool SendBufferPool;

        public MessageRaw.MessageRawPool MsgRawPool;

        public TcpClientSocket()
        {
            //SendBufferPool = new BufferPool(this, 10000);
        }

        public IPEndPoint RemoteEP
        {
            get { return pRemoteEP; }
            set
            {
                if (value != null)
                {
                    pRemoteEP = new IPEndPoint(value.Address, value.Port);
                }
                else
                {
                    pRemoteEP = null;
                }
            }
        }

        public bool IsConnected
        {
            get { return (!MustCloseBeforeAllowConnect && TheSocket != null && TheSocket.Connected); }
        }

        public void Close()
        {
            if (TheSocket != null)
            {
                TheSocket.Close();
                TheSocket = null;
            }
        }

        public bool Connect(out string ErrorMessage)
        {
            bool success = true;
            ErrorMessage = null;

            if (pRemoteEP != null)
            {
                TheSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //TheSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, false);
                //TheSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.DontLinger, new LingerOption(true, 0));
                TheSocket.ReceiveBufferSize = 524288;

                MustCloseBeforeAllowConnect = false;

                try
                {
                    TheSocket.BeginConnect(pRemoteEP, ConnectCallBack, this);
                }
                catch (Exception exp)
                {
                    success = false;
                    ErrorMessage = exp.Message;
                    MustCloseBeforeAllowConnect = true;

                    if (ConnectComplete != null) ConnectComplete.Invoke();
                }
            }
            else
            {
                success = false;
                ErrorMessage = "No remote endpoint assigned.";
                Close();
                if (ConnectComplete != null) ConnectComplete.Invoke();
            }

            return success;
        }

        private static void ConnectCallBack(IAsyncResult ar)
        {
            TcpClientSocket tcs = (TcpClientSocket)ar.AsyncState;

            try
            {
                tcs.TheSocket.EndConnect(ar);
            }
            catch
            {
                tcs.MustCloseBeforeAllowConnect = true;
            }

            if (tcs.ConnectComplete != null) tcs.ConnectComplete.Invoke();
        }

        public bool Send(MessageRaw MsgRaw, out string ErrorMessage)
        {
            bool success = true;
            ErrorMessage = null;

            if (MsgRaw != null && MsgRaw.Count > 0)
            {
                if (TheSocket != null && TheSocket.Connected)
                {
                    try
                    {
                        TheSocket.BeginSend(MsgRaw.Data, 0, MsgRaw.Count, SocketFlags.None, SendCallBack, MsgRaw);
                    }
                    catch (Exception exp)
                    {
                        success = false;
                        ErrorMessage = exp.Message;
                        MustCloseBeforeAllowConnect = true;

                        if (SendComplete != null) SendComplete.Invoke();
                    }
                }
                else
                {
                    success = false;
                    ErrorMessage = "No socket connected.";

                    if (SendComplete != null) SendComplete.Invoke();
                }
            }

            return success;
        }

        private void SendCallBack(IAsyncResult ar)
        {
            MessageRaw msgRaw = (MessageRaw)ar.AsyncState;

            SocketError errorCode = 0;

            try
            {
                TheSocket.EndSend(ar, out errorCode);
            }
            catch
            {
                MustCloseBeforeAllowConnect = true;
            }

            msgRaw.Recycle();

            if (SendComplete != null)
                SendComplete.Invoke();
        }

        public void BeginReceive()
        {
            MessageRaw msgRaw;

            if (TheSocket != null && TheSocket.Connected)
            {
                msgRaw = MsgRawPool.Get(100000);

                try
                {
                    TheSocket.BeginReceive(msgRaw.Data, 0, msgRaw.Data.Length, SocketFlags.None, ReceiveCallBack, msgRaw);
                }
                catch
                {
                    MustCloseBeforeAllowConnect = true;
                    if (DataArrival != null) DataArrival.Invoke(null);
                }
            }
            else if (DataArrival != null) 
                DataArrival.Invoke(null);
        }

        private void ReceiveCallBack(IAsyncResult ar)
        {
            SocketError errorCode = 0;

            MessageRaw msgRaw = (MessageRaw)ar.AsyncState;

            try
            {
                msgRaw.Count = TheSocket.EndReceive(ar, out errorCode);
            }
            catch
            {
                MustCloseBeforeAllowConnect = true;
            }

            if (errorCode == SocketError.Success)
            {
                if (msgRaw.Count > 0)
                {
                    if (DataArrival != null) DataArrival.Invoke(msgRaw);
                    BeginReceive();
                }
                else
                {
                    if (DataArrival != null) DataArrival.Invoke(null);
                    Close();
                }
            }
            else if (DataArrival != null) 
                DataArrival.Invoke(null);
        }
    }
}
