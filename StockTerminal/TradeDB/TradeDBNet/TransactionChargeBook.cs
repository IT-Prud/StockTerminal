using System;
using System.Collections.Generic;
using System.Text;

namespace TradeDB.Net
{
    public class TransactionChargeBook
    {
        private readonly Dictionary<string, TransactionCharge> ByHash = new Dictionary<string, TransactionCharge>(200);
        private readonly object AccessMutex = new object();

        public TransactionCharge GetByHash(string Hash)
        {
            TransactionCharge TransCharge = null;

            if (Hash != null)
            {
                lock (AccessMutex)
                {
                    if (ByHash.TryGetValue(Hash, out TransCharge))
                        TransCharge = TransCharge.Clone();
                }
            }
            return TransCharge;
        }

        public void AddUpdate(TransactionCharge TransCharge)
        {
            if (TransCharge != null && TransCharge.Hash != null)
            {
                lock (AccessMutex)
                {
                    ByHash[TransCharge.Hash] = TransCharge.Clone();
                }
            }
        }

        public void Clear()
        {
            lock (AccessMutex)
            {
                ByHash.Clear();
            }
        }
    }
}
