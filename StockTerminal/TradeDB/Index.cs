using System;
using System.Collections.Generic;
using System.Text;

namespace TradeDB
{
    public class Index
    {
        public readonly string Code;
        public decimal Last;
        public decimal Change;
        public decimal ChangePC;
        public decimal Previous;

        public Index(string Code)
        {
            this.Code = Code;
        }

        public void Update(Index NewIndex)
        {
            if (NewIndex.Code != null)
            {
                Last = NewIndex.Last;
                Change = NewIndex.Change;
                ChangePC = NewIndex.ChangePC;
                Previous = NewIndex.Previous;
            }
        }

        public Index Clone()
        {
            Index clone = new Index(Code);

            clone.Last = Last;
            clone.Change = Change;
            clone.ChangePC = ChangePC;
            clone.Previous = Previous;

            return clone;
        }
    }
}
