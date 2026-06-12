using System;
using System.Collections.Generic;
using System.Text;
using Processors;

namespace TradeDB.Net
{
    public partial class TradeDBNet : Processor, ITradeDB
    {
        private void ProcessMessageSreMks(TradeMessage Msg)
        {
            if (Msg.MessageId == "SRE")
            {
                switch (Msg.MessageType)
                {
                    case "15": // HK Static
                    case "27": // SH & SZ Static with exchange code & market code
                    case "20": // hk dynamic
                    case "28": // dynamic with exchange & market code
                    case "41": // Response to search request
                        pDPStock.Execute(DataProcessor<Stock>.Operation.CodeEnum.ParseData, Msg);
                        break;

                    case "30": // Odd Lot Order Add
                        pDPOddLotOrder.Execute(DataProcessor<OddLotOrder>.Operation.CodeEnum.ParseData, Msg);
                        break;

                    //case "31": // Odd Lot Order Delete
                    //    exCode = Encoding.ASCII.GetString(msgBody, 0, 10).ToUpper().Trim();
                    //    exType = Stock.GetExchangeType(exCode);
                    //    iStockCode = (int)BitConverter.ToUInt32(msgBody, 20);
                    //    codeSignature = exCode + SignatureSep + iStockCode;
                    //    oddLotOrderNo = (ulong)BitConverter.ToUInt64(msgBody, 24);

                    //    if ((oddLotOrderLastUpdate == null || oddLotOrderLastUpdate.OrderNo != oddLotOrderNo) && !OddLotOrderBookUpdateDict.TryGetValue(oddLotOrderNo, out oddLotOrderLastUpdate))
                    //    {
                    //        oddLotOrderLastUpdate = pOddLotOrderBook.GetByOrderNo(oddLotOrderNo);
                    //        if (oddLotOrderLastUpdate == null)
                    //            oddLotOrderLastUpdate = new OddLotOrder();
                    //        OddLotOrderBookUpdateDict.Add(oddLotOrderNo, oddLotOrderLastUpdate);
                    //    }

                    //    if (oddLotOrderLastUpdate != null)
                    //    {
                    //        oddLotOrderLastUpdate.ExType = exType;
                    //        oddLotOrderLastUpdate.StockCode = iStockCode.ToString();
                    //        oddLotOrderLastUpdate.StockSignature = codeSignature;
                    //        oddLotOrderLastUpdate.OrderNo = oddLotOrderNo;
                    //        oddLotOrderLastUpdate.SubmitBrokerNo = (int)BitConverter.ToUInt16(msgBody, 40);
                    //        int side = (int)BitConverter.ToUInt16(msgBody, 42);
                    //        if (side == 0)
                    //            oddLotOrderLastUpdate.Side = 'B';
                    //        else if (side == 1)
                    //            oddLotOrderLastUpdate.Side = 'A';
                    //    }
                    //    break;
                }
            }
            else if (Msg.MessageId == "MKS")
            {
                pDPStock.Execute(DataProcessor<Stock>.Operation.CodeEnum.ParseData, Msg);
            }
        }

