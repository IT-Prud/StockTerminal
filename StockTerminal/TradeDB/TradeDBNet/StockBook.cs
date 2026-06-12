using System;
using System.Collections.Generic;
using System.Text;

namespace TradeDB.Net
{
    class StockBook
    {
        // Key: e.g. HKG~941, SHG~600004
        private readonly Dictionary<string, Stock> ByStockCode = new Dictionary<string, Stock>(50);
        // Key: HKG~MAIN, SHG~MAIN
        private readonly Dictionary<string, Dictionary<string, Stock>> ByMarketCode = new Dictionary<string, Dictionary<string, Stock>>(50);
        private readonly object AccessMutex = new object();

        public Stock AddUpdateStatic(Stock TheStock)
        {
            if (TheStock != null)
            {
                lock (AccessMutex)
                {
                    return AddUpdateStockStaticInternal(TheStock);
                }
            }

            return null;
        }

        public Stock AddUpdateDynamic(Stock TheStock)
        {
            if (TheStock != null)
            {
                lock (AccessMutex)
                {
                    return AddUpdateStockDynamicInternal(TheStock);
                }
            }

            return null;
        }

        // Code = e.g. 941
        public Stock GetByStockSignature(string Code, ExchangeTypeEnum ExchangeType)
        {
            return GetByStockSignature(Stock.GetSignature(ExchangeType, Code));
        }

        // Code = HKG~941
        public Stock GetByStockSignature(string StockSignature)
        {
            Stock stock = null;

            if (StockSignature != null)
            {
                lock (AccessMutex)
                {
                    ByStockCode.TryGetValue(StockSignature, out stock);

                    if (stock != null && stock.StaticVersion != 0 && stock.DynamicVersion != 0)
                        stock = (Stock)stock.Clone();
                }
            }

            return stock;
        }

        public Dictionary<string, Stock> GetByStockSignature(ICollection<string> StockSignatures)
        {
            Stock stock = null;
            Dictionary<string, Stock> stockDict = null;

            if (StockSignatures != null && StockSignatures.Count > 0)
            {
                stockDict = new Dictionary<string, Stock>(StockSignatures.Count);

                lock (AccessMutex)
                {
                    foreach (string stockSignature in StockSignatures)
                    {
                        if (stockSignature != null &&
                            ByStockCode.TryGetValue(stockSignature, out stock) &&
                            stock != null && stock.StockSignature != null &&
                            stock.StaticVersion != 0 && stock.DynamicVersion != 0)
                        {
                            stockDict[stock.StockSignature] = stock.Clone();
                        }
                    }
                }
            }

            return stockDict;
        }

        public List<Stock> GetByMarketCode(string MarketCode, ExchangeTypeEnum exType)
        {
            List<Stock> stocks = new List<Stock>(0);
            Dictionary<string, Stock> stockDict = null;

            if (MarketCode == null || MarketCode.Trim().Length == 0)
                return stocks;

            if (exType == ExchangeTypeEnum.SZE)
                MarketCode = "SZE~" + MarketCode;
            else if (exType == ExchangeTypeEnum.SHG)
                MarketCode = "SHG~" + MarketCode;
            else
                MarketCode = "HKG~" + MarketCode;

            lock (AccessMutex)
            {
                if (ByMarketCode.TryGetValue(MarketCode, out stockDict) && stockDict != null)
                {
                    stocks = new List<Stock>(stockDict.Count);

                    foreach (Stock stock in stockDict.Values)
                    {
                        if (stock.StaticVersion != 0 && stock.DynamicVersion != 0)
                            stocks.Add(stock.Clone());
                    }
                }
            }

            return stocks;
        }

        public void Clear()
        {
            lock (AccessMutex)
            {
                ByStockCode.Clear();
                ByMarketCode.Clear();
            }
        }

        private Stock AddUpdateStockStaticInternal(Stock NewStock)
        {
            Stock theStock = null;
            string marketSignatureOld = null, marketSignatureNew = null;
            Dictionary<string, Stock> stockDict = null;

            // ByStockCode
            if (ByStockCode.TryGetValue(NewStock.StockSignature, out theStock))
            {
                marketSignatureOld = Stock.GetMarketSignature(theStock.ExchangeType, theStock.MarketBelong != null ? theStock.MarketBelong.Code : null);
                marketSignatureNew = Stock.GetMarketSignature(NewStock.ExchangeType, NewStock.MarketBelong != null ? NewStock.MarketBelong.Code : null);

                theStock.UpdateStatic(NewStock);

                if (marketSignatureOld != null && marketSignatureOld != marketSignatureNew && ByMarketCode.TryGetValue(marketSignatureOld, out stockDict))
                {
                    try { stockDict.Remove(theStock.Code); }
                    catch { }
                    if (stockDict.Count <= 0) ByMarketCode.Remove(marketSignatureOld);
                }

                if (marketSignatureNew != null && marketSignatureNew != marketSignatureOld)
                {
                    if (!ByMarketCode.TryGetValue(marketSignatureNew, out stockDict))
                    {
                        stockDict = new Dictionary<string, Stock>(10);
                        ByMarketCode.Add(marketSignatureNew, stockDict);
                    }
                    stockDict.Add(theStock.Code, theStock);
                }
            }
            else
            {
                NewStock.UpdateStaticVersion();

                theStock = (Stock)NewStock.Clone();
                ByStockCode.Add(theStock.StockSignature, theStock);

                marketSignatureNew = Stock.GetMarketSignature(NewStock.ExchangeType, NewStock.MarketBelong != null ? NewStock.MarketBelong.Code : null);

                if (!ByMarketCode.TryGetValue(marketSignatureNew, out stockDict))
                {
                    stockDict = new Dictionary<string, Stock>(10);
                    ByMarketCode.Add(marketSignatureNew, stockDict);
                }
                stockDict.Add(theStock.Code, theStock);
            }

            return NewStock;
        }

        private Stock AddUpdateStockDynamicInternal(Stock NewStock)
        {
            Stock theStock = null;
            string StockCodeKey = null;

            StockCodeKey = NewStock.StockSignature.Trim().ToUpper();

            // ByStockCode
            if (ByStockCode.TryGetValue(StockCodeKey, out theStock))
            {
                theStock.UpdateDynamic(NewStock);
            }
            else
            {
                NewStock.UpdateDynamicVersion();

                theStock = (Stock)NewStock.Clone();
                ByStockCode.Add(theStock.StockSignature, theStock);
            }

            return NewStock;
        }
    }
}
