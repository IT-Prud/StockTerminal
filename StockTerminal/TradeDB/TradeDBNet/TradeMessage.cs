using System;
using System.Collections.Generic;
using System.Text;

namespace TradeDB.Net
{
    public class TradeMessage
    {
        public string MessageId;
        public int pMessageLength;
        public string SourceId;
        public string DestinationId;
        public string MessageType;
        private byte[] pMessageBody;
        private Dictionary<string, TradeMessageTag> pTags = null;
        private List<string> pAnonymousTags = null;
        private Dictionary<string, byte[]> pFieldIDLengthTags = null;
        private byte pLRC;
        public const byte MessageTrailer = 3;

        private byte[] pRaw;

        //private static byte[] HexToInt = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 0, 0, 0, 0, 0, 0, 10, 11, 12, 13, 14, 15 };
        private static byte[] HexToInt = new byte[256];
        private static byte[] IntToHex = new byte[] { 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 65, 66, 67, 68, 69, 70 };

        static TradeMessage()
        {
            HexToInt[48] = 0;   HexToInt[49] = 1;   HexToInt[50] = 2;   HexToInt[51] = 3;
            HexToInt[52] = 4;   HexToInt[53] = 5;   HexToInt[54] = 6;   HexToInt[55] = 7;
            HexToInt[56] = 8;   HexToInt[57] = 9;

            HexToInt[65] = 10;  HexToInt[66] = 11;  HexToInt[67] = 12;
            HexToInt[68] = 13;  HexToInt[66] = 14;  HexToInt[67] = 15;

            HexToInt[97] = 10;  HexToInt[98] = 11;  HexToInt[99] = 12;
            HexToInt[100] = 13; HexToInt[101] = 14; HexToInt[102] = 15;
        }

        public static int GetMessageLength(byte[] Raw, int Index)
        {
            if (Raw[Index + 3] < 48 || Raw[Index + 3] > 57) return -1;
            if (Raw[Index + 4] < 48 || Raw[Index + 4] > 57) return -1;
            if (Raw[Index + 5] < 48 || Raw[Index + 5] > 57) return -1;
            if (Raw[Index + 6] < 48 || Raw[Index + 6] > 57) return -1;

            return (Raw[Index + 3] - 48) * 1000 + (Raw[Index + 4] - 48) * 100 + (Raw[Index + 5] - 48) * 10 + (Raw[Index + 6] - 48);
        }

