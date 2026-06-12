using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ComponentAce.Compression.Libs.zlib;
using Processors;


namespace TradeDB.Net
{
    public class UdpData : Processor
    {
        #region "Define delegates for new ProcessState"

        public delegate void MsgArrivalDelegate(TradeMessage Message);

        public new enum ProcessState { Stopped, Connecting, Ready, Disconnecting, MaxState };

        protected override void InitializeProcessDelegates()
        {
            ProcessDelegates = new ProcessDelegate[(int)ProcessState.MaxState];
            ProcessDelegates[(int)ProcessState.Stopped] = new ProcessDelegate(ProcessStopped);
            ProcessDelegates[(int)ProcessState.Connecting] = new ProcessDelegate(ProcessConnecting);
            ProcessDelegates[(int)ProcessState.Ready] = new ProcessDelegate(ProcessReady);
            ProcessDelegates[(int)ProcessState.Disconnecting] = new ProcessDelegate(ProcessDisconnecting);
        }

        #endregion


        public IPEndPoint TheIPEndPoint = null;

        public MsgArrivalDelegate MsgArrival = null;
        
        private Socket pUdpSocket = null;
        private int pUdpSocketBeginReceive = 0, pUdpSocketBeginReceiveCallBack = 0;
        private TradeMessageBuffer MsgBuffer = new TradeMessageBuffer(204800);
        private TradeMessageBuffer MsgDecompBuffer = new TradeMessageBuffer(204800);


        #region "State Process Delegates"

        protected override void Preprocess(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
            if (Command == ProcessCommand.Start)
            {
                if (NextState == (int)ProcessState.Stopped)
                {
                    NextState = (int)ProcessState.Connecting;
                }
            }
            else if (Command == ProcessCommand.Stop)
            {
                if (NextState != (int)ProcessState.Stopped)
                {
                    NextState = (int)ProcessState.Disconnecting;
                }
            }
        }

        protected override void ProcessStopped(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
            if (Command == ProcessCommand.Start)
            {
                NextState = (int)ProcessState.Connecting;
            }
        }

        private void ProcessConnecting(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
            if (pUdpSocket == null)
            {
                if (TheIPEndPoint != null)
                {
                    pUdpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    pUdpSocket.ReceiveBufferSize = 1048576;
                }
                else
                {
                    NextState = (int)ProcessState.Disconnecting;
                    WaitTime = 0;
                }
            }

            if (pUdpSocket != null)
            {
                try
                {
                    pUdpSocket.Bind(TheIPEndPoint);
                    NextState = (int)ProcessState.Ready;
                    WaitTime = 0;
                }
                catch
                {
                    UdpClose();
                    WaitTime = 10000000L;
                }
            }
        }

        private void ProcessReady(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
            UdpBeginReceive();
        }

        private void ProcessDisconnecting(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
            if (pUdpSocket != null)
            {
                UdpClose();
            }

            if (Command == ProcessCommand.Start)
            {
                NextState = (int)ProcessState.Connecting;      // for Auto Reconnect
                //Stop();                                      // for Manual Reconnect
                //NextState = (int)ProcessState.Stopped;
            }
            else
            {
                NextState = (int)ProcessState.Stopped;
            }
            WaitTime = 0;
        }

        #endregion


        private void UdpBeginReceive()
        {
            int orgInterlockedValue = Interlocked.CompareExchange(ref pUdpSocketBeginReceive, 1, 0);
            if (orgInterlockedValue == 0 || orgInterlockedValue == 1)
            {
                if (pUdpSocket != null)
                {
                    try
                    {
                        pUdpSocket.BeginReceive(MsgBuffer.Buffer, MsgBuffer.StartIndex, MsgBuffer.SpaceAvailable,
                                    SocketFlags.Partial, new AsyncCallback(UdpBeginReceiveCallBack), this);
                    }
                    catch { }
                }

                pUdpSocketBeginReceive = 0;
            }
            else
            {
                //int aaa = 0;
            }
        }

        private static void UdpBeginReceiveCallBack(IAsyncResult ar)
        {
            UdpData ud = (UdpData)ar.AsyncState;
            SocketError errorCode;
            int receivedCount;
            int orgInterlockedValue;

            orgInterlockedValue = Interlocked.CompareExchange(ref ud.pUdpSocketBeginReceiveCallBack, 1, 0);
            if (orgInterlockedValue == 0 || orgInterlockedValue == 1)
            {
                if (ud.pUdpSocket != null)
                {
                    try
                    {
                        receivedCount = ud.pUdpSocket.EndReceive(ar, out errorCode);
                        if (errorCode == SocketError.Success)
                        {
                            if (receivedCount > 0)
                            {
                                ud.MsgBuffer.BytesAppended = receivedCount;
                            }
                        }
                    }
                    catch { }

                    ud.DispatchMessage();
                    ud.pUdpSocketBeginReceiveCallBack = 0;
                    ud.UdpBeginReceive();
                }
                else
                {
                    ud.pUdpSocketBeginReceiveCallBack = 0;
                }
            }
            else
            {
                //int aaa = 0;
            }
        }

        private void UdpClose()
        {
            if (pUdpSocket != null)
            {
                while (Interlocked.CompareExchange(ref pUdpSocketBeginReceive, 2, 0) != 0) Thread.Sleep(100);
                while (Interlocked.CompareExchange(ref pUdpSocketBeginReceiveCallBack, 2, 0) != 0) Thread.Sleep(100);

                pUdpSocket.Shutdown(SocketShutdown.Both);
                pUdpSocket.Close(1000);
                pUdpSocket = null;

                pUdpSocketBeginReceive = 0;
                pUdpSocketBeginReceiveCallBack = 0;
            }
        }

        private void DispatchMessage()
        {
            List<TradeMessage> msgList = MsgBuffer.GetMessages(false);

            if (msgList != null)
            {
                List<TradeMessage> msgDecompList = null;
                TradeMessage msg;
                int i, j, BytesAppended;

                for (i = 0; i < msgList.Count; i++)
                {
                    msg = msgList[i];

                    if (msg.MessageId == "ZIP")
                    {
                        BytesAppended = Uncompress(msg.MessageBody, MsgDecompBuffer.Buffer,
                            MsgDecompBuffer.StartIndex, MsgDecompBuffer.SpaceAvailable);

                        if (BytesAppended > 0)
                        {
                            MsgDecompBuffer.BytesAppended = BytesAppended;
                            msgDecompList = MsgDecompBuffer.GetMessages(false);

                            if (msgDecompList != null && msgDecompList.Count > 0)
                            {
                                for (j = 0; j < msgDecompList.Count; j++)
                                {
                                    MsgArrival.Invoke(msgDecompList[j]);
                                }
                            }
                        }
                        else
                        {   // unzip failed
                            MsgArrival.Invoke(msg);
                        }
                    }
                    else
                    {
                        MsgArrival.Invoke(msg);
                    }
                }
            }
        }

        private static int Uncompress(byte[] pSrc, byte[] pDest, int StartIndex, int SpaceAvailable)
        {
            MemoryStream DestStream = new MemoryStream();
            ZOutputStream zStream = new ZOutputStream(DestStream);

            zStream.Write(pSrc, 0, pSrc.Length);
            zStream.Flush();
            zStream.Close();

            long lSize = DestStream.Position;
            if (lSize > SpaceAvailable) lSize = SpaceAvailable;

            byte[] pTemp = DestStream.ToArray();
            Buffer.BlockCopy(pTemp, 0, pDest, StartIndex, (int)lSize);

            DestStream.Close();
            DestStream.Dispose();

            return (int)lSize;
        }
    }
}
