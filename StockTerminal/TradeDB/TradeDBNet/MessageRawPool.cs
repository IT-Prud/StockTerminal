using System;
using System.Collections.Generic;
using System.Text;
using Utils;

namespace TradeDB.Net
{
    public class MessageRaw
    {
        public class MessageRawPool
        {
            private static readonly QueueLockFree<MessageRaw> Pool = new QueueLockFree<MessageRaw>();

            private MessageRaw Get()
            {
                MessageRaw r = null;

                if ((r = Pool.Dequeue()) == null)
                    r = new MessageRaw(this);

                r.pInPool = false;

                return r;
            }

            public MessageRaw Get(int Length)
            {
                MessageRaw r = Get();

                if (Length > 0)
                {
                    r.Count = 0;

                    if (r.pData == null || r.pData.Length < Length)
                        r.pData = new byte[Length];
                }

                r.pInPool = false;

                return r;
            }

            public void Put(MessageRaw r)
            {
                if (r != null && !r.pInPool)
                {
                    r.pInPool = true;
                    Pool.Enqueue(r);
                }
            }

            public void Clear()
            {
                Pool.Clear();
            }
        }


        private static readonly MessageRawPool MsgRawPool = new MessageRawPool();

        public int Count;

        private byte[] pData;
        private MessageRawPool Pool = null;

        private bool pInPool = false;

        static MessageRaw()
        {
            MessageRaw[] rawArray = new MessageRaw[1000];

            for (int i = 0; i < rawArray.Length; i++)
                rawArray[i] = MessageRaw.Get(3000);

            for (int i = 0; i < rawArray.Length; i++)
                rawArray[i].Recycle();
        }

        public static MessageRaw Get(int Length)
        {
            return MsgRawPool.Get(Length);
        }

        public static MessageRaw Get(int Length, byte[] Data)
        {
            MessageRaw raw = MsgRawPool.Get(Length);
            int dataLength;

            if (Data != null &&
                (dataLength = Math.Min(Length, Data.Length)) > 0)
            {
                Buffer.BlockCopy(Data, 0, raw.Data, 0, dataLength);
                raw.Count = dataLength;
            }

            return raw;
        }

        private MessageRaw()
        {
        }

        private MessageRaw(MessageRawPool Pool)
        {
            this.Pool = Pool;
        }

        public byte[] Data
        {
            get { return pData; }
        }

        public bool Append(MessageRaw Raw)
        {
            bool isSuccess = false;

            if (Raw != null && Raw.Count > 0)
            {
                Buffer.BlockCopy(Raw.Data, 0, pData, Count, Raw.Count);
                Count += Raw.Count;
            }   

            return isSuccess;
        }

        public void Recycle()
        {
            Pool.Put(this);
        }

        public MessageRaw Clone()
        {
            MessageRaw clone = MsgRawPool.Get(Count);

            Buffer.BlockCopy(pData, 0, clone.pData, 0, Count);
            clone.Count = Count;

            return clone;
        }
    }
}
