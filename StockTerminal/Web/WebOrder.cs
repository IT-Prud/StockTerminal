using System;
using System.Collections.Generic;
using System.Text;
using TradeDB;

namespace StockTerminal.Web
{
    class WebOrder : Order
    {
        public string shortSell;
        //public bool creditCheckByPass;
        //public string tradingPassword;
        //public string GUID;

        /*

        new protected WebOrder Clone()
        {
            WebOrder webOrder = new WebOrder();
            webOrder = (WebOrder)this.Clone();
            webOrder.shortSell = this.shortSell;
            webOrder.GUID = this.GUID;
            return webOrder;
        }
         */
    }
}
