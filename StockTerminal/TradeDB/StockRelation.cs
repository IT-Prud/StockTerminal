using System;
using System.Collections.Generic;
using System.Text;

namespace TradeDB
{
    public class StockRelation
    {
        public class StockRelationStock
        {
            public string StockSignature;
            public string InstrumentType;
            public string ProductType;
            public string Currency;
            public decimal LotSize;
            public decimal ShortRatio;
            public decimal Weight;
        }

        public StockRelationStock Short;
        public readonly List<StockRelationStock> From = new List<StockRelationStock>(5);
        public string Relation;
    }
}
