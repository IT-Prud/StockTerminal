using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace TradeDB
{
    public class Order
    {
        public enum OrderStatusEnum
        { 
            Unknown = 0,
            Sending = 9001,
            SendSuccess = 9002,
            SendFail = 9003,
            InvalidTradePass = 9004,
            InputError = 90005,
            Pending = 10001,
            CreditChecking = 20001,
            CreditPass = 20002,
            CreditFailed = 20003,
            SentToOG = 30001,
            Queue = 30002,
            Completed = 80001,
            Cancelled = 80002,
            PartiallyCompleted = 80003,
            RejectedByOG = 90003,
            RejectedBySupervisor = 90006
        }

        public int OrderNo = 0;

        /// <summary>
        /// Unique key for identifying an order with regard to sent and unsent orders, can be used for sorting.
        /// </summary>
        public string OrderNoSort = "";

        public string AccountNo = null;
        public string AECode = null;
        public string ActionPlacedBy = null;
        public string BrokerNo = null;
        public string StockCode = null;
        public char Side = '\0';
        public decimal Price = 0;
        public int Quantity = 0;
        public char OrderType = '\0';
        public decimal AvgPrice = 0;
        public int Filled = 0;
        private DateTime pLastDealTime = DateTime.MinValue;
        public int Status = 0;
        public string StatusMessage = null;
        public string OGMessage = null;
        public string LastAction = null;
        public char Replied = '\0';
        public string Memo = null;
        public List<Action> Actions = new List<Action>(3);
        public Dictionary<int, Deal> Deals = new Dictionary<int, Deal>(3);
        public ExchangeTypeEnum ExType = ExchangeTypeEnum.HKG;
        public decimal FilledAmount = 0;
        public DateTime PlaceDateTime = DateTime.MinValue;
        public string OrderPlacedBy = null;
        public char DPGW = '\0';
        public bool IsAggregateOrder = false;
        public string OTP = null;

        public Order BeforeChange = null;   // for storing order before change, handled externally only to prevent clone loop

        public string FirstActionRef
        {
            get
            {
                if (Actions != null && Actions.Count > 0)
                {
                    return Actions[0].ActionRefNo;
                }
                return null;
            }
        }

        public bool Closed
        {
            get
            {
                return (Status == (int)OrderStatusEnum.SendFail || Status == (int)OrderStatusEnum.InvalidTradePass ||
                    Status == (int)OrderStatusEnum.CreditFailed || Status >= (int)OrderStatusEnum.Completed);
            }
        }

        public bool Rejected
        {
            get
            {
                return (Status == (int)OrderStatusEnum.SendFail || Status == (int)OrderStatusEnum.InvalidTradePass ||
                    Status == (int)OrderStatusEnum.CreditFailed || Status >= 90000);
            }
        }

        public string StockCodeZeroTrimmed
        {
            get
            {
                try
                {
                    return int.Parse(StockCode).ToString();
                }
                catch { }
                return StockCode;
            }
        }

        /*
        public decimal MostUpdateAvgPrice
        {
            get
            {
                if (FilledAmount > 0 && Filled > 0)
                {
                    return (FilledAmount / Filled);
                }
                else if (Filled > 0 && Filled >= pDealQtySum)
                {
                    return (AvgPrice > 0) ? AvgPrice : Price;
                }
                else if (pDealQtySum > 0)
                {
                    return (pDealAmountSum / pDealQtySum);
                }
                return 0;
            }
        }
        
        public int MostUpdateFilled
        {
            get
            {
                if (Filled >= pDealQtySum)
                {
                    return Filled;
                }
                return pDealQtySum;
            }
        }

        public int DealQtySum
        {
            get
            {
                return pDealQtySum;
            }
        }
         */

        public DateTime LastDealTime
        {
            get { return pLastDealTime; }
        }

        public void Update(Order NewOrder)
        {
            if (this == NewOrder) return;   // no need to update if same object

            if (NewOrder.AccountNo != null)
            {
                OrderNoSort = NewOrder.OrderNoSort;
                AccountNo = NewOrder.AccountNo;
                AECode = NewOrder.AECode;
                ActionPlacedBy = NewOrder.ActionPlacedBy;
                BrokerNo = NewOrder.BrokerNo;
                StockCode = NewOrder.StockCode;
                Side = NewOrder.Side;
                Price = NewOrder.Price;
                Quantity = NewOrder.Quantity;
                OrderType = NewOrder.OrderType;
                AvgPrice = NewOrder.AvgPrice;
                Filled = NewOrder.Filled;
                Status = NewOrder.Status;
                StatusMessage = NewOrder.StatusMessage;
                OGMessage = NewOrder.OGMessage;
                LastAction = NewOrder.LastAction;
                Replied = NewOrder.Replied;
                Memo = NewOrder.Memo;
                FilledAmount = NewOrder.FilledAmount;
                OrderPlacedBy = NewOrder.OrderPlacedBy;
                DPGW = NewOrder.DPGW;
                OTP = NewOrder.OTP;
            }

			if (NewOrder.Actions != null && NewOrder.Actions.Count > 0)
			{
				OverwriteActionsBy(NewOrder.Actions);
			}

            if (NewOrder.Deals != null && NewOrder.Deals.Count > 0)
            {
                if (Deals == null) 
                    Deals = new Dictionary<int, Deal>(NewOrder.Deals.Count);

                Deal oldDeal;

                foreach (Deal newDeal in NewOrder.Deals.Values)
                {
                    if (Deals.TryGetValue(newDeal.DealNo, out oldDeal))
                        oldDeal.Update(newDeal);
                    else
                        Deals.Add(newDeal.DealNo, newDeal.Clone());
                }
            }

            if (pLastDealTime < NewOrder.LastDealTime) pLastDealTime = NewOrder.LastDealTime;
            if (PlaceDateTime < NewOrder.PlaceDateTime) PlaceDateTime = NewOrder.PlaceDateTime;
        }

        /// <summary>
        /// Fill the Actions by clones of action objects in ActionList, original objects discarded.
        /// </summary>
        /// <param name="ActionList">List of action objects to put cloned and filled into Actions</param>
        public void OverwriteActionsBy(List<Action> ActionList)
        {
            if (ActionList == null) return;

            if (Actions == null)
                Actions = new List<Action>(ActionList.Count);
            else if (Actions.Capacity < ActionList.Count)
                Actions.Capacity = ActionList.Count;

            int i;

			for (i = 0; i < Actions.Count; i++)
                Actions[i] = (Action)ActionList[i].Clone();

			for (; i < ActionList.Count; i++)
				Actions.Add((Action)ActionList[i].Clone());

            Actions.RemoveRange(i, Actions.Count - i);
        }

        public void AddUpdateDeal(Deal NewDeal)
        {
            if (NewDeal == null) return;

            Deal oldDeal;

            if (Deals == null)
            {
                Deals = new Dictionary<int, Deal>(3);
                Deals.Add(NewDeal.DealNo, NewDeal.Clone());
            }
            else if (Deals.TryGetValue(NewDeal.DealNo, out oldDeal))
                oldDeal.Update(NewDeal);
            else
                Deals.Add(NewDeal.DealNo, NewDeal.Clone());

            if (pLastDealTime < NewDeal.Time) pLastDealTime = NewDeal.Time;
        }

        public Order Clone()
        {
            Order clone = new Order();

            clone.OrderNo = OrderNo;
            clone.OrderNoSort = OrderNoSort;
            clone.AccountNo = AccountNo;
            clone.AECode = AECode;
            clone.ActionPlacedBy = ActionPlacedBy;
            clone.BrokerNo = BrokerNo;
            clone.StockCode = StockCode;
            clone.Side = Side;
            clone.Price = Price;
            clone.Quantity = Quantity;
            clone.OrderType = OrderType;
            clone.AvgPrice = AvgPrice;
            clone.Filled = Filled;
            clone.pLastDealTime = pLastDealTime;
            clone.Status = Status;
            clone.StatusMessage = StatusMessage;
            clone.OGMessage = OGMessage;
            clone.LastAction = LastAction;
            clone.Replied = Replied;
            clone.Memo = Memo;
            clone.ExType = ExType;
            clone.FilledAmount = FilledAmount;
            clone.PlaceDateTime = PlaceDateTime;
            clone.OrderPlacedBy = OrderPlacedBy;
            clone.DPGW = DPGW;
            clone.OTP = OTP;

            int n;
            Deal clonedDeal;

            if (Actions != null)
            {
                n = Actions.Count;
                clone.Actions = new List<Action>(n);
                for (int i = 0; i < n; i++)
                {
                    clone.Actions.Add((Action)Actions[i].Clone());
                    clone.Actions[i].TheOrder = clone;
                }
            }

            if (Deals != null)
            {
                n = Deals.Count;
                clone.Deals = new Dictionary<int, Deal>(n);
                foreach (KeyValuePair<int, Deal> kvp in Deals)
                {
                    clonedDeal = (Deal)kvp.Value.Clone();
                    clonedDeal.TheOrder = clone;
                    clone.Deals.Add(kvp.Key, clonedDeal);
                }
            }

            return clone;
        }
    }
}
