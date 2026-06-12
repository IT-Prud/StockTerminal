using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using TradeDB;
using StockTerminal.Dataset;
using System.Globalization;
using StockTerminal.Forms;

namespace StockTerminal.Report
{
    public partial class DailyReportDetailsForm : StockTerminal.Forms.BaseForm
    {
        DailyReportDataSet dailyReportDataSet;
        public Utils.Utils.OrderStatusState OS = Utils.Utils.OrderStatusState.All;
        private string AECode;

        public DailyReportDetailsForm(CultureInfo Culture, string PersistString, string AECode)
            : base(Culture, PersistString)
        {
            InitializeComponent();
            this.AECode = AECode;
        }

        private void DailyReportForm_Shown(object sender, EventArgs e)
        {
            this.DailyReportDetails1 = new DailyReportDetails();
            DailyReportDetails1.SetDataSource(dailyReportDataSet);

            //PageMargins margins = DailyReportDetails1.PrintOptions.PageMargins;
            //margins.topMargin = 500;
            //DailyReportDetails1.PrintOptions.ApplyPageMargins(margins);
            this.crystalReportViewer1.ReportSource = this.DailyReportDetails1;
            //this.DailyReportDetails1.PrintOptions.PaperOrientation = CrystalDecisions.Shared.PaperOrientation.Landscape;
            ////this.DailyReportDetails1.PrintOptions.PaperOrientation = CrystalDecisions.Shared.PaperOrientation..DefaultPaperOrientation;
            //this.DailyReportDetails1.PrintOptions.PaperSize = CrystalDecisions.Shared.PaperSize.PaperB5;
            ////this.DailyReportDetails1.PrintOptions.PaperSize = CrystalDecisions.Shared.PaperSize.DefaultPaperSize;
            //this.DailyReportDetails1.PrintOptions.PaperSource = CrystalDecisions.Shared.PaperSource.Upper;
            //this.DailyReportDetails1.PrintOptions.PrinterDuplex = CrystalDecisions.Shared.PrinterDuplex.Default;
        }

        private void DailyReportForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            List<BaseForm> mainFormList = BaseForm.GetFormByFormType(typeof(MainForm));
            if (mainFormList != null && mainFormList.Count > 0 && mainFormList[0] != null)
            {
                MainForm mainForm = (MainForm)mainFormList[0];
                mainForm.TopMost = true;
                mainForm.TopMost = false;
            }
        }

