using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace TradeDB
{
    public class Account
    {
        public enum ActionBlockedEnum { Buy = 1, Sell = 2 };

        public readonly string AccountNo = null;
        public char AccountType = '\0';
        public string AccountName = null;
        public string Email = null;
        public string Phone = null;
        public string AECode = null;
        public int TotalStockSpecified = 0;
        public string CreditClass = null;
        public string GemDerivTrade = null;
        public string StructProdExperienced = null;

        public readonly Dictionary<string, AccountBalance> Balances = new Dictionary<string, AccountBalance>(10);
        public readonly Dictionary<string, AccountStock> Stocks = new Dictionary<string,AccountStock>(3);
        public int ActionBlocked = -1;
        public string AllowCscSzeChiNext = null;

        public string DACategory = null;
        public Dictionary<string, byte> DiscretionGrpIDDict = new Dictionary<string, byte>();

        /* obsolete members, moved inside AccountBalance dictionary */
        /*
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
         */

        public Account(string AccountNo)
        {
            this.AccountNo = AccountNo;
        }

        public bool IsNoBuy
        {
            get { return ActionBlocked >= 0 && (ActionBlocked & (int)ActionBlockedEnum.Buy) != 0; }
        }

        public bool IsNoSell
        {
            get { return ActionBlocked >= 0 && (ActionBlocked & (int)ActionBlockedEnum.Sell) != 0; }
        }

        public int TotalStock
        {
            get
            {
                if (Stocks == null) return 0;
                return Stocks.Count;
            }
        }

        public void Update(Account NewAccount)
        {
            if (NewAccount.AccountName != null)
            {
                AccountName = NewAccount.AccountName;
                Email = NewAccount.Email;
                Phone = NewAccount.Phone;
                CreditClass = NewAccount.CreditClass;
                GemDerivTrade = NewAccount.GemDerivTrade;
                StructProdExperienced = NewAccount.StructProdExperienced;
            }

            if (NewAccount.AECode != null)
            {
                //AccountNo = NewAccount.AccountNo; // Account no is readonly, cannot be updated.
                AccountType = NewAccount.AccountType;
                AECode = NewAccount.AECode;
                TotalStockSpecified = NewAccount.TotalStockSpecified;
            }

            if (NewAccount.Balances != null && NewAccount.Balances.Count > 0)
            {
                foreach (KeyValuePair<string, AccountBalance> kvp in NewAccount.Balances)
                {
                    Balances[kvp.Key] = (AccountBalance)kvp.Value.Clone();
                }
            }

            if (NewAccount.Stocks != null && NewAccount.Stocks.Count > 0)   // no account stock removal mechanism because no definite account stock query mechanism, might rely on detecting QIT+QOH = 0
            {
                //if (Stocks == null) Stocks = new Dictionary<string, AccountStock>(10);
                //AccountStock accountStock in NewAccount.Stocks.Values
                foreach (KeyValuePair<string, AccountStock> kvp in NewAccount.Stocks)
                {
                    Stocks[kvp.Key] = kvp.Value.Clone();
                }
            }

            if (NewAccount.ActionBlocked >= 0)
            {
                ActionBlocked = NewAccount.ActionBlocked;
            }

            if (NewAccount.AllowCscSzeChiNext != null)
            {
                AllowCscSzeChiNext = NewAccount.AllowCscSzeChiNext;
            }

            if (NewAccount.DACategory != null)
            {
                DACategory = NewAccount.DACategory;
                DiscretionGrpIDDict = new Dictionary<string, byte>();
            }
            if (NewAccount.DiscretionGrpIDDict != null && NewAccount.DiscretionGrpIDDict.Count > 0)
            {
                if (DiscretionGrpIDDict != null & DiscretionGrpIDDict.Count > 0)
                    DiscretionGrpIDDict = new Dictionary<string, byte>(NewAccount.DiscretionGrpIDDict.Count);
                foreach (KeyValuePair<string, byte> kvp in NewAccount.DiscretionGrpIDDict)
                {
                    DiscretionGrpIDDict[kvp.Key] = (byte)kvp.Value;
                }
            }
        }

        public void AddUpdateStock(AccountStock NewStock)
        {
            if (NewStock == null || NewStock.Code == null) return;

            //if (Stocks == null) Stocks = new Dictionary<string, AccountStock>(10);
            //Stocks[NewStock.Code] = (AccountStock)NewStock.Clone();
            Stocks[NewStock.StockSignature] = NewStock.Clone();
        }

        public bool IsQtyEnough(StockRelationBook SRBook, ExchangeTypeEnum ExchangeType, string StockCode, decimal QtyOutStand)
        {
            bool isEnough = false;
            AccountStock stock, stockFrom;
            decimal qtyAvail;
            StockRelation rel;
            StockRelation.StockRelationStock relStock;
            string stockSignature = Stock.GetSignature(ExchangeType, StockCode);

            if (!string.IsNullOrEmpty(stockSignature))
            {
                qtyAvail = Stocks.TryGetValue(stockSignature, out stock) ? stock.QtyAvailable : 0;

                if (QtyOutStand <= qtyAvail)
                {
                    isEnough = true;
                }
                else if (ExchangeType != ExchangeTypeEnum.SHG && ExchangeType != ExchangeTypeEnum.SZE && (rel = SRBook.GetStockRelated(stockSignature)) != null)
                {
                    QtyOutStand -= qtyAvail;

                    for (int i = 0; i < rel.From.Count; i++)
                    {
                        relStock = rel.From[i];

                        if (Stocks.TryGetValue(relStock.StockSignature, out stockFrom) && (qtyAvail = stockFrom.QtyAvailable) >= relStock.LotSize)
                        {
                            QtyOutStand -= Math.Truncate(qtyAvail / relStock.LotSize) * relStock.LotSize / relStock.Weight;

                            if (QtyOutStand <= 0)
                            {
                                isEnough = true;
                                break;
                            }
                        }
                    }
                }
            }

            return isEnough;
        }

        public void UpdateOnHoldByRelated(StockRelationBook SRBook)
        {
            AccountStock stock, stockFrom;
            StockRelation rel;
            StockRelation.StockRelationStock relStock;
            decimal qtyShort, qtyAvailForRaise, qtyForRaise;

            foreach (KeyValuePair<string, AccountStock> kvp in Stocks)
            {
                kvp.Value.QtyHoldByRelated = 0;
            }

            foreach (KeyValuePair<string, AccountStock> kvp in Stocks)
            {
                stock = kvp.Value;
                qtyShort = stock.QtyShort;

                if (qtyShort > 0)
                {
                    if ((rel = SRBook.GetStockRelated(stock.StockSignature)) != null)
                    {
                        for (int i = 0; i < rel.From.Count && qtyShort > 0; i++)
                        {
                            relStock = rel.From[i];

                            if (relStock.Weight > 0 && Stocks.TryGetValue(relStock.StockSignature, out stockFrom))
                            {
                                qtyAvailForRaise = stockFrom.QtyAvailable;

                                if (qtyAvailForRaise > 0)
                                {
                                    qtyForRaise = Math.Min(qtyShort / relStock.Weight, qtyAvailForRaise);
                                    stockFrom.QtyHoldByRelated += qtyForRaise;

                                    qtyShort -= qtyForRaise * relStock.Weight;
                                }
                            }
                        }
                    }
                }
            }
        }

        public Account Clone()
        {
            Account clone = new Account(AccountNo);

            clone.AccountType = AccountType;
            clone.AccountName = AccountName;
            clone.Email = Email;
            clone.CreditClass = CreditClass;
            clone.GemDerivTrade = GemDerivTrade;
            clone.StructProdExperienced = StructProdExperienced;
            clone.Phone = Phone;
            clone.AECode = AECode;
            clone.TotalStockSpecified = TotalStockSpecified;
            clone.ActionBlocked = ActionBlocked;
            clone.AllowCscSzeChiNext = AllowCscSzeChiNext;

            if (Balances != null)
            {
                foreach (KeyValuePair<string, AccountBalance> kvp in Balances)
                {
                    if (kvp.Value != null) clone.Balances.Add(kvp.Key, (AccountBalance)kvp.Value.Clone());
                }
            }

            if (Stocks != null)
            {
                //clone.Stocks = new Dictionary<string,AccountStock>(10);
                foreach (KeyValuePair<string, AccountStock> kvp in Stocks)
                {
                    if (kvp.Value != null) clone.Stocks.Add(kvp.Key, kvp.Value.Clone());
                }
            }

            clone.DACategory = DACategory;
            if (DiscretionGrpIDDict != null)
            {
                foreach (KeyValuePair<string, byte> kvp in DiscretionGrpIDDict)
                {
                    clone.DiscretionGrpIDDict.Add(kvp.Key, (byte)kvp.Value);
                }
            }

            return clone;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(1000);

            sb.Append(String.Format("ACNO={0} ACN={1} AT={2} AE={3} E={4} P={5} TS={6} TSS={7} NBY={8} NSE={9} CC={10}, GDT={11}, SPE={12}",
                AccountNo, AccountName, AccountType, AECode, Email, Phone,
                TotalStock, TotalStockSpecified, IsNoBuy, IsNoSell, CreditClass, GemDerivTrade, StructProdExperienced));
            sb.Append(Environment.NewLine);

            if (Balances != null && Balances.Count > 0)
            {
                foreach (KeyValuePair<string, AccountBalance> kvp in Balances)
                {
                    sb.Append(kvp.Value.ToString());
                    sb.Append(Environment.NewLine);
                }
            }

            if (Stocks != null && Stocks.Count > 0)
            {
                foreach (KeyValuePair<string, AccountStock> kvp in Stocks)
                {
                    sb.Append(kvp.Value.ToString());
                    sb.Append(Environment.NewLine);
                }
            }

            return sb.ToString();
        }

        public int DiscretionCheck(Stock stock)
        {
            if (DACategory == null || stock == null || DACategory != "Y") //  || DiscretionGrpID.ContainsKey("ALL")
                return -1;

            return DiscretionProductGroupsTable.IsDiscretion(stock, DiscretionGrpIDDict) ? 1 : 0;
        }
    }
}
