using System;
using System.Collections.Generic;
using System.Text;

namespace TradeDB.Net
{
    public class AEBook
    {
        private readonly Dictionary<string, AccountExecutive> ByCode = new Dictionary<string, AccountExecutive>(50);
        private readonly object AccessMutex = new object();

        public int Count
        {
            get { return ByCode.Count; }
        }

        public void AddUpdate(List<AccountExecutive> AEList)
        {
            if (AEList != null)
            {
                lock (AccessMutex)
                {
                    foreach (AccountExecutive ae in AEList)
                    {
                        AddUpdateAccountInternal(ae);
                    }
                }
            }
        }

        public List<AccountExecutive> GetByCode(string Code)
        {
            if (Code == null) return null;

            List<AccountExecutive> AEList = null;
            AccountExecutive AE = null;

            lock (AccessMutex)
            {
                AEList = new List<AccountExecutive>(1);

                if (ByCode.TryGetValue(Code, out AE))
                {
                    AEList.Add((AccountExecutive)AE.Clone());
                }
            }

            return AEList;
        }

        public List<AccountExecutive> GetAEList()
        {
            List<AccountExecutive> AEList = null;

            lock (AccessMutex)
            {
                AEList = new List<AccountExecutive>(ByCode.Count);

                foreach (KeyValuePair<string, AccountExecutive> kvp in ByCode)
                {
                    AEList.Add(kvp.Value);
                }
            }

            return AEList;
        }

        public void Clear()
        {
            lock (AccessMutex)
            {
                ByCode.Clear();
            }
        }

        private void AddUpdateAccountInternal(AccountExecutive NewAE)
        {
            AccountExecutive theAE = null;

            // ByCode
            if (ByCode.TryGetValue(NewAE.Code, out theAE))
                theAE.Update(NewAE);
            else
                ByCode.Add(NewAE.Code, NewAE.Clone());
        }
    }
}