        private void ParseMessageStock(TradeMessage Msg)
        {
            Stock stockUpdate = null;

            string exCode = null;
            string marketCode, stockCode = null, value;
            int iStockCode;
            ExchangeTypeEnum exType = ExchangeTypeEnum.Unassigned;

            byte[] msgBody = Msg.MessageBody;

            byte[] nameCHT = new byte[16], nameCHS = new byte[16];
            int idx = 0, idxEnd = msgBody.Length, fieldId, fieldLength;

            Market market = null;

            if (Msg.MessageId == "SRE")
            {
                switch (Msg.MessageType)
                {
                    case "15": // HK Static
                    case "27": // SH & SZ Static with exchange code & market code
                        if (idxEnd > 6)
                        {
                            if (Msg.MessageType == "27") // exchange code, market code & stock code without space
                            {
                                exCode = Encoding.ASCII.GetString(msgBody, idx, 10).Trim();
                                idx += 10;
                                marketCode = Encoding.ASCII.GetString(msgBody, idx, 10).Trim();
                                idx += 10;
                                stockCode = Encoding.ASCII.GetString(msgBody, idx, 20).Trim();
                                idx += 20;

                                exType = Stock.GetExchangeType(exCode);

                                if (!MarketDict.TryGetValue(marketCode, out market))
                                {
                                    market = new Market(marketCode);
                                    MarketDict.Add(market.Code, market);
                                }
                            }
                            else
                            {
                                while (idx < idxEnd)
                                {
                                    if (msgBody[idx] != 32) { idx++; break; }
                                    idx++;
                                }
                                while (idx < idxEnd)
                                {
                                    if (msgBody[idx] == 32) { idx++; break; }
                                    idx++;
                                }

                                exType = ExchangeTypeEnum.HKG;
                                marketCode = null;
                                stockCode = Encoding.ASCII.GetString(msgBody, 0, idx);

                                exCode = Stock.GetExchangeCode(exType);
                            }

                            //stockCode = Stock.FormatStockCode(exType, stockCode); // remarked because quite some placed in stock terminal do not support HKG stock padded with zeros.
                            if (int.TryParse(stockCode, out iStockCode))
                                stockCode = iStockCode.ToString();

                            if (exType != ExchangeTypeEnum.Unassigned && stockCode != null && stockCode.Trim().Length > 0)
                            {
                                stockUpdate = new Stock(exType, stockCode);
                                stockUpdate.MarketBelong = market;
                                stockUpdate.MarketCode = marketCode;

                                while (idx < idxEnd)
                                {
                                    if (msgBody[idx] == 32)
                                    {
                                        idx++;
                                    }
                                    else
                                    {
                                        fieldId = msgBody[idx] * 100 + msgBody[idx + 1] * 10 + msgBody[idx + 2] - 5328;
                                        fieldLength = msgBody[idx + 4] * 100 + msgBody[idx + 5] * 10 + msgBody[idx + 6] - 5328;

                                        if (fieldLength > 0)
                                        {
                                            value = Encoding.ASCII.GetString(msgBody, idx + 8, fieldLength).Trim();

                                            switch (fieldId)
                                            {
                                                case 001: stockUpdate.NameEN = value; break;
                                                case 002: stockUpdate.High = decimal.Parse(value); break;
                                                case 003: stockUpdate.Low = decimal.Parse(value); break;
                                                case 004: stockUpdate.PrevClose = decimal.Parse(value); break;
                                                case 005: stockUpdate.Nominal = decimal.Parse(value); break;
                                                case 007: stockUpdate.Bid = decimal.Parse(value); break;
                                                case 008: stockUpdate.Ask = decimal.Parse(value); break;
                                                case 009: stockUpdate.Volume = UInt64.Parse(value); break;
                                                case 010: stockUpdate.Turnover = UInt64.Parse(value); break;
                                                case 011: stockUpdate.LotSize = int.Parse(value); break;
                                                case 013: for (int c = int.Parse(value), i = c > 0 ? c : 0; i < stockUpdate.BrokerBid.Length; i++) stockUpdate.BrokerBid[i] = ""; break;
                                                case 014: for (int c = int.Parse(value), i = c > 0 ? c : 0; i < stockUpdate.BrokerAsk.Length; i++) stockUpdate.BrokerAsk[i] = ""; break;
                                                case 015: stockUpdate.Open = decimal.Parse(value); break;
                                                case 017: stockUpdate.Currency = value; break;
                                                case 035: stockUpdate.BrokerBid[0] = value; break;
                                                case 036: stockUpdate.BrokerBid[1] = value; break;
                                                case 037: stockUpdate.BrokerBid[2] = value; break;
                                                case 038: stockUpdate.BrokerBid[3] = value; break;
                                                case 039: stockUpdate.BrokerBid[4] = value; break;
                                                case 040: stockUpdate.BrokerBid[5] = value; break;
                                                case 041: stockUpdate.BrokerBid[6] = value; break;
                                                case 042: stockUpdate.BrokerBid[7] = value; break;
                                                case 043: stockUpdate.BrokerBid[8] = value; break;
                                                case 044: stockUpdate.BrokerBid[9] = value; break;
                                                case 045: stockUpdate.BrokerBid[10] = value; break;
                                                case 046: stockUpdate.BrokerBid[11] = value; break;
                                                case 047: stockUpdate.BrokerBid[12] = value; break;
                                                case 048: stockUpdate.BrokerBid[13] = value; break;
                                                case 049: stockUpdate.BrokerBid[14] = value; break;
                                                case 050: stockUpdate.BrokerBid[15] = value; break;
                                                case 051: stockUpdate.BrokerBid[16] = value; break;
                                                case 052: stockUpdate.BrokerBid[17] = value; break;
                                                case 053: stockUpdate.BrokerBid[18] = value; break;
                                                case 054: stockUpdate.BrokerBid[19] = value; break;
                                                case 055: stockUpdate.BrokerBid[20] = value; break;
                                                case 056: stockUpdate.BrokerBid[21] = value; break;
                                                case 057: stockUpdate.BrokerBid[22] = value; break;
                                                case 058: stockUpdate.BrokerBid[23] = value; break;
                                                case 059: stockUpdate.BrokerBid[24] = value; break;
                                                case 060: stockUpdate.BrokerBid[25] = value; break;
                                                case 061: stockUpdate.BrokerBid[26] = value; break;
                                                case 062: stockUpdate.BrokerBid[27] = value; break;
                                                case 063: stockUpdate.BrokerBid[28] = value; break;
                                                case 064: stockUpdate.BrokerBid[29] = value; break;
                                                case 065: stockUpdate.BrokerBid[30] = value; break;
                                                case 066: stockUpdate.BrokerBid[31] = value; break;
                                                case 067: stockUpdate.BrokerBid[32] = value; break;
                                                case 068: stockUpdate.BrokerBid[33] = value; break;
                                                case 069: stockUpdate.BrokerBid[34] = value; break;
                                                case 070: stockUpdate.BrokerBid[35] = value; break;
                                                case 071: stockUpdate.BrokerBid[36] = value; break;
                                                case 072: stockUpdate.BrokerBid[37] = value; break;
                                                case 073: stockUpdate.BrokerBid[38] = value; break;
                                                case 074: stockUpdate.BrokerBid[39] = value; break;
                                                case 075: stockUpdate.BrokerAsk[0] = value; break;
                                                case 076: stockUpdate.BrokerAsk[1] = value; break;
                                                case 077: stockUpdate.BrokerAsk[2] = value; break;
                                                case 078: stockUpdate.BrokerAsk[3] = value; break;
                                                case 079: stockUpdate.BrokerAsk[4] = value; break;
                                                case 080: stockUpdate.BrokerAsk[5] = value; break;
                                                case 081: stockUpdate.BrokerAsk[6] = value; break;
                                                case 082: stockUpdate.BrokerAsk[7] = value; break;
                                                case 083: stockUpdate.BrokerAsk[8] = value; break;
                                                case 084: stockUpdate.BrokerAsk[9] = value; break;
                                                case 085: stockUpdate.BrokerAsk[10] = value; break;
                                                case 086: stockUpdate.BrokerAsk[11] = value; break;
                                                case 087: stockUpdate.BrokerAsk[12] = value; break;
                                                case 088: stockUpdate.BrokerAsk[13] = value; break;
                                                case 089: stockUpdate.BrokerAsk[14] = value; break;
                                                case 090: stockUpdate.BrokerAsk[15] = value; break;
                                                case 091: stockUpdate.BrokerAsk[16] = value; break;
                                                case 092: stockUpdate.BrokerAsk[17] = value; break;
                                                case 093: stockUpdate.BrokerAsk[18] = value; break;
                                                case 094: stockUpdate.BrokerAsk[19] = value; break;
                                                case 095: stockUpdate.BrokerAsk[20] = value; break;
                                                case 096: stockUpdate.BrokerAsk[21] = value; break;
                                                case 097: stockUpdate.BrokerAsk[22] = value; break;
                                                case 098: stockUpdate.BrokerAsk[23] = value; break;
                                                case 099: stockUpdate.BrokerAsk[24] = value; break;
                                                case 100: stockUpdate.BrokerAsk[25] = value; break;
                                                case 101: stockUpdate.BrokerAsk[26] = value; break;
                                                case 102: stockUpdate.BrokerAsk[27] = value; break;
                                                case 103: stockUpdate.BrokerAsk[28] = value; break;
                                                case 104: stockUpdate.BrokerAsk[29] = value; break;
                                                case 105: stockUpdate.BrokerAsk[30] = value; break;
                                                case 106: stockUpdate.BrokerAsk[31] = value; break;
                                                case 107: stockUpdate.BrokerAsk[32] = value; break;
                                                case 108: stockUpdate.BrokerAsk[33] = value; break;
                                                case 109: stockUpdate.BrokerAsk[34] = value; break;
                                                case 110: stockUpdate.BrokerAsk[35] = value; break;
                                                case 111: stockUpdate.BrokerAsk[36] = value; break;
                                                case 112: stockUpdate.BrokerAsk[37] = value; break;
                                                case 113: stockUpdate.BrokerAsk[38] = value; break;
                                                case 114: stockUpdate.BrokerAsk[39] = value; break;
                                                case 126: stockUpdate.InstrumentType = value; break;
                                                case 130: stockUpdate.BidVol[0] = UInt64.Parse(value); break;
                                                case 131: stockUpdate.BidVol[1] = UInt64.Parse(value); break;
                                                case 132: stockUpdate.BidVol[2] = UInt64.Parse(value); break;
                                                case 133: stockUpdate.BidVol[3] = UInt64.Parse(value); break;
                                                case 134: stockUpdate.BidVol[4] = UInt64.Parse(value); break;
                                                case 384: stockUpdate.BidVol[5] = UInt64.Parse(value); break;
                                                case 385: stockUpdate.BidVol[6] = UInt64.Parse(value); break;
                                                case 386: stockUpdate.BidVol[7] = UInt64.Parse(value); break;
                                                case 387: stockUpdate.BidVol[8] = UInt64.Parse(value); break;
                                                case 388: stockUpdate.BidVol[9] = UInt64.Parse(value); break;
                                                case 135: stockUpdate.BidCount[0] = value == "***" ? -1 : int.Parse(value); break;
                                                case 136: stockUpdate.BidCount[1] = value == "***" ? -1 : int.Parse(value); break;
                                                case 137: stockUpdate.BidCount[2] = value == "***" ? -1 : int.Parse(value); break;
                                                case 138: stockUpdate.BidCount[3] = value == "***" ? -1 : int.Parse(value); break;
                                                case 139: stockUpdate.BidCount[4] = value == "***" ? -1 : int.Parse(value); break;
                                                case 389: stockUpdate.BidCount[5] = value == "***" ? -1 : int.Parse(value); break;
                                                case 390: stockUpdate.BidCount[6] = value == "***" ? -1 : int.Parse(value); break;
                                                case 391: stockUpdate.BidCount[7] = value == "***" ? -1 : int.Parse(value); break;
                                                case 392: stockUpdate.BidCount[8] = value == "***" ? -1 : int.Parse(value); break;
                                                case 393: stockUpdate.BidCount[9] = value == "***" ? -1 : int.Parse(value); break;
                                                case 140: stockUpdate.AskVol[0] = UInt64.Parse(value); break;
                                                case 141: stockUpdate.AskVol[1] = UInt64.Parse(value); break;
                                                case 142: stockUpdate.AskVol[2] = UInt64.Parse(value); break;
                                                case 143: stockUpdate.AskVol[3] = UInt64.Parse(value); break;
                                                case 144: stockUpdate.AskVol[4] = UInt64.Parse(value); break;
                                                case 394: stockUpdate.AskVol[5] = UInt64.Parse(value); break;
                                                case 395: stockUpdate.AskVol[6] = UInt64.Parse(value); break;
                                                case 396: stockUpdate.AskVol[7] = UInt64.Parse(value); break;
                                                case 397: stockUpdate.AskVol[8] = UInt64.Parse(value); break;
                                                case 398: stockUpdate.AskVol[9] = UInt64.Parse(value); break;
                                                case 145: stockUpdate.AskCount[0] = value == "***" ? -1 : int.Parse(value); break;
                                                case 146: stockUpdate.AskCount[1] = value == "***" ? -1 : int.Parse(value); break;
                                                case 147: stockUpdate.AskCount[2] = value == "***" ? -1 : int.Parse(value); break;
                                                case 148: stockUpdate.AskCount[3] = value == "***" ? -1 : int.Parse(value); break;
                                                case 149: stockUpdate.AskCount[4] = value == "***" ? -1 : int.Parse(value); break;
                                                case 399: stockUpdate.AskCount[5] = value == "***" ? -1 : int.Parse(value); break;
                                                case 400: stockUpdate.AskCount[6] = value == "***" ? -1 : int.Parse(value); break;
                                                case 401: stockUpdate.AskCount[7] = value == "***" ? -1 : int.Parse(value); break;
                                                case 402: stockUpdate.AskCount[8] = value == "***" ? -1 : int.Parse(value); break;
                                                case 403: stockUpdate.AskCount[9] = value == "***" ? -1 : int.Parse(value); break;

                                                case 166:
                                                    if (!MarketDict.TryGetValue(value, out market))
                                                    {
                                                        market = new Market(value);
                                                        MarketDict.Add(market.Code, market);
                                                    }
                                                    stockUpdate.MarketBelong = market;
                                                    stockUpdate.MarketCode = value;
                                                    break;

                                                case 167: stockUpdate.NameENShort = value; break;
                                                case 169:
                                                    //if (Msg.MessageType == "15" || (Msg.MessageType == "27" && 
                                                    //    (exType == ExchangeTypeEnum.HKG || exType == ExchangeTypeEnum.PMHKG)))
                                                        stockUpdate.SpreadTableCode = int.Parse(value);
                                                    // Deprecated, done on omd-cc
                                                    //else if (Msg.MessageType == "27")
                                                    //    stockUpdate.SpreadTableCode = int.Parse(value) + 22220;
                                                    break;
                                                case 171: stockUpdate.SuspensionFlag = value; break;

                                                case 192: if (fieldLength >= 2) { nameCHT[0] = msgBody[idx + 8]; nameCHT[1] = msgBody[idx + 9]; } break;
                                                case 193: if (fieldLength >= 2) { nameCHT[2] = msgBody[idx + 8]; nameCHT[3] = msgBody[idx + 9]; } break;
                                                case 194: if (fieldLength >= 2) { nameCHT[4] = msgBody[idx + 8]; nameCHT[5] = msgBody[idx + 9]; } break;
                                                case 195: if (fieldLength >= 2) { nameCHT[6] = msgBody[idx + 8]; nameCHT[7] = msgBody[idx + 9]; } break;
                                                case 196: if (fieldLength >= 2) { nameCHT[8] = msgBody[idx + 8]; nameCHT[9] = msgBody[idx + 9]; } break;
                                                case 197: if (fieldLength >= 2) { nameCHT[10] = msgBody[idx + 8]; nameCHT[11] = msgBody[idx + 9]; } break;
                                                case 198: if (fieldLength >= 2) { nameCHT[12] = msgBody[idx + 8]; nameCHT[13] = msgBody[idx + 9]; } break;
                                                case 199: if (fieldLength >= 2) { nameCHT[14] = msgBody[idx + 8]; nameCHT[15] = msgBody[idx + 9]; } break;

                                                case 200: if (fieldLength >= 2) { nameCHS[0] = msgBody[idx + 8]; nameCHS[1] = msgBody[idx + 9]; } break;
                                                case 201: if (fieldLength >= 2) { nameCHS[2] = msgBody[idx + 8]; nameCHS[3] = msgBody[idx + 9]; } break;
                                                case 202: if (fieldLength >= 2) { nameCHS[4] = msgBody[idx + 8]; nameCHS[5] = msgBody[idx + 9]; } break;
                                                case 203: if (fieldLength >= 2) { nameCHS[6] = msgBody[idx + 8]; nameCHS[7] = msgBody[idx + 9]; } break;
                                                case 204: if (fieldLength >= 2) { nameCHS[8] = msgBody[idx + 8]; nameCHS[9] = msgBody[idx + 9]; } break;
                                                case 205: if (fieldLength >= 2) { nameCHS[10] = msgBody[idx + 8]; nameCHS[11] = msgBody[idx + 9]; } break;
                                                case 206: if (fieldLength >= 2) { nameCHS[12] = msgBody[idx + 8]; nameCHS[13] = msgBody[idx + 9]; } break;
                                                case 207: if (fieldLength >= 2) { nameCHS[14] = msgBody[idx + 8]; nameCHS[15] = msgBody[idx + 9]; } break;

                                                case 274: if (stockUpdate.Ticker[0] == null) stockUpdate.Ticker[0] = new StockTicker(); stockUpdate.Ticker[0].Time = value; break;
                                                case 275: if (stockUpdate.Ticker[0] == null) stockUpdate.Ticker[0] = new StockTicker(); stockUpdate.Ticker[0].Quantity = int.Parse(value); break;
                                                case 276: if (stockUpdate.Ticker[0] == null) stockUpdate.Ticker[0] = new StockTicker(); stockUpdate.Ticker[0].Price = decimal.Parse(value); break;
                                                case 278: if (stockUpdate.Ticker[0] == null) stockUpdate.Ticker[0] = new StockTicker(); stockUpdate.Ticker[0].Remarks = value; break;
                                                case 280: if (stockUpdate.Ticker[1] == null) stockUpdate.Ticker[1] = new StockTicker(); stockUpdate.Ticker[1].Time = value; break;
                                                case 281: if (stockUpdate.Ticker[1] == null) stockUpdate.Ticker[1] = new StockTicker(); stockUpdate.Ticker[1].Quantity = int.Parse(value); break;
                                                case 282: if (stockUpdate.Ticker[1] == null) stockUpdate.Ticker[1] = new StockTicker(); stockUpdate.Ticker[1].Price = decimal.Parse(value); break;
                                                case 284: if (stockUpdate.Ticker[1] == null) stockUpdate.Ticker[1] = new StockTicker(); stockUpdate.Ticker[1].Remarks = value; break;
                                                case 286: if (stockUpdate.Ticker[2] == null) stockUpdate.Ticker[2] = new StockTicker(); stockUpdate.Ticker[2].Time = value; break;
                                                case 287: if (stockUpdate.Ticker[2] == null) stockUpdate.Ticker[2] = new StockTicker(); stockUpdate.Ticker[2].Quantity = int.Parse(value); break;
                                                case 288: if (stockUpdate.Ticker[2] == null) stockUpdate.Ticker[2] = new StockTicker(); stockUpdate.Ticker[2].Price = decimal.Parse(value); break;
                                                case 290: if (stockUpdate.Ticker[2] == null) stockUpdate.Ticker[2] = new StockTicker(); stockUpdate.Ticker[2].Remarks = value; break;
                                                case 292: if (stockUpdate.Ticker[3] == null) stockUpdate.Ticker[3] = new StockTicker(); stockUpdate.Ticker[3].Time = value; break;
                                                case 293: if (stockUpdate.Ticker[3] == null) stockUpdate.Ticker[3] = new StockTicker(); stockUpdate.Ticker[3].Quantity = int.Parse(value); break;
                                                case 294: if (stockUpdate.Ticker[3] == null) stockUpdate.Ticker[3] = new StockTicker(); stockUpdate.Ticker[3].Price = decimal.Parse(value); break;
                                                case 296: if (stockUpdate.Ticker[3] == null) stockUpdate.Ticker[3] = new StockTicker(); stockUpdate.Ticker[3].Remarks = value; break;

                                                case 298: if (stockUpdate.Ticker[4] == null) stockUpdate.Ticker[4] = new StockTicker(); stockUpdate.Ticker[4].Time = value; break;
                                                case 299: if (stockUpdate.Ticker[4] == null) stockUpdate.Ticker[4] = new StockTicker(); stockUpdate.Ticker[4].Quantity = int.Parse(value); break;
                                                case 300: if (stockUpdate.Ticker[4] == null) stockUpdate.Ticker[4] = new StockTicker(); stockUpdate.Ticker[4].Price = decimal.Parse(value); break;
                                                case 302: if (stockUpdate.Ticker[4] == null) stockUpdate.Ticker[4] = new StockTicker(); stockUpdate.Ticker[4].Remarks = value; break;
                                                case 304: if (stockUpdate.Ticker[5] == null) stockUpdate.Ticker[5] = new StockTicker(); stockUpdate.Ticker[5].Time = value; break;
                                                case 305: if (stockUpdate.Ticker[5] == null) stockUpdate.Ticker[5] = new StockTicker(); stockUpdate.Ticker[5].Quantity = int.Parse(value); break;
                                                case 306: if (stockUpdate.Ticker[5] == null) stockUpdate.Ticker[5] = new StockTicker(); stockUpdate.Ticker[5].Price = decimal.Parse(value); break;
                                                case 308: if (stockUpdate.Ticker[5] == null) stockUpdate.Ticker[5] = new StockTicker(); stockUpdate.Ticker[5].Remarks = value; break;
                                                case 310: if (stockUpdate.Ticker[6] == null) stockUpdate.Ticker[6] = new StockTicker(); stockUpdate.Ticker[6].Time = value; break;
                                                case 311: if (stockUpdate.Ticker[6] == null) stockUpdate.Ticker[6] = new StockTicker(); stockUpdate.Ticker[6].Quantity = int.Parse(value); break;
                                                case 312: if (stockUpdate.Ticker[6] == null) stockUpdate.Ticker[6] = new StockTicker(); stockUpdate.Ticker[6].Price = decimal.Parse(value); break;
                                                case 314: if (stockUpdate.Ticker[6] == null) stockUpdate.Ticker[6] = new StockTicker(); stockUpdate.Ticker[6].Remarks = value; break;
                                                case 316: if (stockUpdate.Ticker[7] == null) stockUpdate.Ticker[7] = new StockTicker(); stockUpdate.Ticker[7].Time = value; break;
                                                case 317: if (stockUpdate.Ticker[7] == null) stockUpdate.Ticker[7] = new StockTicker(); stockUpdate.Ticker[7].Quantity = int.Parse(value); break;
                                                case 318: if (stockUpdate.Ticker[7] == null) stockUpdate.Ticker[7] = new StockTicker(); stockUpdate.Ticker[7].Price = decimal.Parse(value); break;
                                                case 320: if (stockUpdate.Ticker[7] == null) stockUpdate.Ticker[7] = new StockTicker(); stockUpdate.Ticker[7].Remarks = value; break;

                                                case 322: if (stockUpdate.Ticker[8] == null) stockUpdate.Ticker[8] = new StockTicker(); stockUpdate.Ticker[8].Time = value; break;
                                                case 323: if (stockUpdate.Ticker[8] == null) stockUpdate.Ticker[8] = new StockTicker(); stockUpdate.Ticker[8].Quantity = int.Parse(value); break;
                                                case 324: if (stockUpdate.Ticker[8] == null) stockUpdate.Ticker[8] = new StockTicker(); stockUpdate.Ticker[8].Price = decimal.Parse(value); break;
                                                case 326: if (stockUpdate.Ticker[8] == null) stockUpdate.Ticker[8] = new StockTicker(); stockUpdate.Ticker[8].Remarks = value; break;
                                                case 328: if (stockUpdate.Ticker[9] == null) stockUpdate.Ticker[9] = new StockTicker(); stockUpdate.Ticker[9].Time = value; break;
                                                case 329: if (stockUpdate.Ticker[9] == null) stockUpdate.Ticker[9] = new StockTicker(); stockUpdate.Ticker[9].Quantity = int.Parse(value); break;
                                                case 330: if (stockUpdate.Ticker[9] == null) stockUpdate.Ticker[9] = new StockTicker(); stockUpdate.Ticker[9].Price = decimal.Parse(value); break;
                                                case 332: if (stockUpdate.Ticker[9] == null) stockUpdate.Ticker[9] = new StockTicker(); stockUpdate.Ticker[9].Remarks = value; break;
                                                case 334: if (stockUpdate.Ticker[10] == null) stockUpdate.Ticker[10] = new StockTicker(); stockUpdate.Ticker[10].Time = value; break;
                                                case 335: if (stockUpdate.Ticker[10] == null) stockUpdate.Ticker[10] = new StockTicker(); stockUpdate.Ticker[10].Quantity = int.Parse(value); break;
                                                case 336: if (stockUpdate.Ticker[10] == null) stockUpdate.Ticker[10] = new StockTicker(); stockUpdate.Ticker[10].Price = decimal.Parse(value); break;
                                                case 338: if (stockUpdate.Ticker[10] == null) stockUpdate.Ticker[10] = new StockTicker(); stockUpdate.Ticker[10].Remarks = value; break;
                                                case 340: if (stockUpdate.Ticker[11] == null) stockUpdate.Ticker[11] = new StockTicker(); stockUpdate.Ticker[11].Time = value; break;
                                                case 341: if (stockUpdate.Ticker[11] == null) stockUpdate.Ticker[11] = new StockTicker(); stockUpdate.Ticker[11].Quantity = int.Parse(value); break;
                                                case 342: if (stockUpdate.Ticker[11] == null) stockUpdate.Ticker[11] = new StockTicker(); stockUpdate.Ticker[11].Price = decimal.Parse(value); break;
                                                case 344: if (stockUpdate.Ticker[11] == null) stockUpdate.Ticker[11] = new StockTicker(); stockUpdate.Ticker[11].Remarks = value; break;

                                                case 346: if (stockUpdate.Ticker[12] == null) stockUpdate.Ticker[12] = new StockTicker(); stockUpdate.Ticker[12].Time = value; break;
                                                case 347: if (stockUpdate.Ticker[12] == null) stockUpdate.Ticker[12] = new StockTicker(); stockUpdate.Ticker[12].Quantity = int.Parse(value); break;
                                                case 348: if (stockUpdate.Ticker[12] == null) stockUpdate.Ticker[12] = new StockTicker(); stockUpdate.Ticker[12].Price = decimal.Parse(value); break;
                                                case 350: if (stockUpdate.Ticker[12] == null) stockUpdate.Ticker[12] = new StockTicker(); stockUpdate.Ticker[12].Remarks = value; break;
                                                case 352: if (stockUpdate.Ticker[13] == null) stockUpdate.Ticker[13] = new StockTicker(); stockUpdate.Ticker[13].Time = value; break;
                                                case 353: if (stockUpdate.Ticker[13] == null) stockUpdate.Ticker[13] = new StockTicker(); stockUpdate.Ticker[13].Quantity = int.Parse(value); break;
                                                case 354: if (stockUpdate.Ticker[13] == null) stockUpdate.Ticker[13] = new StockTicker(); stockUpdate.Ticker[13].Price = decimal.Parse(value); break;
                                                case 356: if (stockUpdate.Ticker[13] == null) stockUpdate.Ticker[13] = new StockTicker(); stockUpdate.Ticker[13].Remarks = value; break;
                                                case 358: if (stockUpdate.Ticker[14] == null) stockUpdate.Ticker[14] = new StockTicker(); stockUpdate.Ticker[14].Time = value; break;
                                                case 359: if (stockUpdate.Ticker[14] == null) stockUpdate.Ticker[14] = new StockTicker(); stockUpdate.Ticker[14].Quantity = int.Parse(value); break;
                                                case 360: if (stockUpdate.Ticker[14] == null) stockUpdate.Ticker[14] = new StockTicker(); stockUpdate.Ticker[14].Price = decimal.Parse(value); break;
                                                case 362: if (stockUpdate.Ticker[14] == null) stockUpdate.Ticker[14] = new StockTicker(); stockUpdate.Ticker[14].Remarks = value; break;

                                                //case 365: stockUpdate.TradingStatus = int.Parse(value); break;
                                                //case 382: stockUpdate.OrderPlaceEnable = int.Parse(value); break;
                                                //case 383: stockUpdate.OrderModifyEnable = int.Parse(value); break;
                                                case 406:
                                                    string[] itemArray = value.Split('=');
                                                    if (itemArray.Length >= 2 && itemArray[0] != null && itemArray[0].Trim().ToLower() == "phase" && itemArray[1] != null)
                                                    {
                                                        string phaseCode = itemArray[1].Trim().Substring(0, 1).ToUpper();
                                                        stockUpdate.FusingFlag = (phaseCode == "M" || phaseCode == "N") ? "Y" : "N";
                                                    }
                                                    else
                                                        stockUpdate.FusingFlag = "N";
                                                    break;
                                                case 407:
                                                    break;
                                                case 410: // Simplified chinese name (base64)
                                                    stockUpdate.NameCHS = Encoding.Unicode.GetString(Convert.FromBase64String(value)).Trim('\0', ' ', '　');
                                                    break;
                                                case 411: // product type
                                                    int iProductType;
                                                    if (int.TryParse(value, out iProductType))
                                                        stockUpdate.ProductType = iProductType;
                                                    break;
                                                case 422: // Traditional chinese name (base64)
                                                    stockUpdate.NameCHT = Encoding.Unicode.GetString(Convert.FromBase64String(value)).Trim('\0', ' ', '　');
                                                    break;
                                            }
                                        }
                                        else if (fieldId == 406)
                                        {
                                            stockUpdate.FusingFlag = "N";
                                        }
                                        idx += (9 + fieldLength);
                                    }
                                }

                                if (stockUpdate.NameCHT == null) // i.e. if new field with base64 format not exist, then use old big5 field
                                    stockUpdate.NameCHT = Encoding.GetEncoding(950).GetString(nameCHT).Trim().Replace("\0", "");

                                if (stockUpdate.NameCHS == null)
                                    stockUpdate.NameCHS = Encoding.GetEncoding(936).GetString(nameCHT).Trim().Replace("\0", "");

                                if (pStockBook.AddUpdateStatic(stockUpdate) != null)
                                    DPStock.Execute(DataProcessor<Stock>.Operation.CodeEnum.DataArrival, null, stockUpdate.StockSignature, stockUpdate);

                                /*  The arrival of stock static should not be regarded as stock arrival (Except PM-HKG stock which only has static and no dynamic) as latest dynamic might not have arrived yet (mostly not).
                                 *  Assume stock dynamic will always arrive and always arrive after static, it should be safe not to trigger further DataArrival processing.

                                if (stockUpdate.DynamicVersion != 0)
                                    DPStock.Execute(DataProcessor<Stock>.Operation.CodeEnum.DataArrival, null, stockUpdate.StockSignature, stockUpdate);
                                 */

                                //if (stockUpdate.ExchangeType == ExchangeTypeEnum.PMHKG)
                                //    DPStock.Execute(DataProcessor<Stock>.Operation.CodeEnum.DataArrival, null, stockUpdate.StockSignature, stockUpdate);
                            }
                        }
                        break;

                    case "20": // hk dynamic
                    case "28": // dynamic with exchange & market code 
                        int readIdx = 8;
                        exType = ExchangeTypeEnum.Unassigned;

                        if (Msg.MessageType == "28")
                        {
                            exCode = Encoding.ASCII.GetString(msgBody, 0, 10).Trim().ToUpper();
                            readIdx += 20;
                            exType = Stock.GetExchangeType(exCode);
                        }
                        else
                        {
                            exType = ExchangeTypeEnum.HKG;
                            exCode = Stock.GetExchangeCode(exType);
                        }

                        if (exType != ExchangeTypeEnum.Unassigned)
                        {
                            iStockCode = (int)BitConverter.ToUInt32(msgBody, readIdx);
                            stockCode = iStockCode.ToString();
                            stockUpdate = new Stock(exType, stockCode);

                            readIdx += 4;
                            stockUpdate.Nominal = BitConverter.ToInt32(msgBody, readIdx) * 0.001m;
                            readIdx += 4;
                            readIdx += 1;
                            //if (msgBody[readIdx] > 0 || msgBody[readIdx + 1] > 0 || msgBody[readIdx + 2] > 0 || msgBody[readIdx + 3] > 0)
                            //    stockUpdate.Nominal = BitConverter.ToInt32(msgBody, readIdx) * 0.001m;
                            readIdx += 4;
                            readIdx += 8;
                            stockUpdate.Bid = BitConverter.ToInt32(msgBody, readIdx) * 0.001m;
                            readIdx += 4;

                            for (int j = 0; j < 10; j++)
                            {
                                stockUpdate.BidCount[j] = BitConverter.ToInt32(msgBody, readIdx);
                                readIdx += 4;
                                stockUpdate.BidVol[j] = BitConverter.ToUInt64(msgBody, readIdx);
                                readIdx += 8;
                            }
                            stockUpdate.Ask = BitConverter.ToInt32(msgBody, readIdx) * 0.001m;
                            readIdx += 4;

                            for (int j = 0; j < 10; j++)
                            {
                                stockUpdate.AskCount[j] = BitConverter.ToInt32(msgBody, readIdx);
                                readIdx += 4;
                                stockUpdate.AskVol[j] = BitConverter.ToUInt64(msgBody, readIdx);
                                readIdx += 8;
                            }
                            stockUpdate.Volume = BitConverter.ToUInt64(msgBody, readIdx);
                            readIdx += 8;
                            stockUpdate.Turnover = BitConverter.ToUInt64(msgBody, readIdx);
                            readIdx += 8;
                            stockUpdate.High = BitConverter.ToInt32(msgBody, readIdx) * 0.001m;
                            readIdx += 4;
                            stockUpdate.Low = BitConverter.ToInt32(msgBody, readIdx) * 0.001m;
                            readIdx += 4;
                            short tickerCount = BitConverter.ToInt16(msgBody, readIdx);
                            readIdx += 2;
                            //char MoreFlagBid = (Encoding.ASCII.GetChars(msgBody, readIdx, 1))[0];
                            readIdx += 1;
                            short NumOfBidItem = BitConverter.ToInt16(msgBody, readIdx);
                            readIdx += 2;
                            //char MoreFlagAsk = (Encoding.ASCII.GetChars(msgBody, readIdx, 1))[0];
                            readIdx += 1;
                            short NumOfAskItem = BitConverter.ToInt16(msgBody, readIdx);
                            readIdx += 2;
                            //char TradeSession = (Encoding.ASCII.GetChars(msgBody, readIdx, 1))[0];
                            readIdx += 1;
                            char[] TradingStatus = new char[2];
                            char[] TradingStatusDesc = new char[50];
                            TradingStatus[0] = (Encoding.ASCII.GetChars(msgBody, readIdx, 1))[0];
                            readIdx += 1;
                            TradingStatus[1] = (Encoding.ASCII.GetChars(msgBody, readIdx, 1))[0];
                            readIdx += 1;
                            string strTradingStatus = TradingStatus[0].ToString() + TradingStatus[1].ToString();
                            readIdx += 50;
                            //uint TradingStatusStartDate = BitConverter.ToUInt32(msgBody, readIdx);
                            readIdx += 4;
                            //uint TradingStatusStartTime = BitConverter.ToUInt32(msgBody, readIdx);
                            readIdx += 4;
                            //uint TradingStatusEndDate = BitConverter.ToUInt32(msgBody, readIdx);
                            readIdx += 4;
                            //uint TradingStatusEndTime = BitConverter.ToUInt32(msgBody, readIdx);
                            readIdx += 4;
                            stockUpdate.InstrumentType = Encoding.ASCII.GetString(msgBody, readIdx, 4);
                            readIdx += 48;

                            // Trade Ticker Information (Num. of occurance specified in TickerCount, Max occur=15) 
                            int tickerHour, tickerMinute;
                            for (int j = 0; j < 15; j++)
                            {
                                if (j < tickerCount)
                                {
                                    tickerHour = Math.DivRem(BitConverter.ToUInt16(msgBody, readIdx), 60, out tickerMinute);
                                    stockUpdate.Ticker[j].Time = tickerHour.ToString("00") + ":" + tickerMinute.ToString("00");
                                    readIdx += 2;
                                    stockUpdate.Ticker[j].Quantity = BitConverter.ToInt32(msgBody, readIdx);
                                    readIdx += 4;
                                    stockUpdate.Ticker[j].Price = BitConverter.ToInt32(msgBody, readIdx) * 0.001m;
                                    readIdx += 4;
                                    //stockUpdate.Ticker[j].PublicTradeType = (Encoding.ASCII.GetChars(msgBody, readIdx, 1))[0];
                                    readIdx += 1;
                                    stockUpdate.Ticker[j].Remarks = Encoding.ASCII.GetString(msgBody, readIdx, 1); // movement
                                    readIdx += 1;
                                    //stockUpdate.Ticker[j].RejectFlag = (Encoding.ASCII.GetChars(msgBody, readIdx, 1))[0];
                                    readIdx += 1;
                                }
                                else
                                {
                                    stockUpdate.Ticker[j].Time = "";
                                    stockUpdate.Ticker[j].Quantity = 0;
                                    stockUpdate.Ticker[j].Price = 0;
                                    stockUpdate.Ticker[j].Remarks = "";
                                }
                            }

                            // Broker Queue Information (Num. of occurance specified in NumOfBidItem, Max occur=40) 
                            short itemValue;
                            char itemType;

                            for (int j = 0, iSpread = 1; j < 40; j++)
                            {
                                if (j < NumOfBidItem)
                                {
                                    itemValue = BitConverter.ToInt16(msgBody, readIdx);
                                    readIdx += 2;
                                    itemType = Encoding.ASCII.GetString(msgBody, readIdx, 1).ToUpper()[0];
                                    readIdx += 1;
                                    stockUpdate.BrokerBid[j] = itemType == 'S' ? "-" + iSpread++ + "s" : (itemValue > 0 ? itemValue.ToString("0000") : "");
                                }
                                else
                                    stockUpdate.BrokerBid[j] = null;
                            }

                            for (int j = 0, iSpread = 1; j < 40; j++)
                            {
                                if (j < NumOfAskItem)
                                {
                                    itemValue = BitConverter.ToInt16(msgBody, readIdx);
                                    readIdx += 2;
                                    itemType = Encoding.ASCII.GetString(msgBody, readIdx, 1).ToUpper()[0];
                                    readIdx += 1;
                                    stockUpdate.BrokerAsk[j] = itemType == 'S' ? "+" + iSpread++ + "s" : (itemValue > 0 ? itemValue.ToString("0000") : "");
                                }
                                else
                                    stockUpdate.BrokerAsk[j] = null;
                            }

                            //if (msgBody.Length >= 910) // CAS & VCM    <=== wrong if ticker or broker queue has fewer items
                            if ((msgBody.Length - readIdx) >= 49)
                            {
                                long secondsSince1970 = (long)(BitConverter.ToUInt64(msgBody, readIdx) * 0.000000001);
                                readIdx += 8;
                                DateTime date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                                int minutesSinceToday = (int)date.AddSeconds(secondsSince1970).TimeOfDay.TotalMinutes + 480; // add 8 hours hk time
                                int tickerSecond;
                                Math.DivRem((int)date.AddSeconds(secondsSince1970).TimeOfDay.TotalSeconds, 60, out tickerSecond);
                                tickerHour = Math.DivRem(minutesSinceToday, 60, out tickerMinute);
                                stockUpdate.VCMCoolOffStartTime = tickerHour.ToString("00") + ":" + tickerMinute.ToString("00") + ":" + tickerSecond.ToString("00");

                                secondsSince1970 = (long)(BitConverter.ToUInt64(msgBody, readIdx) * 0.000000001);
                                readIdx += 8;
                                date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                                minutesSinceToday = (int)date.AddSeconds(secondsSince1970).TimeOfDay.TotalMinutes + 480; // add 8 hours hk time
                                tickerHour = Math.DivRem(minutesSinceToday, 60, out tickerMinute);
                                Math.DivRem((int)date.AddSeconds(secondsSince1970).TimeOfDay.TotalSeconds, 60, out tickerSecond);
                                stockUpdate.VCMCoolOffEndTime = tickerHour.ToString("00") + ":" + tickerMinute.ToString("00") + ":" + tickerSecond.ToString("00");
                                DateTime nowTime = DateTime.Now;
                                stockUpdate.dtVCMCoolOffEndTime = new DateTime(nowTime.Year, nowTime.Month, nowTime.Day, tickerHour, tickerMinute, tickerSecond, DateTimeKind.Local);

                                stockUpdate.VCMReferencePrice = BitConverter.ToInt32(msgBody, readIdx) * 0.001m; readIdx += 4;
                                stockUpdate.VCMLowerPrice = BitConverter.ToInt32(msgBody, readIdx) * 0.001m; readIdx += 4;
                                stockUpdate.VCMUpperPrice = BitConverter.ToInt32(msgBody, readIdx) * 0.001m; readIdx += 4;
                                stockUpdate.CASReferencePrice = BitConverter.ToInt32(msgBody, readIdx) * 0.001m; readIdx += 4;
                                stockUpdate.CASLowerPrice = BitConverter.ToInt32(msgBody, readIdx) * 0.001m; readIdx += 4;
                                stockUpdate.CASUpperPrice = BitConverter.ToInt32(msgBody, readIdx) * 0.001m; readIdx += 4;
                                stockUpdate.OrderImbalanceDirection = Encoding.ASCII.GetString(msgBody, readIdx, 1); readIdx += 1;
                                stockUpdate.OrderImbalanceQuantity = BitConverter.ToUInt64(msgBody, readIdx); readIdx += 8;
                            }

                            /*
                            string QuoteStr = "";
                            QuoteStr += stockUpdate.StockSignature.PadRight(10, ' ') + "nominal: " + stockUpdate.Nominal.ToString().PadRight(8, ' ') +
                                " bid: " + stockUpdate.Bid.ToString().PadRight(8, ' ') + " ask: " + stockUpdate.Ask.ToString().PadRight(8, ' ') +
                                " Vol: " + stockUpdate.Volume.ToString().PadRight(12, ' ') + " BidVol: [";
                            for (int i = 0; i < stockUpdate.BidVol.Length; i++)
                                QuoteStr += stockUpdate.BidVol[i].ToString().PadRight(8, ' ') + " ";
                            QuoteStr += "] AskVol: [";
                            for (int i = 0; i < stockUpdate.AskVol.Length; i++)
                                QuoteStr += stockUpdate.AskVol[i].ToString().PadRight(8, ' ') + " ";
                            QuoteStr += "] BidCount: [";
                            for (int i = 0; i < stockUpdate.BidCount.Length; i++)
                                QuoteStr += stockUpdate.BidCount[i].ToString().PadRight(4, ' ') + " ";
                            QuoteStr += "] AskCount: [";
                            for (int i = 0; i < stockUpdate.AskCount.Length; i++)
                                QuoteStr += stockUpdate.AskCount[i].ToString().PadRight(4, ' ') + " ";
                            QuoteStr += "] ";
                            DynamicInLog.Append("In", QuoteStr);
                             */

                            pStockBook.AddUpdateDynamic(stockUpdate);

                            if (stockUpdate.StaticVersion != 0)
                                DPStock.Execute(DataProcessor<Stock>.Operation.CodeEnum.DataArrival, null, stockUpdate.StockSignature, stockUpdate);
                        }
                        break;

                    case "41":
                        if (msgBody != null && msgBody.Length > 40)
                        {
                            exCode = Encoding.ASCII.GetString(msgBody, idx, 10).Trim();
                            idx += 10;
                            marketCode = Encoding.ASCII.GetString(msgBody, idx, 10).Trim();
                            idx += 10;
                            stockCode = Encoding.ASCII.GetString(msgBody, idx, 20).Trim();
                            idx += 20;

                            exType = Stock.GetExchangeType(exCode);

                            stockUpdate = new Stock(exType, stockCode);
                            stockUpdate.StockSignatureAlternative = Encoding.ASCII.GetString(msgBody, 40, msgBody.Length - 40);
                            DPStock.Execute(DataProcessor<Stock>.Operation.CodeEnum.DataArrival, null, stockUpdate.StockSignature, stockUpdate);
                        }
                        break;
                }
            }
            else if (Msg.MessageId == "MKS")
            {
                if (Msg.MessageType == "01")
                {
                    string code = Msg.Tags["NAME"].Value;
                    int status;
                    bool statusChanged = false;

                    if (!MarketDict.TryGetValue(code, out market))
                    {
                        market = new Market(code);
                        MarketDict.Add(code, market);
                        statusChanged = true;
                    }
                    if (int.TryParse(Msg.Tags["STAT"].Value, out status))
                    {
                        statusChanged = market.SetStatus(status, ServerTime);
                    }

                    if (statusChanged && DPStock != null)
                    {
                        List<Stock> stockList = GetStockByMarketCode(code, ExchangeTypeEnum.HKG);

                        foreach (Stock stock in stockList)
                            DPStock.Execute(DataProcessor<Stock>.Operation.CodeEnum.DataArrival, null, stock.StockSignature, stock);
                    }
                }
            }
        }

