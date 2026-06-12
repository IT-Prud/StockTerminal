using System;
using System.Collections.Generic;
using System.Text;

namespace TradeDB.Net
{
    class IndexBook
    {
        private readonly Dictionary<string, Index> ByCode = new Dictionary<string, Index>(50);
        private readonly object AccessMutex = new object();

        public void AddUpdate(Index NewIndex)
        {
            if (NewIndex != null && NewIndex.Code != null)
            {
                lock (AccessMutex)
                {
                    AddUpdateInternal(NewIndex);
                }
            }
        }

        /*
        public void AddUpdate(Dictionary<string, Index> dict)
        {
            if (dict != null)
            {
                lock (AccessMutex)
                {
                    foreach (KeyValuePair<string, Index> kvp in dict)
                        AddUpdateInternal(kvp.Value);
                }
            }
        }
         */

        public Index GetByIndexCode(string Code)
        {
            Index idx = null;

            if (Code != null)
            {
                lock (AccessMutex)
                {
                    if (ByCode.TryGetValue(Code, out idx))
                        idx = idx.Clone();
                }
            }

            return idx;
        }

        public void Clear()
        {
            lock (AccessMutex)
            {
                ByCode.Clear();
            }
        }

        private void AddUpdateInternal(Index NewIndex)
        {
            Index theIndex = null;

            if (ByCode.TryGetValue(NewIndex.Code, out theIndex))
                theIndex.Update(NewIndex);
            else
                ByCode.Add(NewIndex.Code, NewIndex.Clone());
        }
    }
}
