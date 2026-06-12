using System;
using System.Collections.Generic;
using System.Text;

namespace TradeDB
{
    public class AccountBalance : ICloneable
    {
        public decimal T0DayBal = 0;
        public decimal T1DayBal = 0;
        public decimal T2DayBal = 0;
        public decimal T0DayOut = 0;
        public decimal T1DayOut = 0;
        public decimal T2DayOut = 0;
        public decimal MarginCall = 0;
        public decimal FundHold = 0;
        public decimal Interest = 0;
        public decimal Dividend = 0;
        public decimal NetOutLimit = 0;
        public decimal DailyNetBoughtLimit = 0;
        public decimal CreditIndex = 0;
        public decimal CreditLimit = 0;
        public char NoCreditLimit = '\0';
        public decimal UnclearChequeAmount = 0;
        public decimal SellQueue = 0;
        public decimal MarketValue = 0;
        public decimal AcceptableMarketValue = 0;
        public decimal AvailableCredit = 0;

        public object Clone()
        {
            AccountBalance clone = new AccountBalance();

            clone.T0DayBal = T0DayBal;
            clone.T1DayBal = T1DayBal;
            clone.T2DayBal = T2DayBal;
            clone.T0DayOut = T0DayOut;
            clone.T1DayOut = T1DayOut;
            clone.T2DayOut = T2DayOut;
            clone.MarginCall = MarginCall;
            clone.FundHold = FundHold;
            clone.Interest = Interest;
            clone.Dividend = Dividend;
            clone.NetOutLimit = NetOutLimit;
            clone.DailyNetBoughtLimit = DailyNetBoughtLimit;
            clone.CreditIndex = CreditIndex;
            clone.CreditLimit = CreditLimit;
            clone.NoCreditLimit = NoCreditLimit;
            clone.UnclearChequeAmount = UnclearChequeAmount;
            clone.SellQueue = SellQueue;
            clone.MarketValue = MarketValue;
            clone.AcceptableMarketValue = AcceptableMarketValue;
            clone.AvailableCredit = AvailableCredit;

            return (object)clone;
        }

        public override string ToString()
        {
            return String.Format("B0={0} B1={1} B2={2} O0={3} O1={4} O2={5} MC={6} FH={7} SQ={8} MV={9} AMV={10} I={11} D={12} NO={13} DNB={14} CI={15} CL={16} NC={17} UCA={18} AVB={19}",
                T0DayBal, T1DayBal, T2DayBal, T0DayOut, T1DayOut, T2DayOut,
                MarginCall, FundHold, SellQueue, MarketValue, AcceptableMarketValue,
                Interest, Dividend, NetOutLimit, DailyNetBoughtLimit,
                CreditIndex, CreditLimit, NoCreditLimit, UnclearChequeAmount, AvailableCredit);
        }
    }
}
