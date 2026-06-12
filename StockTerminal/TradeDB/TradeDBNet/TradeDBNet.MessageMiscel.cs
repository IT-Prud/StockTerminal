using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using Processors;
using Utils;

namespace TradeDB.Net
{
    public partial class TradeDBNet : Processor, ITradeDB
    {
        private void ProcessMessageIND(TradeMessage Msg)
        {
            pDPIndex.Execute(DataProcessor<Index>.Operation.CodeEnum.ParseData, Msg);
        }

        private void ParseMessageIndex(TradeMessage Msg)
        {
            string[] itemArray = null;
            string Code = null;
            decimal Last, Change;
            string Diff = null;
            Index indexUpdate = null;

            switch (Msg.MessageType)
            {
                case "01":
                case "05":
                    itemArray = Encoding.ASCII.GetString(Msg.MessageBody).Split(':');
                    if (itemArray.Length >= 3)
                    {
                        Code = itemArray[0].Trim();
                        Diff = itemArray[2].Trim();
                        if (Diff.Substring(0, 6) == "(Diff)" &&
                            Decimal.TryParse(itemArray[1], out Last) && Last > 0 &&
                            Decimal.TryParse(Diff.Substring(6), out Change) &&
                            Math.Abs(Change) <= (Last * 0.2M))
                        {
                            indexUpdate = new Index(Code);

                            indexUpdate.Last = Last;
                            indexUpdate.Change = Change;
                            indexUpdate.Previous = Last - Change;
                            indexUpdate.ChangePC = indexUpdate.Previous > 0 ? Change * 100 / indexUpdate.Previous : 0;

                            pDPIndex.Execute(DataProcessor<Index>.Operation.CodeEnum.DataArrival, null, indexUpdate.Code, indexUpdate);
                        }
                    }
                    break;

                case "06": // CCOG Daily Quota Balance
                    itemArray = Encoding.ASCII.GetString(Msg.MessageBody).Split(':');
                    Code = itemArray[0].Trim();
                    if (itemArray.Length >= 2 && Code == "ASHRQUOTA")
                    {
                        long quota;
                        if (long.TryParse(itemArray[1].Trim(), out quota))
                        {
                            indexUpdate = new Index(Code);
                            indexUpdate.Last = quota;

                            pDPIndex.Execute(DataProcessor<Index>.Operation.CodeEnum.DataArrival, null, indexUpdate.Code, indexUpdate);
                        }
                    }
                    break;
            }
        }

        private void ProcessMessageMkt(TradeMessage Msg)
        {
            pDPMarketTurnover.Execute(DataProcessor<MarketTurnover>.Operation.CodeEnum.ParseData, Msg);
        }

        private void ParseMessageMarketTurnover(TradeMessage Msg)
        {
            if (Msg != null && Msg.MessageType == "01")
            {
                string body = Encoding.ASCII.GetString(Msg.MessageBody);
                if (body != null)
                {
                    string[] parts = body.Split(' ');
                    decimal turnover;

                    if (parts != null && parts.Length >= 2)
                    {
                        if (decimal.TryParse(parts[1], out turnover) && turnover >= 0)
                        {
                            MarketTurnover mt = new MarketTurnover{ MarketCode = parts[0], Turnover = turnover };

                            pDPMarketTurnover.Execute(DataProcessor<MarketTurnover>.Operation.CodeEnum.DataArrival, null, mt.MarketCode, mt);
                        }
                    }
                }
            }
        }

        private void ProcessMessageSET(TradeMessage Msg)
        {
            pDPCentralUserSetting.Execute(DataProcessor<SettingKeyValue>.Operation.CodeEnum.ParseData, Msg);
        }

        private void ParseMessageCentralUserSetting(TradeMessage Msg)
        {
            Dictionary<string, TradeMessageTag> tags = null;
            string key = null;
            string value = null;

            if (Msg.MessageType == "02")    // Central User Settings
            {
                if ((tags = Msg.Tags) != null)
                {
                    key = tags["KEY"].Value;
                    if (key != null)
                    {
                        key = HttpUtility.UrlDecode(key);
                        value = tags["VAL"].Value;
                        value = value != null ? HttpUtility.UrlDecode(value) : "";

                        if (key == "FormCentralSettings")
                        {
                            lock (pSettingFormCentralMutex)
                            {
                                pSettingFormCentral.PersistString = value;
                            }
                        }

                        DPCentralUserSetting.Execute(DataProcessor<SettingKeyValue>.Operation.CodeEnum.DataArrival, null, key, new SettingKeyValue(key, value));
                    }
                }
            }
        }
    }
}
