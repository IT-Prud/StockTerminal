using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    public class DirectBuffer
    {
        private byte[] pBuffer = null;
        private int pBufferTail = 0;

        public DirectBuffer(int Capacity)
        {
            pBuffer = new byte[Capacity];
        }

        public int Capacity
        {
            get { return pBuffer.Length; }

            set
            {
                if (value != pBuffer.Length)
                {
                    byte[] newBuffer = new byte[value];
                    Buffer.BlockCopy(pBuffer, 0, newBuffer, 0, pBufferTail);
                    pBuffer = newBuffer;
                }
            }
        }

        public int SpaceAvailable
        {
            get { return pBuffer.Length - pBufferTail; }
        }

        public int SpaceOccupied
        {
            get { return pBufferTail; }
        }

        /// <summary>
        /// Set the number of bytes just appended to the buffer directly without using Append member function.
        /// If number of bytes appended to the buffer is greater than the Capacity, Capacity remains unchanged and SpaceOccupied equals to Capacity.
        /// </summary>
        public int BytesAppended
        {
            set
            {
                if (value <= 0) return;

                pBufferTail += value;
                if (pBufferTail > pBuffer.Length) pBufferTail = pBuffer.Length;
            }
        }

        /// <summary>
        /// Set the number of bytes just read from the buffer directly without using Read member function.
        /// The read bytes will be removed from the buffer.
        /// </summary>
        public int BytesRead
        {
            set
            {
                if (value <= 0) return;

                pBufferTail -= value;
                if (pBufferTail > 0)
                {
                    Buffer.BlockCopy(pBuffer, value, pBuffer, 0, pBufferTail);    // shift remaining to the front
                }
                else
                {
                    pBufferTail = 0;
                }
            }
        }

        /// <summary>
        /// Get the buffer for direct manipulation
        /// </summary>
        public byte[] UnderBuffer
        {
            get { return pBuffer; }
        }

        /// <summary>
        /// Clear the buffer.
        /// </summary>
        public void Clear()
        {
            pBufferTail = 0;
        }

        /// <summary>
        /// Append Bytes to the end of the buffer, auto increase Capacity of the buffer if needed.
        /// </summary>
        /// <param name="Bytes">Bytes to be appended to the buffer.</param>
        /// <param name="Count">Number of bytes to be appended to the buffer.</param>
        /// <param name="KeepCapacity">Keep Capacity unchanged.</param>
        /// <returns>true if appended successfully. false if KeepCapacity is set to true while Capacity is not enough.</returns>
        public bool Append(byte[] Bytes, int Count, bool KeepCapacity)
        {
            if (Bytes == null || Bytes.Length <= 0 || Count <= 0) return true;

            int inCount = Bytes.Length;
            if (inCount > Count) inCount = Count;

            int newCount = pBufferTail + inCount;
            if (newCount > pBuffer.Length)
            {
                if (KeepCapacity) return false;
                Capacity = (newCount << 1);
            }

            Buffer.BlockCopy(Bytes, 0, pBuffer, pBufferTail, inCount);
            pBufferTail = newCount;

            return true;
        }

        /// <summary>
        /// Return all bytes from the buffer. The bytes are then removed from the buffer.
        /// </summary>
        /// <returns>An array of all bytes from the buffer. The array length might be less than MaxCount.</returns>
        public byte[] Read()
        {
            return Read(-1);
        }

        /// <summary>
        /// Return first "MaxCount" bytes from the buffer. The bytes are then removed from the buffer.
        /// </summary>
        /// <param name="MaxCount">Maximum number of bytes to be returned. Negative value results in all bytes from buffer returned.</param>
        /// <returns>An array of first "MaxCount" bytes from the buffer. The array length might be less than MaxCount.</returns>
        public byte[] Read(int MaxCount)
        {
            byte[] newBuffer = null;

            if (MaxCount > 0)
            {
                int readCount = (pBufferTail > MaxCount) ? MaxCount : pBufferTail;

                if (readCount > 0)
                {
                    newBuffer = new byte[readCount];
                    Buffer.BlockCopy(pBuffer, 0, newBuffer, 0, readCount);

                    pBufferTail -= readCount;
                    if (pBufferTail > 0) Buffer.BlockCopy(pBuffer, readCount, pBuffer, 0, pBufferTail);
                }
            }

            return newBuffer;
        }
    }
}