        private void DailyReportDetailsForm_Load(object sender, EventArgs e)
        {

            LayoutLockable = false;
            dailyReportDataSet = new DailyReportDataSet();
            //DataTable orderTableA = dailyReportDataSet.Tables["OrderDataTable"];
            DataTable orderTableA = new DataTable();
            orderTableA.Columns.Add("AccNo", typeof(string));
            orderTableA.Columns.Add("OrderNo", typeof(string));
            orderTableA.Columns.Add("BuySell", typeof(string));
            orderTableA.Columns.Add("ExchangeCode", typeof(string));
            orderTableA.Columns.Add("StkCode", typeof(int));
            orderTableA.Columns.Add("AvgPrice", typeof(string));
            orderTableA.Columns.Add("Qty", typeof(string));
            orderTableA.Columns.Add("OrderPrice", typeof(string));
            orderTableA.Columns.Add("OrderQty", typeof(string));
            orderTableA.Columns.Add("OrderStatus", typeof(string));
            orderTableA.Columns.Add("PlaceDateTime", typeof(string));
            orderTableA.Columns.Add("LastActionPlacedBy", typeof(string));
            orderTableA.Columns.Add("OrderPlacedBy", typeof(string));

            //dailyReportDataSet.DataSetName = "Norm";
            //dailyReportDataSet.Locale = new System.Globalization.CultureInfo("zh-HK");

            List<Order> Orders;
            int buyCount = 0, sellCount = 0, tradeBuyCnt = 0, tradeSellCnt = 0;
            decimal ttlAmt = 0.0M, ttlBuyAmt = 0.0M, ttlSellAmt = 0.0M, ttlOrderAmt = 0.0M, ttlBuyOrderAmt = 0.0M, ttlSellOrderAmt = 0.0M;
            Orders = TradeDB.GetOrderByAECode(AECode);
            foreach (Order ord in Orders)
            {
                if (OS == StockTerminal.Utils.Utils.OrderStatusState.All ||
                    (OS == StockTerminal.Utils.Utils.OrderStatusState.Filled && (ord.Status == (int)Order.OrderStatusEnum.Completed || ord.Status == (int)Order.OrderStatusEnum.PartiallyCompleted)) ||
                    (OS == StockTerminal.Utils.Utils.OrderStatusState.Queue && (ord.Status == (int)Order.OrderStatusEnum.Queue)) ||
                    (OS == StockTerminal.Utils.Utils.OrderStatusState.CancelRejected && (ord.Status == (int)Order.OrderStatusEnum.Cancelled || ord.Status == (int)Order.OrderStatusEnum.RejectedByOG || ord.Status == (int)Order.OrderStatusEnum.RejectedBySupervisor)))
                //if (ord.Deals.Count > 0 || ord.MostUpdateFilled > 0)
                {
                    string side = (ord.Side == 'B') ? GetResxString("Buy") : GetResxString("Sell");
                    int iStkCode = 0;
                    string outputPrice, exChangeCode, currencyCode;

                    exChangeCode = Stock.GetExchangeCode(ord.ExType);
                    switch (ord.ExType)
                    {
                        case ExchangeTypeEnum.HKG:
                        case ExchangeTypeEnum.PMHKG:
                            currencyCode = "HKD";
                            break;

                        case ExchangeTypeEnum.SHG:
                        case ExchangeTypeEnum.SZE:
                            currencyCode = "CNY";
                            break;

                        default:
                            currencyCode = "";
                            break;
                    }

                    decimal amt = Currency.ExchangeToBase(currencyCode, ord.AvgPrice * ord.Filled);
                    decimal buyAmt = Currency.ExchangeToBase(currencyCode, ord.Price * ord.Quantity);
                    if (side == GetResxString("Buy"))
                    {
                        buyCount++;
                        if (ord.Filled > 0)
                            tradeBuyCnt++;
                        ttlBuyAmt += amt;
                        ttlBuyOrderAmt += buyAmt;
                    }
                    else if (side == GetResxString("Sell"))
                    {
                        sellCount++;
                        if (ord.Filled > 0)
                            tradeSellCnt++;
                        ttlSellAmt += amt;
                        ttlSellOrderAmt += buyAmt;
                    }
                    ttlAmt += amt;
                    ttlOrderAmt += buyAmt;
                    int.TryParse(ord.StockCode, out iStkCode);
                    outputPrice = ord.AvgPrice != ord.Price ? "***" + String.Format("{0:0.###}", ord.AvgPrice) : "" + String.Format("{0:0.###}", ord.AvgPrice);
                    string strOrderStatus = GetResxString(Utils.Utils.GetStatusString(ord.Status));
                    orderTableA.Rows.Add(new string[] { ord.AccountNo, ord.OrderNo.ToString(), side, exChangeCode, iStkCode.ToString(), outputPrice, String.Format("{0:N0}", ord.Filled), String.Format("{0:0.###}", ord.Price), String.Format("{0:N0}", ord.Quantity), strOrderStatus, ord.PlaceDateTime.ToString("HH:mm:ss"), ord.ActionPlacedBy, ord.OrderPlacedBy });
                }
            }
            DataRow[] rows = orderTableA.Select("", "BuySell DESC, ExchangeCode ASC, StkCode ASC, AvgPrice ASC");
            DataTable orderTableB = dailyReportDataSet.Tables["OrderOrderDetailsDataTableDataTable"];
            foreach (DataRow row in rows)
                orderTableB.Rows.Add(row[0].ToString(), row[1].ToString(), row[2].ToString(), row[3].ToString(), Convert.ToInt32(row[4].ToString()), row[5].ToString(),
                    row[6].ToString(), row[7].ToString(), row[8].ToString(), row[9].ToString(), row[10].ToString(), row[11].ToString(), row[12].ToString());

            DataTable generalTable = dailyReportDataSet.Tables["GeneralDataTable"];
            List<AccountExecutive> aeList = GetAEByCode(AECode.ToString());
            string AEName = (aeList != null && aeList.Count > 0) ? aeList[0].Name : "";
            generalTable.Rows.Add(new string[] { AECode.ToString(), AEName, (buyCount + sellCount).ToString(), (buyCount + sellCount).ToString(), buyCount.ToString(), sellCount.ToString(), 
                String.Format("{0:N0}", ttlAmt), String.Format("{0:N0}", ttlBuyAmt), String.Format("{0:N0}", ttlSellAmt), GetResxString("AccNoHead"), GetResxString("OrderNoHead"), 
                GetResxString("BuySellHead"), GetResxString("StkCodeHead"), GetResxString("AvgPriceHead"), GetResxString("QtyHead"), GetResxString("DailyTradeRptTittle"), 
                GetResxString("Page"), GetResxString("OrderPriceHead"), GetResxString("OrderQtyHead"), GetResxString("OrderStatusHead"), 
                String.Format("{0:N0}", ttlOrderAmt), String.Format("{0:N0}", ttlBuyOrderAmt), String.Format("{0:N0}", ttlSellOrderAmt), GetResxString("OrderPlaceDateTimeHead"), 
                tradeBuyCnt.ToString(), tradeSellCnt.ToString(), (tradeSellCnt + tradeBuyCnt).ToString(), GetResxString("LastActionPlacedByHead"), GetResxString("OrderPlacedByHead"), 
                TradeDB.ServerTime.ToString("d/M/yyyy    HH:mm:ss"), GetResxString("ExchangeCodeHead")});
        }
    }
}
