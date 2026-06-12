using System;
using System.Collections.Generic;
using System.Text;

namespace TradeDB.TradeDBNet
{
    class OddLotOrderBook
    {
        private readonly Dictionary<ulong, OddLotOrder> ByOrderNo = new Dictionary<ulong, OddLotOrder>(50);
        private readonly Dictionary<string, Dictionary<ulong, OddLotOrder>> ByStockSignature = new Dictionary<string, Dictionary<ulong, OddLotOrder>>(50);

        private readonly object AccessMutex = new object();

        public void AddUpdate(OddLotOrder NewOrder)
        {
            if (NewOrder != null)
            {
                lock (AccessMutex)
                {
                    AddUpdateOrderInternal(NewOrder);
                }
            }
        }

        /*
        public List<OddLotOrder> AddUpdate(Dictionary<ulong, OddLotOrder> dict)
        {
            if (dict == null || dict.Count <= 0) return null;

            List<OddLotOrder> list = new List<OddLotOrder>(dict.Count);

            lock (AccessMutex)
            {
                foreach (OddLotOrder stock in dict.Values)
                {
                    list.Add(AddUpdateOrderInternal(stock));
                }
            }

            return list;
        }
         */

        public List<OddLotOrder> GetAll()
        {
            List<OddLotOrder> orders = null;

            lock (AccessMutex)
            {
                orders = new List<OddLotOrder>(ByOrderNo.Count);
                foreach (OddLotOrder order in ByOrderNo.Values)
                {
                    orders.Add((OddLotOrder)order.Clone());
                }
            }

            return orders;
        }

        /// <summary>
        /// Get list of odd lot order by supplying list of stock code(s).
        /// </summary>
        public List<OddLotOrder> GetByStockCode(List<string> StockCodeList)
        {
            if (StockCodeList == null || StockCodeList.Count <= 0) return null;

            List<OddLotOrder> orders = null;
            Dictionary<ulong, OddLotOrder> oddLotOrderDict = null;

            lock (AccessMutex)
            {
                foreach (string stockCode in StockCodeList)
                {
                    if (ByStockSignature.TryGetValue(stockCode.ToUpper(), out oddLotOrderDict) && oddLotOrderDict != null)
                    {
                        orders = new List<OddLotOrder>(oddLotOrderDict.Count);

                        foreach (OddLotOrder order in oddLotOrderDict.Values)
                        {
                            orders.Add((OddLotOrder)order.Clone());
                        }
                    }
                    else
                    {
                        orders = new List<OddLotOrder>(0);
                    }
                }
            }

            return orders;
        }

        public OddLotOrder GetByOrderNo(ulong OrderNo)
        {
            OddLotOrder order = null;

            lock (AccessMutex)
            {
                ByOrderNo.TryGetValue(OrderNo, out order);
            }

            return order;

        }

        public void Clear()
        {
            lock (AccessMutex)
            {
                ByOrderNo.Clear();
                ByStockSignature.Clear();
            }
        }

        //public List<OddLotOrder> GetByOrderNo(List<int> OrderNoList)
        //{
        //    if (OrderNoList == null || OrderNoList.Count <= 0) return null;

        //    List<OddLotOrder> orders = new List<OddLotOrder>(OrderNoList.Count);
        //    OddLotOrder order = null;

        //    lock (AccessMutex)
        //    {
        //        foreach (int orderNo in OrderNoList)
        //        {
        //            if (ByOrderNo.TryGetValue(orderNo, out order))
        //            {
        //                orders.Add((OddLotOrder)order.Clone());
        //            }
        //        }
        //    }

        //    return orders;
        //}

        private void AddUpdateOrderInternal(OddLotOrder NewOrder)
        {
            OddLotOrder theOrder = null;
            Dictionary<ulong, OddLotOrder> orderDict = null;
            string StockSignature = null;

            // ByOrderNo
            if (ByOrderNo.TryGetValue(NewOrder.OrderNo, out theOrder))
            {
                StockSignature = theOrder.StockSignature;
                theOrder.Update(NewOrder);
                NewOrder.Update(theOrder);
            }
            else
            {
                theOrder = (OddLotOrder)NewOrder.Clone();
                ByOrderNo.Add(theOrder.OrderNo, theOrder);
            }

            // ByStockCode
            if (theOrder.StockSignature != StockSignature)
            {
                if (StockSignature != null && ByStockSignature.TryGetValue(StockSignature, out orderDict))
                {
                    try { orderDict.Remove(theOrder.OrderNo); }
                    catch { }
                    if (orderDict.Count <= 0) ByStockSignature.Remove(StockSignature);
                }

                if (theOrder.StockSignature != null)
                {
                    if (!ByStockSignature.TryGetValue(theOrder.StockSignature, out orderDict))
                        ByStockSignature.Add(theOrder.StockSignature, orderDict = new Dictionary<ulong, OddLotOrder>(50));

                    orderDict.Add(theOrder.OrderNo, theOrder);
                }
            }
        }

        private void RemoveOrderInternal(ulong OrderNo)
        {
            Dictionary<ulong, OddLotOrder> orderDict = null;
            OddLotOrder order = null;

            if (ByOrderNo.TryGetValue(OrderNo, out order))
            {
                ByOrderNo.Remove(OrderNo);

                if (order.StockSignature != null && ByStockSignature.TryGetValue(order.StockSignature, out orderDict))
                {
                    orderDict.Remove(OrderNo);

                    if (orderDict.Count <= 0)
                        ByStockSignature.Remove(order.StockSignature);
                }

            }
        }
    }
}
