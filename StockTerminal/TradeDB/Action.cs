using System;
using System.Collections.Generic;
using System.Text;

namespace TradeDB
{
    public class Action : ICloneable
    {
        public enum ActionTypeEnum { Place = 1, Cancel = 2, Amend = 4 }

        public int ActionNo = 0;
        public string ActionRefNo = null;     // for client side to verify if the action is delivered to server sucessfully
        public int OrderNo = 0;
        public Order TheOrder = null;
        public int ActionType = 0;
        public decimal Price = 0;
        public int Quantity = 0;
        public decimal NewPrice = 0;
        public int NewQuantity = 0;
        public string UserId = null;
        public int Channel = 0;
        public DateTime ActionTime = DateTime.MinValue;
        public int Status = 0;
        public string StatusMessage = null;

        public object Clone()
        {
            Action clone = new Action();
            clone.ActionNo = ActionNo;
            clone.ActionRefNo = ActionRefNo;
            clone.OrderNo = OrderNo;
            clone.ActionType = ActionType;
            clone.Price = Price;
            clone.Quantity = Quantity;
            clone.NewPrice = NewPrice;
            clone.NewQuantity = NewQuantity;
            clone.UserId = UserId;
            clone.Channel = Channel;
            clone.ActionTime = ActionTime;
            clone.Status = Status;
            clone.StatusMessage = StatusMessage;

            return (object) clone;
        }
    }
}
