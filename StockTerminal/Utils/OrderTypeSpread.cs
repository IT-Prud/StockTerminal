using System;
using System.Collections.Generic;
using System.Text;
using TradeDB;

namespace StockTerminal.Utils
{
    class OrderTypeSpread
    {
        private static int E_QueueSpread = 24;
        private static int E_OppositeSpread = 10;
        private static int L_QueueSpread = 24;
        private static int L_OppositeSpread = 1;
        private static int S_QueueSpread = 24;  //special        
        //private static int A_QueueSpread = 24;
        private static int I_QueueSpread = 40;
        private static int I_OppositeSpread = 40;
        private static int S_OppositeSpread = 10; //special
        private static decimal L_BidQueuePercent_ASHR = 0.9M;  //ASHR Bid 10%
        private static decimal L_AskQueuePercent_ASHR = 1.1M;  //ASHR Ask 10%
        //private static decimal PriceSpread_ASHR = 0.01M;  //ASHR fixed spread = 0.01
        //private static int A_OppositeSpread = 20;        


        public static decimal CheckSpread_ASHR(decimal OrderPrice, char OrderType, char BuySell, int Index, Stock Stock1, ITradeDB TradeDB1, out bool CanBuy, out bool CanSell)
        {
            CanBuy = false;
            CanSell = false;

            if (OrderType.ToString().Trim() == "" || Stock1 == null || TradeDB1 == null)
                return -1;

            Market.OrderActionEnable oae = Stock1.MarketBelong != null ? Stock1.MarketBelong.OrderAction : new Market.OrderActionEnable(15, 15);
            Market m = new Market(Stock1.Code);
            int s = m.Status;


            decimal NewPrice = 0;
            try
            {                
                //if ((OrderPrice * 100) % (PriceSpread_ASHR * 100) > 0)  // wrong spread
                //{
                //    return -1;
                //}                

                //NewPrice = OrderPrice + (PriceSpread_ASHR * Index);
                NewPrice = GetSpreadByIndex(Stock1.SpreadTableCode, OrderPrice, Index, TradeDB1.SpreadTables);
                switch (OrderType)
                {
                    //I=3,15
                    //E=1,12,13
                    case 'X': //Auto Order
                        //////if (oae.Place == 3 || oae.Place == 15)
                        //////{
                        //////    //Auction Limit
                        //////    Check_I_Spread(NewPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                        //////    if (CanBuy == true || CanSell == true)
                        //////        return NewPrice;
                        //////    else
                        //////        //check existing price correct or not
                        //////        Check_I_Spread(OrderPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                        //////    return -1;
                        //////}
                        //////else if (oae.Place == 1 || oae.Place == 12 || oae.Place == 13)
                        //////{
                        //////    //Enhanced
                        //////    Check_E_Spread(NewPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                        //////    if (CanBuy == true || CanSell == true)
                        //////        return NewPrice;
                        //////    else
                        //////        //check existing price correct or not
                        //////        Check_E_Spread(OrderPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                        //////    return -1;
                        //////}
                        //////else
                        //////{
                            //Enhanced
                            Check_E_Spread_ASHR(NewPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                            if (CanBuy == true || CanSell == true)
                                return NewPrice;
                            else
                                //check existing price correct or not
                                Check_E_Spread_ASHR(OrderPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                            return -1;
                        ////}

                    //////case 'A': //Auction Order
                    //////    if (oae.Place == 1 || oae.Place == 3 || oae.Place == 13 || oae.Place == 15)
                    //////    {
                    //////        if (OrderPrice == 0)
                    //////        {
                    //////            CanBuy = true;
                    //////            CanSell = true;
                    //////            return OrderPrice;
                    //////        }
                    //////        else
                    //////        {
                    //////            return -1;
                    //////        }
                    //////    }
                    //////    else
                    //////    {
                    //////        //Auction period closed
                    //////        return -1;
                    //////    }

                    ////////I=3,15
                    ////////E=1,12,13
                    //////case 'L': //Limit Order (Only for Amend)
                    //////    if (oae.Place == 3 || oae.Place == 15)
                    //////    {
                    //////        //Auction Limit
                    //////        Check_I_Spread(NewPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                    //////        if (CanBuy == true || CanSell == true)
                    //////            return NewPrice;
                    //////        else
                    //////            //check existing price correct or not
                    //////            Check_I_Spread(OrderPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                    //////        return -1;

                    //////        //// Limit
                    //////        //Check_L_Spread(NewPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                    //////        //if (CanBuy == true || CanSell == true)
                    //////        //    return NewPrice;
                    //////        //else
                    //////        //    //check existing price correct or not
                    //////        //    Check_L_Spread(OrderPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                    //////        //return -1;
                    //////    }
                    //////    else if (oae.Place == 1 || oae.Place == 12 || oae.Place == 13)
                    //////    {
                    //////        //Limit
                    //////        Check_L_Spread(NewPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                    //////        if (CanBuy == true || CanSell == true)
                    //////            return NewPrice;
                    //////        else
                    //////            //check existing price correct or not
                    //////            Check_L_Spread(OrderPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                    //////        return -1;
                    //////    }
                    //////    else
                    //////    {
                    //////        //Limit
                    //////        Check_L_Spread(NewPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                    //////        if (CanBuy == true || CanSell == true)
                    //////            return NewPrice;
                    //////        else
                    //////            //check existing price correct or not
                    //////            Check_L_Spread(OrderPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                    //////        return -1;
                    //////    }

                    //////case 'S': //Special Order
                    //////    if (oae.Place == 3 || oae.Place == 15)
                    //////    {
                    //////        //Auction Limit
                    //////        Check_I_Spread(NewPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                    //////        if (CanBuy == true || CanSell == true)
                    //////            return NewPrice;
                    //////        else
                    //////            //check existing price correct or not
                    //////            Check_I_Spread(OrderPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                    //////        return -1;
                    //////    }
                    //////    else if (oae.Place == 1 || oae.Place == 12 || oae.Place == 13)
                    //////    {
                    //////        //Enhanced
                    //////        Check_E_Spread(NewPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                    //////        if (CanBuy == true || CanSell == true)
                    //////            return NewPrice;
                    //////        else
                    //////            //check existing price correct or not
                    //////            Check_E_Spread(OrderPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                    //////        return -1;
                    //////    }
                    //////    else
                    //////    {
                    //////        //Enhanced
                    //////        Check_E_Spread(NewPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                    //////        if (CanBuy == true || CanSell == true)
                    //////            return NewPrice;
                    //////        else
                    //////            //check existing price correct or not
                    //////            Check_E_Spread(OrderPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                    //////        return -1;
                    //////    }



                    default:
                        break;
                }
            }
            catch
            {
            }

            return -1;
        }


        // return NewPrice=-1 if invalid.  return canbuy,cansell=true if OrderPrice is valid
        public static decimal CheckSpread(decimal OrderPrice, char OrderType, char BuySell, int Index, Stock Stock1, ITradeDB TradeDB1, out bool CanBuy, out bool CanSell)
        {
            CanBuy = false;
            CanSell = false;

            if (OrderType.ToString().Trim() == "" || Stock1 == null || TradeDB1 == null)
                return -1;

            Market.OrderActionEnable oae = Stock1.MarketBelong != null ? Stock1.MarketBelong.OrderAction : new Market.OrderActionEnable(15, 15);

            decimal NewPrice = -1;
            try
            {                
                NewPrice = GetSpreadByIndex(Stock1.SpreadTableCode, OrderPrice, Index, TradeDB1.SpreadTables);                
                switch (OrderType)
                {
                    //I=3,15
                    //E=1,12,13
                    case 'X': //Auto Order
                        if (oae.Place == 3 || oae.Place == 15)
                        {
                            //Auction Limit
                            Check_I_Spread(NewPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                            /*
                            if (CanBuy == true || CanSell == true)
                                return NewPrice;
                            else
                                //check existing price correct or not
                                Check_I_Spread(OrderPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                            return NewPrice;*/
                        }
                        else if (oae.Place == 1 || oae.Place == 12 || oae.Place == 13)
                        {
                            //Enhanced
                            Check_E_Spread(NewPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                            /*
                            if (CanBuy == true || CanSell == true)
                                return NewPrice;
                            else
                                //check existing price correct or not
                                Check_E_Spread(OrderPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                                return -1;*/
                        }
                        else
                        {
                            //Enhanced
                            Check_E_Spread(NewPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                            /*
                            if (CanBuy == true || CanSell == true)
                                return NewPrice;
                            else
                                //check existing price correct or not
                                Check_E_Spread(OrderPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                                return -1;*/
                        }
                        break;

                    case 'A': //Auction Order
                        if (oae.Place == 1 || oae.Place == 3 || oae.Place == 13 || oae.Place == 15)
                        {
                            if (OrderPrice == 0)
                            {
                                CanBuy = true;
                                CanSell = true;
                                return OrderPrice;
                            }
                            else
                            {
                                return -1;
                            }
                        }
                        else
                        {
                            //Auction period closed
                            return -1;
                        }


                    case 'Z': //Auction Order for Amend +1/ -1
                        if (oae.Place == 1 || oae.Place == 3 || oae.Place == 13 || oae.Place == 15)
                        {
                            if (OrderPrice == 0)
                            {
                                //Auction Limit
                                Check_I_Spread(NewPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                                if (CanBuy == true || CanSell == true)
                                    return NewPrice;
                                else
                                    //check existing price correct or not
                                    Check_I_Spread(OrderPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                                    return -1;
                            }
                            else
                            {
                                return -1;
                            }
                        }
                        else
                        {
                            //Auction period closed
                            return -1;
                        }

                    //I=3,15
                    //E=1,12,13
                    case 'L': //Limit Order (Only for Amend)
                        if (oae.Place == 3 || oae.Place == 15)
                        {
                            //Auction Limit
                            Check_I_Spread(NewPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                            /*
                            if (CanBuy == true || CanSell == true)
                                return NewPrice;
                            else
                                //check existing price correct or not
                                Check_I_Spread(OrderPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                            return -1;*/

                            //// Limit
                            //Check_L_Spread(NewPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                            //if (CanBuy == true || CanSell == true)
                            //    return NewPrice;
                            //else
                            //    //check existing price correct or not
                            //    Check_L_Spread(OrderPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                            //return -1;
                        }
                        else if (oae.Place == 1 || oae.Place == 12 || oae.Place == 13)
                        {
                            //Limit
                            Check_L_Spread(NewPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                            /*
                            if (CanBuy == true || CanSell == true)
                                return NewPrice;
                            else
                                //check existing price correct or not
                                Check_L_Spread(OrderPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                            return -1;*/
                        }
                        else
                        {
                            //Limit
                            Check_L_Spread(NewPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                            /*
                            if (CanBuy == true || CanSell == true)
                                return NewPrice;
                            else
                                //check existing price correct or not
                                Check_L_Spread(OrderPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                            return -1;*/
                        }
                        break;

                    case 'S': //Special Order
                        if (oae.Place == 3 || oae.Place == 15)
                        {
                            //Auction Limit
                            Check_I_Spread(NewPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                            /*
                            if (CanBuy == true || CanSell == true)
                                return NewPrice;
                            else
                                //check existing price correct or not
                                Check_I_Spread(OrderPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                            return -1;*/
                        }
                        else if (oae.Place == 1 || oae.Place == 12 || oae.Place == 13)
                        {
                            //Enhanced
                            Check_E_Spread(NewPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                            /*
                            if (CanBuy == true || CanSell == true)
                                return NewPrice;
                            else
                                //check existing price correct or not
                                Check_E_Spread(OrderPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                            return -1;*/
                        }
                        else
                        {
                            //Enhanced
                            Check_E_Spread(NewPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                            /*
                            if (CanBuy == true || CanSell == true)
                                return NewPrice;
                            else
                                //check existing price correct or not
                                Check_E_Spread(OrderPrice, BuySell, Stock1, TradeDB1, out CanBuy, out CanSell);
                            return -1;*/
                        }
                        break;

                    default:
                        break;
                }
            }
            catch
            {
            }

            return NewPrice;
        }


        public static decimal GetSpreadRange(decimal OrderPrice, char OrderType, Stock Stock1, ITradeDB TradeDB1, out char OrderTypeNow, out decimal BidRange, out decimal AskRange)
        {
            decimal NewPrice = 0;            
            OrderTypeNow = ' ';
            BidRange = 0;
            AskRange = 0;

            if (OrderType.ToString().Trim() == "" || Stock1 == null || TradeDB1 == null)
                return -1;

            Market.OrderActionEnable oae = Stock1.MarketBelong != null ? Stock1.MarketBelong.OrderAction : new Market.OrderActionEnable(15, 15);

            try
            {                
                switch (OrderType)
                {
                    //I=3,15
                    //E=1,12,13
                    case 'X': //Auto Order
                        if (oae.Place == 3 || oae.Place == 15)
                        {
                            OrderTypeNow = 'I';
                            //Ask side
                            if (Stock1.Ask <= 0)
                                NewPrice = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Nominal, I_QueueSpread - 1, TradeDB1.SpreadTables);
                            else
                                NewPrice = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Ask, I_QueueSpread - 1, TradeDB1.SpreadTables);
                            AskRange = (NewPrice <= 0) ? 0 : NewPrice;

                            //Bid side
                            if (Stock1.Bid <= 0)
                                NewPrice = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Nominal, -I_QueueSpread + 1, TradeDB1.SpreadTables);
                            else
                                NewPrice = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Bid, -I_QueueSpread + 1, TradeDB1.SpreadTables);
                            BidRange = (NewPrice <= 0) ? 0 : NewPrice;
                            return 1;
                        }
                        else if (oae.Place == 1 || oae.Place == 12 || oae.Place == 13)
                        {
                            OrderTypeNow = 'E';
                            //Ask side
                            if (Stock1.Ask <= 0)
                                NewPrice = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Nominal, E_QueueSpread - 1, TradeDB1.SpreadTables);
                            else
                                NewPrice = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Ask, E_QueueSpread - 1, TradeDB1.SpreadTables);
                            AskRange = (NewPrice <= 0) ? 0 : NewPrice;

                            //Bid side
                            if (Stock1.Bid <= 0)
                                NewPrice = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Nominal, -E_QueueSpread + 1, TradeDB1.SpreadTables);
                            else
                                NewPrice = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Bid, -E_QueueSpread + 1, TradeDB1.SpreadTables);                            
                            BidRange = (NewPrice <= 0) ? 0 : NewPrice;
                            return 1;
                        }
                        else if (oae.Place == 0)
                        {
                            //before 9:30am

                            OrderTypeNow = 'N';
                            //Ask side
                            if (Stock1.Ask <= 0)
                                NewPrice = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Nominal, E_QueueSpread - 1, TradeDB1.SpreadTables);
                            else
                                NewPrice = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Ask, E_QueueSpread - 1, TradeDB1.SpreadTables);
                            AskRange = (NewPrice <= 0) ? 0 : NewPrice;

                            //Bid side
                            if (Stock1.Bid <= 0)
                                NewPrice = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Nominal, -E_QueueSpread + 1, TradeDB1.SpreadTables);
                            else
                                NewPrice = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Bid, -E_QueueSpread + 1, TradeDB1.SpreadTables);
                            BidRange = (NewPrice <= 0) ? 0 : NewPrice;
                            return 1;
                        }
                        else
                        {
                            OrderTypeNow = 'E';
                            //Ask side
                            if (Stock1.Ask <= 0)
                                NewPrice = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Nominal, E_QueueSpread - 1, TradeDB1.SpreadTables);
                            else
                                NewPrice = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Ask, E_QueueSpread - 1, TradeDB1.SpreadTables);
                            AskRange = (NewPrice <= 0) ? 0 : NewPrice;

                            //Bid side
                            if (Stock1.Bid <= 0)
                                NewPrice = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Nominal, -E_QueueSpread + 1, TradeDB1.SpreadTables);
                            else
                                NewPrice = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Bid, -E_QueueSpread + 1, TradeDB1.SpreadTables);
                            BidRange = (NewPrice <= 0) ? 0 : NewPrice;
                            return 1;
                        }

