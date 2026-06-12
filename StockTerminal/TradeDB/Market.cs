using System;
using System.Collections.Generic;
using System.Text;

namespace TradeDB
{
    public class Market
    {
        public class OrderActionEnable
        {
            public readonly int Place;
            public readonly int Modify;

            public OrderActionEnable(int Place, int Modify)
            {
                this.Place = Place;
                this.Modify = Modify;
            }

            public OrderActionEnable Clone()
            {
                return new OrderActionEnable(Place, Modify);
            }
        }

        public readonly string Code;
        private int pStatus;
        private OrderActionEnable[] OrderActionEnableArray = new OrderActionEnable[2] { new OrderActionEnable(0, 0), new OrderActionEnable(0,0) };
        private int CurrentOrderActionEnableIndex = 0;

        public Market Clone()
        {
            Market clone = new Market(Code);

            clone.pStatus = pStatus;
            clone.OrderActionEnableArray[0] = OrderActionEnableArray[0].Clone();
            clone.OrderActionEnableArray[1] = OrderActionEnableArray[1].Clone();

            return clone;
        }

        public Market(string Code)
        {
            this.Code = Code != null ? Code : "";
        }

        public OrderActionEnable OrderAction
        {
            get { return OrderActionEnableArray[CurrentOrderActionEnableIndex]; }
        }

        public int Status
        {
            get { return pStatus; }
        }

        /// <summary>
        /// Set the status and update OrderActionEnable according to Status and CurrentTime.
        /// </summary>
        /// <param name="Status">Market status</param>
        /// <param name="CurrentTime">Time of the status</param>
        /// <returns>true if changed, otherwise false.</returns>
        public bool SetStatus(int Status, DateTime CurrentTime)
        {
            bool changed = (pStatus != Status);
            pStatus = Status;
            int NextIndex = 1 - CurrentOrderActionEnableIndex;

            switch (pStatus)
            {
                case 0:
                    if (CurrentTime.Hour < 11)
                    {
                        OrderActionEnableArray[NextIndex] = new OrderActionEnable(15, 15);
                    }
                    else
                    {
                        OrderActionEnableArray[NextIndex] = new OrderActionEnable(12, 12);
                    }
                    break;

                case 1:     // Order input: 9:00-9:15
                    OrderActionEnableArray[NextIndex] = new OrderActionEnable(15, 15);
                    break;

                case 2:     // No Cancel: 9:15-9:20
                case 15:    // Random Matching: 9:22-9:22
                    //OrderActionEnableArray[NextIndex] = new OrderActionEnable(13, 12);
                    // Under POS model, No cancel peroid (9:15-9:20) can place both aution & auction limit order
                    OrderActionEnableArray[NextIndex] = new OrderActionEnable(15, 12);
                    break;

                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
                    OrderActionEnableArray[NextIndex] = new OrderActionEnable(12, 12);
                    break;

                case 10:
                case 11:
                case 12:
                case 13:
                    OrderActionEnableArray[NextIndex] = new OrderActionEnable(3, 3);
                    break;

                default:
                    OrderActionEnableArray[NextIndex] = new OrderActionEnable(0, 0);
                    break;
            }

            if (!changed && 
                (OrderActionEnableArray[NextIndex].Place != OrderActionEnableArray[CurrentOrderActionEnableIndex].Place ||
                OrderActionEnableArray[NextIndex].Modify != OrderActionEnableArray[CurrentOrderActionEnableIndex].Modify))
            {
                changed = true;
            }

            CurrentOrderActionEnableIndex = NextIndex;

            return changed;
        }

        public override string ToString()
        {
            OrderActionEnable oae = OrderActionEnableArray[CurrentOrderActionEnableIndex];
            return Code + " " + pStatus.ToString() + " " + oae.Place.ToString() + " " + oae.Modify.ToString();
        }
    }
}
