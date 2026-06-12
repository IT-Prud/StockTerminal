using System;
using System.Collections.Generic;
using System.Text;

namespace TradeDB
{
    public class MarketTurnover
    {
        public string MarketCode;
        public decimal Turnover;

        public MarketTurnover Clone()
        {
            return new MarketTurnover { 
                MarketCode = MarketCode, 
                Turnover = Turnover
            };
        }
    }
}
