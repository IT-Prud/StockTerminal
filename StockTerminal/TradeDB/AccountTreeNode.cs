using System;
using System.Collections.Generic;
using System.Text;

namespace TradeDB
{
    public class AccountTreeNode
    {
        private readonly List<string> AccountNoList;
        private readonly Dictionary<char, AccountTreeNode> DirectChildren = new Dictionary<char, AccountTreeNode>(40);

        internal AccountTreeNode(int Capacity)
        {
            AccountNoList = new List<string>(Capacity);
        }

        internal void AddNode(string Value, string Display)
        {
            if (Value != null)
            {
                AccountNoList.Add(Display);

                if (Value.Length > 0)
                {
                    AccountTreeNode child;

                    if (!DirectChildren.TryGetValue(Value[0], out child))
                        DirectChildren.Add(Value[0], child = new AccountTreeNode(Math.Max(10, (int)Math.Ceiling(AccountNoList.Capacity * 0.1))));

                    child.AddNode(Value.Substring(1), Display);
                }
            }
        }

        internal void Sort()
        {
            AccountNoList.Sort();

            foreach (KeyValuePair<char, AccountTreeNode> kvp in DirectChildren)
                kvp.Value.Sort();
        }

        public List<string> GetAccountNoList(string SearchText)
        {
            AccountTreeNode child;

            if (SearchText.Length > 0)
            {
                if (DirectChildren.TryGetValue(SearchText[0], out child))
                    return child.GetAccountNoList(SearchText.Substring(1));
                else
                    return null;
            }
            else
                return AccountNoList;
        }
    }
}
