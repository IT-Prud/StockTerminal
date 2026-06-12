using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using TradeDB.Net;
using ComponentAce.Compression.Libs.zlib;

namespace StockTerminal.Utils
{
    public class FileFetcher
    {
        private string pFullPath = null;
        private string pBasePath = null;
        private string pSubPath = null;
        private byte[] SubPathBytes = null;
        private FileStream Fs = null;
        private byte[] BufferRaw = new byte[99990];
        private int BufferRawTail = 0;

        public FileFetcher(string BasePath, string SubPath)
        {
            if (BasePath == null || SubPath == null) return;

            pBasePath = BasePath;
            pSubPath = SubPath;
            pFullPath = Path.Combine(pBasePath, pSubPath);
            byte[] buf = Encoding.UTF8.GetBytes(pSubPath);
            int n = buf.Length;
            SubPathBytes = new byte[n + 2];
            SubPathBytes[0] = (byte)(n >> 8);
            SubPathBytes[1] = (byte)(n & 255);
            Buffer.BlockCopy(buf, 0, SubPathBytes, 2, n);
        }

        public string FullPath
        {
            get { return pFullPath; }
        }

        public string BasePath
        {
            get { return pBasePath; }
        }

        public string SubPath
        {
            get { return pSubPath; }
        }

        public List<TradeMessage> GetNext()
        {
            if (pFullPath == null) return null;

            if (Fs == null)
            {
                try
                {
                    Fs = new FileStream(pFullPath, FileMode.Open, FileAccess.Read);
                }
                catch
                {
                    return null;
                }
            }

            try
            {
                BufferRawTail = Fs.Read(BufferRaw, 0, BufferRaw.Length);
            }
            catch
            {
                Fs.Close();
                return null;
            }

            byte[] bufferCompressed = Compress(BufferRaw, BufferRawTail);

            List<TradeMessage> msgList = new List<TradeMessage>(20);

            int i = 0, n = bufferCompressed.Length;
            int m = (n & -2048); // make it integral of 2048
            int subPathByteCount = SubPathBytes.Length;
            int msgBodyLength = 2048 + subPathByteCount;
            TradeMessage msg = null;

            for (i = 0; i < m; i+=2048)
            {
                msg = new TradeMessage();
                msg.MessageId = "FIL";
                msg.MessageType = "01";
                msg.MessageBody = new byte[msgBodyLength];
                SubPathBytes.CopyTo(msg.MessageBody, 0);
                Buffer.BlockCopy(bufferCompressed, i, msg.MessageBody, subPathByteCount, 2048);
                msgList.Add(msg);
            }
            if (m < n)
            {
                msg = new TradeMessage();
                msg.MessageId = "FIL";
                msg.MessageType = "01";
                msg.MessageBody = new byte[n - m + subPathByteCount];
                SubPathBytes.CopyTo(msg.MessageBody, 0);
                Buffer.BlockCopy(bufferCompressed, i, msg.MessageBody, subPathByteCount, n - m);
                msgList.Add(msg);
            }

            return msgList;
        }

        private static byte[] Compress(byte[] pSrc, int Length)
        {
            MemoryStream DestStream = new MemoryStream();
            ZOutputStream zStream = new ZOutputStream(DestStream, zlibConst.Z_BEST_COMPRESSION);

            zStream.Write(pSrc, 0, Length);
            zStream.Flush();
            zStream.Close();

            long lSize = DestStream.Position;
            byte[] pTemp = DestStream.ToArray();
            byte[] pDest = new byte[lSize];
            Buffer.BlockCopy(pTemp, 0, pDest, 0, (int)lSize);

            DestStream.Close();
            DestStream.Dispose();

            return pDest;
        }
    }
}
