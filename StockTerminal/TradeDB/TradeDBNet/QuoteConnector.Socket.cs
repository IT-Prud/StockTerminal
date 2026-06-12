using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ComponentAce.Compression.Libs.zlib;
using Utils;
using Processors;

namespace TradeDB.Net
{
    public partial class QuoteConnector : Processor
    {
        #region Socket operations

        private void OnSocketConnectComplete()
        {
            ProcessEvent.Set();
        }

        private void OnSocketDataArrival(MessageRaw MsgRaw)
        {
            if (MsgRaw != null)
            {
                MsgRawQueue.Enqueue(MsgRaw);
                ProcessEvent.Set();
            }
        }

        private void ProcessReceived()
        {
            MessageRaw msgRaw;

            int dataLength = 0;
            int startIndex = 0;
            List<TradeMessage> msgList;
            List<TradeMessage> msgDecompList;
            TradeMessage msg;

            do
            {
                while ((msgRaw = MsgRawQueue.Dequeue()) != null)
                {
                    MsgBuffer.Append(msgRaw.Data, msgRaw.Count, false);
                    msgRaw.Recycle();

                    if (MsgBuffer.SpaceAvailable < 9999)
                        break;
                }

                if (MsgBuffer.SpaceOccupied > 0)
                {
                    startIndex = 0;
                    msgList = TradeMessage.Parse(MsgBuffer.UnderBuffer, ref startIndex, MsgBuffer.SpaceOccupied, true);
                    MsgBuffer.BytesRead = startIndex;

                    if (msgList != null && msgList.Count > 0)
                    {
                        for (int i = 0; i < msgList.Count; i++)
                        {
                            msg = msgList[i];

                            switch (msg.MessageId)
                            {
                                case "ZIP":
                                    dataLength = Uncompress(msg.MessageBody, DcpBuffer, 0, DcpBuffer.Length);
                                    startIndex = 0;
                                    msgDecompList = TradeMessage.Parse(DcpBuffer, ref startIndex, dataLength, true);
                                    if (msgDecompList != null && msgDecompList.Count > 0)
                                    {
                                        TimeLastReceive = ProcessTimer.TickCount;

                                        for (int j = 0; j < msgDecompList.Count; j++)
                                        {
                                            if (msgDecompList[j].MessageId == "LRE")
                                                SessionMessageQueue.Enqueue(msgDecompList[j]);
                                            else
                                                MsgArrival.Invoke(msgDecompList[j]);

                                            AppendInLog("In", msgDecompList[j].ToString());
                                        }
                                    }
                                    break;
                                case "LRE":
                                    TimeLastReceive = ProcessTimer.TickCount;
                                    SessionMessageQueue.Enqueue(msg);
                                    AppendInLog("In", msg.ToString());
                                    break;

                                case "INV":
                                    AppendInLog("In", msg.ToString());
                                    break;

                                default:
                                    TimeLastReceive = ProcessTimer.TickCount;
                                    MsgArrival.Invoke(msg);

                                    if (msg.MessageId != "SRE" || (msg.MessageType != "20" && msg.MessageType != "25" && msg.MessageType != "28"))
                                        AppendInLog("In", msg.ToString());
                                    break;
                            }
                        }
                    }
                }
            } while (msgRaw != null);
        }

        private static byte[] Compress(byte[] pSrc)
        {
            MemoryStream DestStream = new MemoryStream();
            ZOutputStream zStream = new ZOutputStream(DestStream, zlibConst.Z_BEST_COMPRESSION);

            zStream.Write(pSrc, 0, pSrc.Length);
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

        #endregion
    }
}
