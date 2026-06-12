using System;
using System.Collections.Generic;
using System.Text;

namespace TradeDB
{
    public class OddLotOrder
    {
        public ulong OrderNo = 0;
        public int SubmitBrokerNo = 0;
        public string StockCode = null;
        public string StockSignature = null;
        //public int Source = 8;
        public ExchangeTypeEnum ExType = ExchangeTypeEnum.HKG;
        //public string BrokerLocationID = null;
        public DateTime TransactionTime = DateTime.MinValue;
        public char Side = '\0';
        public char OrderType = '\0';
        public decimal Price = 0;
        public int Quantity = 0;
        public bool Deleted = false;

        public void Update(OddLotOrder NewOrder)
        {
            if (this == NewOrder) return;   // no need to update if same object

            SubmitBrokerNo = NewOrder.SubmitBrokerNo;
            StockCode = NewOrder.StockCode;
            ExType = NewOrder.ExType;
            StockSignature = NewOrder.StockSignature;
            TransactionTime = NewOrder.TransactionTime;
            Side = NewOrder.Side;
            OrderType = NewOrder.OrderType;
            Price = NewOrder.Price;
            Quantity = NewOrder.Quantity;
            Deleted = NewOrder.Deleted;

            if (NewOrder.TransactionTime > TransactionTime) TransactionTime = NewOrder.TransactionTime;
        }

        public OddLotOrder Clone()
        {
            OddLotOrder clone = new OddLotOrder();

            clone.OrderNo = OrderNo;
            clone.SubmitBrokerNo = SubmitBrokerNo;
            clone.StockCode = StockCode;
            clone.Side = Side;
            clone.Price = Price;
            clone.Quantity = Quantity;
            clone.OrderType = OrderType;
            clone.ExType = ExType;
            clone.StockSignature = StockSignature;
            clone.TransactionTime = TransactionTime;
            clone.Deleted = Deleted;

            return clone;
        }
    }
}
