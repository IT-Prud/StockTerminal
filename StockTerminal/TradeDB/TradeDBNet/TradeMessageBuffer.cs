using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace TradeDB.Net
{
    class TradeMessageBuffer : DirectAppendBuffer
    {
        public TradeMessageBuffer(int Capacity) : base(Capacity)
        {

        }

        /// <summary>
        /// Get first "MaxCount" messages from the buffer. The messages are then removed from the buffer.
        /// </summary>
        /// <returns>A List of messages obtained from the buffer.</returns>
        public List<TradeMessage> GetMessages(bool KeepRaw)
        {
            List<TradeMessage> msgs = TradeMessage.Parse(pBuffer, ref pBufferHead, pBufferTail, KeepRaw);

            Debug.Assert(pBufferHead <= pBufferHead, "TradeMessage.Parse resulted in BufferHead get pass BufferTail.");

            if (pBufferHead >= pBufferTail)
            {
                pBufferHead = 0;
                pBufferTail = 0;
            }
            else if (pBufferHead > 0)
            {
                pBufferTail -= pBufferHead;
                Array.Copy(pBuffer, pBufferHead, pBuffer, 0, pBufferTail);
                pBufferHead = 0;
            }
            return msgs;
        }
    }
}
