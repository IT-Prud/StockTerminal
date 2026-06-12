using System;
using System.Collections.Generic;
using System.Text;

namespace TradeDB
{
    public class DiscretionProductGroupsTable
    {
        private static Dictionary<string, Dictionary<string, int>> ExchangeCodeDict = new Dictionary<string, Dictionary<string, int>>();
        private static Dictionary<string, Dictionary<string, int>> MarketCodeDict = new Dictionary<string, Dictionary<string, int>>();
        private static Dictionary<string, Dictionary<string, int>> InstrumentCodeDict = new Dictionary<string, Dictionary<string, int>>();
        private static Dictionary<string, Dictionary<string, int>> ProductCodeDict = new Dictionary<string, Dictionary<string, int>>();
        private static Dictionary<string, Dictionary<string, int>> StockCodeDict = new Dictionary<string, Dictionary<string, int>>();

        private static object DiscretionGroupTableDictLock = new object();

        //CCK00549905090306GID=ALL|GN=All|EXC=|MKCD=|INST=|STKC=|PDT=67
        //CCK00829905090306GID=HKGETFLIP|GN=ETF / LIP|EXC=HKG|MKCD=|INST=|STKC=|PDT=5,10,16,17,1874
        //CCK00789905090306GID=HKGGEMEQTY|GN=Equity GEM|EXC=HKG|MKCD=GEM|INST=EQTY|STKC=|PDT=2D
        //CCK00789905090306GID=HKGGOVBOND|GN=HK Gov Bond|EXC=HKG|MKCD=|INST=|STKC=04239|PDT= 58
        //CCK00819905090306GID=HKGMAINEQTY|GN=Equity Main|EXC=HKG|MKCD=MAIN|INST=EQTY|STKC=|PDT=49
        //CCK00639905090306GID=HKGREIT|GN=REIT|EXC=HKG|MKCD=|INST=|STKC=|PDT=85F
        //CCK00779905090306GID=HKGWRNT|GN=Warrants / CBBC|EXC=HKG|MKCD=|INST=WRNT|STKC=|PDT=76
        public static void AddUpdate(DiscretionProductGroup dpg)
        {
            lock (DiscretionGroupTableDictLock)
            {
                InitDict(ExchangeCodeDict, dpg.ExchangeCode, dpg.GroupID);
                InitDict(MarketCodeDict, dpg.MarketCode, dpg.GroupID);
                InitDict(InstrumentCodeDict, dpg.InstrumentType, dpg.GroupID);
                InitDict(ProductCodeDict, dpg.ProductTypes, dpg.GroupID);
                InitDict(StockCodeDict, dpg.StockCode, dpg.GroupID);
            }
        }

        private static void InitDict(Dictionary<string, Dictionary<string, int>> dict, string key1, string key2)
        {
            string[] items = key1.Split(',');
            foreach (string item in items)
            {
                if (dict.ContainsKey(item))
                {
                    if (dict[item].ContainsKey(key2))
                        dict[item][key2] = 0;
                    else
                        dict[item].Add(key2, 0);
                }
                else
                {
                    Dictionary<string, int> grpIDDict = new Dictionary<string, int>();
                    grpIDDict.Add(key2, 0);
                    dict.Add(item, grpIDDict);
                }
            }
        }

        public static bool IsDiscretion(Stock stock, Dictionary<string, byte> DiscretionGrpID)
        {
            int iStockCode;
            int.TryParse(stock.Code, out iStockCode);
            foreach (KeyValuePair<string, byte> kvp in DiscretionGrpID)
            {
                if ((ExchangeCodeDict.ContainsKey(Stock.GetExchangeCode(stock.ExchangeType)) && ExchangeCodeDict[Stock.GetExchangeCode(stock.ExchangeType)].ContainsKey(kvp.Key)) ||
                    (ExchangeCodeDict.ContainsKey("") && ExchangeCodeDict[""].ContainsKey(kvp.Key)))
                    if ((MarketCodeDict.ContainsKey(stock.MarketCode) && MarketCodeDict[stock.MarketCode].ContainsKey(kvp.Key)) || 
                        (MarketCodeDict.ContainsKey("") && MarketCodeDict[""].ContainsKey(kvp.Key)))
                        if ((InstrumentCodeDict.ContainsKey(stock.InstrumentType) && InstrumentCodeDict[stock.InstrumentType].ContainsKey(kvp.Key)) ||
                            (InstrumentCodeDict.ContainsKey("") && InstrumentCodeDict[""].ContainsKey(kvp.Key)))
                            if ((ProductCodeDict.ContainsKey(stock.ProductType + "") && ProductCodeDict[stock.ProductType + ""].ContainsKey(kvp.Key)) ||
                                (ProductCodeDict.ContainsKey("") && ProductCodeDict[""].ContainsKey(kvp.Key)))
                                if ((StockCodeDict.ContainsKey(string.Format("{0:00000}", iStockCode)) && StockCodeDict[string.Format("{0:00000}", iStockCode)].ContainsKey(kvp.Key)) ||
                                    (StockCodeDict.ContainsKey("") && StockCodeDict[""].ContainsKey(kvp.Key)))
                                    return false;
            }
            return true;
        }

    }

    public class DiscretionProductGroup
    {
        public string GroupID, GroupName, ExchangeCode, MarketCode, InstrumentType, StockCode, ProductTypes;
    }
}