        public static List<TradeMessage> Parse(byte[] Raw, ref int StartIndex, int EndIndex, bool KeepRaw)
        {
            if (Raw.Length < EndIndex) EndIndex = Raw.Length;

            int dataCount = EndIndex - StartIndex;
            if (dataCount < 20) return null;    // data bytes less than minimum message length

            int estimatedMsgCount = dataCount >> 4; // >> 4 is an estimation of minimum message length
            List<TradeMessage> msgs = new List<TradeMessage>(estimatedMsgCount);
            TradeMessage msg = null;

            int Index = StartIndex;
            //int IndexLastMessage = StartIndex;
            int MaxLastMsgIndex = EndIndex - 20;
            int msgLength;
            string header;

            //bool Valid = true;

            while (Index <= MaxLastMsgIndex)
            {
                if (Raw[Index] >= 65 && Raw[Index] <= 90 &&
                    Raw[Index + 1] >= 65 && Raw[Index + 1] <= 90 &&
                    Raw[Index + 2] >= 65 && Raw[Index + 2] <= 90 &&
                    Raw[Index + 3] >= 48 && Raw[Index + 3] <= 57 &&
                    Raw[Index + 4] >= 48 && Raw[Index + 4] <= 57 &&
                    Raw[Index + 5] >= 48 && Raw[Index + 5] <= 57 &&
                    Raw[Index + 6] >= 48 && Raw[Index + 6] <= 57)
                {
                    msgLength = (Raw[Index + 3] - 48) * 1000 + (Raw[Index + 4] - 48) * 100 + (Raw[Index + 5] - 48) * 10 + (Raw[Index + 6] - 48);
                    if ((Index + 8 + msgLength) <= EndIndex)
                    {
                        if (Raw[Index + msgLength + 7] == MessageTrailer)
                        {
                            /*
                            if (Valid != true)
                            {
                                if (Index > IndexLastMessage)
                                {
                                    msg = new TradeMessage();
                                    msg.MessageId = "INV";
                                    msg.SourceId = "ALID";
                                    msg.DestinationId = " MSG";
                                    msg.MessageType = "00";
                                    msg.pMessageBody = new byte[Index - IndexLastMessage];
                                    Array.Copy(Raw, IndexLastMessage, msg.pMessageBody, 0, Index - IndexLastMessage);
                                    msg.pTags = null;
                                    msg.pAnonymousTags = null;
                                    msgs.Add(msg);
                                }
                                Valid = true;
                            }
                             */

                            header = Encoding.ASCII.GetString(Raw, Index, 17);

                            msg = new TradeMessage();

                            if (KeepRaw)
                            {
                                msg.pRaw = new byte[msgLength + 8];
                                Buffer.BlockCopy(Raw, Index, msg.pRaw, 0, msg.pRaw.Length);
                            }

                            msg.MessageId = header.Substring(0, 3);
                            msg.SourceId = header.Substring(7, 4);
                            msg.DestinationId = header.Substring(11, 4);
                            msg.MessageType = header.Substring(15, 2);

                            msg.pMessageBody = new byte[msgLength - 12];
                            Array.Copy(Raw, Index + 17, msg.pMessageBody, 0, msgLength - 12);
                            msg.pTags = null;
                            msg.pAnonymousTags = null;

                            msg.pLRC = (byte)((HexToInt[Raw[Index + msgLength + 5]] << 4) |
                                HexToInt[Raw[Index + msgLength + 6]]);

                            msg.pMessageLength = msgLength;

                            msgs.Add(msg);

                            Index += msgLength + 8;
                            //IndexLastMessage = Index;
                        }
                        else
                        {
                            //Valid = false;
                            Index++;
                        }
                    }
                    else { break; } // incomplete message detected
                }
                else
                {
                    //Valid = false;
                    Index++;
                }
            }

            StartIndex = Index;

            return msgs;
        }

        public int TotalLength
        {
            get
            {
                return ((pMessageBody != null) ? pMessageBody.Length : 0) + 20;
            }
        }

        public byte[] Raw
        {
            get { return pRaw; }
        }

        public byte[] MessageBody
        {
            get
            {
                return pMessageBody;
            }

            set
            {
                pMessageBody = value;
                pTags = null;
                pAnonymousTags = null;
            }
        }

        public string LRC
        {
            get
            {
                return pLRC.ToString("X2");
            }
        }

        public MessageRaw GetRaw(MessageRaw.MessageRawPool Pool)
        {
            MessageRaw msgRaw;

            pMessageLength = ((pMessageBody != null) ? pMessageBody.Length : 0) + 12;
            int totalLength = pMessageLength + 8;

            msgRaw = Pool.Get(totalLength);
            msgRaw.Count = totalLength;

            int idx = 0;
            string field;

            field = MessageId ?? "   ";
            Encoding.ASCII.GetBytes(field, 0, field.Length, msgRaw.Data, idx);
            idx += field.Length;

            field = pMessageLength.ToString("0000");
            Encoding.ASCII.GetBytes(field, 0, field.Length, msgRaw.Data, idx);
            idx += field.Length;

            field = (SourceId ?? "    ").PadRight(4, ' ');
            Encoding.ASCII.GetBytes(field, 0, field.Length, msgRaw.Data, idx);
            idx += field.Length;

            field = (DestinationId ?? "    ").PadRight(4, ' ');
            Encoding.ASCII.GetBytes(field, 0, field.Length, msgRaw.Data, idx);
            idx += field.Length;

            field = (MessageType ?? "  ").PadRight(2, ' ');
            Encoding.ASCII.GetBytes(field, 0, field.Length, msgRaw.Data, idx);
            idx += field.Length;

            if (pMessageBody != null)
            {
                Buffer.BlockCopy(pMessageBody, 0, msgRaw.Data, idx, pMessageBody.Length);
                idx += pMessageBody.Length;
            }

            int n = pMessageLength - 2;
            byte c = msgRaw.Data[7];

            for (int i = 8; i < n; i++)
                c ^= msgRaw.Data[i];

            pLRC = c;
            msgRaw.Data[idx] = IntToHex[(c & 240) >> 4];
            msgRaw.Data[idx + 1] = IntToHex[c & 15];
            msgRaw.Data[idx + 2] = MessageTrailer;

            return msgRaw;
        }

