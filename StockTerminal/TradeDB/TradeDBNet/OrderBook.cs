using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace TradeDB.Net
{
    class OrderBook
    {
        private readonly Dictionary<int, Order> ByOrderNo = new Dictionary<int, Order>(50);
        private readonly Dictionary<string, Dictionary<int, Order>> ByAECode = new Dictionary<string, Dictionary<int, Order>>(50);
        private readonly Dictionary<string, Dictionary<int, Order>> ByAccountNo = new Dictionary<string, Dictionary<int, Order>>(50);
        //private readonly Dictionary<string, Dictionary<int, Order>> ByStockCode = new Dictionary<string, Dictionary<int, Order>>(50);
        private readonly Dictionary<int, Dictionary<int, Order>> ByStatus = new Dictionary<int, Dictionary<int, Order>>(50);
        private readonly Dictionary<char, Dictionary<int, Order>> ByReplied = new Dictionary<char, Dictionary<int, Order>>(50);
        private int OrderNoMax = 0;
        private int OrderNoMin = 0;

        private readonly object AccessMutex = new object();

        public bool OrderPlacePending
        {
            get
            {
                bool pending;

                lock (AccessMutex)
                {
                    pending = ByOrderNo.ContainsKey(0); // Can only have one pending order place. The order no is zero
                }

                return pending;
            }
        }

        public void AddUpdate(Order NewOrder)
        {
            if (NewOrder != null)
            {
                lock (AccessMutex)
                {
                    AddUpdateOrderInternal(NewOrder);
                }
            }
        }

        public Order AddUpdate(Deal NewDeal)
        {
            Order order = null;

            if (NewDeal != null)
            {
                lock (AccessMutex)
                {
                    if (ByOrderNo.TryGetValue(NewDeal.OrderNo, out order))
                    {
                        order.AddUpdateDeal(NewDeal);
                        order = order.Clone();
                    }
                    else
                    {
                        order = new Order();
                        order.OrderNo = NewDeal.OrderNo;
                        order.OrderNoSort = GetOrderNoSort(order.OrderNo);
                        order.AddUpdateDeal(NewDeal);

                        AddUpdateOrderInternal(order);
                    }
                }
            }

            return order;
        }

        public void AddUpdate(Dictionary<int, Order> dict)
        {
            if (dict != null && dict.Count > 0)
            {
                lock (AccessMutex)
                {
                    foreach (Order order in dict.Values)
                    {
                        AddUpdateOrderInternal(order);
                    }
                }
            }
        }

        public Order ConfirmNewOrder(string ActionRef, int OrderNo)
        {
            if (ActionRef == null || OrderNo <= 0) return null;

            Order orderZero = null;
            Order order = null;

            lock (AccessMutex)
            {
                if (ByOrderNo.TryGetValue(0, out orderZero))
                {
                    if (orderZero.FirstActionRef == ActionRef)
                    {
                        RemoveOrderInternal(0);
                        if (ByOrderNo.TryGetValue(OrderNo, out order))
                        {
                            order.OverwriteActionsBy(orderZero.Actions);    // mainly to fill in the FirstActionRef which isn't filled in properly due to early arrival of the order object prior to the order place action acknowledgement.
                            order = order.Clone();
                        }
                        else
                        {
                            order = orderZero;
                            order.OrderNo = OrderNo;
                            order.OrderNoSort = GetOrderNoSort(order.OrderNo);
                            AddUpdateOrderInternal(order);
                        }
                    }
                }
                else if (ByOrderNo.TryGetValue(OrderNo, out order))
                {
                    if (order.FirstActionRef == ActionRef)
                    {
                        order = order.Clone();
                    }
                    else
                    {
                        order = null;
                    }
                }
            }

            return order;
        }

        public Order RejectNewOrder(string ActionRef, string Message)
        {
            if (ActionRef == null) return null;

            Order orderZero = null;

            lock (AccessMutex)
            {
                if (ByOrderNo.TryGetValue(0, out orderZero))
                {
                    if (orderZero.FirstActionRef == ActionRef)
                    {
                        RemoveOrderInternal(0);
                        orderZero.OrderNo = --OrderNoMin;
                        orderZero.OrderNoSort = GetOrderNoSort(orderZero.OrderNo);
                        string messageLower = Message.ToLower();
                        if (messageLower.Contains("wrong trading pass"))
                        {
                            orderZero.Status = (int)Order.OrderStatusEnum.InvalidTradePass;
                        }
                        else
                        {
                            orderZero.Status = (int)Order.OrderStatusEnum.InputError;
                        }
                        orderZero.StatusMessage = Message;
                    }
                    else
                        orderZero = null;
                }
                else
                {
                    orderZero = null;
                }
            }

            return orderZero;
        }

        public Order SetOrderLastAction(int OrderNo, string LastAction)
        {
            Order order = null;

            if (LastAction != null && LastAction.Length > 0)
            {
                lock (AccessMutex)
                {
                    if (ByOrderNo.TryGetValue(OrderNo, out order))
                    {
                        if (order.LastAction == null || order.LastAction.Length <= 0)
                        {
                            order.LastAction = LastAction;
                            order = order.Clone();
                        }
                    }
                }
            }

            return order;
        }

        public List<Order> GetAll()
        {
            List<Order> orders = null;

            lock (AccessMutex)
            {
                orders = new List<Order>(ByOrderNo.Count);
                foreach (Order order in ByOrderNo.Values)
                {
                    orders.Add(order.Clone());
                }
            }

            return orders;        
        }

        public Order GetByOrderNo(int OrderNo)
        {
            Order order = null;

            lock (AccessMutex)
            {
                ByOrderNo.TryGetValue(OrderNo, out order);
            }

            return order;

        }

        public List<Order> GetByOrderNo(List<int> OrderNoList)
        {
            if (OrderNoList == null || OrderNoList.Count <= 0) return null;

            List<Order> orders = new List<Order>(OrderNoList.Count);
            Order order = null;

            lock (AccessMutex)
            {
                foreach (int orderNo in OrderNoList)
                {
                    if (ByOrderNo.TryGetValue(orderNo, out order))
                    {
                        orders.Add(order.Clone());
                    }
                }
            }

            return orders;
        }

        public List<Order> GetByAECode(string AECode)
        {
            List<Order> orders = null;
            Dictionary<int, Order> orderDict = null;

            lock (AccessMutex)
            {
                if (ByAECode.TryGetValue(AECode, out orderDict) && orderDict != null)
                {
                    orders = new List<Order>(orderDict.Count);

                    foreach (Order order in orderDict.Values)
                    {
                        orders.Add(order.Clone());
                    }
                }
                else
                {
                    orders = new List<Order>(0);
                }
            }

            return orders;
        }

        public List<Order> GetByAccountNo(string AccountNo)
        {
            List<Order> orders = null;
            Dictionary<int, Order> orderDict = null;

            lock (AccessMutex)
            {
                if (ByAccountNo.TryGetValue(AccountNo.ToUpper(), out orderDict) && orderDict != null)
                {
                    orders = new List<Order>(orderDict.Count);

                    foreach (Order order in orderDict.Values)
                    {
                        orders.Add(order.Clone());
                    }
                }
                else
                {
                    orders = new List<Order>(0);
                }
            }

            return orders;
        }

        //public List<Order> GetByStockCode(string StockCode)
        //{
        //    List<Order> orders = null;
        //    Dictionary<int, Order> orderDict = null;

        //    lock (AccessMutex)
        //    {
        //        if (ByStockCode.TryGetValue(StockCode.ToUpper(), out orderDict) && orderDict != null)
        //        {
        //            orders = new List<Order>(orderDict.Count);

        //            foreach (Order order in orderDict.Values)
        //            {
        //                orders.Add((Order)order.Clone());
        //            }
        //        }
        //        else
        //        {
        //            orders = new List<Order>(0);
        //        }
        //    }
            
        //    return orders;
        //}

        public List<Order> GetByStatus(int Status)
        {
            List<Order> orders = null;
            Dictionary<int, Order> orderDict = null;

            lock (AccessMutex)
            {
                if (ByStatus.TryGetValue(Status, out orderDict) && orderDict != null)
                {
                    orders = new List<Order>(orderDict.Count);

                    foreach (Order order in orderDict.Values)
                    {
                        orders.Add(order.Clone());
                    }
                }
                else
                {
                    orders = new List<Order>(0);
                }
            }

            return orders;
        }

        public List<Order> GetByReplied(char Replied)
        {
            List<Order> orders = null;
            Dictionary<int, Order> orderDict = null;

            lock (AccessMutex)
            {
                if (ByReplied.TryGetValue(Replied, out orderDict) && orderDict != null)
                {
                    orders = new List<Order>(orderDict.Count);

                    foreach (Order order in orderDict.Values)
                    {
                        orders.Add(order.Clone());
                    }
                }
                else
                {
                    orders = new List<Order>(0);
                }
            }

            return orders;
        }


        /// <summary>
        /// Get the order no for sorting purpose
        /// </summary>
        /// <param name="OrderNo">The order number.</param>
        public string GetOrderNoSort(int OrderNo)
        {
            string pOrderNoSort = null;

            if (OrderNo > 0)
            {
                pOrderNoSort = OrderNo.ToString("000000") + " 000000";
            }
            else if (OrderNo == 0)
            {
                pOrderNoSort = "z";
            }
            else
            {
                pOrderNoSort = String.Format("{0:000000} {1:000000}", OrderNoMax, -OrderNo);
            }

            return pOrderNoSort;
        }

        private void AddUpdateOrderInternal(Order NewOrder)
        {
            Order theOrder = null;

            Dictionary<int, Order> orderDict = null;
            string AECode = null;
            string AccountNo = null;
            string StockCode = null;
            int Status = 0;
            char Replied = '\0';
            bool OrderFound = false;

            // ByOrderNo
            if (ByOrderNo.TryGetValue(NewOrder.OrderNo, out theOrder))
            {
                AECode = (theOrder.AECode != null ? theOrder.AECode.ToUpper().Trim() : null);
                AccountNo = (theOrder.AccountNo != null ? theOrder.AccountNo.ToUpper().Trim() : null);
                StockCode = (theOrder.StockCode != null ? theOrder.StockCode.ToUpper().Trim() : null);
                Status = theOrder.Status;
                Replied = theOrder.Replied;

                NewOrder.BeforeChange = theOrder.Clone();

                theOrder.Update(NewOrder);
                NewOrder.Update(theOrder);  // supply field values not exist in NewOrder from existing order.

                OrderFound = true;
            }
            else
            {
                if (OrderNoMax < NewOrder.OrderNo) OrderNoMax = NewOrder.OrderNo;
                if (OrderNoMin > NewOrder.OrderNo) OrderNoMin = NewOrder.OrderNo;
                theOrder = (Order)NewOrder.Clone();
                ByOrderNo.Add(theOrder.OrderNo, theOrder);
            }

            // ByAECode
/*          if (!OrderFound || AECode != theOrder.AECode)
            {
                if (OrderFound && ByAECode.TryGetValue(AECode, out orderDict))
                {
                    try { orderDict.Remove(theOrder.OrderNo); }
                    catch { }
                    if (orderDict.Count <= 0) ByAECode.Remove(AECode);
                }

                if (!ByAECode.TryGetValue(theOrder.AECode, out orderDict))
                {
                    orderDict = new Dictionary<int, Order>(20);
                    ByAECode.Add(theOrder.AECode, orderDict);
                }
                orderDict[theOrder.OrderNo] = theOrder;
            }
*/
            // ByAECode
            if (theOrder.AECode != null && theOrder.AECode.ToUpper().Trim() != AECode ||
                AECode != null)
            {
                if (AECode != null && ByAECode.TryGetValue(AECode, out orderDict))
                {
                    try { orderDict.Remove(theOrder.OrderNo); }
                    catch { }
                    if (orderDict.Count <= 0) ByAECode.Remove(AECode);
                }

                if (theOrder.AECode != null)
                {
                    AECode = theOrder.AECode.ToUpper().Trim();
                    if (!ByAECode.TryGetValue(AECode, out orderDict))
                    {
                        orderDict = new Dictionary<int, Order>(10);
                        ByAECode.Add(AECode, orderDict);
                    }
                    orderDict[theOrder.OrderNo] = theOrder;
                }
            }


            // ByAccountNo
            if (theOrder.AccountNo != null && theOrder.AccountNo.ToUpper().Trim() != AccountNo ||
                AccountNo != null)
            {
                if (AccountNo != null && ByAccountNo.TryGetValue(AccountNo, out orderDict))
                {
                    try { orderDict.Remove(theOrder.OrderNo); }
                    catch { }
                    if (orderDict.Count <= 0) ByAccountNo.Remove(AccountNo);
                }

                if (theOrder.AccountNo != null)
                {
                    AccountNo = theOrder.AccountNo.ToUpper().Trim();
                    if (!ByAccountNo.TryGetValue(AccountNo, out orderDict))
                    {
                        orderDict = new Dictionary<int, Order>(10);
                        ByAccountNo.Add(AccountNo, orderDict);
                    }
                    orderDict[theOrder.OrderNo] = theOrder;
                }
            }

            // ByStockCode
            //if (theOrder.StockCode != null && theOrder.StockCode.ToUpper().Trim() != StockCode ||
            //    StockCode != null)
            //{
            //    if (StockCode != null && ByStockCode.TryGetValue(StockCode, out orderDict))
            //    {
            //        try { orderDict.Remove(theOrder.OrderNo); }
            //        catch { }
            //        if (orderDict.Count <= 0) ByStockCode.Remove(StockCode);
            //    }

            //    if (theOrder.StockCode != null)
            //    {
            //        StockCode = theOrder.StockCode.ToUpper().Trim();
            //        if (!ByStockCode.TryGetValue(StockCode, out orderDict))
            //        {
            //            orderDict = new Dictionary<int, Order>(10);
            //            ByStockCode.Add(StockCode, orderDict);
            //        }
            //        orderDict[theOrder.OrderNo] = theOrder;
            //    }
            //}

            // ByStatus
            if (!OrderFound || Status != theOrder.Status)
            {
                if (OrderFound && ByStatus.TryGetValue(Status, out orderDict))
                {
                    try { orderDict.Remove(theOrder.OrderNo); }
                    catch { }
                    if (orderDict.Count <= 0) ByStatus.Remove(Status);
                }

                if (!ByStatus.TryGetValue(theOrder.Status, out orderDict))
                {
                    orderDict = new Dictionary<int, Order>(20);
                    ByStatus.Add(theOrder.Status, orderDict);
                }
                orderDict[theOrder.OrderNo] = theOrder;
            }

            // ByReplied
            if (!OrderFound || Replied != theOrder.Replied)
            {
                if (OrderFound && ByReplied.TryGetValue(Replied, out orderDict))
                {
                    try { orderDict.Remove(theOrder.OrderNo); }
                    catch { }
                    if (orderDict.Count <= 0) ByReplied.Remove(Replied);
                }

                if (!ByReplied.TryGetValue(theOrder.Replied, out orderDict))
                {
                    orderDict = new Dictionary<int, Order>(20);
                    ByReplied.Add(theOrder.Replied, orderDict);
                }
                orderDict[theOrder.OrderNo] = theOrder;
            }
        }

        private void RemoveOrderInternal(int OrderNo)
        {
            Dictionary<int, Order> orderDict = null;
            Order order = null;

            if (ByOrderNo.TryGetValue(OrderNo, out order))
            {
                ByOrderNo.Remove(OrderNo);

                if (order.AECode != null && ByAECode.TryGetValue(order.AECode, out orderDict))
                {
                    if (orderDict.Remove(OrderNo) && orderDict.Count <= 0) ByAECode.Remove(order.AECode);
                }

                if (order.AccountNo != null && ByAccountNo.TryGetValue(order.AccountNo, out orderDict))
                {
                    if (orderDict.Remove(OrderNo) && orderDict.Count <= 0) ByAccountNo.Remove(order.AccountNo);
                }

                //if (order.StockCode != null && ByStockCode.TryGetValue(order.StockCode, out orderDict))
                //{
                //    if (orderDict.Remove(OrderNo) && orderDict.Count <= 0) ByStockCode.Remove(order.StockCode);
                //}

                if (ByStatus.TryGetValue(order.Status, out orderDict))
                {
                    if (orderDict.Remove(OrderNo) && orderDict.Count <= 0) ByStatus.Remove(order.Status);
                }
            }
        }

        public void Clear()
        {
            lock (AccessMutex)
            {
                ByOrderNo.Clear();
                ByAECode.Clear();
                ByAccountNo.Clear();
                //ByStockCode.Clear();
                ByStatus.Clear();
                ByReplied.Clear();
                OrderNoMax = 0;
                OrderNoMin = 0;
            }
        }
    }
}
