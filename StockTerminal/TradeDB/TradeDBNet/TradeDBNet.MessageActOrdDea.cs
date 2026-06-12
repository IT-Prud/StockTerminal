using System;
using System.Collections.Generic;
using System.Text;
using Processors;

namespace TradeDB.Net
{
    public partial class TradeDBNet : Processor, ITradeDB
    {
        private void ProcessMessageActOrdDea(TradeMessage Msg)
        {
            pDPOrder.Execute(DataProcessor<Order>.Operation.CodeEnum.ParseData, Msg);
        }

        private void ParseMessageOrder(TradeMessage Msg)
        {
            Dictionary<string, TradeMessageTag> tags = Msg.Tags;
            TradeMessageTag tag = null;
            int orderNo;
            //decimal avgPrice;
            Order orderUpdate;

            if (Msg.MessageId == "ACT")
            {
                if (Msg.MessageType == "21")    // Action Response - Success
                {
                    if (Msg.Tags.ContainsKey("ORDN") && int.TryParse(Msg.Tags["ORDN"].Value, out orderNo))
                    {
                        if ((orderUpdate = pOrderBook.ConfirmNewOrder(Msg.Tags["REF"].Value, orderNo)) != null)
                        {
                            ProcessReceivedOrderEvent(orderUpdate);
                            DPOrder.Execute(DataProcessor<Order>.Operation.CodeEnum.DataArrival, null, "", orderUpdate);
                        }
                    }
                }
                else if (Msg.MessageType == "22")   // Action Response - Failure
                {
                    if ((orderUpdate = pOrderBook.RejectNewOrder(Msg.Tags["REF"].Value, Msg.Tags["MSG"].Value)) != null)
                    {
                        ProcessReceivedOrderEvent(orderUpdate);
                        DPOrder.Execute(DataProcessor<Order>.Operation.CodeEnum.DataArrival, null, "", orderUpdate);
                    }
                }
            }
            else if (Msg.MessageId == "ORD")
            {
                if (Msg.MessageType == "21")
                {
                    if (!pOrderBookSynced)
                    {
                        DateTime nowTime = DateTime.Now;
                        if (orderBookSyncTime > DateTime.MinValue && (nowTime - orderBookSyncTime).TotalSeconds > 4)
                            pOrderBookSynced = true;
                        else
                            orderBookSyncTime = nowTime;
                    }

                    if (!int.TryParse(tags["ORNO"].Value, out orderNo)) return;

                    if (MaxOrderNo < orderNo) MaxOrderNo = orderNo;

                    orderUpdate = new Order();
                    orderUpdate.OrderNo = orderNo;
                    orderUpdate.OrderNoSort = pOrderBook.GetOrderNoSort(orderNo);

                    orderUpdate.AECode = tags["AECD"].Value;
                    orderUpdate.AccountNo = tags["ACNO"].Value;
                    if (tags.ContainsKey("PLBY")) orderUpdate.ActionPlacedBy = tags["PLBY"].Value;

                    if (tags.ContainsKey("BKNO")) orderUpdate.BrokerNo = tags["BKNO"].Value;

                    orderUpdate.Side = tags["SIDE"].Value[0];

                    string stkCode = tags["STKC"].Value.Trim();
                    int iStkcode;
                    int.TryParse(stkCode, out iStkcode);
                    orderUpdate.StockCode = iStkcode.ToString();

                    decimal.TryParse(tags["PRIC"].Value, out orderUpdate.Price);
                    int.TryParse(tags["QTY"].Value, out orderUpdate.Quantity);
                    //decimal.TryParse(tags["APRC"].Value, out avgPrice);
                    //orderUpdate.AvgPrice = avgPrice;
                    int.TryParse(tags["FILL"].Value, out orderUpdate.Filled);
                    int.TryParse(tags["STAT"].Value, out orderUpdate.Status);

                    orderUpdate.StatusMessage = tags["STAM"].Value;

                    if (tags.TryGetValue("OGM", out tag)) orderUpdate.OGMessage = tags["OGM"].Value;

                    orderUpdate.LastAction = tags["LACT"].Value;
                    orderUpdate.Replied = tags["RPLY"].Value.Trim()[0];
                    orderUpdate.Memo = tags["MEMO"].Value.Trim();

                    if (tags.ContainsKey("FAMT")) decimal.TryParse(tags["FAMT"].Value, out orderUpdate.FilledAmount);
                    if (orderUpdate.Filled > 0)
                        orderUpdate.AvgPrice = orderUpdate.FilledAmount / (decimal)orderUpdate.Filled;

                    orderUpdate.ExType = tags.ContainsKey("EXC") ? Stock.GetExchangeType(tags["EXC"].Value.Trim()) : ExchangeTypeEnum.HKG;

                    if (tags.ContainsKey("TIME")) DateTime.TryParse(tags["TIME"].Value, out orderUpdate.PlaceDateTime);
                    if (tags.ContainsKey("OPLBY")) orderUpdate.OrderPlacedBy = tags["OPLBY"].Value;

                    pOrderBook.AddUpdate(orderUpdate);
                    ProcessReceivedOrderEvent(orderUpdate);

                    DPOrder.Execute(DataProcessor<Order>.Operation.CodeEnum.DataArrival, null, "", orderUpdate);
                }
            }
            else if (Msg.MessageId == "DEA")
            {
                if (Msg.MessageType == "21")
                {
                    if (!pOrderBookSynced)
                    {
                        DateTime nowTime = DateTime.Now;
                        if (orderBookSyncTime > DateTime.MinValue && (nowTime - orderBookSyncTime).TotalSeconds > 4)
                            pOrderBookSynced = true;
                        else
                            orderBookSyncTime = nowTime;
                    }

                    Deal deal = new Deal();

                    if (!int.TryParse(tags["ORNO"].Value, out deal.OrderNo)) return;
                    if (!int.TryParse(tags["DLNO"].Value, out deal.DealNo)) return;
                    deal.Time = Deal.GetDealTimeFromMsg(tags["TIME"].Value);
                    int.TryParse(tags["QTY"].Value, out deal.Quantity);
                    decimal.TryParse(tags["PRIC"].Value, out deal.Price);
                    deal.BrokerId = tags["EXBK"].Value;

                    orderUpdate = pOrderBook.AddUpdate(deal);

                    ProcessReceivedOrderEvent(orderUpdate);

                    DPOrder.Execute(DataProcessor<Order>.Operation.CodeEnum.DataArrival, null, "", orderUpdate);
                }
            }
        }
    }
}