        public int ToRaw(byte[] DataBuffer, int Index)
        {
            pMessageLength = ((pMessageBody != null) ? pMessageBody.Length : 0) + 12;
            int totalLength = pMessageLength + 8;

            int idx = 0;
            string field;

            field = MessageId ?? "   ";
            Encoding.ASCII.GetBytes(field, 0, field.Length, DataBuffer, idx);
            idx += field.Length;

            field = pMessageLength.ToString("0000");
            Encoding.ASCII.GetBytes(field, 0, field.Length, DataBuffer, idx);
            idx += field.Length;

            field = (SourceId ?? "    ").PadRight(4, ' ');
            Encoding.ASCII.GetBytes(field, 0, field.Length, DataBuffer, idx);
            idx += field.Length;

            field = (DestinationId ?? "    ").PadRight(4, ' ');
            Encoding.ASCII.GetBytes(field, 0, field.Length, DataBuffer, idx);
            idx += field.Length;

            field = (MessageType ?? "  ").PadRight(2, ' ');
            Encoding.ASCII.GetBytes(field, 0, field.Length, DataBuffer, idx);
            idx += field.Length;

            if (pMessageBody != null)
            {
                Buffer.BlockCopy(pMessageBody, 0, DataBuffer, idx, pMessageBody.Length);
                idx += pMessageBody.Length;
            }

            int n = pMessageLength - 2;
            byte c = DataBuffer[7];

            for (int i = 8; i < n; i++)
                c ^= DataBuffer[i];

            pLRC = c;
            DataBuffer[idx] = IntToHex[(c & 240) >> 4];
            DataBuffer[idx + 1] = IntToHex[c & 15];
            DataBuffer[idx + 2] = MessageTrailer;

            return totalLength;
        }

        public override string ToString()
        {
            byte[] buffer = new byte[TotalLength];
            ToRaw(buffer, 0);
            return Encoding.ASCII.GetString(buffer);
        }

        public Dictionary<string, TradeMessageTag> Tags
        {
            get
            {
                if (pMessageBody == null || pMessageBody.Length < 0) return null;
                if (pTags == null)
                {
                    pTags = new Dictionary<string, TradeMessageTag>(10);

                    string body = Encoding.UTF8.GetString(pMessageBody);
                    string[] parts = body.Split('|');
                    int i, n = parts.Length;

                    for (i = 0; i < n; i++)
                    {
                        TradeMessageTag tag = new TradeMessageTag(parts[i]);
                        if (tag.Id.Length > 0)
                        {
                            try
                            {
                                pTags.Add(tag.Id, tag);
                            }
                            catch
                            {
                                pTags[tag.Id] = tag;    // overwrite existing tag
                            }
                        }
                    }
                }
                return pTags;
            }

            set
            {
                pAnonymousTags = null;
                pFieldIDLengthTags = null;
                if (value == null)
                {
                    pTags = null;
                    pMessageBody = null;
                }
                else
                {
                    pTags = value;
                    
                    StringBuilder sb = new StringBuilder(200);

                    if (value.Count > 0)
                    {
                        string s = "";
                        foreach (KeyValuePair<string, TradeMessageTag> kvp in value)
                        {
                            sb.Append(s);
                            sb.Append(kvp.Value.ToString());
                            s = "|";
                        }
                    }

                    pMessageBody = Encoding.UTF8.GetBytes(sb.ToString());
                }
            }
        }