                    default:
                        return -1;
                }
            }
            catch
            {
                AskRange = (AskRange <= 0) ? (decimal)0.01 : AskRange;
                BidRange = (BidRange <= 0) ? (decimal)0.01 : BidRange;
                return -1;
            }            
        }


        private static void Check_E_Spread_ASHR(decimal OrderPrice, char BuySell, Stock Stock1, ITradeDB TradeDB1, out bool CanBuy, out bool CanSell)
        {
            decimal adjustAsk = 0;
            decimal adjustBid = 0;

            CanBuy = false;
            CanSell = false;

            if (Stock1 == null) return;

            adjustBid = Math.Round(Stock1.PrevClose * L_BidQueuePercent_ASHR, 2, MidpointRounding.AwayFromZero);            
            adjustAsk = Math.Round(Stock1.PrevClose * L_AskQueuePercent_ASHR, 2, MidpointRounding.AwayFromZero);

            if (BuySell.ToString().ToUpper().Trim() == "B" || BuySell.ToString().ToUpper().Trim() == "")
            {
                if (OrderPrice >= adjustBid && OrderPrice <= adjustAsk)
                    CanBuy = true;
                else
                    CanBuy = false;
            }
            else
                CanBuy = false;
            
            if (BuySell.ToString().ToUpper().Trim() == "S" || BuySell.ToString().ToUpper().Trim() == "")
            {
                if (OrderPrice >= adjustBid && OrderPrice <= adjustAsk)
                    CanSell = true;
                else
                    CanSell = false;
            }
            else
                CanSell = false;
            
        }


        private static void Check_E_Spread(decimal OrderPrice, char BuySell, Stock Stock1, ITradeDB TradeDB1, out bool CanBuy, out bool CanSell)
        {
            decimal adjustAsk = 0;
            decimal adjustBid = 0;

            CanBuy = false;
            CanSell = false;

            if (Stock1 == null) return;

            if (BuySell.ToString().ToUpper().Trim() == "B" || BuySell.ToString().ToUpper().Trim() == "")
            {
                //CASE 1 - Bid, Ask, Norm  exist
                if ((Stock1.Bid > 0 && (Stock1.BidVol[0] > 0 || Stock1.BidCount[0] > 0)) &&
                    (Stock1.Ask > 0 && (Stock1.AskVol[0] > 0 || Stock1.AskCount[0] > 0)))
                {
                    adjustAsk = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Ask, E_OppositeSpread - 1, TradeDB1.SpreadTables);
                    if (ChkBidSide(Stock1.Bid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, E_QueueSpread, TradeDB1.SpreadTables))
                        CanBuy = true;
                    else
                        CanBuy = false;
                }

                //CASE 2 - Bid, Norm exist , No Ask
                else if ((Stock1.Bid > 0 && (Stock1.BidVol[0] > 0 || Stock1.BidCount[0] > 0)) &&
                    (Stock1.Ask <= 0 || (Stock1.AskVol[0] <= 0 && Stock1.AskCount[0] <= 0)))
                {
                    adjustAsk = Math.Max(Stock1.Bid, Stock1.Nominal);
                    adjustAsk = GetSpreadByIndex(Stock1.SpreadTableCode, adjustAsk, E_OppositeSpread - 1, TradeDB1.SpreadTables);
                    adjustAsk = Math.Max(adjustAsk, (decimal)0.01);

                    if (ChkBidSide(Stock1.Bid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, E_QueueSpread, TradeDB1.SpreadTables))
                        CanBuy = true;
                    else
                        CanBuy = false;
                }

                //CASE 3 - Ask, Norm exist , No Bid
                else if ((Stock1.Bid <= 0 || (Stock1.BidVol[0] <= 0 && Stock1.BidCount[0] <= 0)) &&
                    (Stock1.Ask > 0 && (Stock1.AskVol[0] > 0 || Stock1.AskCount[0] > 0)))
                {
                    adjustBid = Math.Min(Stock1.Ask, Stock1.Nominal);
                    adjustBid = Math.Max(adjustBid, (decimal)0.01);
                    adjustAsk = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Ask, E_OppositeSpread - 1, TradeDB1.SpreadTables);

                    if (ChkBidSide(adjustBid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, E_QueueSpread, TradeDB1.SpreadTables))
                        CanBuy = true;
                    else
                        CanBuy = false;
                }

                //CASE 4 - Norm exist , No Bid, Ask
                else if ((Stock1.Bid <= 0 || (Stock1.BidVol[0] <= 0 && Stock1.BidCount[0] <= 0)) &&
                    (Stock1.Ask <= 0 || (Stock1.AskVol[0] <= 0 && Stock1.AskCount[0] <= 0)) && Stock1.Nominal > 0)
                {                    
                    adjustAsk = Math.Max(Stock1.Nominal, (decimal)0.01);
                    adjustAsk = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Nominal, E_OppositeSpread - 1, TradeDB1.SpreadTables);
                    adjustBid = Math.Max(Stock1.Nominal, (decimal)0.01);

                    if (ChkBidSide(adjustBid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, E_QueueSpread, TradeDB1.SpreadTables))
                        CanBuy = true;
                    else
                        CanBuy = false;
                }

                //CASE 5 - No Bid, Ask, Norm
                else if ((Stock1.Bid <= 0 || (Stock1.BidVol[0] <= 0 && Stock1.BidCount[0] <= 0)) &&
                    (Stock1.Ask <= 0 || (Stock1.AskVol[0] <= 0 && Stock1.AskCount[0] <= 0)) && Stock1.Nominal <= 0)
                {
                    if (Stock1.PrevClose > 0)
                    {
                        //adjustAsk = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.PrevClose, E_OppositeSpread - 1, TradeDB1.SpreadTables);
                        adjustAsk = Math.Max(Stock1.PrevClose, (decimal)0.01);
                        adjustBid = Math.Max(Stock1.PrevClose, (decimal)0.01);

                        if (ChkBidSide(adjustBid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, E_QueueSpread, TradeDB1.SpreadTables))
                            CanBuy = true;
                        else
                            CanBuy = false;
                    }
                    else
                        CanBuy = false;
                }
                else
                    CanBuy = false;

            }
            
            //Also check Sell side if No Buy/Sell side given
            if (BuySell.ToString().ToUpper().Trim() == "S" || BuySell.ToString().ToUpper().Trim() == "")
            {
                //CASE 1 - Bid, Ask, Norm  exist
                if ((Stock1.Bid > 0 && (Stock1.BidVol[0] > 0 || Stock1.BidCount[0] > 0)) &&
                    (Stock1.Ask > 0 && (Stock1.AskVol[0] > 0 || Stock1.AskCount[0] > 0)))
                {
                    adjustBid = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Bid, -(E_OppositeSpread - 1), TradeDB1.SpreadTables);
                    if (ChkAskSide(adjustBid, Stock1.Ask, OrderPrice, Stock1.SpreadTableCode, E_QueueSpread, TradeDB1.SpreadTables))
                        CanSell = true;
                    else
                        CanSell = false;
                }

                //CASE 2 - Bid, Norm exist , No Ask
                else if ((Stock1.Bid > 0 && (Stock1.BidVol[0] > 0 || Stock1.BidCount[0] > 0)) &&
                    (Stock1.Ask <= 0 || (Stock1.AskVol[0] <= 0 && Stock1.AskCount[0] <= 0)))
                {
                    adjustAsk = Math.Max(Stock1.Bid, Stock1.Nominal);
                    adjustAsk = Math.Max(adjustAsk, (decimal)0.01);

                    adjustBid = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Bid, -(E_OppositeSpread - 1), TradeDB1.SpreadTables);
                    if (ChkAskSide(adjustBid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, E_QueueSpread, TradeDB1.SpreadTables))
                        CanSell = true;
                    else
                        CanSell = false;
                }

                //CASE 3 - Ask, Norm exist , No Bid
                else if ((Stock1.Bid <= 0 || (Stock1.BidVol[0] <= 0 && Stock1.BidCount[0] <= 0)) &&
                    (Stock1.Ask > 0 && (Stock1.AskVol[0] > 0 || Stock1.AskCount[0] > 0)))
                {
                    adjustBid = Math.Min(Stock1.Ask, Stock1.Nominal);
                    adjustBid = GetSpreadByIndex(Stock1.SpreadTableCode, adjustBid, -(E_OppositeSpread - 1), TradeDB1.SpreadTables);
                    adjustBid = Math.Max(adjustBid, (decimal)0.01);

                    if (ChkAskSide(adjustBid, Stock1.Ask, OrderPrice, Stock1.SpreadTableCode, E_QueueSpread, TradeDB1.SpreadTables))
                        CanSell = true;
                    else
                        CanSell = false;
                }

                //CASE 4 - Norm exist , No Bid, Ask
                else if ((Stock1.Bid <= 0 || (Stock1.BidVol[0] <= 0 && Stock1.BidCount[0] <= 0)) &&
                    (Stock1.Ask <= 0 || (Stock1.AskVol[0] <= 0 && Stock1.AskCount[0] <= 0)) && Stock1.Nominal > 0)
                {
                    adjustAsk = Math.Max(Stock1.Nominal, (decimal)0.01);
                    adjustBid = Math.Max(Stock1.Nominal, (decimal)0.01);
                    adjustBid = GetSpreadByIndex(Stock1.SpreadTableCode, adjustBid, -(E_OppositeSpread - 1), TradeDB1.SpreadTables);

                    if (ChkAskSide(adjustBid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, E_QueueSpread, TradeDB1.SpreadTables))
                        CanSell = true;
                    else
                        CanSell = false;
                }

                //CASE 5 - No Bid, Ask, Norm
                else if ((Stock1.Bid <= 0 || (Stock1.BidVol[0] <= 0 && Stock1.BidCount[0] <= 0)) &&
                    (Stock1.Ask <= 0 || (Stock1.AskVol[0] <= 0 && Stock1.AskCount[0] <= 0)) && Stock1.Nominal <= 0)
                {
                    if (Stock1.PrevClose > 0)
                    {
                        adjustAsk = Math.Max(Stock1.PrevClose, (decimal)0.01);
                        adjustBid = Math.Max(Stock1.PrevClose, (decimal)0.01);

                        if (ChkAskSide(adjustBid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, E_QueueSpread, TradeDB1.SpreadTables))
                            CanSell = true;
                        else
                            CanSell = false;
                    }
                    else 
                        CanSell = false;
                }
                else
                    CanSell = false;
            }
            else
            {
                // Buy sell error
                CanSell = false;
            }
        }


        private static void Check_S_Spread(decimal OrderPrice, char BuySell, Stock Stock1, ITradeDB TradeDB1, out bool CanBuy, out bool CanSell)
        {
            decimal adjustAsk = 0;
            decimal adjustBid = 0;

            CanBuy = false;
            CanSell = false;

            if (Stock1 == null) return;

            if (BuySell.ToString().ToUpper().Trim() == "B" || BuySell.ToString().ToUpper().Trim() == "")
            {
                //CASE 1 - Bid, Ask, Norm  exist
                if ((Stock1.Bid > 0 && (Stock1.BidVol[0] > 0 || Stock1.BidCount[0] > 0)) &&
                    (Stock1.Ask > 0 && (Stock1.AskVol[0] > 0 || Stock1.AskCount[0] > 0)))
                {
                    adjustAsk = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Ask, S_OppositeSpread - 1, TradeDB1.SpreadTables);
                    if (ChkBidSide(Stock1.Bid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, S_QueueSpread, TradeDB1.SpreadTables))
                        CanBuy = true;
                    else
                        CanBuy = false;
                }

                //CASE 2 - Bid, Norm exist , No Ask
                else if ((Stock1.Bid > 0 && (Stock1.BidVol[0] > 0 || Stock1.BidCount[0] > 0)) &&
                    (Stock1.Ask <= 0 || (Stock1.AskVol[0] <= 0 && Stock1.AskCount[0] <= 0)))
                {
                    adjustAsk = Math.Max(Stock1.Bid, Stock1.Nominal);
                    adjustAsk = GetSpreadByIndex(Stock1.SpreadTableCode, adjustAsk, S_OppositeSpread - 1, TradeDB1.SpreadTables);
                    adjustAsk = Math.Max(adjustAsk, (decimal)0.01);

                    if (ChkBidSide(Stock1.Bid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, S_QueueSpread, TradeDB1.SpreadTables))
                        CanBuy = true;
                    else
                        CanBuy = false;
                }

                //CASE 3 - Ask, Norm exist , No Bid
                else if ((Stock1.Bid <= 0 || (Stock1.BidVol[0] <= 0 && Stock1.BidCount[0] <= 0)) &&
                    (Stock1.Ask > 0 && (Stock1.AskVol[0] > 0 || Stock1.AskCount[0] > 0)))
                {
                    adjustBid = Math.Min(Stock1.Ask, Stock1.Nominal);
                    adjustBid = Math.Max(adjustBid, (decimal)0.01);
                    adjustAsk = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Ask, S_OppositeSpread - 1, TradeDB1.SpreadTables);

                    if (ChkBidSide(adjustBid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, S_QueueSpread, TradeDB1.SpreadTables))
                        CanBuy = true;
                    else
                        CanBuy = false;
                }

                //CASE 4 - Norm exist , No Bid, Ask
                else if ((Stock1.Bid <= 0 || (Stock1.BidVol[0] <= 0 && Stock1.BidCount[0] <= 0)) &&
                    (Stock1.Ask <= 0 || (Stock1.AskVol[0] <= 0 && Stock1.AskCount[0] <= 0)) && Stock1.Nominal > 0)
                {
                    adjustAsk = Math.Max(Stock1.Nominal, (decimal)0.01);
                    adjustAsk = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Nominal, S_OppositeSpread - 1, TradeDB1.SpreadTables);
                    adjustBid = Math.Max(Stock1.Nominal, (decimal)0.01);

                    if (ChkBidSide(adjustBid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, S_QueueSpread, TradeDB1.SpreadTables))
                        CanBuy = true;
                    else
                        CanBuy = false;
                }

                //CASE 5 - No Bid, Ask, Norm
                else if ((Stock1.Bid <= 0 || (Stock1.BidVol[0] <= 0 && Stock1.BidCount[0] <= 0)) &&
                    (Stock1.Ask <= 0 || (Stock1.AskVol[0] <= 0 && Stock1.AskCount[0] <= 0)) && Stock1.Nominal <= 0)
                {
                    if (Stock1.PrevClose > 0)
                    {
                        //adjustAsk = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.PrevClose, S_OppositeSpread - 1, TradeDB1.SpreadTables);
                        adjustAsk = Math.Max(Stock1.PrevClose, (decimal)0.01);
                        adjustBid = Math.Max(Stock1.PrevClose, (decimal)0.01);

                        if (ChkBidSide(adjustBid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, S_QueueSpread, TradeDB1.SpreadTables))
                            CanBuy = true;
                        else
                            CanBuy = false;
                    }
                    else
                        CanBuy = false;
                }
                else
                    CanBuy = false;

            }

            //Also check Sell side if No Buy/Sell side given
            if (BuySell.ToString().ToUpper().Trim() == "S" || BuySell.ToString().ToUpper().Trim() == "")
            {
                //CASE 1 - Bid, Ask, Norm  exist
                if ((Stock1.Bid > 0 && (Stock1.BidVol[0] > 0 || Stock1.BidCount[0] > 0)) &&
                    (Stock1.Ask > 0 && (Stock1.AskVol[0] > 0 || Stock1.AskCount[0] > 0)))
                {
                    adjustBid = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Bid, -(S_OppositeSpread - 1), TradeDB1.SpreadTables);
                    if (ChkAskSide(adjustBid, Stock1.Ask, OrderPrice, Stock1.SpreadTableCode, S_QueueSpread, TradeDB1.SpreadTables))
                        CanSell = true;
                    else
                        CanSell = false;
                }

                //CASE 2 - Bid, Norm exist , No Ask
                else if ((Stock1.Bid > 0 && (Stock1.BidVol[0] > 0 || Stock1.BidCount[0] > 0)) &&
                    (Stock1.Ask <= 0 || (Stock1.AskVol[0] <= 0 && Stock1.AskCount[0] <= 0)))
                {
                    adjustAsk = Math.Max(Stock1.Bid, Stock1.Nominal);
                    adjustAsk = Math.Max(adjustAsk, (decimal)0.01);

                    adjustBid = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Bid, -(S_OppositeSpread - 1), TradeDB1.SpreadTables);
                    if (ChkAskSide(adjustBid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, S_QueueSpread, TradeDB1.SpreadTables))
                        CanSell = true;
                    else
                        CanSell = false;
                }

                //CASE 3 - Ask, Norm exist , No Bid
                else if ((Stock1.Bid <= 0 || (Stock1.BidVol[0] <= 0 && Stock1.BidCount[0] <= 0)) &&
                    (Stock1.Ask > 0 && (Stock1.AskVol[0] > 0 || Stock1.AskCount[0] > 0)))
                {
                    adjustBid = Math.Min(Stock1.Ask, Stock1.Nominal);
                    adjustBid = GetSpreadByIndex(Stock1.SpreadTableCode, adjustBid, -(S_OppositeSpread - 1), TradeDB1.SpreadTables);
                    adjustBid = Math.Max(adjustBid, (decimal)0.01);

                    if (ChkAskSide(adjustBid, Stock1.Ask, OrderPrice, Stock1.SpreadTableCode, S_QueueSpread, TradeDB1.SpreadTables))
                        CanSell = true;
                    else
                        CanSell = false;
                }

                //CASE 4 - Norm exist , No Bid, Ask
                else if ((Stock1.Bid <= 0 || (Stock1.BidVol[0] <= 0 && Stock1.BidCount[0] <= 0)) &&
                    (Stock1.Ask <= 0 || (Stock1.AskVol[0] <= 0 && Stock1.AskCount[0] <= 0)) && Stock1.Nominal > 0)
                {
                    adjustAsk = Math.Max(Stock1.Nominal, (decimal)0.01);
                    adjustBid = Math.Max(Stock1.Nominal, (decimal)0.01);
                    adjustBid = GetSpreadByIndex(Stock1.SpreadTableCode, adjustBid, -(S_OppositeSpread - 1), TradeDB1.SpreadTables);

                    if (ChkAskSide(adjustBid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, S_QueueSpread, TradeDB1.SpreadTables))
                        CanSell = true;
                    else
                        CanSell = false;
                }

                //CASE 5 - No Bid, Ask, Norm
                else if ((Stock1.Bid <= 0 || (Stock1.BidVol[0] <= 0 && Stock1.BidCount[0] <= 0)) &&
                    (Stock1.Ask <= 0 || (Stock1.AskVol[0] <= 0 && Stock1.AskCount[0] <= 0)) && Stock1.Nominal <= 0)
                {
                    if (Stock1.PrevClose > 0)
                    {
                        adjustAsk = Math.Max(Stock1.PrevClose, (decimal)0.01);
                        adjustBid = Math.Max(Stock1.PrevClose, (decimal)0.01);

                        if (ChkAskSide(adjustBid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, S_QueueSpread, TradeDB1.SpreadTables))
                            CanSell = true;
                        else
                            CanSell = false;
                    }
                    else
                        CanSell = false;
                }
                else
                    CanSell = false;
            }
            else
            {
                // Buy sell error
                CanSell = false;
            }
        }


        private static void Check_L_Spread(decimal OrderPrice, char BuySell, Stock Stock1, ITradeDB TradeDB1, out bool CanBuy, out bool CanSell)
        {
            decimal adjustAsk = 0;
            decimal adjustBid = 0;

            CanBuy = false;
            CanSell = false;

            if (Stock1 == null) return;

            if (BuySell.ToString().ToUpper().Trim() == "B" || BuySell.ToString().ToUpper().Trim() == "")
            {
                //CASE 1 - Bid, Ask, Norm  exist
                if ((Stock1.Bid > 0 && (Stock1.BidVol[0] > 0 || Stock1.BidCount[0] > 0)) &&
                    (Stock1.Ask > 0 && (Stock1.AskVol[0] > 0 || Stock1.AskCount[0] > 0)))
                {
                    adjustAsk = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Ask, L_OppositeSpread - 1, TradeDB1.SpreadTables);
                    if (ChkBidSide(Stock1.Bid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, L_QueueSpread, TradeDB1.SpreadTables))
                        CanBuy = true;
                    else
                        CanBuy = false;
                }

                //CASE 2 - Bid, Norm exist , No Ask
                else if ((Stock1.Bid > 0 && (Stock1.BidVol[0] > 0 || Stock1.BidCount[0] > 0)) &&
                    (Stock1.Ask <= 0 || (Stock1.AskVol[0] <= 0 && Stock1.AskCount[0] <= 0)))
                {
                    adjustAsk = Math.Max(Stock1.Bid, Stock1.Nominal);
                    adjustAsk = GetSpreadByIndex(Stock1.SpreadTableCode, adjustAsk, E_OppositeSpread - 1, TradeDB1.SpreadTables);
                    //////adjustAsk = GetSpreadByIndex(Stock1.SpreadTableCode, adjustAsk, L_OppositeSpread - 1, TradeDB1.SpreadTables);
                    adjustAsk = Math.Max(adjustAsk, (decimal)0.01);

                    if (ChkBidSide(Stock1.Bid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, L_QueueSpread, TradeDB1.SpreadTables))
                        CanBuy = true;
                    else
                        CanBuy = false;
                }

                //CASE 3 - Ask, Norm exist , No Bid
                else if ((Stock1.Bid <= 0 || (Stock1.BidVol[0] <= 0 && Stock1.BidCount[0] <= 0)) &&
                    (Stock1.Ask > 0 && (Stock1.AskVol[0] > 0 || Stock1.AskCount[0] > 0)))
                {
                    adjustBid = Math.Min(Stock1.Ask, Stock1.Nominal);
                    adjustBid = Math.Max(adjustBid, (decimal)0.01);
                    adjustAsk = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Ask, E_OppositeSpread - 1, TradeDB1.SpreadTables);
                    //////adjustAsk = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Ask, L_OppositeSpread - 1, TradeDB1.SpreadTables);
                    if (ChkBidSide(adjustBid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, L_QueueSpread, TradeDB1.SpreadTables))
                        CanBuy = true;
                    else
                        CanBuy = false;
                }

                //CASE 4 - Norm exist , No Bid, Ask
                else if ((Stock1.Bid <= 0 || (Stock1.BidVol[0] <= 0 && Stock1.BidCount[0] <= 0)) &&
                    (Stock1.Ask <= 0 || (Stock1.AskVol[0] <= 0 && Stock1.AskCount[0] <= 0)) && Stock1.Nominal > 0)
                {
                    adjustAsk = Math.Max(Stock1.Nominal, (decimal)0.01);
                    adjustAsk = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Nominal, E_OppositeSpread - 1, TradeDB1.SpreadTables);
                    //////adjustAsk = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Nominal, L_OppositeSpread - 1, TradeDB1.SpreadTables);
                    adjustBid = Math.Max(Stock1.Nominal, (decimal)0.01);

                    if (ChkBidSide(adjustBid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, L_QueueSpread, TradeDB1.SpreadTables))
                        CanBuy = true;
                    else
                        CanBuy = false;
                }

                //CASE 5 - No Bid, Ask, Norm
                else if ((Stock1.Bid <= 0 || (Stock1.BidVol[0] <= 0 && Stock1.BidCount[0] <= 0)) &&
                    (Stock1.Ask <= 0 || (Stock1.AskVol[0] <= 0 && Stock1.AskCount[0] <= 0)) && Stock1.Nominal <= 0)
                {
                    if (Stock1.PrevClose > 0)
                    {
                        //adjustAsk = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.PrevClose, L_OppositeSpread - 1, TradeDB1.SpreadTables);
                        adjustAsk = Math.Max(Stock1.PrevClose, (decimal)0.01);
                        adjustBid = Math.Max(Stock1.PrevClose, (decimal)0.01);

                        if (ChkBidSide(adjustBid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, L_QueueSpread, TradeDB1.SpreadTables))
                            CanBuy = true;
                        else
                            CanBuy = false;
                    }
                    else
                        CanBuy = false;
                }
                else
                    CanBuy = false;

            }

            //Also check Sell side if No Buy/Sell side given
            if (BuySell.ToString().ToUpper().Trim() == "S" || BuySell.ToString().ToUpper().Trim() == "")
            {
                //CASE 1 - Bid, Ask, Norm  exist
                if ((Stock1.Bid > 0 && (Stock1.BidVol[0] > 0 || Stock1.BidCount[0] > 0)) &&
                    (Stock1.Ask > 0 && (Stock1.AskVol[0] > 0 || Stock1.AskCount[0] > 0)))
                {
                    adjustBid = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Bid, -(L_OppositeSpread - 1), TradeDB1.SpreadTables);
                    if (ChkAskSide(adjustBid, Stock1.Ask, OrderPrice, Stock1.SpreadTableCode, L_QueueSpread, TradeDB1.SpreadTables))
                        CanSell = true;
                    else
                        CanSell = false;
                }

                //CASE 2 - Bid, Norm exist , No Ask
                else if ((Stock1.Bid > 0 && (Stock1.BidVol[0] > 0 || Stock1.BidCount[0] > 0)) &&
                    (Stock1.Ask <= 0 || (Stock1.AskVol[0] <= 0 && Stock1.AskCount[0] <= 0)))
                {
                    adjustAsk = Math.Max(Stock1.Bid, Stock1.Nominal);
                    adjustAsk = Math.Max(adjustAsk, (decimal)0.01);
                    adjustBid = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Bid, -(E_OppositeSpread - 1), TradeDB1.SpreadTables);
                    //////adjustBid = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Bid, -(L_OppositeSpread - 1), TradeDB1.SpreadTables);
                    if (ChkAskSide(Stock1.Bid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, L_QueueSpread, TradeDB1.SpreadTables))
                        CanSell = true;
                    else
                        CanSell = false;
                }

                //CASE 3 - Ask, Norm exist , No Bid
                else if ((Stock1.Bid <= 0 || (Stock1.BidVol[0] <= 0 && Stock1.BidCount[0] <= 0)) &&
                    (Stock1.Ask > 0 && (Stock1.AskVol[0] > 0 || Stock1.AskCount[0] > 0)))
                {
                    adjustBid = Math.Min(Stock1.Ask, Stock1.Nominal);
                    adjustBid = GetSpreadByIndex(Stock1.SpreadTableCode, adjustBid, -(E_OppositeSpread - 1), TradeDB1.SpreadTables);
                    //////adjustBid = GetSpreadByIndex(Stock1.SpreadTableCode, adjustBid, -(L_OppositeSpread - 1), TradeDB1.SpreadTables);
                    adjustBid = Math.Max(adjustBid, (decimal)0.01);

                    if (ChkAskSide(adjustBid, Stock1.Ask, OrderPrice, Stock1.SpreadTableCode, L_QueueSpread, TradeDB1.SpreadTables))
                        CanSell = true;
                    else
                        CanSell = false;
                }

                //CASE 4 - Norm exist , No Bid, Ask
                else if ((Stock1.Bid <= 0 || (Stock1.BidVol[0] <= 0 && Stock1.BidCount[0] <= 0)) &&
                    (Stock1.Ask <= 0 || (Stock1.AskVol[0] <= 0 && Stock1.AskCount[0] <= 0)) && Stock1.Nominal > 0)
                {
                    adjustAsk = Math.Max(Stock1.Nominal, (decimal)0.01);
                    adjustBid = Math.Max(Stock1.Nominal, (decimal)0.01);
                    adjustBid = GetSpreadByIndex(Stock1.SpreadTableCode, adjustBid, -(E_OppositeSpread - 1), TradeDB1.SpreadTables);
                    //////adjustBid = GetSpreadByIndex(Stock1.SpreadTableCode, adjustBid, -(L_OppositeSpread - 1), TradeDB1.SpreadTables);
                    if (ChkAskSide(adjustBid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, L_QueueSpread, TradeDB1.SpreadTables))
                        CanSell = true;
                    else
                        CanSell = false;
                }

                //CASE 5 - No Bid, Ask, Norm
                else if ((Stock1.Bid <= 0 || (Stock1.BidVol[0] <= 0 && Stock1.BidCount[0] <= 0)) &&
                    (Stock1.Ask <= 0 || (Stock1.AskVol[0] <= 0 && Stock1.AskCount[0] <= 0)) && Stock1.Nominal <= 0)
                {
                    if (Stock1.PrevClose > 0)
                    {
                        adjustAsk = Math.Max(Stock1.PrevClose, (decimal)0.01);
                        adjustBid = Math.Max(Stock1.PrevClose, (decimal)0.01);

                        if (ChkAskSide(adjustBid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, L_QueueSpread, TradeDB1.SpreadTables))
                            CanSell = true;
                        else
                            CanSell = false;
                    }
                    else
                        CanSell = false;
                }
                else
                    CanSell = false;
            }
            else
            {
                // Buy sell error
                CanSell = false;
            }
        }



        private static void Check_I_Spread(decimal OrderPrice, char BuySell, Stock Stock1, ITradeDB TradeDB1, out bool CanBuy, out bool CanSell)
        {
            decimal adjustAsk = 0;
            decimal adjustBid = 0;

            CanBuy = false;
            CanSell = false;

            if (Stock1 == null) return;

            if (BuySell.ToString().ToUpper().Trim() == "B" || BuySell.ToString().ToUpper().Trim() == "")
            {
                //CASE 1 - Bid, Ask, Norm  exist
                if ((Stock1.Bid > 0 && (Stock1.BidVol[0] > 0 || Stock1.BidCount[0] > 0)) &&
                    (Stock1.Ask > 0 && (Stock1.AskVol[0] > 0 || Stock1.AskCount[0] > 0)))
                {
                    adjustAsk = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Ask, I_OppositeSpread - 1, TradeDB1.SpreadTables);
                    if (ChkBidSide(Stock1.Bid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, I_QueueSpread, TradeDB1.SpreadTables))
                        CanBuy = true;
                    else
                        CanBuy = false;
                }

                //CASE 2 - Bid, Norm exist , No Ask
                else if ((Stock1.Bid > 0 && (Stock1.BidVol[0] > 0 || Stock1.BidCount[0] > 0)) &&
                    (Stock1.Ask <= 0 || (Stock1.AskVol[0] <= 0 && Stock1.AskCount[0] <= 0)))
                {
                    adjustAsk = Math.Max(Stock1.Bid, Stock1.Nominal);
                    //adjustAsk = GetSpreadByIndex(Stock1.SpreadTableCode, adjustAsk, I_OppositeSpread - 1, TradeDB1.SpreadTables);
                    adjustAsk = Math.Max(adjustAsk, (decimal)0.01);

                    if (ChkBidSide(Stock1.Bid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, I_QueueSpread, TradeDB1.SpreadTables))
                        CanBuy = true;
                    else
                        CanBuy = false;
                }

                //CASE 3 - Ask, Norm exist , No Bid
                else if ((Stock1.Bid <= 0 || (Stock1.BidVol[0] <= 0 && Stock1.BidCount[0] <= 0)) &&
                    (Stock1.Ask > 0 && (Stock1.AskVol[0] > 0 || Stock1.AskCount[0] > 0)))
                {
                    adjustBid = Math.Min(Stock1.Ask, Stock1.Nominal);
                    adjustBid = Math.Max(adjustBid, (decimal)0.01);

                    adjustAsk = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Ask, I_OppositeSpread - 1, TradeDB1.SpreadTables);

                    if (ChkBidSide(adjustBid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, I_QueueSpread, TradeDB1.SpreadTables))
                        CanBuy = true;
                    else
                        CanBuy = false;
                }

                //CASE 4 - Norm exist , No Bid, Ask
                else if ((Stock1.Bid <= 0 || (Stock1.BidVol[0] <= 0 && Stock1.BidCount[0] <= 0)) &&
                    (Stock1.Ask <= 0 || (Stock1.AskVol[0] <= 0 && Stock1.AskCount[0] <= 0)) && Stock1.Nominal > 0)
                {
                    //adjustAsk = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Nominal, I_OppositeSpread - 1, TradeDB1.SpreadTables);
                    //adjustAsk = Math.Max(adjustAsk, 1);
                    //adjustBid = Math.Max(Stock1.Nominal, 1);
                    //adjustBid = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Nominal, -(I_OppositeSpread - 1), TradeDB1.SpreadTables);

                    adjustBid = Stock1.Nominal;
                    adjustAsk = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Nominal, I_OppositeSpread - 1, TradeDB1.SpreadTables);

                    if (ChkBidSide(adjustBid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, I_QueueSpread, TradeDB1.SpreadTables))
                        CanBuy = true;
                    else
                        CanBuy = false;
                }

                //CASE 5 - No Bid, Ask, Norm
                else if ((Stock1.Bid <= 0 || (Stock1.BidVol[0] <= 0 && Stock1.BidCount[0] <= 0)) &&
                    (Stock1.Ask <= 0 || (Stock1.AskVol[0] <= 0 && Stock1.AskCount[0] <= 0)) && Stock1.Nominal <= 0)
                {
                    if (Stock1.PrevClose > 0)
                    {
                        //adjustAsk = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.PrevClose, I_OppositeSpread - 1, TradeDB1.SpreadTables);
                        adjustAsk = Math.Max(adjustAsk, (decimal)0.01);
                        adjustBid = Math.Max(Stock1.PrevClose, (decimal)0.01);

                        if (ChkBidSide(adjustBid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, I_QueueSpread, TradeDB1.SpreadTables))
                            CanBuy = true;
                        else
                            CanBuy = false;
                    }
                    else
                        CanBuy = false;
                }
                else
                    CanBuy = false;

            }

            //Also check Sell side if No Buy/Sell side given
            if (BuySell.ToString().ToUpper().Trim() == "S" || BuySell.ToString().ToUpper().Trim() == "")
            {
                //CASE 1 - Bid, Ask, Norm  exist
                if ((Stock1.Bid > 0 && (Stock1.BidVol[0] > 0 || Stock1.BidCount[0] > 0)) &&
                    (Stock1.Ask > 0 && (Stock1.AskVol[0] > 0 || Stock1.AskCount[0] > 0)))
                {
                    adjustBid = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Bid, -(I_OppositeSpread - 1), TradeDB1.SpreadTables);
                    if (ChkAskSide(adjustBid, Stock1.Ask, OrderPrice, Stock1.SpreadTableCode, I_QueueSpread, TradeDB1.SpreadTables))
                        CanSell = true;
                    else
                        CanSell = false;
                }

                //CASE 2 - Bid, Norm exist , No Ask
                else if ((Stock1.Bid > 0 && (Stock1.BidVol[0] > 0 || Stock1.BidCount[0] > 0)) &&
                    (Stock1.Ask <= 0 || (Stock1.AskVol[0] <= 0 && Stock1.AskCount[0] <= 0)))
                {
                    adjustAsk = Math.Max(Stock1.Bid, Stock1.Nominal);
                    //////adjustAsk = GetSpreadByIndex(Stock1.SpreadTableCode, adjustAsk, -(I_OppositeSpread - 1), TradeDB1.SpreadTables);
                    adjustAsk = Math.Max(adjustAsk, (decimal)0.01);

                    adjustBid = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Bid, -(I_OppositeSpread - 1), TradeDB1.SpreadTables);
                    //////if (ChkAskSide(Stock1.Bid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, I_QueueSpread, TradeDB1.SpreadTables))
                    if (ChkAskSide(adjustBid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, I_QueueSpread, TradeDB1.SpreadTables))
                        CanSell = true;
                    else
                        CanSell = false;
                }

                //CASE 3 - Ask, Norm exist , No Bid
                else if ((Stock1.Bid <= 0 || (Stock1.BidVol[0] <= 0 && Stock1.BidCount[0] <= 0)) &&
                    (Stock1.Ask > 0 && (Stock1.AskVol[0] > 0 || Stock1.AskCount[0] > 0)))
                {
                    adjustBid = Math.Min(Stock1.Ask, Stock1.Nominal);
                    adjustBid = Math.Max(adjustBid, (decimal)0.01);

                    //adjustAsk = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Ask, -(I_OppositeSpread - 1), TradeDB1.SpreadTables);

                    if (ChkAskSide(adjustBid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, I_QueueSpread, TradeDB1.SpreadTables))
                        CanSell = true;
                    else
                        CanSell = false;
                }

                //CASE 4 - Norm exist , No Bid, Ask
                else if ((Stock1.Bid <= 0 || (Stock1.BidVol[0] <= 0 && Stock1.BidCount[0] <= 0)) &&
                    (Stock1.Ask <= 0 || (Stock1.AskVol[0] <= 0 && Stock1.AskCount[0] <= 0)) && Stock1.Nominal > 0)
                {
                    //adjustAsk = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Nominal, -(I_OppositeSpread - 1), TradeDB1.SpreadTables);
                    //adjustAsk = Math.Max(adjustAsk, 1);
                    //adjustBid = Math.Max(Stock1.Nominal, 1);

                    adjustAsk = Stock1.Nominal;
                    adjustBid = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.Nominal, -(I_OppositeSpread - 1), TradeDB1.SpreadTables);

                    if (ChkAskSide(adjustBid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, I_QueueSpread, TradeDB1.SpreadTables))
                        CanSell = true;
                    else
                        CanSell = false;
                }

                //CASE 5 - No Bid, Ask, Norm
                else if ((Stock1.Bid <= 0 || (Stock1.BidVol[0] <= 0 && Stock1.BidCount[0] <= 0)) &&
                    (Stock1.Ask <= 0 || (Stock1.AskVol[0] <= 0 && Stock1.AskCount[0] <= 0)) && Stock1.Nominal <= 0)
                {
                    if (Stock1.PrevClose > 0)
                    {
                        //adjustAsk = GetSpreadByIndex(Stock1.SpreadTableCode, Stock1.PrevClose, -(I_OppositeSpread - 1), TradeDB1.SpreadTables);
                        adjustAsk = Math.Max(adjustAsk, (decimal)0.01);
                        adjustBid = Math.Max(Stock1.PrevClose, (decimal)0.01);

                        if (ChkAskSide(adjustBid, adjustAsk, OrderPrice, Stock1.SpreadTableCode, I_QueueSpread, TradeDB1.SpreadTables))
                            CanSell = true;
                        else
                            CanSell = false;
                    }
                    else
                        CanSell = false;
                }
                else
                    CanSell = false;
            }
            else
            {
                // Buy sell error
                CanSell = false;
            }
        }

        /*
        private static decimal GetSpreadByIndex_ASHR(int SpTbCode, decimal Price, int index, SpreadTableSet SpreadTable1)
        {
            if (index == 0) return Price;
            if (index < 0 && Price == (decimal)0.01) return Price;

            decimal NewPrice = 0;

            NewPrice = SpreadTable1.GetSpread(SpTbCode, Price, index);
            NewPrice = NewPrice > 0 ? NewPrice /= 1000 : -1;

            return NewPrice;
        }
        */

        private static decimal GetSpreadByIndex(int SpTbCode, decimal Price, int Index, SpreadTableSet SpreadTable1)
        {
            int resultIndex;
            return SpreadTable1[SpTbCode].GetSpread(Price, Index, out resultIndex);

            //if (Index == 0) return Price;
            //if (Index < 0 && Price == (decimal)0.01) return Price;

            //decimal NewPrice = 0;
            
            //NewPrice = SpreadTable1[SpTbCode].GetSpread(Price, Index, out resultIndex);
            //NewPrice = NewPrice > 0 ? NewPrice /= 1000 : -1; 
            
            //return NewPrice;
        }

        private static bool ChkBidSide(decimal Bid, decimal Ask, decimal OrdPrice, int SpTbCode, int QueueSpread, SpreadTableSet SpreadTable1)
        {
            decimal NewPrice = 0;

            if (OrdPrice <= 0 || Bid < 0 || Ask < 0) return false;
            else if (OrdPrice > Ask) return false;
            else if (Ask == OrdPrice) return true;
            else if (Bid == OrdPrice) return true;
            else if (OrdPrice > Bid && Bid <= Ask)
            {
                //NewPrice = SpreadTable1.GetSpread(SpTbCode, Ask, -1);
                NewPrice = GetSpreadByIndex(SpTbCode, Ask, -1, SpreadTable1);
                while (NewPrice >= Bid)
                {
                    if (OrdPrice == NewPrice)
                        return true;
                    else NewPrice = GetSpreadByIndex(SpTbCode, NewPrice, -1, SpreadTable1);
                    //else NewPrice = SpreadTable1.GetSpread(SpTbCode, NewPrice, -1);
                }
                return false;
            }
            else if (OrdPrice < Bid && Bid <= Ask)
            {
                //NewPrice = SpreadTable1.GetSpread(SpTbCode, Bid, -1);
                NewPrice = GetSpreadByIndex(SpTbCode, Bid, -1, SpreadTable1);
                if (OrdPrice == NewPrice) return true;
                else
                {
                    for (int i = 1; i < QueueSpread -1; i++)
                    {
                        NewPrice = GetSpreadByIndex(SpTbCode, NewPrice, -1, SpreadTable1);
                        //NewPrice = SpreadTable1.GetSpread(SpTbCode, NewPrice, -1);
                        if (OrdPrice == NewPrice) return true;
                    }
                }
                return false;
            }
            else
                return false;
        }

        private static bool ChkAskSide(decimal Bid, decimal Ask, decimal OrdPrice, int SpTbCode, int QueueSpread, SpreadTableSet SpreadTable1)
        {
            decimal NewPrice = 0;

            if (OrdPrice <= 0 || Bid < 0 || Ask < 0) return false;
            else if (OrdPrice < Bid) return false;
            else if (Ask == OrdPrice) return true;
            else if (Bid == OrdPrice) return true;
            else if (OrdPrice < Ask && Bid <= Ask)
            {
                //NewPrice = SpreadTable1.GetSpread(SpTbCode, Ask, -1);
                NewPrice = GetSpreadByIndex(SpTbCode, Bid, 1, SpreadTable1);
                while (NewPrice <= Ask)
                {
                    if (OrdPrice == NewPrice) return true;
                    NewPrice = GetSpreadByIndex(SpTbCode, NewPrice, 1, SpreadTable1);
                    //else NewPrice = SpreadTable1.GetSpread(SpTbCode, NewPrice, -1);
                }
                return false;
            }
            else if (OrdPrice > Ask && Bid <= Ask)
            {                
                NewPrice = GetSpreadByIndex(SpTbCode, Ask, 1, SpreadTable1);
                if (OrdPrice == NewPrice)
                    return true;
                else
                {
                    for (int i = 1; i < QueueSpread - 1; i++)
                    {                        
                        NewPrice = GetSpreadByIndex(SpTbCode, NewPrice, 1, SpreadTable1);
                        if (OrdPrice == NewPrice) return true;
                    }
                }
                return false;
            }
            else
                return false;
        }

        public static decimal GetSpreadNoStkPrice(char BuySell, int Index, Stock Stock1, ITradeDB TradeDB1, out bool CanBuy, out bool CanSell)
        {
            decimal stkPrice = 0;
            CanBuy = false;
            CanSell = false;

            if (Stock1 == null || TradeDB1 == null) return -1;

            if (BuySell.ToString().ToUpper().Trim() == "B")            
                stkPrice = CheckSpread(Stock1.Bid, 'X', ' ', Index, Stock1, TradeDB1, out CanBuy, out CanSell);
            else if (BuySell.ToString().ToUpper().Trim() == "S")
                stkPrice = CheckSpread(Stock1.Ask, 'X', ' ', Index, Stock1, TradeDB1, out CanBuy, out CanSell);
            else
                stkPrice = CheckSpread(Stock1.Nominal, 'X', ' ', Index, Stock1, TradeDB1, out CanBuy, out CanSell);

            return stkPrice;
        }

    }
}
