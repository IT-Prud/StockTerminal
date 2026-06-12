using System;
using System.Collections.Generic;
using System.Text;

namespace TradeDB
{
    public class AccountStock 
    {
        public readonly string AccountNo = null;
        public readonly ExchangeTypeEnum ExchangeType = ExchangeTypeEnum.Unassigned;
        public readonly string ExchangeCode;
        public readonly string Code = null;
        public readonly string StockSignature;
        public string BestName;
        public string Currency = null;
        public decimal QtyInTransit = 0;
        public decimal QtyInTransitSold = 0;
        public decimal QtyOnHand = 0;
        public decimal QtyBuying = 0;
        public decimal QtySelling = 0;
        public decimal QtyBought = 0;
        public decimal QtySold = 0;
        public decimal QtyReceivable = 0;
        public decimal MarketValue;
        public decimal QtyHoldByRelated = 0;
        public string SuspensionFlag;

        public bool SellableSameDay
        {
            get { return ExchangeType != ExchangeTypeEnum.SHG && ExchangeType != ExchangeTypeEnum.SZE; }
        }

        public decimal QtyAvailable
        {
            get
            {
                return Math.Max(0, QtyOnHand
                    + (SellableSameDay ? QtyBought : 0)
                    - QtySelling
                    - QtySold
                    - QtyHoldByRelated);
            }
        }

        public decimal QtyShort
        {
            get
            {
                return Math.Max(0, QtySelling
                    + QtySold
                    + QtyHoldByRelated
                    - QtyOnHand
                    - (SellableSameDay ? QtyBought : 0));
            }
        }

        public AccountStock(string AccountNo, string ExchangeCode, string Code)
        {
            int iStockCode;

            if (int.TryParse(Code, out iStockCode))
                this.Code = iStockCode.ToString();
            else
                this.Code = Code.Trim();

            this.ExchangeCode = ExchangeCode.Trim();
            ExchangeType = Stock.GetExchangeType(ExchangeCode);

            if (ExchangeType != ExchangeTypeEnum.Unassigned)
                StockSignature = Stock.GetSignature(ExchangeType, this.Code);
            else
                StockSignature = Stock.GetSignature(ExchangeCode, this.Code);
        }

        public AccountStock(string AccountNo, ExchangeTypeEnum ExchangeType, string Code)
        {
            int iStockCode;

            if (int.TryParse(Code, out iStockCode))
                this.Code = iStockCode.ToString();
            else
                this.Code = Code.Trim();

            this.ExchangeType = ExchangeType;
            this.ExchangeCode = Stock.GetExchangeCode(ExchangeType);
            this.StockSignature = Stock.GetSignature(ExchangeType, this.Code);
        }

        public AccountStock Clone()
        {
            AccountStock clone = ExchangeType == ExchangeTypeEnum.Unassigned ?
                new AccountStock(AccountNo, ExchangeCode, Code) :
                new AccountStock(AccountNo, ExchangeType, Code);

            clone.Currency = Currency;
            clone.QtyInTransit = QtyInTransit;
            clone.QtyInTransitSold = QtyInTransitSold;
            clone.QtyOnHand = QtyOnHand;
            clone.QtyBuying = QtyBuying;
            clone.QtySelling = QtySelling;
            clone.QtyBought = QtyBought;
            clone.QtySold = QtySold;
            clone.QtyReceivable = QtyReceivable;
            clone.QtyHoldByRelated = QtyHoldByRelated;
            clone.MarketValue = MarketValue;
            clone.SuspensionFlag = SuspensionFlag;

            return clone;
        }

        public override string ToString()
        {
            return String.Format("ACNO={0} SC={1} QIT={2} QITS={3} QOH={4} QRV={5} CU={6} EXT={7} QB={8} QS={9} QBT={10} QSD={11} QHBR={12} MV={13} SF={14}",
                AccountNo, Code, QtyInTransit, QtyInTransitSold, QtyOnHand, QtyReceivable, Currency, ExchangeType, QtyBuying, QtySelling, QtyBought, QtySold, QtyHoldByRelated, MarketValue, SuspensionFlag);
        }
    }
}
