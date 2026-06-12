using System;
using System.Collections.Generic;
using System.Text;

namespace TradeDB
{
    public class Deal
    {
        public int DealNo = 0;
        public int OrderNo = 0;
        public Order TheOrder = null;
        public decimal Price = 0;
        public int Quantity = 0;
        public DateTime Time = DateTime.MinValue;
        public string BrokerId = null;

        public static DateTime GetDealTimeFromMsg(string Value)
        {
            string v = Value.Trim();
            DateTime d;

            try
            {
                d = new DateTime(
                    int.Parse(v.Substring(0, 4)),
                    int.Parse(v.Substring(5, 2)),
                    int.Parse(v.Substring(8, 2)),
                    int.Parse(v.Substring(11, 2)),
                    int.Parse(v.Substring(14, 2)),
                    int.Parse(v.Substring(17, 2)));
            }
            catch
            {
                return DateTime.MinValue;
            }

            return d;
        }

        public void Update(Deal NewDeal)
        {
            DealNo = NewDeal.DealNo;
            OrderNo = NewDeal.OrderNo;
            Price = NewDeal.Price;
            Quantity = NewDeal.Quantity;
            Time = NewDeal.Time;
            BrokerId = NewDeal.BrokerId;
        }

        public Deal Clone()
        {
            Deal clone = new Deal();

            clone.DealNo = DealNo;
            clone.OrderNo = OrderNo;
            clone.Price = Price;
            clone.Quantity = Quantity;
            clone.Time = Time;
            clone.BrokerId = BrokerId;

            return clone;
        }
    }
}
