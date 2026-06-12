using System;
using System.Collections.Generic;
using System.Text;

namespace TradeDB.Net
{
    class DirectAppendBuffer
    {
        protected byte[] pBuffer = null;
        protected int pBufferHead = 0;
        protected int pBufferTail = 0;

        public DirectAppendBuffer(int Capacity)
        {
            pBuffer = new byte[Capacity];
        }

        public int Capacity
        {
            get { return pBuffer.Length; }

            set
            {
                byte[] newBuffer = new byte[value];
                Array.Copy(pBuffer, pBufferHead, newBuffer, 0, pBufferTail - pBufferHead);
                pBuffer = newBuffer;
            }
        }

        public int Count
        {
            get
            {
                return pBufferTail - pBufferHead;
            }
        }

        public int StartIndex
        {
            get { return pBufferTail; }
        }

        public int SpaceAvailable
        {
            get
            {
                return pBuffer.Length - pBufferTail;
            }
        }

        public int BytesAppended
        {
            set
            {
                if (value <= 0) return;
                pBufferTail += value;
                if (pBufferTail > pBuffer.Length) pBufferTail = pBuffer.Length;
            }
        }

        public int BytesRead
        {
            set
            {
                if (value <= 0) return;
                int newBufferHead = pBufferHead + value;
                int dataCount = pBufferTail - newBufferHead;
                if (dataCount > 0)
                {
                    Array.Copy(pBuffer, newBufferHead, pBuffer, 0, dataCount);    // shift remaining to the front
                }
                else
                {
                    dataCount = 0;
                }
                pBufferHead = 0;
                pBufferTail = dataCount;
            }
        }

        public byte[] Buffer
        {
            get { return pBuffer; }
        }

        public void Clear()
        {
            pBufferHead = 0;
            pBufferTail = 0;
        }

        /// <summary>
        /// Get first "MaxCount" bytes from the buffer. The bytes are then removed from the buffer.
        /// </summary>
        /// <param name="MaxCount">Maximum number of bytes to be returned. Zero or less results in all bytes from buffer returned.</param>
        /// <returns>An array of first "MaxCount" bytes from the buffer. The array length might be less than MaxCount.</returns>
        public byte[] GetBytes(int MaxCount)
        {
            int dataCount = pBufferTail - pBufferHead;
            if (MaxCount > 0 && dataCount < MaxCount) dataCount = MaxCount;

            byte[] newBuffer = new byte[dataCount];
            Array.Copy(pBuffer, pBufferHead, newBuffer, 0, dataCount);

            pBufferHead += dataCount;

            if (pBufferHead == pBufferTail)
            {
                pBufferHead = 0;
                pBufferTail = 0;
            }

            return newBuffer;
        }
    }
}