        private void ParseMessageOddLotOrder(TradeMessage Msg)
        {
            string exCode;
            ExchangeTypeEnum exType;
            int iStockCode;
            ulong oddLotOrderNo;
            OddLotOrder oddLotOrderUpdate;

            byte[] msgBody = Msg.MessageBody;

            exCode = Encoding.ASCII.GetString(msgBody, 0, 10).ToUpper().Trim();
            exType = Stock.GetExchangeType(exCode);
            iStockCode = (int)BitConverter.ToUInt32(msgBody, 20);
            oddLotOrderNo = (ulong)BitConverter.ToUInt64(msgBody, 24);

            oddLotOrderUpdate = new OddLotOrder();
            oddLotOrderUpdate.ExType = exType;
            oddLotOrderUpdate.StockCode = iStockCode.ToString();
            oddLotOrderUpdate.StockSignature = Stock.GetSignature(exType, oddLotOrderUpdate.StockCode);
            oddLotOrderUpdate.OrderNo = oddLotOrderNo;
            oddLotOrderUpdate.Price = BitConverter.ToInt32(msgBody, 32) * 0.001m;
            oddLotOrderUpdate.Quantity = (int)BitConverter.ToUInt32(msgBody, 36);
            oddLotOrderUpdate.SubmitBrokerNo = (int)BitConverter.ToUInt16(msgBody, 40);
            int side = (int)BitConverter.ToUInt16(msgBody, 42);
            if (side == 0)
                oddLotOrderUpdate.Side = 'B';
            else if (side == 1)
                oddLotOrderUpdate.Side = 'A';
            oddLotOrderUpdate.Deleted = (int)BitConverter.ToUInt16(msgBody, 44) == 1; // 1 = deleted

            pOddLotOrderBook.AddUpdate(oddLotOrderUpdate);

            DPOddLotOrder.Execute(DataProcessor<OddLotOrder>.Operation.CodeEnum.DataArrival, null, oddLotOrderUpdate.StockSignature, oddLotOrderUpdate);
        }
    }
}