        public List<string> AnonymousTags
        {
            get
            {
                if (pMessageBody == null || pMessageBody.Length < 0) return null;
                if (pAnonymousTags == null)
                {
                    pAnonymousTags = new List<string>(10);

                    string body = Encoding.UTF8.GetString(pMessageBody);

                    string value = body.Substring(0, 2);    // sub-type of message
                    pAnonymousTags.Add(value);

                    int bodyLength = body.Length;
                    int fieldLength;
                    int idx = 3;
                    int k;
                    int j = body.IndexOf(':', idx);

                    while (idx < bodyLength)
                    {
                        if (j <= 0) return null;

                        value = null;

                        if (j > idx && int.TryParse(body.Substring(idx, j - idx), out fieldLength))  // length found, process field
                        {
                            value = body.Substring(j + 1, fieldLength).Trim();
                            idx = j + 1 + fieldLength;
                            j = body.IndexOf(':', idx);
                        }
                        else // length not found, detect length
                        {
                            idx = j + 1;
                            j = body.IndexOf(':', idx); // find next colon
                            if (j > idx)
                            {
                                k = body.LastIndexOf(' ', idx, j - idx);    // find the last space between 2 colons
                                if (k > idx)
                                {
                                    value = body.Substring(idx, k - idx).Trim();
                                }
                                idx = k + 1;
                            }
                            else
                            {
                                value = body.Substring(idx).Trim();
                            }
                        }

                        if (value != null)
                        {
                            pAnonymousTags.Add(value);
                        }
                    }
                }
                return pAnonymousTags;
            }

            set
            {
                pTags = null;
                pFieldIDLengthTags = null;
                if (value == null)
                {
                    pAnonymousTags = null;
                    pMessageBody = null;
                }
                else
                {
                    pAnonymousTags = value;

                    StringBuilder sb = new StringBuilder(200);

                    if (value.Count > 0)
                    {
                        int n = pAnonymousTags.Count;

                        if (n >= 1) sb.Append(pAnonymousTags[0].Trim());
                        if (n >= 2)
                        {
                            sb.Append("T:");
                            sb.Append(pAnonymousTags[1].Trim());
                        }

                        for (int i = 2; i < n; i++)
                        {
                            sb.Append(' ');
                            sb.Append(pAnonymousTags[i].Length);
                            sb.Append(':');
                            sb.Append(pAnonymousTags[i]);
                        }
                    }

                    pMessageBody = Encoding.UTF8.GetBytes(sb.ToString());
                }
            }
        }

        public Dictionary<string, byte[]> FieldIDLengthTags
        {
            get
            {
                if (pMessageBody == null || pMessageBody.Length < 0) return null;
                if (pFieldIDLengthTags == null)
                {
                    pFieldIDLengthTags = new Dictionary<string, byte[]>(10);

                    //string body = Encoding.GetEncoding(950).GetString(pMessageBody).Trim();

                    int idx = 0, j, k, p, n = pMessageBody.Length;
                    string FieldID = "";
                    int FieldLength = 0;
                    byte[] value = null;

                    while (idx < n)
                    {
                        p = idx + FieldLength;
                        for (j = p; j < n; j++) if (pMessageBody[j] == 58) break;
                        if (j < n)
                        {
                            if (FieldLength <= 0)
                            {
                                for (k = j - 1; k >= p; k--) if (pMessageBody[k] == 32) break;
                                if (k < idx) k = idx;
                            }
                            else
                            {
                                k = p;
                            }
                            value = new byte[k - idx];
                            Array.Copy(pMessageBody, idx, value, 0, k - idx);
                            pFieldIDLengthTags[FieldID] = value;
                            FieldID = Encoding.ASCII.GetString(pMessageBody, k + 1, j - k - 1).Trim();

                            idx = j + 1;
                            for (j = idx; j < n; j++) if (pMessageBody[j] == 58) break;
                            if (j < n)
                            {
                                FieldLength = (pMessageBody[idx] * 100 + pMessageBody[idx+1] * 10 + pMessageBody[idx+2]) - 5328;
                                /*
                                if (!int.TryParse(Encoding.ASCII.GetString(pMessageBody, idx, j - idx), out FieldLength))
                                {
                                    FieldLength = 0;
                                }
                                */
                                idx = j + 1;
                            }
                            else break;
                        }
                        else
                        {
                            value = new byte[n - idx];
                            Array.Copy(pMessageBody, idx, value, 0, n - idx);
                            pFieldIDLengthTags[FieldID] = value;
                            break;
                        }
                    }
                }

                return pFieldIDLengthTags;
            }
        }
    }
}
