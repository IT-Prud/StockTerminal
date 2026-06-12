using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace TradeDB.Net
{
    class AccountBook
    {
        private readonly Dictionary<string, Account> ByAccountNo = new Dictionary<string, Account>(50);
        private readonly Dictionary<string, Dictionary<string, Account>> ByAECode = new Dictionary<string, Dictionary<string, Account>>(50);
        private readonly AccountTreeNode pAccountTreeNode = new AccountTreeNode(10000);

        private readonly object AccessMutex = new object();

        public int Count
        {
            get { return ByAccountNo.Count; }
        }

        public void AddUpdateList(List<AccountAE> AccountAEList)
        {
            Account ac;
            string oldAECode = null;

            if (AccountAEList != null)
            {
                lock (AccessMutex)
                {
                    foreach (AccountAE acAe in AccountAEList)
                    {
                        if (ByAccountNo.TryGetValue(acAe.AccountNo, out ac))
                        {
                            oldAECode = ac.AECode;
                            ac.AECode = acAe.AECode;

                            UpdateIndexes(ac, oldAECode);
                        }
                        else
                        {
                            ac = new Account(acAe.AccountNo);
                            ac.AECode = acAe.AECode;

                            ByAccountNo.Add(ac.AccountNo, ac);

                            BuildAccountNodeTree(ac.AccountNo);
                            UpdateIndexes(ac, null);
                        }
                    }

                    pAccountTreeNode.Sort();
                }
            }
        }

        public void AddUpdate(Account NewAccount)
        {
            if (NewAccount != null && NewAccount.AccountNo != null)
            {
                lock (AccessMutex)
                {
                    AddUpdateAccountInternal(NewAccount);
                }
            }
        }

        public List<string> GetList()
        {
            lock (AccessMutex)
            {
                return new List<string>(ByAccountNo.Keys);
            }
        }

        public string[] GetList(string SearchText)
        {
            lock (AccessMutex)
            {
                List<string> list = pAccountTreeNode.GetAccountNoList(SearchText);
                if (list != null && list.Count > 0)
                    return list.ToArray();
            }

            return null;
        }

        public List<string> GetListByAECode(string AECode)
        {
            List<string> list = null;
            Dictionary<string, Account> accountDict = null;

            lock (AccessMutex)
            {
                if (ByAECode.TryGetValue(AECode, out accountDict))
                {
                    list = new List<string>(accountDict.Count);

                    foreach (string accountNo in accountDict.Keys)
                    {
                        list.Add(accountNo);
                    }
                }
                else
                {
                    list = new List<string>(0);
                }
            }

            return list;
        }

        public Account GetByAccountNo(string AccountNo)
        {
            Account account = null;

            lock (AccessMutex)
            {
                if (AccountNo != null)
                {
                    ByAccountNo.TryGetValue(AccountNo, out account);

                    if (account != null)
                        account = account.Clone();
                }
            }

            return account;
        }

        public List<Account> GetByAECode(string AECode)
        {
            List<Account> accounts = null;
            Dictionary<string, Account> accountDict = null;

            lock (AccessMutex)
            {
                if (AECode != null && ByAECode.TryGetValue(AECode, out accountDict) && accountDict != null)
                {
                    accounts = new List<Account>(accountDict.Count);

                    foreach (Account account in accountDict.Values)
                    {
                        accounts.Add((Account)account.Clone());
                    }
                }
                else
                {
                    accounts = new List<Account>(0);
                }
            }

            return accounts;
        }

        public void Clear()
        {
            lock (AccessMutex)
            {
                ByAccountNo.Clear();
                ByAECode.Clear();
            }
        }

        private void AddUpdateAccountInternal(Account NewAccount)
        {
            Account theAccount = null;
            string accountNo = null;
            string oldAECode = null;

            accountNo = NewAccount.AccountNo.Trim().ToUpper();

            // ByAccountNo
            if (ByAccountNo.TryGetValue(accountNo, out theAccount))
            {
                oldAECode = theAccount.AECode;

                theAccount.Update(NewAccount);
                NewAccount.Update(theAccount);
            }
            else
            {
                theAccount = (Account)NewAccount.Clone();
                ByAccountNo.Add(theAccount.AccountNo, theAccount);
            }

            UpdateIndexes(theAccount, oldAECode);
        }

        private void BuildAccountNodeTree(string AccountNo)
        {
            int idx;
            char c, cLast;

            pAccountTreeNode.AddNode(AccountNo, AccountNo);

            cLast = AccountNo[0];
            if (cLast < '1' || cLast > '9')
            {
                for (idx = 1; idx < AccountNo.Length; idx++)
                {
                    c = AccountNo[idx];
                    if (c >= '1' && c <= '9')
                    {
                        pAccountTreeNode.AddNode(AccountNo.Substring(idx), AccountNo);
                        break;
                    }
                    else if (c == '0' && cLast != '0')
                    {
                        pAccountTreeNode.AddNode(AccountNo.Substring(idx), AccountNo);
                        cLast = c;
                    }
                }
            }
        }

        private void UpdateIndexes(Account TheAccount, string OldAECode)
        {
            Dictionary<string, Account> accountDict = null;

            // ByAECode
            if (TheAccount.AECode != null)
            {
                if (TheAccount.AECode != OldAECode)
                {
                    if (OldAECode != null && ByAECode.TryGetValue(OldAECode, out accountDict))
                    {
                        accountDict.Remove(TheAccount.AccountNo);
                        if (accountDict.Count <= 0) ByAECode.Remove(OldAECode);
                    }

                    if (!ByAECode.TryGetValue(TheAccount.AECode, out accountDict))
                        ByAECode.Add(TheAccount.AECode, accountDict = new Dictionary<string, Account>(10));

                    accountDict[TheAccount.AccountNo] = TheAccount;
                }
            }
            else if (OldAECode != null)
            {
                if (ByAECode.TryGetValue(OldAECode, out accountDict))
                {
                    accountDict.Remove(TheAccount.AccountNo);

                    if (accountDict.Count <= 0)
                        ByAECode.Remove(OldAECode);
                }
            }
        }
    }
}
