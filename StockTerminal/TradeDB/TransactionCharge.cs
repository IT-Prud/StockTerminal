using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TradeDB
{
    public class TransactionCharge
    {
        public readonly string Hash;

        public readonly string AccountNo;
        public readonly char Side;
        public readonly ExchangeTypeEnum ExType;
        public readonly string StockCode;
        public readonly List<decimal> PriceList = new List<decimal>();
        public readonly List<decimal> QuantityList = new List<decimal>();

        private decimal pConsideration = -1;
        private decimal pCommission = -1;
        private decimal pRebate = -1;
        private decimal pStampDuty = -1;
        private decimal pLevy = -1;
        private decimal pTradingTariff = -1;
        private decimal pCCASSFee = -1;
        private decimal pNetAmount = -1;
        private decimal pTradingFee = -1;

        public TransactionCharge(string AccountNo, char Side, ExchangeTypeEnum ExType, string StockCode, List<decimal> PriceList, List<decimal> QuantityList)
        {
            this.AccountNo = AccountNo;
            this.Side = Side;
            this.ExType = ExType;
            this.StockCode = StockCode;

            if (PriceList != null && QuantityList != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    this.PriceList.Add(i < PriceList.Count ? PriceList[i] : 0.0m);
                    this.QuantityList.Add(i < QuantityList.Count ? QuantityList[i] : 0.0m);
                }
            }

            this.Hash = GetHash(this);
        }

        public static string GetHash(TransactionCharge tc)
        {
            if (tc != null && tc.AccountNo != null && (tc.Side == 'B' || tc.Side == 'S') && tc.StockCode != null && tc.ExType != ExchangeTypeEnum.Unassigned && tc.PriceList != null && tc.PriceList.Count > 0 && tc.QuantityList != null && tc.QuantityList.Count > 0) 
            {
                CultureInfo ci = new CultureInfo("en-us");

                StringBuilder sb = new StringBuilder(50);
                sb.Append(tc.AccountNo.Trim().ToUpper());
                sb.Append('|');
                sb.Append(tc.Side);
                sb.Append('|');
                sb.Append(Stock.GetSignature(tc.ExType, tc.StockCode));
                for (int i = 0; i < 5; i++)
                {
                    sb.Append('|');
                    sb.Append(Math.Round(tc.PriceList[i], 7).ToString("G", ci));
                }
                for (int i = 0; i < 5; i++)
                {
                    sb.Append('|');
                    sb.Append(tc.QuantityList[i].ToString("G", ci));
                }
                return sb.ToString();
            }
            return null;
        }

        public static void ParseHash(string Hash, out string AccountNo, out char Side, out string ExCode, out string StockCode, out List<decimal> PriceList, out List<decimal> QuantityList)
        {
            string[] parts = null;
            if (Hash != null) parts = Hash.Split('|');
            ExchangeTypeEnum exType;

            if (parts == null)   // || parts.Length != 5)
            {
                AccountNo = null;
                Side = '\0';
                StockCode = null;
                PriceList = null;
                QuantityList = null;
                ExCode = null;
            }
            else
            {
                AccountNo = parts[0];
                Side = parts[1].Length == 1 ? parts[1][0] : '\0';
                Stock.GetExchangeTypeStockCode(parts[2], out exType, out StockCode);
                ExCode = Stock.GetExchangeCode(exType);

                PriceList = new List<decimal>(5);
                for (int i = 0; i < 5; i++)
                {
                    decimal tempPrice;
                    if (decimal.TryParse(parts[3 + i], out tempPrice))
                        PriceList.Add(tempPrice);
                    else
                    {
                        PriceList = null;
                        break;
                    }
                }

                QuantityList = new List<decimal>(5);
                for (int i = 0; i < 5; i++)
                {
                    decimal tempQty;
                    if (decimal.TryParse(parts[8 + i], out tempQty))
                        QuantityList.Add(tempQty);
                    else
                    {
                        QuantityList = null;
                        break;
                    }
                }

            }
        }

        public decimal Consideration
        {
            get { return pConsideration; }
        }

        public decimal Commission
        {
            get { return pCommission; }
        }

        public decimal Rebate
        {
            get { return pRebate; }
        }

        public decimal StampDuty
        {
            get { return pStampDuty; }
        }

        public decimal Levy
        {
            get { return pLevy; }
        }

        public decimal TradingTariff
        {
            get { return pTradingTariff; }
        }

        public decimal CCASSFee
        {
            get { return pCCASSFee; }
        }

        public decimal NetAmount
        {
            get { return pNetAmount; }
        }

        public decimal TradingFee
        {
            get { return pTradingFee; }
        }

        public void SetCharges(decimal Consideration, decimal Commission, decimal Rebate, decimal StampDuty, decimal Levy, decimal TradingTariff, decimal CCASSFee, decimal NetAmount, decimal TradingFee)
        {
            pConsideration = Consideration;
            pCommission = Commission;
            pRebate = Rebate;
            pStampDuty = StampDuty;
            pLevy = Levy;
            pTradingTariff = TradingTariff;
            pCCASSFee = CCASSFee;
            pNetAmount = NetAmount;
            pTradingFee = TradingFee;
        }

        private void Clear()
        {
            pConsideration = -1;
            pCommission = -1;
            pRebate = -1;
            pStampDuty = -1;
            pLevy = -1;
            pTradingTariff = -1;
            pCCASSFee = -1;
            pNetAmount = -1;
            pTradingFee = -1;
        }

        public TransactionCharge Clone()
        {
            TransactionCharge clone = new TransactionCharge(AccountNo, Side, ExType, StockCode, PriceList, QuantityList);

            clone.pConsideration = pConsideration;
            clone.pCommission = pCommission;
            clone.pRebate = pRebate;
            clone.pStampDuty = pStampDuty;
            clone.pLevy = pLevy;
            clone.pTradingTariff = pTradingTariff;
            clone.pCCASSFee = pCCASSFee;
            clone.pNetAmount = pNetAmount;
            clone.pTradingFee = pTradingFee;

            return clone;
        }
    }
}
