using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Data.OleDb;
using System.Configuration;
using System.Collections.Generic;
using System.Globalization;
using WeifenLuo.WinFormsUI.Docking;
using StockTerminal.Dataset;
using TradeDB;
using StockTerminal.Utils;
using System.Resources;
using System.Reflection;
using System.Threading;
using Utils;

namespace StockTerminal.Forms
{
    public partial class OrderBookForm : StockTerminal.Forms.BaseForm
    {
        #region "Variables"
        // General
        private readonly object GridMutex = new object();
        DataGridView currDGV;      // data grid that's currently showing
        //private object GridClickMutex = new object();
        //double ttlTime = 0.0;

        // Column related
        readonly int TOTAL_COLUMN = 17;
        private readonly int iOrderNoColIdx = 0, iOrderPriceColIdx = 6, iOrderAvgPriceColIdx = 8, iOrderNoSortColIdx = 14;
        readonly string strOrderCol_OrderNo = "Orderno", strOrderCol_AccNo = "Account No", strOrderCol_AECode = "AECode", strOrderCol_OrderSide = "Order_Side";
        readonly string strOrderCol_StkCode = "StockCode", strOrderCol_ExCode = "ExCode", strOrderCol_OrderPrice = "Order_Price", strOrderCol_OrderQty = "Order_Qty";
        readonly string strOrderCol_AvgPrice = "avgPrice", strOrderCol_FilledQty = "Filled_Qty", strOrderCol_OrderStatus = "OrderStatus", strOrderCol_LastAction = "LastAction";
        readonly string strOrderCol_DelBtn = "DelButton", strOrderCol_OrderStatusCode = "OrderStatusCode", strOrderCol_OrderType = "OrderType";
        readonly string strOrderCol_OrderNoSort = "OrderNoSort", strOrderCol_ReplyChkBox = "ReplyChkBox", strOrderCol_StatusMessage = "StatusMessage";
        readonly string strOrderCol_OGMessage = "OGMessage", strOrderCol_Memo = "Memo", strOrderCol_AAmendBtnUp = "+1s", strOrderCol_AAmendBtnDown = "-1s";

        // AAmend
        readonly bool AutoAmend = true;        
        private bool IsAutoAmend = false;
        //readonly bool AutoClosePos = true;
        Order AAmendOrder = null;
        private int intAAmendSpread = 0;

        // Amend form related
        DockPanel dockPanelMain;
        AmendForm amForm = null;

        Dictionary<string, string> GridFirstActionRefDict = new Dictionary<string, string>(5);
        Dictionary<string, DataGridViewRow> AllOrderGridDict = new Dictionary<string, DataGridViewRow>(20);
        Dictionary<string, DataGridViewRow> RepliedOrderGridDict = new Dictionary<string, DataGridViewRow>(20);
        Dictionary<string, DataGridViewRow> PendOrderGridDict = new Dictionary<string, DataGridViewRow>(20);
        Dictionary<string, DataGridViewRow> CompletedOrderGridDict = new Dictionary<string, DataGridViewRow>(20);

        // OrderBook Grid Sort related
        private int[] OrderGridSortedColumnIndexList = {0, 0, 0, 0, 0};
        private SortOrder[] OrderGridSortedOrderList = { SortOrder.Descending, SortOrder.Descending, SortOrder.Descending, SortOrder.Descending, SortOrder.Descending };
        private bool OrderGridSelectRowAfterSort = false;
        private string selectRowOrderNoSort = "";        

        // Cell blinking related
        const int RestoreNonBlinkCell_Interval = 1000;
        class CellData
        {
            public int currTabIdx = 3;
            public string ColumnName;
            public string orderNo;
            public DateTime Time;
            public DataGridView dataGridView;
            public bool painted = false;
            public bool highlighted = false;
        }
        private Thread restoreNonBlinkThread = null;
        private List<CellData> blinkCellData = null;
        private volatile bool threadAlive = true;
        string[] prevSelectedOrderNo = {"","","",""};

        // Search related
        public bool SearchEnabled = false;
        public delegate void SearchChangedDelegate(BaseForm SourceForm, SearchOption Option);
        public SearchChangedDelegate SearchOptionChanged = null;
        public class SearchOption
        {
            public string AECode;
            public string AccNo;
            public string StkCode;
        }

        private EventForm.EventDoubleClickedDelegate pEventDoubleClickedDelegate = null;
        private Dictionary<BaseForm, SystemEvent> pSystemEventDict = new Dictionary<BaseForm, SystemEvent>(10);

        // Deal grid related
        private string selectedStockCode = "", selectedExchangeCode = "", selectedOrderSide = "";
        private Stock selectedDealStock = null;
        private Order DealGridArrivedOrder = null;
        private int DealGridSortedColumnIndex = 6;
        private SortOrder DealGridSortedOrder = SortOrder.Descending;

        private Stock CurrStock = null;

        #endregion

        public OrderBookForm() : this(null, null, null)
        {
            //InitializeComponent();
        }

        public OrderBookForm(CultureInfo Culture, string PersistString, DockPanel dockPanelMain) : base(Culture, PersistString)
        {
            InitializeComponent();
            Utils.Utils.EnableDoubleBuffered(dataGridViewAllOrder);
            Utils.Utils.EnableDoubleBuffered(dataGridViewRepliedOrder);
            Utils.Utils.EnableDoubleBuffered(dataGridViewCompletedOrder);
            Utils.Utils.EnableDoubleBuffered(dataGridViewPendOrder);

            pEventDoubleClickedDelegate = new EventForm.EventDoubleClickedDelegate(OnEventFormDoubleClicked);
            AddEventDoubleClickedDelegate();

            this.dockPanelMain = dockPanelMain;
            blinkCellData = new List<CellData>();
            ListenFormListChange();
        }

        protected override void OnCultureChange(CultureInfo Culture)
        {
            Refresh_AllOrderGridHeader(false);
            ShowTradeStatus(selectedDealStock);
            OnOrderStatus(TradeDB.GetOrder());
            Refresh_DealGridHeader();
            UpdatePanelChkBoxLocation();
        }

        private void SetCurrentStockCode(string NewStockCode, ExchangeTypeEnum ete)
        {
            NewStockCode = NewStockCode == null ? "" : NewStockCode.Trim();
            if (NewStockCode.Trim() == "") return;
            
            ExchangeTypeEnum curr_ete = ExchangeTypeEnum.HKG;
            if (CurrStock != null)
            {
                curr_ete = CurrStock.ExchangeType;
                UnListenStock(new List<string> { Stock.GetSignature(curr_ete, CurrStock.Code) });
            }
            CurrStock = null;

            try
            {                
                //ListenStock(new List<string> { Stock.GetSignature(ete, NewStockCode) });
                GetStock(new List<string> { Stock.GetSignature(ete, NewStockCode) });
            }
            catch
            { }
        }

        private void DeleteCurrentStockCodeAAmend()
        {
            ExchangeTypeEnum ete = ExchangeTypeEnum.HKG;
            if (CurrStock != null) UnListenStock(new List<string> { Stock.GetSignature(ete, CurrStock.Code) });
            CurrStock = null;
            intAAmendSpread = 0;
            AAmendOrder = null;
        }

        protected override void OnOrderStatus(Order TheOrder)
        {
			// log orders details from Subscription class
            if (TheOrder != null)
			{
                if (TheOrder.StatusMessage == null) TheOrder.StatusMessage = "";
                if (TheOrder.OGMessage == null) TheOrder.OGMessage = "";
                if (TheOrder.LastAction == null) TheOrder.LastAction = "";

                AppendLog("Order Arrive", Environment.NewLine + "OnOrderStatus Single Order No.: " + TheOrder.OrderNo + "  OrderNoSort: " + TheOrder.OrderNoSort +
					"  AccountNo: " + TheOrder.AccountNo + "  AECode: " + TheOrder.AECode + "  StockCode: " + TheOrder.StockCode + "  Side: " + TheOrder.Side +
				"  Price: " + TheOrder.Price + "  Quantity: " + TheOrder.Quantity + "  OrderType: " + TheOrder.OrderType + 
				"  AvgPrice: " + TheOrder.AvgPrice + "  Filled: " + TheOrder.Filled +
				"  Status: " + TheOrder.Status + "  StatusMessage: " + TheOrder.StatusMessage + "  OGMessage: " + TheOrder.OGMessage +
				"  LastAction: " + TheOrder.LastAction, "OrderBookForm", false);
			}

            //DateTime Time = DateTime.Now;
            RefreshGrid(dataGridViewAllOrder, TheOrder);
            RefreshGrid(dataGridViewRepliedOrder, TheOrder);
            RefreshGrid(dataGridViewCompletedOrder, TheOrder);
            RefreshGrid(dataGridViewPendOrder, TheOrder);
            RefreshDealGrid(TheOrder);
            //TimeSpan elapsed = DateTime.Now - Time;
            //ttlTime += elapsed.TotalMilliseconds;
            //Console.WriteLine("OnOrderStatus: ttlTime: " + ttlTime + " elapsed: " + elapsed.TotalMilliseconds + " Count: " + Orders.Count);
            ////MessageBox.Show("OnOrderStatus: " + elapsed.TotalMilliseconds + "ms\n" + "Count: " + Orders.Count);
        }

        protected void OnOrderStatus(List<Order> Orders)
        {
            if (Orders == null)
                return;

            // log orders details from Subscription class
            foreach (Order TheOrder in Orders)
            {
                if (TheOrder != null)
                {
                    if (TheOrder.StatusMessage == null) TheOrder.StatusMessage = "";
                    if (TheOrder.OGMessage == null) TheOrder.OGMessage = "";
                    if (TheOrder.LastAction == null) TheOrder.LastAction = "";

                    AppendLog("Order Arrive", Environment.NewLine + "OnOrderStatus List Order No.: " + TheOrder.OrderNo + "  OrderNoSort: " + TheOrder.OrderNoSort +
                    "  AccountNo: " + TheOrder.AccountNo + "  AECode: " + TheOrder.AECode + "  StockCode: " + TheOrder.StockCode + "  Side: " + TheOrder.Side +
                    "  Price: " + TheOrder.Price + "  Quantity: " + TheOrder.Quantity + "  OrderType: " + TheOrder.OrderType +
                    "  AvgPrice: " + TheOrder.AvgPrice + "  Filled: " + TheOrder.Filled + "  Status: " + TheOrder.Status + "  StatusMessage: " + TheOrder.StatusMessage + "  OGMessage: " + TheOrder.OGMessage +
                    "  LastAction: " + TheOrder.LastAction, "OrderBookForm", false);
                }
            }

            //DateTime Time = DateTime.Now;
            RefreshGrid(dataGridViewAllOrder, Orders);
            RefreshGrid(dataGridViewRepliedOrder, Orders);
            RefreshGrid(dataGridViewCompletedOrder, Orders);
            RefreshGrid(dataGridViewPendOrder, Orders);
            //TimeSpan elapsed = DateTime.Now - Time;
            //ttlTime += elapsed.TotalMilliseconds;
            //Console.WriteLine("OnOrderStatus: ttlTime: " + ttlTime + " elapsed: " + elapsed.TotalMilliseconds + " Count: " + Orders.Count);
            ////MessageBox.Show("OnOrderStatus: " + elapsed.TotalMilliseconds + "ms\n" + "Count: " + Orders.Count);
        }

        private void tabControlOrders_SelectedIndexChanged(object sender, EventArgs e)
        {
            ClearStockSrchLabel();
            lock (GridMutex)
            {
                switch (tabControlOrders.SelectedIndex)
                {
                    case (0):
                        currDGV = dataGridViewPendOrder;
                        break;
                    case (1):
                        currDGV = dataGridViewCompletedOrder;
                        break;
                    case (2):
                        if (TradeDB.UserType == UserTypeEnum.AE)
                            currDGV = dataGridViewRepliedOrder;
                        else // no need to have reply tab in client edition
                            currDGV = dataGridViewAllOrder;
                        break;
                    case (3):
                        currDGV = dataGridViewAllOrder;
                        break;
                }
            }
            RefreshGrid(currDGV, TradeDB.GetOrder());
            SortGrid();
            lock (GridMutex)
            {
                if (checkBoxShowInternetOrders.Checked || checkBoxShowAEOrders.Checked)
                    currDGV.Columns[strOrderCol_Memo].HeaderCell.Style.BackColor = Color.Lime;
                else
                    currDGV.Columns[strOrderCol_Memo].HeaderCell.Style.BackColor = currDGV.Columns[strOrderCol_Memo].DefaultCellStyle.BackColor;
                if (comboBoxAE != null && checkBoxEnableSrch.Checked && labelAETarget.Text.Trim() != "")
                    currDGV.Columns[strOrderCol_AECode].HeaderCell.Style.BackColor = Color.Lime;
                else
                    currDGV.Columns[strOrderCol_AECode].HeaderCell.Style.BackColor = SystemColors.Control;
                if (comboBoxAccount != null && checkBoxEnableSrch.Checked && labelAccTarget.Text.Trim() != "")
                    currDGV.Columns[strOrderCol_AccNo].HeaderCell.Style.BackColor = Color.Lime;
                else
                    currDGV.Columns[strOrderCol_AccNo].HeaderCell.Style.BackColor = SystemColors.Control;
            }

            ClearDealGrid();
        }

        # region "Form Load, Shown & Closing"

        private void OrderBookForm_Shown(object sender, EventArgs e)
        {
            //DateTime Time = DateTime.Now;
            Refresh_AllOrderGridHeader(true);
            Init_DealGrid();
            labelCurrency.Text = "";
            ListenOrderStatus(new List<string> { "" });
            currDGV = dataGridViewAllOrder;
            OnOrderStatus(TradeDB.GetOrder());
            if (TradeDB.UserType == UserTypeEnum.Client) // no need to have reply tab if not AE edition
            {
                tabControlOrders.TabPages.Remove(tabPage3);
                labelTransacChargeVal.Visible = false;
                buttonGetTransacCharge.Visible = false;
                checkBoxShowAEOrders.Visible = false;
                checkBoxShowInternetOrders.Visible = false;
                checkBoxShowSearch.Visible = false;
            }

            // Start thread that restore the cells to the non blink state
            restoreNonBlinkThread = new Thread(new ThreadStart(RestoreNonBlinkThreadFunc));
            restoreNonBlinkThread.IsBackground = true;
            restoreNonBlinkThread.Start();

            int dealGridWidth = dataGridViewDeal.Width;
            int PanelBottomHeight = panelBottom.Height;
            bool showDealGrid = false;
            if (LocalFormSettings["DealGridWidth"] != null && int.TryParse(LocalFormSettings["DealGridWidth"], out dealGridWidth))
                    dataGridViewDeal.Width = dealGridWidth;
            if (LocalFormSettings["PanelBottomHeight"] != null && int.TryParse(LocalFormSettings["PanelBottomHeight"], out PanelBottomHeight))
                panelBottom.Height = PanelBottomHeight;
            Boolean.TryParse(LocalFormSettings["ShowDealGrid"], out showDealGrid);
            checkBoxShowDeal.Checked = showDealGrid;
            HideShowDealGrid(showDealGrid);

            if (TradeDB.UserType == UserTypeEnum.AE)
            {
                labelBuy.BackColor = Color.FromArgb(206, 235, 244);
                labelBuyTtlQty.BackColor = Color.FromArgb(206, 235, 244);
                labelBuyFilled.BackColor = Color.FromArgb(206, 235, 244);
                labelBuyAvgPrice.BackColor = Color.FromArgb(206, 235, 244);
                labelAETarget.Text = LocalFormSettings["SearchAETaget"];
                labelAccTarget.Text = LocalFormSettings["SearchAccountTaget"];
                checkBoxEnableSrch.Checked = false;
                EnableSearch();
                comboBoxAE.AutoCompleteSource = AutoCompleteSource.ListItems;
                //comboBoxAE.AutoCompleteMode = AutoCompleteMode.Suggest;
                comboBoxAE.Sorted = true;
                comboBoxAccount.AutoCompleteMode = AutoCompleteMode.Suggest;
                comboBoxAccount.AutoCompleteSource = AutoCompleteSource.None;
                comboBoxAccount.Sorted = true;
                comboBoxShowEx.Sorted = true;
                comboBoxShowEx.Items.AddRange(new string[] { "ALL", Stock.GetExchangeCode(ExchangeTypeEnum.HKG), Stock.GetExchangeCode(ExchangeTypeEnum.SHG), Stock.GetExchangeCode(ExchangeTypeEnum.SZE) });
                comboBoxShowEx.SelectedIndex = 0;
            }
            else if (TradeDB.UserType == UserTypeEnum.Client)
            {
                checkBoxEnableSrch.Checked = false;
                myGroupBoxSrch.Visible = false;
            }
            Refresh_AllOrderGridHeader(false);
            panelMiddle.BringToFront();
            panelChkBox.BringToFront();
            UpdatePanelChkBoxLocation();
            //TimeSpan elapsed = DateTime.Now - Time;
            //ttlTime += elapsed.TotalMilliseconds;
            //MessageBox.Show("OrderBookForm_Load: " + elapsed.TotalMilliseconds + "ms");
        }

        private void OrderBookForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            RemoveEventDoubleClickedDelegate();
            // terminate thread
            threadAlive = false;
            if (restoreNonBlinkThread != null)
            {
                restoreNonBlinkThread.Join(RestoreNonBlinkCell_Interval + 100);
                if (restoreNonBlinkThread.IsAlive)
                    restoreNonBlinkThread.Abort();
                SortGridTimer.Stop();
                SortGridTimer.Dispose();
            }
        }

        #endregion

        # region "Highlight, Restore Row Color"

        private void RestoreNonBlinkThreadFunc()
        {
            while (threadAlive)
            {
                if (blinkCellData.Count > 0)
                {
                    // Make a copy to avoid invalid operation exception while iterating through the map
                    List<CellData> tempBlinkData;
                    lock (blinkCellData)
                    {
                        tempBlinkData = new List<CellData>(blinkCellData);
                    }

                    foreach (CellData data in tempBlinkData)
                    {
                        TimeSpan elapsed = DateTime.Now - data.Time;
                        if (elapsed.TotalMilliseconds < 2000.0 || data.painted == false) // 500 is the Blink delay
                            break;
                        if (data.dataGridView.IsDisposed || data.orderNo == null)
                        {
                            lock (blinkCellData)
                            {
                                blinkCellData.Remove(data);
                            }
                            return;
                        }

                        //data.dataGridView.Invoke((MethodInvoker)delegate()
                        //{
                            // restore the default background colour
                            lock (GridMutex)
                            {
                                DataGridViewRow row = null;
                                if (!GetGridDict(data.dataGridView).TryGetValue(data.orderNo, out row))
                                    row = GetRow(data.dataGridView, data.orderNo);
                                if (row != null && row.Index >= 0)
                                {
                                    if (data.orderNo == prevSelectedOrderNo[data.currTabIdx] && data.highlighted)
                                    {
                                        row.Cells[data.ColumnName].Style.BackColor = Color.DarkBlue;
                                        row.Cells[data.ColumnName].Style.ForeColor = Color.White;
                                    }
                                    else
                                    {
                                        row.Cells[data.ColumnName].Style.BackColor = data.dataGridView.Columns[data.ColumnName].DefaultCellStyle.BackColor;
                                        row.Cells[data.ColumnName].Style.ForeColor = data.dataGridView.Columns[data.ColumnName].DefaultCellStyle.ForeColor;
                                    }
                                }
                            }
                        //});

                        lock (blinkCellData)
                        {
                            blinkCellData.Remove(data);
                        }
                    }
                }
                Thread.Sleep(RestoreNonBlinkCell_Interval);     // without this, CPU load will be very high
            }
            if (blinkCellData.Count > 0)
                blinkCellData.Clear();
        }

        private void RestoreRowColor(DataGridViewRow row)
        {
            if (row == null || row.Index < 0)
                return;

            if (GetResxString("Buy") == row.Cells[strOrderCol_OrderSide].Value.ToString())
            {
                for (int i = 1; i < row.Cells.Count; i++)
                {
                    row.Cells[i].Style.BackColor = Color.FromArgb(206, 235, 244);
                    row.Cells[i].Style.ForeColor = Color.Black;
                }
            }
            else if (GetResxString("Sell") == row.Cells[strOrderCol_OrderSide].Value.ToString())
            {
                for (int i = 1; i < row.Cells.Count; i++)
                {
                    row.Cells[i].Style.BackColor = Color.LightPink;
                    row.Cells[i].Style.ForeColor = Color.Black;
                }
            }
        }

        # endregion

        # region "Update Order Grid"

        private void RefreshGrid(DataGridView DataGridView1, Order ord)
        {
            DataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;    // performance reason

            // tradeDB 2nd reply
            string strGrid1stActionRef;
            if (GridFirstActionRefDict.TryGetValue(DataGridView1.Name, out strGrid1stActionRef) && strGrid1stActionRef != null && ord.FirstActionRef == strGrid1stActionRef &&
                DataGridView1 != dataGridViewCompletedOrder)
            {
                lock (GridMutex)
                {
                    DataGridViewRow row;
                    if (GetGridDict(DataGridView1).TryGetValue("z", out row) && row != null && row.Index >= 0)
                    {
                        // Remove the "Sending" order row
                        DataGridView1.Rows.Remove(row);
                        GetGridDict(DataGridView1).Remove("z");
                        GridFirstActionRefDict[DataGridView1.Name] = "";
                    }
                }
            }
            // tradeDB 1st reply - status = "Sending" 9001 with orderNoSort = "z"
            if (ord.OrderNo == 0)
                GridFirstActionRefDict[DataGridView1.Name] = ord.FirstActionRef;

            // to prevent error orders
            int iStkCode;
            if (ord.OrderNoSort == null || ord.OrderNoSort.Trim().Length <= 0 || ord.AccountNo == null || (ord.Side != 'B' && ord.Side != 'A') || ord.Side.ToString().Trim() == "" || ord.StockCode == null || ord.StockCode.ToString().Trim() == "" || !int.TryParse(ord.StockCode, out iStkCode))
            {
                AppendLog("Order Arrive", Environment.NewLine + "Invalid!!! Single Order No.: " + ord.OrderNo + " OrderNoSort: " + (ord.OrderNoSort == null ? "" : ord.OrderNoSort) +
                "  AccountNo: " + (ord.AccountNo == null ? "" : ord.OrderNoSort) + "  StockCode: " + (ord.AccountNo == null ? "" : ord.StockCode) + 
                "  Side: " + ord.Side + "  Price: " + ord.Price + "  Quantity: " + ord.Quantity + "  OrderType: " + ord.OrderType +
                "  AvgPrice: " + ord.AvgPrice + "  Filled: " + ord.Filled +
                "  Status: " + ord.Status + "  StatusMessage: " + ord.StatusMessage + "  OGMessage: " + ord.OGMessage +
                "  LastAction: " + ord.LastAction, "OrderBookForm", false);
                return;
            }

            if ((checkBoxGrayStock.Checked && ord.ExType != ExchangeTypeEnum.FTHKG && ord.ExType != ExchangeTypeEnum.PMHKG) || (!checkBoxGrayStock.Checked && (ord.ExType == ExchangeTypeEnum.FTHKG || ord.ExType == ExchangeTypeEnum.PMHKG)))
                return;

            string strOrderNoSort = (ord.OrderNoSort == null || ord.OrderNoSort.Trim() == "") ? "" : ord.OrderNoSort;
            string strAccountNo = (ord.AccountNo == null || ord.AccountNo.Trim() == "") ? "" : ord.AccountNo;
            string strAECode = (ord.AECode == null || ord.AECode.Trim() == "") ? "" : ord.AECode;
            string strOrderSide = ord.Side.ToString();
            string strExCode = Stock.GetExchangeCode(ord.ExType);
            string strStockCode = (ord.StockCode == null || ord.StockCode.Trim() == "") ? "" : iStkCode.ToString();
            string lastAction = (ord.LastAction == null || ord.LastAction.Trim() == "") ? "" : GetResxString(ord.LastAction);
            string strOrderStatus = GetResxString(Utils.Utils.GetStatusString(ord.Status));
            string strOrderNo = (ord.OrderNo <= 0) ? strOrderStatus : ord.OrderNo.ToString();
            string strOrderQty = String.Format("{0:N0}", ord.Quantity);
            string strFilledQty = String.Format("{0:N0}", ord.Filled);
            string strOrderPrice = String.Format("{0:0.###}", ord.Price);
            string strOrderAvgPrice = String.Format("{0:0.###}", ord.AvgPrice);
            string strMemo = (ord.Memo == null || ord.Memo.Trim() == "") ? "" : ord.Memo;
            string strStatusMessage = (ord.StatusMessage == null || ord.StatusMessage.Trim() == "") ? "" : ord.StatusMessage;
            string strOGMessage = (ord.OGMessage == null || ord.OGMessage.Trim() == "") ? "" : ord.OGMessage;

            switch (strMemo)
            {
                case "Internet Order":
                case "TSCI0":
                case "TSCISTOCKTERMINAL_Client":
                case "STOCKTERMINAL_Client":
                case "STOCK TERMINAL_Client":
                    strMemo = "I";
                    break;
                default: // STOCKTERMINAL_AE
                    strMemo = "";
                    break;
            }
            DataGridViewRow foundRow = null;
            DataGridViewDisableCheckBoxCell replyChkBoxCell = new DataGridViewDisableCheckBoxCell();
            DataGridViewDisableButtonCell cancelBtnCell = new DataGridViewDisableButtonCell();
            DataGridViewDisableButtonCell aamendBtnCellUp = new DataGridViewDisableButtonCell();
            DataGridViewDisableButtonCell aamendBtnCellDown = new DataGridViewDisableButtonCell();

            // If search filter enabled and search criteria doesn't match, then no need to display
            if (checkBoxEnableSrch.Checked && ((labelAETarget != null && labelAETarget.Text != "" && labelAETarget.Text != strAECode) ||
                (labelAccTarget != null && labelAccTarget.Text != "" && labelAccTarget.Text != strAccountNo) ||
                (labelStkCodeTarget != null && labelStkCodeTarget.Text != "" && labelStkCodeTarget.Text != strStockCode)))
                return;
            if (!checkBoxShowInternetOrders.Checked || !checkBoxShowAEOrders.Checked)
                if ((checkBoxShowInternetOrders.Checked && strMemo != "I") || (checkBoxShowAEOrders.Checked && strMemo == "I"))
                    return;
            if (comboBoxShowEx.Visible && comboBoxShowEx != null && comboBoxShowEx.SelectedItem != null && comboBoxShowEx.SelectedItem.ToString() != "ALL" && comboBoxShowEx.SelectedItem.ToString() != strExCode)
                return;

            if (strOrderSide == "B")
                strOrderSide = GetResxString("Buy");
            else if (strOrderSide == "A")
                strOrderSide = GetResxString("Sell");
            if (ord.Price > 0.0M && ord.AvgPrice > 0.0M && ord.Price != ord.AvgPrice)
                strOrderAvgPrice = "* " + strOrderAvgPrice;

            lock (GridMutex)
            {
                if (GetGridDict(DataGridView1).TryGetValue(strOrderNoSort, out foundRow) && foundRow != null && foundRow.Index >= 0)
                {
                    if (foundRow.Cells[strOrderCol_OrderNoSort].Value.ToString() != strOrderNoSort)
                    {
                        GetGridDict(DataGridView1).Remove(strOrderNoSort);
                        foundRow = null;
                    }
                }
                else
                    foundRow = null;
            }
            if (foundRow != null)
            {   // Row exist, then update/delete existing row (order)
                lock (GridMutex)
                {
                    if (CheckCanDisplay(DataGridView1, ord.Status, IsReplied(ord.Replied), ord.ExType) == false)   // If order status no longer suitable in this grid, then delete the row
                    {
                        DataGridView1.Rows.Remove(foundRow);
                        GetGridDict(DataGridView1).Remove(strOrderNoSort);
                        foundRow = null;
                        return;
                    }

                    // Update Cell and highlight background (if necessary)
                    string[] strArrHeader = new string[] { strOrderCol_OrderNo, strOrderCol_AccNo, strOrderCol_AECode, strOrderCol_OrderSide, 
                                strOrderCol_ExCode, strOrderCol_StkCode, strOrderCol_OrderPrice, strOrderCol_OrderQty, strOrderCol_AvgPrice, strOrderCol_FilledQty, 
                                strOrderCol_OrderStatus, strOrderCol_OrderStatusCode, strOrderCol_LastAction, strOrderCol_OrderType, strOrderCol_OrderNoSort, 
                                strOrderCol_StatusMessage, strOrderCol_OGMessage, strOrderCol_Memo};
                    string[] strArrNewCellValue = new string[] { strOrderNo, strAccountNo, strAECode, strOrderSide, strExCode, strStockCode, 
                                strOrderPrice, strOrderQty, strOrderAvgPrice, strFilledQty, strOrderStatus, ord.Status.ToString(), lastAction, ord.OrderType.ToString(), 
                                strOrderNoSort, strStatusMessage, strOGMessage, strMemo };

                    UpdateCell(DataGridView1, strOrderNoSort, foundRow, strArrHeader, strArrNewCellValue);

                    //Reply check box column
                    if (Enable_RpyChkBoxCol(DataGridView1))
                    {
                        foundRow.Cells[strOrderCol_ReplyChkBox].Value = IsReplied(ord.Replied);
                        replyChkBoxCell = foundRow.Cells[strOrderCol_ReplyChkBox] as DataGridViewDisableCheckBoxCell;
                        if (DataGridView1 == dataGridViewAllOrder)
                        {
                            replyChkBoxCell.Enabled = false;
                            replyChkBoxCell.ChkBoxVisible = IsStatusReadyToReply(ord.Status);
                        }
                        else
                        {
                            replyChkBoxCell.ChkBoxVisible = true;
                            replyChkBoxCell.Enabled = IsStatusReadyToReply(ord.Status);
                        }
                    }
                    // Cancel button column
                    if (Enable_CancelOrderBtnCol(DataGridView1))
                    {
                        cancelBtnCell = foundRow.Cells[strOrderCol_DelBtn] as DataGridViewDisableButtonCell;
                        cancelBtnCell.Value = GetResxString("AllOrder_DeleteBtnVal");
                        cancelBtnCell.Enabled = ((ord.Status == (int)Order.OrderStatusEnum.Pending || ord.Status == (int)Order.OrderStatusEnum.Queue ||
                            (SettingsForms["CreditPassCancel"] != null && SettingsForms["CreditPassCancel"] == "1" &&
                            ord.Status == (int)Order.OrderStatusEnum.CreditPass)) && lastAction == "");
                    }
                    // AAmend button column
                    if (AutoAmend == true)
                    {
                        if (Enable_CancelOrderBtnCol(DataGridView1))
                        {
                            aamendBtnCellDown = foundRow.Cells[strOrderCol_AAmendBtnDown] as DataGridViewDisableButtonCell;
                            aamendBtnCellDown.Value = GetResxString("AllOrder_AAmendOrderDown");
                            aamendBtnCellUp = foundRow.Cells[strOrderCol_AAmendBtnUp] as DataGridViewDisableButtonCell;
                            aamendBtnCellUp.Value = GetResxString("AllOrder_AAmendOrderUp");

                            if (ord.ExType == ExchangeTypeEnum.SHG || ord.ExType == ExchangeTypeEnum.SZE)
                            {
                                if (strOrderSide == GetResxString("Buy") && ord.Filled > 0)
                                {
                                    aamendBtnCellDown.Enabled = false;
                                    aamendBtnCellUp.Enabled = false;
                                }
                                else
                                {
                                    aamendBtnCellDown.Enabled = ((ord.Status == (int)Order.OrderStatusEnum.Queue) && (lastAction == ""));
                                    aamendBtnCellUp.Enabled = ((ord.Status == (int)Order.OrderStatusEnum.Queue) && (lastAction == ""));
                                }
                            }
                            else
                            {
                                aamendBtnCellDown.Enabled = ((ord.Status == (int)Order.OrderStatusEnum.Queue) && (lastAction == ""));
                                aamendBtnCellUp.Enabled = ((ord.Status == (int)Order.OrderStatusEnum.Queue) && (lastAction == ""));
                            }


                            //////aamendBtnCellDown = foundRow.Cells[strOrderCol_AAmendBtnDown] as DataGridViewDisableButtonCell;
                            //////aamendBtnCellDown.Value = GetResxString("AllOrder_AAmendOrderDown");
                            //////aamendBtnCellDown.Enabled = ((ord.Status == (int)Order.OrderStatusEnum.Queue) && (lastAction == ""));

                            //////aamendBtnCellUp = foundRow.Cells[strOrderCol_AAmendBtnUp] as DataGridViewDisableButtonCell;
                            //////aamendBtnCellUp.Value = GetResxString("AllOrder_AAmendOrderUp");
                            //////aamendBtnCellUp.Enabled = ((ord.Status == (int)Order.OrderStatusEnum.Queue) && (lastAction == ""));
                        }
                    }
                }
                if (ord.Status == (int)Order.OrderStatusEnum.Sending)
                {
                    OrderGridSelectRowAfterSort = true;
                    selectRowOrderNoSort = strOrderNoSort;
                }
            }
            else if (CheckCanDisplay(DataGridView1, ord.Status, IsReplied(ord.Replied), ord.ExType) == true)
            {   // Row not exist, then add new row (order)
                object[] row1;
                //NeedSort = true;
                DataGridViewRow dgvRow = null;

                row1 = new object[] { strOrderNo, strAECode, strAccountNo, strOrderSide, strExCode, strStockCode, strOrderPrice, 
                            strOrderQty, strOrderAvgPrice, strFilledQty, strOrderStatus, lastAction + "", ord.Status.ToString(), ord.OrderType, strOrderNoSort, strStatusMessage, strOGMessage};//, strMemo};
                lock (GridMutex)
                {
                    GetGridDict(DataGridView1)[strOrderNoSort] = DataGridView1.Rows[DataGridView1.Rows.Add(row1)];
                }
                dgvRow = GetGridDict(DataGridView1)[strOrderNoSort] as DataGridViewRow;

                if (strOrderSide == GetResxString("Buy"))
                    dgvRow.DefaultCellStyle.BackColor = Color.FromArgb(206, 235, 244);
                else if (strOrderSide == GetResxString("Sell"))
                    dgvRow.DefaultCellStyle.BackColor = Color.LightPink;
                dgvRow.DefaultCellStyle.SelectionBackColor = Color.DarkBlue;
                dgvRow.DefaultCellStyle.SelectionForeColor = Color.White;
                dgvRow.Height = 19;

                dgvRow.Cells[strOrderCol_Memo].Value = strMemo;

                if (Enable_RpyChkBoxCol(DataGridView1))
                {
                    dgvRow.Cells[strOrderCol_ReplyChkBox].Value = IsReplied(ord.Replied);
                    replyChkBoxCell = dgvRow.Cells[strOrderCol_ReplyChkBox] as DataGridViewDisableCheckBoxCell;
                    if (DataGridView1 == dataGridViewAllOrder)
                    {
                        replyChkBoxCell.Enabled = false;
                        replyChkBoxCell.ChkBoxVisible = IsStatusReadyToReply(ord.Status);
                    }
                    else
                    {
                        replyChkBoxCell.ChkBoxVisible = true;
                        replyChkBoxCell.Enabled = IsStatusReadyToReply(ord.Status);
                    }
                }
                if (Enable_CancelOrderBtnCol(DataGridView1))
                {
                    cancelBtnCell = dgvRow.Cells[strOrderCol_DelBtn] as DataGridViewDisableButtonCell;
                    cancelBtnCell.Value = GetResxString("AllOrder_DeleteBtnVal");
                    cancelBtnCell.Enabled = ((ord.Status == (int)Order.OrderStatusEnum.Pending || ord.Status == (int)Order.OrderStatusEnum.Queue ||
                        (SettingsForms["CreditPassCancel"] != null && SettingsForms["CreditPassCancel"] == "1" &&
                        ord.Status == (int)Order.OrderStatusEnum.CreditPass)) && lastAction == "");
                }
                // AAmend
                if (AutoAmend == true)
                {
                    if (Enable_CancelOrderBtnCol(DataGridView1))
                    {
                        aamendBtnCellDown = dgvRow.Cells[strOrderCol_AAmendBtnDown] as DataGridViewDisableButtonCell;
                        aamendBtnCellDown.Value = GetResxString("AllOrder_AAmendOrderDown");
                        aamendBtnCellUp = dgvRow.Cells[strOrderCol_AAmendBtnUp] as DataGridViewDisableButtonCell;
                        aamendBtnCellUp.Value = GetResxString("AllOrder_AAmendOrderUp");

                        if (ord.ExType == ExchangeTypeEnum.SHG || ord.ExType == ExchangeTypeEnum.SZE)
                        {
                            if (strOrderSide == GetResxString("Buy") && ord.Filled > 0)
                            {
                                aamendBtnCellDown.Enabled = false;
                                aamendBtnCellUp.Enabled = false;
                            }
                            else
                            {
                                aamendBtnCellDown.Enabled = ((ord.Status == (int)Order.OrderStatusEnum.Queue) && (lastAction == ""));
                                aamendBtnCellUp.Enabled = ((ord.Status == (int)Order.OrderStatusEnum.Queue) && (lastAction == ""));
                            }
                        }
                        else
                        {
                            aamendBtnCellDown.Enabled = ((ord.Status == (int)Order.OrderStatusEnum.Queue) && (lastAction == ""));
                            aamendBtnCellUp.Enabled = ((ord.Status == (int)Order.OrderStatusEnum.Queue) && (lastAction == ""));
                        }

                        //////aamendBtnCellDown.Enabled = ((ord.Status == (int)Order.OrderStatusEnum.Queue) && (lastAction == ""));

                        //////aamendBtnCellUp = dgvRow.Cells[strOrderCol_AAmendBtnUp] as DataGridViewDisableButtonCell;
                        //////aamendBtnCellUp.Value = GetResxString("AllOrder_AAmendOrderUp");
                        //////aamendBtnCellUp.Enabled = ((ord.Status == (int)Order.OrderStatusEnum.Queue) && (lastAction == ""));
                    }
                }
                if (ord.Status == (int)Order.OrderStatusEnum.Sending)
                {
                    OrderGridSelectRowAfterSort = true;
                    selectRowOrderNoSort = strOrderNoSort;
                }
            }

            if (checkBoxEnableSrch.Checked && DataGridView1 == currDGV &&
                (labelAccTarget != null && labelAccTarget.Text != "") && (labelStkCodeTarget != null && labelStkCodeTarget.Text != ""))
            {
                long buyTtlQty = 0, buyfilledQty = 0;
                long sellTtlQty = 0, sellfilledQty = 0;
                decimal ttlBuyCost = 0.0M;
                decimal ttlSellCost = 0.0M;

                // Using grid to search
                foreach (KeyValuePair<string, DataGridViewRow> kvp in GetGridDict(DataGridView1))
                {
                    if (kvp.Value != null)
                    {
                        strAccountNo = ((DataGridViewRow)kvp.Value).Cells[strOrderCol_AccNo].Value.ToString().Trim();
                        strStockCode = ((DataGridViewRow)kvp.Value).Cells[strOrderCol_StkCode].Value.ToString().Trim();
                        if (labelAccTarget.Text == strAccountNo && labelStkCodeTarget.Text == strStockCode)
                        {
                            long iFilled = 0;
                            long iOrderQty = 0;
                            decimal dAvgPrice;
                            int iOrderStatusCode;
                            long.TryParse(((DataGridViewRow)kvp.Value).Cells[strOrderCol_FilledQty].Value.ToString().Replace(",", "").Trim(), out iFilled);
                            long.TryParse(((DataGridViewRow)kvp.Value).Cells[strOrderCol_OrderQty].Value.ToString().Replace(",", "").Trim(), out iOrderQty);
                            decimal.TryParse(((DataGridViewRow)kvp.Value).Cells[strOrderCol_AvgPrice].Value.ToString().Replace("*", "").Trim(), out dAvgPrice);
                            int.TryParse(((DataGridViewRow)kvp.Value).Cells[strOrderCol_OrderStatusCode].Value.ToString().Replace(",", "").Trim(), out iOrderStatusCode);
                            //if (iFilled > 0)
                            if (iOrderStatusCode == (int)Order.OrderStatusEnum.Queue || iOrderStatusCode == (int)Order.OrderStatusEnum.PartiallyCompleted ||
                                iOrderStatusCode == (int)Order.OrderStatusEnum.Completed)
                            {
                                strOrderSide = "";
                                if (((DataGridViewRow)kvp.Value).Cells[strOrderCol_OrderSide].Value.ToString().Trim() == GetResxString("Buy"))
                                    strOrderSide = "B";
                                else if (((DataGridViewRow)kvp.Value).Cells[strOrderCol_OrderSide].Value.ToString().Trim() == GetResxString("Sell"))
                                    strOrderSide = "A";
                                if (strOrderSide == "B")
                                {
                                    buyTtlQty += iOrderQty;
                                    buyfilledQty += iFilled;
                                    labelBuyTtlQty.Text = String.Format("{0:N0}", buyTtlQty);
                                    labelBuyFilled.Text = String.Format("{0:N0}", buyfilledQty);
                                    ttlBuyCost += iFilled * dAvgPrice;
                                    if (buyfilledQty == 0)
                                        labelBuyAvgPrice.Text = "0";
                                    else
                                        labelBuyAvgPrice.Text = Math.Round((ttlBuyCost / buyfilledQty), 3, MidpointRounding.AwayFromZero).ToString();
                                }
                                else if (strOrderSide == "A")
                                {
                                    sellTtlQty += iOrderQty;
                                    sellfilledQty += iFilled;
                                    labelSellTtlQty.Text = String.Format("{0:N0}", sellTtlQty);
                                    labelSellFilled.Text = String.Format("{0:N0}", sellfilledQty);
                                    ttlSellCost += iFilled * dAvgPrice;
                                    if (sellfilledQty == 0)
                                        labelSellAvgPrice.Text = "0";
                                    else
                                        labelSellAvgPrice.Text = Math.Round((ttlSellCost / sellfilledQty), 3, MidpointRounding.AwayFromZero).ToString();
                                }
                            }
                        }
                    }
                }
            }

            if (SortGridTimer.Enabled)
                SortGridTimer.Enabled = false;
            SortGridTimer.Interval = 110;
            SortGridTimer.Enabled = true;
        }

        private void RefreshGrid(DataGridView DataGridView1, List<Order> Orders)
        {
            if (Orders == null || Orders.Count == 0 || (DataGridView1 == dataGridViewRepliedOrder && TradeDB.UserType == UserTypeEnum.Client)) 
                return;
            try
            {
                DataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;    // performance reason
                foreach (Order ord in Orders)
                {
                    // tradeDB 2nd reply
                    string strGrid1stActionRef;
                    if (GridFirstActionRefDict.TryGetValue(DataGridView1.Name, out strGrid1stActionRef) && strGrid1stActionRef != null && ord.FirstActionRef == strGrid1stActionRef &&
                        DataGridView1 != dataGridViewCompletedOrder)
                    {
                        DataGridViewRow row;
                        lock (GridMutex)
                        {
                            if (GetGridDict(DataGridView1).TryGetValue("z", out row) && row != null && row.Index >= 0)
                            {
                                // Remove the "Sending" order row
                                DataGridView1.Rows.Remove(row);
                                GetGridDict(DataGridView1).Remove("z");
                                GridFirstActionRefDict[DataGridView1.Name] = "";
                            }
                        }
                    }
                    // tradeDB 1st reply - status = "Sending" 9001 with orderNoSort = "z"
                    if (ord.OrderNo == 0)
                        GridFirstActionRefDict[DataGridView1.Name] = ord.FirstActionRef;

                    // to prevent error orders
                    int iStkCode;
                    if (ord.OrderNoSort == null || ord.OrderNoSort.Trim().Length <= 0 || ord.AccountNo == null || (ord.Side != 'B' && ord.Side != 'A') || ord.Side.ToString().Trim() == "" || ord.StockCode == null || ord.StockCode.ToString().Trim() == "" || !int.TryParse(ord.StockCode, out iStkCode))
                    {
                        AppendLog("Order Arrive", Environment.NewLine + "Invalid!!! List Order No.: " + ord.OrderNo + " OrderNoSort: " + (ord.OrderNoSort == null ? "" : ord.OrderNoSort) +
                        "  AccountNo: " + (ord.AccountNo == null ? "" : ord.OrderNoSort) + "  StockCode: " + (ord.AccountNo == null ? "" : ord.StockCode) +
                        "  Side: " + ord.Side + "  Price: " + ord.Price + "  Quantity: " + ord.Quantity + "  OrderType: " + ord.OrderType +
                        "  AvgPrice: " + ord.AvgPrice + "  Filled: " + ord.Filled +
                        "  Status: " + ord.Status + "  StatusMessage: " + ord.StatusMessage + "  OGMessage: " + ord.OGMessage +
                        "  LastAction: " + ord.LastAction, "OrderBookForm", false);
                        continue;
                    }

                    if ((checkBoxGrayStock.Checked && ord.ExType != ExchangeTypeEnum.FTHKG && ord.ExType != ExchangeTypeEnum.PMHKG) || (!checkBoxGrayStock.Checked && (ord.ExType == ExchangeTypeEnum.FTHKG || ord.ExType == ExchangeTypeEnum.PMHKG)))
                        continue;

                    string strOrderNoSort = (ord.OrderNoSort == null || ord.OrderNoSort.Trim() == "") ? "" : ord.OrderNoSort;
                    string strAccountNo = (ord.AccountNo == null || ord.AccountNo.Trim() == "") ? "" : ord.AccountNo;
                    string strAECode = (ord.AECode == null || ord.AECode.Trim() == "") ? "" : ord.AECode;
                    string strOrderSide = ord.Side.ToString();
                    string strExCode = Stock.GetExchangeCode(ord.ExType);
                    string strStockCode = (ord.StockCode == null || ord.StockCode.Trim() == "") ? "" : iStkCode.ToString();
                    string lastAction = (ord.LastAction == null || ord.LastAction.Trim() == "") ? "" : GetResxString(ord.LastAction);
                    string strOrderStatus = GetResxString(Utils.Utils.GetStatusString(ord.Status));
                    string strOrderNo = (ord.OrderNo <= 0) ? strOrderStatus : ord.OrderNo.ToString();
                    string strOrderQty = String.Format("{0:N0}", ord.Quantity);
                    string strFilledQty = String.Format("{0:N0}", ord.Filled);
                    string strOrderPrice = String.Format("{0:0.###}", ord.Price);
                    string strOrderAvgPrice = String.Format("{0:0.###}", ord.AvgPrice);
                    string strMemo = (ord.Memo == null || ord.Memo.Trim() == "") ? "" : ord.Memo;
                    string strStatusMessage = (ord.StatusMessage == null || ord.StatusMessage.Trim() == "") ? "" : ord.StatusMessage;
                    string strOGMessage = (ord.OGMessage == null || ord.OGMessage.Trim() == "") ? "" : ord.OGMessage;

                    switch (strMemo)
                    {
                        case "Internet Order":
                        case "TSCI0":
                        case "TSCISTOCKTERMINAL_Client":
                        case "STOCKTERMINAL_Client":
                        case "STOCK TERMINAL_Client":
                            strMemo = "I";
                            break;
                        default: // STOCKTERMINAL_AE
                            strMemo = "";
                            break;
                    }
                    DataGridViewRow foundRow = null;
                    DataGridViewDisableCheckBoxCell replyChkBoxCell = new DataGridViewDisableCheckBoxCell();
                    DataGridViewDisableButtonCell cancelBtnCell = new DataGridViewDisableButtonCell();
                    DataGridViewDisableButtonCell aamendBtnCellUp = new DataGridViewDisableButtonCell();
                    DataGridViewDisableButtonCell aamendBtnCellDown = new DataGridViewDisableButtonCell();

                    // If search filter enabled and search criteria doesn't match, then no need to display
                    if (checkBoxEnableSrch.Checked && ((labelAETarget != null && labelAETarget.Text != "" && labelAETarget.Text != strAECode) ||
                        (labelAccTarget != null && labelAccTarget.Text != "" && labelAccTarget.Text != strAccountNo) ||
                        (labelStkCodeTarget != null && labelStkCodeTarget.Text != "" && labelStkCodeTarget.Text != strStockCode)))
                        continue;
                    if (!checkBoxShowInternetOrders.Checked || !checkBoxShowAEOrders.Checked)
                        if ((checkBoxShowInternetOrders.Checked && strMemo != "I") || (checkBoxShowAEOrders.Checked && strMemo == "I"))
                            continue;
                    if (comboBoxShowEx.Visible && comboBoxShowEx != null && comboBoxShowEx.SelectedItem != null && comboBoxShowEx.SelectedItem.ToString() != "ALL" && comboBoxShowEx.SelectedItem.ToString() != strExCode)
                        continue;

                    if (strOrderSide == "B")
                        strOrderSide = GetResxString("Buy");
                    else if (strOrderSide == "A")
                        strOrderSide = GetResxString("Sell");
                    if (ord.Price > 0.0M && ord.AvgPrice > 0.0M && ord.Price != ord.AvgPrice)
                        strOrderAvgPrice = "* " + strOrderAvgPrice;

                    lock (GridMutex)
                    {
                        if (GetGridDict(DataGridView1).TryGetValue(strOrderNoSort, out foundRow) && foundRow != null && foundRow.Index >= 0)
                        {
                            if (foundRow.Cells[strOrderCol_OrderNoSort].Value.ToString() != strOrderNoSort)
                            {
                                GetGridDict(DataGridView1).Remove(strOrderNoSort);
                                foundRow = null;
                            }
                        }
                        else
                            foundRow = null;
                    }
                    if (foundRow != null)
                    {   // Row exist, then update/delete existing row (order)
                        lock (GridMutex)
                        {
                            if (CheckCanDisplay(DataGridView1, ord.Status, IsReplied(ord.Replied), ord.ExType) == false)   // If order status no longer suitable in this grid, then delete the row
                            {
                                DataGridView1.Rows.Remove(foundRow);
                                GetGridDict(DataGridView1).Remove(strOrderNoSort);
                                foundRow = null;
                                continue;
                            }

                            // Update Cell and highlight background (if necessary)
                            string[] strArrHeader = new string[] { strOrderCol_OrderNo, strOrderCol_AccNo, strOrderCol_AECode, strOrderCol_OrderSide, 
                                strOrderCol_ExCode, strOrderCol_StkCode, strOrderCol_OrderPrice, strOrderCol_OrderQty, strOrderCol_AvgPrice, strOrderCol_FilledQty, 
                                strOrderCol_OrderStatus, strOrderCol_OrderStatusCode, strOrderCol_LastAction, strOrderCol_OrderType, strOrderCol_OrderNoSort, 
                                strOrderCol_StatusMessage, strOrderCol_OGMessage, strOrderCol_Memo};
                            string[] strArrNewCellValue = new string[] { strOrderNo, strAccountNo, strAECode, strOrderSide, strExCode, strStockCode, 
                                strOrderPrice, strOrderQty, strOrderAvgPrice, strFilledQty, strOrderStatus, ord.Status.ToString(), lastAction, ord.OrderType.ToString(), 
                                strOrderNoSort, strStatusMessage, strOGMessage, strMemo };

                            UpdateCell(DataGridView1, strOrderNoSort, foundRow, strArrHeader, strArrNewCellValue);

                            //Reply check box column
                            if (Enable_RpyChkBoxCol(DataGridView1))
                            {
                                foundRow.Cells[strOrderCol_ReplyChkBox].Value = IsReplied(ord.Replied);
                                replyChkBoxCell = foundRow.Cells[strOrderCol_ReplyChkBox] as DataGridViewDisableCheckBoxCell;
                                if (DataGridView1 == dataGridViewAllOrder)
                                {
                                    replyChkBoxCell.Enabled = false;
                                    replyChkBoxCell.ChkBoxVisible = IsStatusReadyToReply(ord.Status);
                                }
                                else
                                {
                                    replyChkBoxCell.ChkBoxVisible = true;
                                    replyChkBoxCell.Enabled = IsStatusReadyToReply(ord.Status);
                                }
                            }
                            // Cancel button column
                            if (Enable_CancelOrderBtnCol(DataGridView1))
                            {
                                cancelBtnCell = foundRow.Cells[strOrderCol_DelBtn] as DataGridViewDisableButtonCell;
                                cancelBtnCell.Value = GetResxString("AllOrder_DeleteBtnVal");
                                cancelBtnCell.Enabled = ((ord.Status == (int)Order.OrderStatusEnum.Pending || ord.Status == (int)Order.OrderStatusEnum.Queue ||
                                    (SettingsForms["CreditPassCancel"] != null && SettingsForms["CreditPassCancel"] == "1" &&
                                    ord.Status == (int)Order.OrderStatusEnum.CreditPass)) && lastAction == "");
                            }
                            // AAmend button column
                            if (AutoAmend == true)
                            {
                                if (Enable_CancelOrderBtnCol(DataGridView1))
                                {
                                    aamendBtnCellDown = foundRow.Cells[strOrderCol_AAmendBtnDown] as DataGridViewDisableButtonCell;
                                    aamendBtnCellDown.Value = GetResxString("AllOrder_AAmendOrderDown");
                                    aamendBtnCellUp = foundRow.Cells[strOrderCol_AAmendBtnUp] as DataGridViewDisableButtonCell;
                                    aamendBtnCellUp.Value = GetResxString("AllOrder_AAmendOrderUp");

                                    if (ord.ExType == ExchangeTypeEnum.SHG || ord.ExType == ExchangeTypeEnum.SZE)
                                    {
                                        if (strOrderSide == GetResxString("Buy") && ord.Filled > 0)
                                        {
                                            aamendBtnCellDown.Enabled = false;
                                            aamendBtnCellUp.Enabled = false;
                                        }
                                        else
                                        {
                                            aamendBtnCellDown.Enabled = ((ord.Status == (int)Order.OrderStatusEnum.Queue) && (lastAction == ""));
                                            aamendBtnCellUp.Enabled = ((ord.Status == (int)Order.OrderStatusEnum.Queue) && (lastAction == ""));
                                        }
                                    }
                                    else
                                    {
                                        aamendBtnCellDown.Enabled = ((ord.Status == (int)Order.OrderStatusEnum.Queue) && (lastAction == ""));
                                        aamendBtnCellUp.Enabled = ((ord.Status == (int)Order.OrderStatusEnum.Queue) && (lastAction == ""));
                                    }


                                    //////aamendBtnCellDown = foundRow.Cells[strOrderCol_AAmendBtnDown] as DataGridViewDisableButtonCell;
                                    //////aamendBtnCellDown.Value = GetResxString("AllOrder_AAmendOrderDown");
                                    //////aamendBtnCellDown.Enabled = ((ord.Status == (int)Order.OrderStatusEnum.Queue) && (lastAction == ""));

                                    //////aamendBtnCellUp = foundRow.Cells[strOrderCol_AAmendBtnUp] as DataGridViewDisableButtonCell;
                                    //////aamendBtnCellUp.Value = GetResxString("AllOrder_AAmendOrderUp");
                                    //////aamendBtnCellUp.Enabled = ((ord.Status == (int)Order.OrderStatusEnum.Queue) && (lastAction == ""));
                                }
                            }
                        }
                        if (ord.Status == (int)Order.OrderStatusEnum.Sending)
                        {
                            OrderGridSelectRowAfterSort = true;
                            selectRowOrderNoSort = strOrderNoSort;
                        }
                    }
                    else if (CheckCanDisplay(DataGridView1, ord.Status, IsReplied(ord.Replied), ord.ExType) == true)
                    {   // Row not exist, then add new row (order)
                        object[] row1;
                        //NeedSort = true;
                        DataGridViewRow dgvRow = null;

                        row1 = new object[] { strOrderNo, strAECode, strAccountNo, strOrderSide, strExCode, strStockCode, strOrderPrice, 
                            strOrderQty, strOrderAvgPrice, strFilledQty, strOrderStatus, lastAction + "", ord.Status.ToString(), ord.OrderType, strOrderNoSort, strStatusMessage, strOGMessage};//, strMemo};

                        lock (GridMutex)
                        {
                            GetGridDict(DataGridView1)[strOrderNoSort] = DataGridView1.Rows[DataGridView1.Rows.Add(row1)];
                            dgvRow = GetGridDict(DataGridView1)[strOrderNoSort] as DataGridViewRow;
                        }

                        if (strOrderSide == GetResxString("Buy"))
                            dgvRow.DefaultCellStyle.BackColor = Color.FromArgb(206, 235, 244);
                        else if (strOrderSide == GetResxString("Sell"))
                            dgvRow.DefaultCellStyle.BackColor = Color.LightPink;
                        dgvRow.DefaultCellStyle.SelectionBackColor = Color.DarkBlue;
                        dgvRow.DefaultCellStyle.SelectionForeColor = Color.White;
                        dgvRow.Height = 19;

                        dgvRow.Cells[strOrderCol_Memo].Value = strMemo;

                        if (Enable_RpyChkBoxCol(DataGridView1))
                        {
                            dgvRow.Cells[strOrderCol_ReplyChkBox].Value = IsReplied(ord.Replied);
                            replyChkBoxCell = dgvRow.Cells[strOrderCol_ReplyChkBox] as DataGridViewDisableCheckBoxCell;
                            if (DataGridView1 == dataGridViewAllOrder)
                            {
                                replyChkBoxCell.Enabled = false;
                                replyChkBoxCell.ChkBoxVisible = IsStatusReadyToReply(ord.Status);
                            }
                            else
                            {
                                replyChkBoxCell.ChkBoxVisible = true;
                                replyChkBoxCell.Enabled = IsStatusReadyToReply(ord.Status);
                            }
                        }
                        if (Enable_CancelOrderBtnCol(DataGridView1))
                        {
                            cancelBtnCell = dgvRow.Cells[strOrderCol_DelBtn] as DataGridViewDisableButtonCell;
                            cancelBtnCell.Value = GetResxString("AllOrder_DeleteBtnVal");
                            cancelBtnCell.Enabled = ((ord.Status == (int)Order.OrderStatusEnum.Pending || ord.Status == (int)Order.OrderStatusEnum.Queue ||
                                (SettingsForms["CreditPassCancel"] != null && SettingsForms["CreditPassCancel"] == "1" &&
                                ord.Status == (int)Order.OrderStatusEnum.CreditPass)) && lastAction == "");
                        }
                        // AAmend
                        if (AutoAmend == true)
                        {
                            if (Enable_CancelOrderBtnCol(DataGridView1))
                            {
                                aamendBtnCellDown = dgvRow.Cells[strOrderCol_AAmendBtnDown] as DataGridViewDisableButtonCell;
                                aamendBtnCellDown.Value = GetResxString("AllOrder_AAmendOrderDown");
                                aamendBtnCellUp = dgvRow.Cells[strOrderCol_AAmendBtnUp] as DataGridViewDisableButtonCell;
                                aamendBtnCellUp.Value = GetResxString("AllOrder_AAmendOrderUp");

                                if (ord.ExType == ExchangeTypeEnum.SHG || ord.ExType == ExchangeTypeEnum.SZE)
                                {
                                    if (strOrderSide == GetResxString("Buy") && ord.Filled > 0)
                                    {
                                        aamendBtnCellDown.Enabled = false;
                                        aamendBtnCellUp.Enabled = false;
                                    }
                                    else
                                    {
                                        aamendBtnCellDown.Enabled = ((ord.Status == (int)Order.OrderStatusEnum.Queue) && (lastAction == ""));
                                        aamendBtnCellUp.Enabled = ((ord.Status == (int)Order.OrderStatusEnum.Queue) && (lastAction == ""));
                                    }
                                }
                                else
                                {
                                    aamendBtnCellDown.Enabled = ((ord.Status == (int)Order.OrderStatusEnum.Queue) && (lastAction == ""));
                                    aamendBtnCellUp.Enabled = ((ord.Status == (int)Order.OrderStatusEnum.Queue) && (lastAction == ""));
                                }

                                //////aamendBtnCellDown.Enabled = ((ord.Status == (int)Order.OrderStatusEnum.Queue) && (lastAction == ""));

                                //////aamendBtnCellUp = dgvRow.Cells[strOrderCol_AAmendBtnUp] as DataGridViewDisableButtonCell;
                                //////aamendBtnCellUp.Value = GetResxString("AllOrder_AAmendOrderUp");
                                //////aamendBtnCellUp.Enabled = ((ord.Status == (int)Order.OrderStatusEnum.Queue) && (lastAction == ""));
                            }
                        }
                        if (ord.Status == (int)Order.OrderStatusEnum.Sending)
                        {
                            OrderGridSelectRowAfterSort = true;
                            selectRowOrderNoSort = strOrderNoSort;
                        }
                    }
                }

                // Stock search
                //lock (GridClickMutex)
                lock (GridMutex)
                {
                    if (checkBoxEnableSrch.Checked && DataGridView1 == currDGV &&
                        (labelAccTarget != null && labelAccTarget.Text != "") && (labelStkCodeTarget != null && labelStkCodeTarget.Text != ""))
                    {
                        long buyTtlQty = 0, buyfilledQty = 0;
                        long sellTtlQty = 0, sellfilledQty = 0;
                        decimal ttlBuyCost = 0.0M;
                        decimal ttlSellCost = 0.0M;

                        // Using grid to search
                        foreach (KeyValuePair<string, DataGridViewRow> kvp in GetGridDict(DataGridView1))
                        {
                            if (kvp.Value != null)
                            {
                                string strAccountNo = ((DataGridViewRow)kvp.Value).Cells[strOrderCol_AccNo].Value.ToString().Trim();
                                string strStockCode = ((DataGridViewRow)kvp.Value).Cells[strOrderCol_StkCode].Value.ToString().Trim();
                                if (labelAccTarget.Text == strAccountNo && labelStkCodeTarget.Text == strStockCode)
                                {
                                    long iFilled = 0;
                                    long iOrderQty = 0;
                                    decimal dAvgPrice;
                                    int iOrderStatusCode;
                                    long.TryParse(((DataGridViewRow)kvp.Value).Cells[strOrderCol_FilledQty].Value.ToString().Replace(",", "").Trim(), out iFilled);
                                    long.TryParse(((DataGridViewRow)kvp.Value).Cells[strOrderCol_OrderQty].Value.ToString().Replace(",", "").Trim(), out iOrderQty);
                                    decimal.TryParse(((DataGridViewRow)kvp.Value).Cells[strOrderCol_AvgPrice].Value.ToString().Replace("*", "").Trim(), out dAvgPrice);
                                    int.TryParse(((DataGridViewRow)kvp.Value).Cells[strOrderCol_OrderStatusCode].Value.ToString().Replace(",", "").Trim(), out iOrderStatusCode);
                                    //if (iFilled > 0)
                                    if (iOrderStatusCode == (int)Order.OrderStatusEnum.Queue || iOrderStatusCode == (int)Order.OrderStatusEnum.PartiallyCompleted ||
                                        iOrderStatusCode == (int)Order.OrderStatusEnum.Completed)
                                    {
                                        string strOrderSide = "";
                                        if (((DataGridViewRow)kvp.Value).Cells[strOrderCol_OrderSide].Value.ToString().Trim() == GetResxString("Buy"))
                                            strOrderSide = "B";
                                        else if (((DataGridViewRow)kvp.Value).Cells[strOrderCol_OrderSide].Value.ToString().Trim() == GetResxString("Sell"))
                                            strOrderSide = "A";
                                        if (strOrderSide == "B")
                                        {
                                            buyTtlQty += iOrderQty;
                                            buyfilledQty += iFilled;
                                            labelBuyTtlQty.Text = String.Format("{0:N0}", buyTtlQty);
                                            labelBuyFilled.Text = String.Format("{0:N0}", buyfilledQty);
                                            ttlBuyCost += iFilled * dAvgPrice;
                                            if (buyfilledQty == 0)
                                                labelBuyAvgPrice.Text = "0";
                                            else
                                                labelBuyAvgPrice.Text = Math.Round((ttlBuyCost / buyfilledQty), 3, MidpointRounding.AwayFromZero).ToString();
                                        }
                                        else if (strOrderSide == "A")
                                        {
                                            sellTtlQty += iOrderQty;
                                            sellfilledQty += iFilled;
                                            labelSellTtlQty.Text = String.Format("{0:N0}", sellTtlQty);
                                            labelSellFilled.Text = String.Format("{0:N0}", sellfilledQty);
                                            ttlSellCost += iFilled * dAvgPrice;
                                            if (sellfilledQty == 0)
                                                labelSellAvgPrice.Text = "0";
                                            else
                                                labelSellAvgPrice.Text = Math.Round((ttlSellCost / sellfilledQty), 3, MidpointRounding.AwayFromZero).ToString();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                // Stock search end

                if (SortGridTimer.Enabled)
                    SortGridTimer.Enabled = false;
                SortGridTimer.Interval = 110;
                SortGridTimer.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "RefreshGrid: " + DataGridView1.Name);
                throw;
            }
        }

        private int GetCurrTabIndex(DataGridView dgv)
        {
            int currTabIdx = -1;
            if (dgv == dataGridViewPendOrder)
                currTabIdx = 0;
            else if (dgv == dataGridViewCompletedOrder)
                currTabIdx = 1;
            else if (dgv == dataGridViewRepliedOrder)
                currTabIdx = 2;
            else if (dgv == dataGridViewAllOrder)
            {
                if (TradeDB.UserType == UserTypeEnum.AE)
                    currTabIdx = 3;
                else
                    currTabIdx = 2;
            }
            return currTabIdx;
        }

        private void UpdateCell(DataGridView dgv, string orderNo, DataGridViewRow row, string[] headerName, string[] NewCellValue)
        {
            if (headerName.Length != NewCellValue.Length)
            {
                MessageBox.Show("UpdateCell length not equal");
                return;
            }
            for (int i = 0; i < headerName.Length; i++)
            {
                if (row.Cells[headerName[i]].Value != null && row.Cells[headerName[i]].Value.ToString() != NewCellValue[i] && row.Cells[headerName[i]].Visible == true)
                {
                    CellData data = new CellData();
                    data.dataGridView = dgv;
                    data.ColumnName = headerName[i];
                    data.orderNo = orderNo;
                    data.Time = DateTime.Now;
                    data.currTabIdx = GetCurrTabIndex(dgv);
					data.highlighted = (row.Cells[data.ColumnName].Style.BackColor == Color.DarkBlue || 
						row.Cells[data.ColumnName].Style.BackColor == Color.Red) ? true : false;

                    row.Cells[data.ColumnName].Style.BackColor = Color.Red;
                    row.Cells[data.ColumnName].Style.ForeColor = Color.Black;
                    data.painted = true;

                    lock (blinkCellData)
                    {
                        blinkCellData.Add(data);
                    }
                }
                row.Cells[headerName[i]].Value = NewCellValue[i];
            }
        }

        #endregion

        #region "Sorting"

        private void SortGrid()
        { // Sort only currently viewing grid (for performance reason)
            lock (GridMutex)
            {
                if (currDGV == null || currDGV.Columns.Count == 0 || (currDGV == dataGridViewRepliedOrder && TradeDB.UserType == UserTypeEnum.Client))
                    return;

                int currTabIdx = GetCurrTabIndex(currDGV);
                if (OrderGridSortedOrderList[currTabIdx] == SortOrder.None)
                    OrderGridSortedOrderList[currTabIdx] = SortOrder.Descending;
                if (OrderGridSortedColumnIndexList[currTabIdx] < 0 || OrderGridSortedColumnIndexList[currTabIdx] == iOrderNoColIdx)
                    OrderGridSortedColumnIndexList[currTabIdx] = iOrderNoSortColIdx;

                if (currDGV.Columns[OrderGridSortedColumnIndexList[currTabIdx]].SortMode == DataGridViewColumnSortMode.Programmatic)
                {
                    if (OrderGridSortedColumnIndexList[currTabIdx] == iOrderAvgPriceColIdx || OrderGridSortedColumnIndexList[currTabIdx] == iOrderPriceColIdx)
                        currDGV.Sort(new Double32Comparer(OrderGridSortedOrderList[currTabIdx], OrderGridSortedColumnIndexList[currTabIdx]));
                    else
                        currDGV.Sort(new Int32Comparer(OrderGridSortedOrderList[currTabIdx], OrderGridSortedColumnIndexList[currTabIdx]));
                }
                else
                {
                    currDGV.Sort(currDGV.Columns[OrderGridSortedColumnIndexList[currTabIdx]],
                        OrderGridSortedOrderList[currTabIdx] != SortOrder.Descending ? ListSortDirection.Ascending : ListSortDirection.Descending);
                }
                currDGV.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                //Console.WriteLine("Sorted!!");
                //prevSelectedOrderNo[currTabIdx] = "";
                currDGV.ClearSelection();
                currDGV.Refresh();

                // Set sort glyph direction
                int sortGlyphIndex = OrderGridSortedColumnIndexList[currTabIdx] == iOrderNoSortColIdx ? iOrderNoColIdx : OrderGridSortedColumnIndexList[currTabIdx];
                if (currDGV.Columns[sortGlyphIndex].SortMode != DataGridViewColumnSortMode.NotSortable)
                {
                    ClearSortGlyph(currDGV, sortGlyphIndex);
                    currDGV.Columns[sortGlyphIndex].HeaderCell.SortGlyphDirection = OrderGridSortedOrderList[currTabIdx];
                }

                //#if DEBUG
                // check sum of order price (to compare with SQL DB)
                labelTtlOrders.Visible = true;
                labelTtlOrdersVal.Visible = true;
                labelTtlOrderPrice.Visible = true;
                labelTtlOrderPriceVal.Visible = true;
                labelTtlOrdersVal.Text = currDGV.RowCount.ToString();
                decimal orderPrice = 0;
                for (int i = 0; i < currDGV.RowCount; i++)
                    orderPrice += Convert.ToDecimal(currDGV.Rows[i].Cells[strOrderCol_OrderPrice].Value.ToString().Replace(",", ""));
                labelTtlOrderPriceVal.Text = orderPrice.ToString();
                //#endif

                // handle double click from event form, highlight the the double clicked orders (row)
                if (OrderGridSelectRowAfterSort)//currDGV == dataGridViewAllOrder && 
                {
                    DataGridViewRow eventFormDoubleClickedRow = null;
                    if (!GetGridDict(currDGV).TryGetValue(selectRowOrderNoSort, out eventFormDoubleClickedRow) || eventFormDoubleClickedRow == null ||
                        eventFormDoubleClickedRow.Index < 0)
                        return;
                    // Highlight selected row 
                    for (int i = 1; i < currDGV.ColumnCount - 1; i++)
                    {
                        eventFormDoubleClickedRow.Cells[i].Style.BackColor = Color.DarkBlue;
                        eventFormDoubleClickedRow.Cells[i].Style.ForeColor = Color.White;
                    }
                    currDGV.FirstDisplayedCell = eventFormDoubleClickedRow.Cells[0];
                    string strOrderNoSort = eventFormDoubleClickedRow.Cells[iOrderNoSortColIdx].Value.ToString();
                    ClearPreviousSelectedRow();
                    prevSelectedOrderNo[currTabIdx] = strOrderNoSort;
                    currDGV.ClearSelection();
                    currDGV.Refresh();
                    eventFormDoubleClickedRow = null;
                    OrderGridSelectRowAfterSort = false;
                }
            }
        }

        private void ClearSortGlyph(DataGridView DataGridView1, int columnIndex)
        {
            for (int i = 0; i < DataGridView1.Columns.Count; i++)
            {
                if (i != columnIndex)
                    DataGridView1.Columns[i].HeaderCell.SortGlyphDirection = SortOrder.None;
            }
        }

        private void SortGridTimer_Tick(object sender, EventArgs e)
        {
            SortGridTimer.Enabled = false;
            SortGrid();
            //Console.WriteLine("sort counter: " + SortCounter);
        }

        #endregion

        #region "Init Refresh Grid"

        private void Refresh_OrderGridHeader(DataGridView DataGridView1, bool IsInit)
        {
            if (DataGridView1 == dataGridViewRepliedOrder && TradeDB.UserType == UserTypeEnum.Client)
                return;
            if (DataGridView1.Columns.Count < TOTAL_COLUMN) 
                DataGridView1.ColumnCount = TOTAL_COLUMN;

            //if (IsInit)
            //{
                DataGridView1.BackgroundColor = Color.AliceBlue;
                DataGridView1.Columns[iOrderNoColIdx].Name = strOrderCol_OrderNo;
                DataGridView1.Columns[1].Name = strOrderCol_AECode;
                DataGridView1.Columns[2].Name = strOrderCol_AccNo;
                DataGridView1.Columns[3].Name = strOrderCol_OrderSide;
                DataGridView1.Columns[4].Name = strOrderCol_ExCode;
                DataGridView1.Columns[5].Name = strOrderCol_StkCode;
                DataGridView1.Columns[iOrderPriceColIdx].Name = strOrderCol_OrderPrice;
                DataGridView1.Columns[7].Name = strOrderCol_OrderQty;
                DataGridView1.Columns[iOrderAvgPriceColIdx].Name = strOrderCol_AvgPrice;
                DataGridView1.Columns[9].Name = strOrderCol_FilledQty;
                DataGridView1.Columns[10].Name = strOrderCol_OrderStatus;
                DataGridView1.Columns[11].Name = strOrderCol_LastAction;
                DataGridView1.Columns[12].Name = strOrderCol_OrderStatusCode;
                DataGridView1.Columns[13].Name = strOrderCol_OrderType;
                DataGridView1.Columns[iOrderNoSortColIdx].Name = strOrderCol_OrderNoSort;
                DataGridView1.Columns[15].Name = strOrderCol_StatusMessage;
                DataGridView1.Columns[16].Name = strOrderCol_OGMessage;

                DataGridView1.Columns[strOrderCol_OrderNo].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                DataGridView1.Columns[strOrderCol_AECode].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                DataGridView1.Columns[strOrderCol_OrderSide].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                DataGridView1.Columns[strOrderCol_StkCode].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                DataGridView1.Columns[strOrderCol_ExCode].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                DataGridView1.Columns[strOrderCol_OrderPrice].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                DataGridView1.Columns[strOrderCol_OrderQty].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                DataGridView1.Columns[strOrderCol_AvgPrice].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                DataGridView1.Columns[strOrderCol_FilledQty].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

                DataGridView1.Columns[strOrderCol_OrderNo].SortMode = DataGridViewColumnSortMode.Programmatic;
                DataGridView1.Columns[strOrderCol_OrderNoSort].SortMode = DataGridViewColumnSortMode.Programmatic;
                DataGridView1.Columns[strOrderCol_StkCode].SortMode = DataGridViewColumnSortMode.Programmatic;
                DataGridView1.Columns[strOrderCol_ExCode].SortMode = DataGridViewColumnSortMode.Programmatic;
                DataGridView1.Columns[strOrderCol_OrderPrice].SortMode = DataGridViewColumnSortMode.Programmatic;
                DataGridView1.Columns[strOrderCol_OrderQty].SortMode = DataGridViewColumnSortMode.Programmatic;
                DataGridView1.Columns[strOrderCol_AvgPrice].SortMode = DataGridViewColumnSortMode.Programmatic;
                DataGridView1.Columns[strOrderCol_FilledQty].SortMode = DataGridViewColumnSortMode.Programmatic;
                DataGridView1.Columns[strOrderCol_LastAction].SortMode = DataGridViewColumnSortMode.Programmatic;

                DataGridView1.Columns[strOrderCol_OrderNo].FillWeight = 50;
                DataGridView1.Columns[strOrderCol_AECode].FillWeight = 80;
                DataGridView1.Columns[strOrderCol_AccNo].FillWeight = 100;
                DataGridView1.Columns[strOrderCol_OrderSide].FillWeight = 60;
                DataGridView1.Columns[strOrderCol_StkCode].FillWeight = 80;
                DataGridView1.Columns[strOrderCol_ExCode].FillWeight = 50;
                DataGridView1.Columns[strOrderCol_OrderPrice].FillWeight = 90;
                DataGridView1.Columns[strOrderCol_OrderQty].FillWeight = 120;
                DataGridView1.Columns[strOrderCol_AvgPrice].FillWeight = 90;
                DataGridView1.Columns[strOrderCol_FilledQty].FillWeight = 120;
                DataGridView1.Columns[strOrderCol_OrderStatus].FillWeight = 120;
                DataGridView1.Columns[strOrderCol_LastAction].FillWeight = 120;

                DataGridView1.Columns[strOrderCol_OrderNo].MinimumWidth = 43;
                DataGridView1.Columns[strOrderCol_AECode].MinimumWidth = 33;
                DataGridView1.Columns[strOrderCol_AccNo].MinimumWidth = 55;
                DataGridView1.Columns[strOrderCol_OrderSide].MinimumWidth = 40;
                DataGridView1.Columns[strOrderCol_StkCode].MinimumWidth = 51;
                DataGridView1.Columns[strOrderCol_ExCode].MinimumWidth = 35;
                DataGridView1.Columns[strOrderCol_OrderPrice].MinimumWidth = 48;
                DataGridView1.Columns[strOrderCol_OrderQty].MinimumWidth = 65;
                DataGridView1.Columns[strOrderCol_AvgPrice].MinimumWidth = 48;
                DataGridView1.Columns[strOrderCol_FilledQty].MinimumWidth = 65;
                DataGridView1.Columns[strOrderCol_OrderStatus].MinimumWidth = 50;
                DataGridView1.Columns[strOrderCol_LastAction].MinimumWidth = 15;
                //}

            DataGridView1.Columns[strOrderCol_OrderNo].HeaderText = GetResxString("AllOrder_Orderno");
            DataGridView1.Columns[strOrderCol_AccNo].HeaderText = GetResxString("AllOrder_AccountNo");
            DataGridView1.Columns[strOrderCol_AECode].HeaderText = GetResxString("AllOrder_AECode");
            DataGridView1.Columns[strOrderCol_StkCode].HeaderText = GetResxString("AllOrder_StockCode");
            DataGridView1.Columns[strOrderCol_ExCode].HeaderText = GetResxString("AllOrder_Exchange");
            DataGridView1.Columns[strOrderCol_OrderSide].HeaderText = GetResxString("AllOrder_Order_Side");
            DataGridView1.Columns[strOrderCol_OrderPrice].HeaderText = GetResxString("AllOrder_Order_Price");
            DataGridView1.Columns[strOrderCol_OrderQty].HeaderText = GetResxString("AllOrder_Order_Qty");
            DataGridView1.Columns[strOrderCol_AvgPrice].HeaderText = GetResxString("AllOrder_AvgPrice");
            DataGridView1.Columns[strOrderCol_FilledQty].HeaderText = GetResxString("AllOrder_Filled_Qty");
            DataGridView1.Columns[strOrderCol_OrderStatus].HeaderText = GetResxString("AllOrder_OrderStatus");
            DataGridView1.Columns[strOrderCol_LastAction].HeaderText = GetResxString("AllOrder_LastAction");
            DataGridView1.Columns[strOrderCol_OrderStatusCode].Visible = false;
            DataGridView1.Columns[strOrderCol_OrderType].Visible = false;
            DataGridView1.Columns[strOrderCol_OrderNoSort].Visible = false;
            DataGridView1.Columns[strOrderCol_StatusMessage].Visible = false;
            DataGridView1.Columns[strOrderCol_OGMessage].Visible = false;
            //DataGridView1.Columns[strOrderCol_ExCode].Visible = false;

            if (Enable_CancelOrderBtnCol(DataGridView1))
            {
                if (IsInit)
                {
                    DataGridViewDisableButtonColumn delColumn = new DataGridViewDisableButtonColumn();
                    delColumn.Name = strOrderCol_DelBtn;
                    delColumn.HeaderText = GetResxString("AllOrder_DeleteOrder");
                    DataGridView1.Columns.Add(delColumn);
                }
                DataGridView1.Columns[strOrderCol_DelBtn].HeaderText = GetResxString("AllOrder_DeleteOrder");
                DataGridView1.Columns[strOrderCol_DelBtn].FillWeight = 80;
                DataGridView1.Columns[strOrderCol_DelBtn].MinimumWidth = 40;
            }

            if (IsInit)
            {
                DataGridViewTextBoxColumn memoColumn = new DataGridViewTextBoxColumn();
                memoColumn.Name = strOrderCol_Memo;
                memoColumn.HeaderText = GetResxString("AllOrder_Memo");
                DataGridView1.Columns.Add(memoColumn);
            }
            DataGridView1.Columns[strOrderCol_Memo].HeaderText = GetResxString("AllOrder_Memo");
            DataGridView1.Columns[strOrderCol_Memo].FillWeight = 60;
            DataGridView1.Columns[strOrderCol_Memo].MinimumWidth = 20;
            if (TradeDB.UserType != UserTypeEnum.AE)
            {
                DataGridView1.Columns[strOrderCol_AccNo].Visible = false;
                DataGridView1.Columns[strOrderCol_AECode].Visible = false;
                DataGridView1.Columns[strOrderCol_Memo].Visible = false;
            }

            if (Enable_RpyChkBoxCol(DataGridView1))
            {
                if (IsInit)
                {
                    DataGridViewDisableCheckBoxColumn chkBoxRplyColumn = new DataGridViewDisableCheckBoxColumn();
                    chkBoxRplyColumn.Name = strOrderCol_ReplyChkBox;
                    chkBoxRplyColumn.HeaderText = GetResxString("AllOrder_Reply");
                    DataGridView1.Columns.Add(chkBoxRplyColumn);
                }
                DataGridView1.Columns[strOrderCol_ReplyChkBox].HeaderText = GetResxString("AllOrder_Reply");
                DataGridView1.Columns[strOrderCol_ReplyChkBox].FillWeight = 83;
                DataGridView1.Columns[strOrderCol_ReplyChkBox].MinimumWidth = 20;
            }

            //////if (Enable_CancelOrderBtnCol(DataGridView1))
            //////{
            //////    if (IsInit)
            //////    {
            //////        DataGridViewDisableButtonColumn delColumn = new DataGridViewDisableButtonColumn();
            //////        delColumn.Name = strOrderCol_DelBtn;
            //////        delColumn.HeaderText = GetResxString("AllOrder_DeleteOrder");
            //////        DataGridView1.Columns.Add(delColumn);
            //////    }
            //////    DataGridView1.Columns[strOrderCol_DelBtn].HeaderText = GetResxString("AllOrder_DeleteOrder");
            //////    DataGridView1.Columns[strOrderCol_DelBtn].FillWeight = 80;
            //////    DataGridView1.Columns[strOrderCol_DelBtn].MinimumWidth = 40;
            //////}

            //int colCount = DataGridView1.ColumnCount;
            //for (int i = 0; i < colCount; i++)
            //{
            //    if (Culture.Name == "en-US" || DataGridView1.Columns[i].Name != strOrderCol_OrderSide || DataGridView1.Columns[i].Name != strOrderCol_OrderStatus || 
            //        DataGridView1.Columns[i].Name != strOrderCol_DelBtn)
            //        DataGridView1.Columns[i].DefaultCellStyle.Font = new System.Drawing.Font("Arial", 9F, FontStyle.Bold);
            //    else
            //        DataGridView1.Columns[i].DefaultCellStyle.Font = new System.Drawing.Font("MingLiU", 9F, FontStyle.Bold);
            //}
            DataGridView1.Columns[strOrderCol_OrderNo].DefaultCellStyle.Font = new System.Drawing.Font("Arial", 9F, FontStyle.Bold);
            DataGridView1.Columns[strOrderCol_AECode].DefaultCellStyle.Font = new System.Drawing.Font("Arial", 9F, FontStyle.Bold);
            DataGridView1.Columns[strOrderCol_AccNo].DefaultCellStyle.Font = new System.Drawing.Font("Arial", 9F, FontStyle.Bold);
            DataGridView1.Columns[strOrderCol_StkCode].DefaultCellStyle.Font = new System.Drawing.Font("Arial", 9F, FontStyle.Bold);
            DataGridView1.Columns[strOrderCol_ExCode].DefaultCellStyle.Font = new System.Drawing.Font("Arial", 9F, FontStyle.Bold);
            DataGridView1.Columns[strOrderCol_OrderPrice].DefaultCellStyle.Font = new System.Drawing.Font("Arial", 9F, FontStyle.Bold);
            DataGridView1.Columns[strOrderCol_OrderQty].DefaultCellStyle.Font = new System.Drawing.Font("Arial", 9F, FontStyle.Bold);
            DataGridView1.Columns[strOrderCol_AvgPrice].DefaultCellStyle.Font = new System.Drawing.Font("Arial", 9F, FontStyle.Bold);
            DataGridView1.Columns[strOrderCol_FilledQty].DefaultCellStyle.Font = new System.Drawing.Font("Arial", 9F, FontStyle.Bold);
            DataGridView1.Columns[strOrderCol_LastAction].DefaultCellStyle.Font = new System.Drawing.Font("Arial", 9F, FontStyle.Bold);
            DataGridView1.Columns[strOrderCol_Memo].DefaultCellStyle.Font = new System.Drawing.Font("Arial", 9F, FontStyle.Bold);

            if (Culture.Name == "en-US")
            {
                DataGridView1.Columns[strOrderCol_OrderSide].DefaultCellStyle.Font = new System.Drawing.Font("Arial", 9F, FontStyle.Bold);
                DataGridView1.Columns[strOrderCol_OrderStatus].DefaultCellStyle.Font = new System.Drawing.Font("Arial", 9F, FontStyle.Bold);
                DataGridView1.Columns[strOrderCol_LastAction].DefaultCellStyle.Font = new System.Drawing.Font("Arial", 9F, FontStyle.Bold);
            }
            else
            {
                DataGridView1.Columns[strOrderCol_OrderSide].DefaultCellStyle.Font = new System.Drawing.Font("MingLiU", 9F, FontStyle.Bold);
                DataGridView1.Columns[strOrderCol_OrderStatus].DefaultCellStyle.Font = new System.Drawing.Font("MingLiU", 9F, FontStyle.Bold);
                DataGridView1.Columns[strOrderCol_LastAction].DefaultCellStyle.Font = new System.Drawing.Font("MingLiU", 9F, FontStyle.Bold);
            }

            DataGridView1.ShowCellToolTips = false;
            DataGridView1.Columns[0].Selected = true;
            DataGridView1.Sort(DataGridView1.Columns[0], ListSortDirection.Descending);

            // Automend
            if (AutoAmend == true)
            {
                if (Enable_CancelOrderBtnCol(DataGridView1))
                {
                    if (IsInit)
                    {
                        DataGridViewDisableButtonColumn AAmendColumnDown = new DataGridViewDisableButtonColumn();
                        AAmendColumnDown.Name = strOrderCol_AAmendBtnDown;
                        AAmendColumnDown.HeaderText = GetResxString("AllOrder_AAmendOrderDown");
                        DataGridView1.Columns.Add(AAmendColumnDown);

                        DataGridViewDisableButtonColumn AAmendColumnUp = new DataGridViewDisableButtonColumn();
                        AAmendColumnUp.Name = strOrderCol_AAmendBtnUp;
                        AAmendColumnUp.HeaderText = GetResxString("AllOrder_AAmendOrderUp");
                        DataGridView1.Columns.Add(AAmendColumnUp);

                    }
                    DataGridView1.Columns[strOrderCol_AAmendBtnUp].HeaderText = GetResxString("AllOrder_AAmendOrderUpHeader");
                    DataGridView1.Columns[strOrderCol_AAmendBtnUp].FillWeight = 60;
                    DataGridView1.Columns[strOrderCol_AAmendBtnUp].MinimumWidth = 40;

                    DataGridView1.Columns[strOrderCol_AAmendBtnDown].HeaderText = GetResxString("AllOrder_AAmendOrderDownHeader");
                    DataGridView1.Columns[strOrderCol_AAmendBtnDown].FillWeight = 60;
                    DataGridView1.Columns[strOrderCol_AAmendBtnDown].MinimumWidth = 40;
                }

                DataGridView1.ShowCellToolTips = false;
                DataGridView1.Columns[0].Selected = true;
                DataGridView1.Sort(DataGridView1.Columns[0], ListSortDirection.Descending);
            }
        }

        private void Init_DealGrid()
        {
            DataGridViewTextBoxColumn Column1 = new DataGridViewTextBoxColumn(), Column2 = new DataGridViewTextBoxColumn();
            DataGridViewTextBoxColumn Column3 = new DataGridViewTextBoxColumn(), Column4 = new DataGridViewTextBoxColumn();
            DataGridViewTextBoxColumn Column5 = new DataGridViewTextBoxColumn(), Column6 = new DataGridViewTextBoxColumn();
            DataGridViewTextBoxColumn Column7 = new DataGridViewTextBoxColumn(), Column8 = new DataGridViewTextBoxColumn();
            Column1.Name = "OrderNo"; Column2.Name = "Exchange"; Column3.Name = "StockCode"; Column4.Name = "Order_Side";
            Column5.Name = "Price"; Column6.Name = "Qty"; Column7.Name = "Time"; Column8.Name = "BrokerId";


            Column1.DefaultCellStyle.Alignment = Column2.DefaultCellStyle.Alignment = Column3.DefaultCellStyle.Alignment =
                Column4.DefaultCellStyle.Alignment = Column5.DefaultCellStyle.Alignment = Column6.DefaultCellStyle.Alignment =
                Column7.DefaultCellStyle.Alignment = Column8.DefaultCellStyle.Alignment = 
                DataGridViewContentAlignment.MiddleRight;

            Column1.SortMode = Column2.SortMode = Column3.SortMode = Column4.SortMode = Column5.SortMode = 
                Column6.SortMode = Column7.SortMode = Column8.SortMode = DataGridViewColumnSortMode.Programmatic;

            Column2.FillWeight = Column4.FillWeight = Column8.FillWeight = 35;
            Column1.FillWeight = Column3.FillWeight = Column5.FillWeight = 50;
            Column6.FillWeight = Column7.FillWeight = 65;

            Column2.MinimumWidth = Column4.MinimumWidth = Column8.MinimumWidth = 20;
            Column1.MinimumWidth = Column3.MinimumWidth = Column5.MinimumWidth = 33;
            Column6.MinimumWidth = Column7.MinimumWidth = 45;

            Column1.HeaderText = GetResxString("AllOrder_Orderno"); Column2.HeaderText = GetResxString("AllOrder_Exchange");
            Column3.HeaderText = GetResxString("Deal_StkCode"); Column4.HeaderText = GetResxString("Deal_BuySell");
            Column5.HeaderText = GetResxString("Deal_Price"); Column6.HeaderText = GetResxString("Deal_FilledQty");
            Column7.HeaderText = GetResxString("Deal_Time"); Column8.HeaderText = GetResxString("Deal_BrokerId");
            dataGridViewDeal.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { Column1, Column2, Column3, Column4, Column5, Column6, Column7, Column8 });
            dataGridViewDeal.BackgroundColor = Color.AliceBlue;
        }

        private void Refresh_DealGridHeader()
        {
            if (dataGridViewDeal.Columns.Count <= 0)
                return;

            dataGridViewDeal.Columns["OrderNo"].HeaderText = GetResxString("AllOrder_Orderno"); dataGridViewDeal.Columns["Exchange"].HeaderText = GetResxString("AllOrder_Exchange");
            dataGridViewDeal.Columns["StockCode"].HeaderText = GetResxString("Deal_StkCode"); dataGridViewDeal.Columns["Order_Side"].HeaderText = GetResxString("Deal_BuySell");
            dataGridViewDeal.Columns["Price"].HeaderText = GetResxString("Deal_Price"); dataGridViewDeal.Columns["Qty"].HeaderText = GetResxString("Deal_FilledQty");
            dataGridViewDeal.Columns["Time"].HeaderText = GetResxString("Deal_Time"); dataGridViewDeal.Columns["BrokerId"].HeaderText = GetResxString("Deal_BrokerId");
        }

        private void Refresh_AllOrderGridHeader(bool IsInit)
        {
            Refresh_OrderGridHeader(dataGridViewAllOrder, IsInit);
            Refresh_OrderGridHeader(dataGridViewRepliedOrder, IsInit);
            Refresh_OrderGridHeader(dataGridViewCompletedOrder, IsInit);
            Refresh_OrderGridHeader(dataGridViewPendOrder, IsInit);
        }

        #endregion

        # region "Assistant Functions"

        private bool CheckCanDisplay(DataGridView datagridview1, int orderstatus, bool replied, ExchangeTypeEnum ExType)
        {
            bool CanDisplay = false;

            if (datagridview1.Name == "dataGridViewPendOrder")
            {
                if (orderstatus == (int)Order.OrderStatusEnum.Pending || orderstatus == (int)Order.OrderStatusEnum.CreditChecking ||
                    orderstatus == (int)Order.OrderStatusEnum.CreditPass || orderstatus == (int)Order.OrderStatusEnum.CreditFailed ||
                    orderstatus == (int)Order.OrderStatusEnum.Queue || orderstatus == (int)Order.OrderStatusEnum.Sending || 
                    orderstatus == (int)Order.OrderStatusEnum.SentToOG)
                    CanDisplay = true;
            }
            else if (datagridview1.Name == "dataGridViewCompletedOrder")
            {
                if ((orderstatus == (int)Order.OrderStatusEnum.Completed || orderstatus == (int)Order.OrderStatusEnum.Cancelled ||
                    orderstatus == (int)Order.OrderStatusEnum.PartiallyCompleted || orderstatus == (int)Order.OrderStatusEnum.RejectedByOG || 
                    orderstatus == (int)Order.OrderStatusEnum.RejectedBySupervisor || orderstatus == (int)Order.OrderStatusEnum.InvalidTradePass || 
                    orderstatus == (int)Order.OrderStatusEnum.InputError) && 
                    (!replied || TradeDB.UserType == UserTypeEnum.Client))
                    CanDisplay = true;
            }
            else if (datagridview1.Name == "dataGridViewRepliedOrder")
            {
                if ((orderstatus == (int)Order.OrderStatusEnum.Completed || orderstatus == (int)Order.OrderStatusEnum.PartiallyCompleted) && replied)
                    CanDisplay = true;
            }
            else if (datagridview1.Name == "dataGridViewAllOrder")
                CanDisplay = true;

            return CanDisplay;
        }

        private bool PrepareCancel(string strOrderNo)
        {
            string ErrorMsg;

            int DelOrderNo;
            int.TryParse(strOrderNo, out DelOrderNo);
            bool success = TradeDB.OrderCancel(DelOrderNo, out ErrorMsg);
            return success;
        }

        private bool PrepareAAmend(string Stockcode, char OrderSide, int Orderno, decimal Price, int Qty)
        {
            string ErrorMsg;
            decimal newPrice = -1;
            //int newQty;

            //Order order1 = new Order();
            //int.TryParse(labelOrderNum.Text, out order1.OrderNo);
            //decimal.TryParse(labelStockPrice.Text, out order1.Price);
            //int.TryParse(labelStockQty.Text.Replace(",", ""), out order1.Quantity);
            //decimal.TryParse(textBoxNewStkPrice.Text, out newPrice);
            //int.TryParse(textBoxNewStkQty.Text.Replace(",", ""), out newQty);
            //if (order1.Price == newPrice && order1.Quantity == newQty)
            //{
            //    // no changed
            //}
            //else
            //{

            bool CanBuy = false, CanSell = false;

            if (AAmendOrder.ExType == ExchangeTypeEnum.HKG)
                newPrice = Utils.OrderTypeSpread.CheckSpread(Price, 'L', ' ', intAAmendSpread, CurrStock, TradeDB, out CanBuy, out CanSell);
            else if (AAmendOrder.ExType == ExchangeTypeEnum.SHG || AAmendOrder.ExType == ExchangeTypeEnum.SZE)
                newPrice = Utils.OrderTypeSpread.CheckSpread_ASHR(Price, 'X', ' ', intAAmendSpread, CurrStock, TradeDB, out CanBuy, out CanSell);

            if (OrderSide == 'B')
            {
                if (newPrice <= 0 || (newPrice > CurrStock.Ask && CurrStock.Ask > 0)) return false;
            }
            else if (OrderSide == 'A')
            {
                if (newPrice <= 0 || newPrice < CurrStock.Bid) return false;
            }

            if (newPrice <= 0) return false;  // out of arrange

            bool success = TradeDB.OrderAmend(Orderno, newPrice, Qty, 0, out ErrorMsg);
            //    if (success == true)
            //    {
            //        textBoxNewStkPrice.Enabled = false;
            //        textBoxNewStkQty.Enabled = false;
            //        buttonGo.Enabled = false;
            //        buttonMax.Enabled = false;
            //        vScrollBarPrice.Enabled = false;
            //        vScrollBarQty.Enabled = false;
            //    }
            //}
            return success;
        }


        private bool IsReplied(char replied)
        {
            return (replied == 'Y');
        }

        private bool IsStatusReadyToReply(int status)
        {
            return (status == (int)Order.OrderStatusEnum.Completed || status == (int)Order.OrderStatusEnum.PartiallyCompleted ||
                status == (int)Order.OrderStatusEnum.Cancelled || status == (int)Order.OrderStatusEnum.RejectedByOG ||
                status == (int)Order.OrderStatusEnum.InvalidTradePass || status == (int)Order.OrderStatusEnum.InputError ||
                status == (int)Order.OrderStatusEnum.RejectedBySupervisor);
            //status == (int)Order.OrderStatusEnum.Cancelled || status == (int)Order.OrderStatusEnum.RejectedByOG);
        }

        private bool Enable_CancelOrderBtnCol(DataGridView DataGridView1)
        {
            return (DataGridView1 != dataGridViewCompletedOrder && DataGridView1 != dataGridViewRepliedOrder);
        }

        /// <summary>
        /// Whether to enable reply check box column
        /// </summary>
        /// <returns></returns>
        private bool Enable_RpyChkBoxCol(DataGridView DataGridView1)
        {
            if (TradeDB.UserType == UserTypeEnum.Client)
                return false;
            else
                return (DataGridView1 != dataGridViewRepliedOrder && DataGridView1 != dataGridViewPendOrder);
        }

        private Dictionary<string, DataGridViewRow> GetGridDict(DataGridView dgv)
        {
            if (dgv == dataGridViewAllOrder)
                return AllOrderGridDict;
            else if (dgv == dataGridViewRepliedOrder)
                return RepliedOrderGridDict;
            else if (dgv == dataGridViewCompletedOrder)
                return CompletedOrderGridDict;
            else if (dgv == dataGridViewPendOrder)
                return PendOrderGridDict;
            return null;
        }

        private DataGridViewRow GetRow(DataGridView dgv, string orderNo)
        {
            DataGridViewRow returnRow = null;

            foreach (DataGridViewRow row in dgv.Rows)
                if (orderNo == row.Cells[strOrderCol_OrderNoSort].Value.ToString())
                    returnRow = row;
            return returnRow;
        }

        #endregion

        #region "Hide / Show Deal Grid"

        private void HideShowDealGrid(bool IsShow)
        {
            if (IsShow)
                panelBottom.Visible = true;
            else
                panelBottom.Visible = false;
        }

        private void checkBoxShowDeal_Click(object sender, EventArgs e)
        {
            LocalFormSettings["ShowDealGrid"] = checkBoxShowDeal.Checked.ToString();
            HideShowDealGrid(checkBoxShowDeal.Checked);
        }

        #endregion

        #region "IComparer"

        private class Int32Comparer : System.Collections.IComparer
        {
            private static int sortOrderModifier = 1;
            private static int sortColumnIndex = 13;

            public Int32Comparer(SortOrder sortOrder, int sortColumnIndex)
            {
                if (sortOrder == SortOrder.Descending)
                {
                    sortOrderModifier = -1;
                }
                else if (sortOrder == SortOrder.Ascending)
                {
                    sortOrderModifier = 1;
                }
                Int32Comparer.sortColumnIndex = sortColumnIndex;
            }

            public int Compare(object x, object y)
            {
                DataGridViewRow DataGridViewRow1 = (DataGridViewRow)x;
                DataGridViewRow DataGridViewRow2 = (DataGridViewRow)y;

                // Try to sort based on the Last Name column.
                string sx = DataGridViewRow1.Cells[sortColumnIndex].Value != null ? DataGridViewRow1.Cells[sortColumnIndex].Value.ToString().Replace(",", "") : "";
                string sy = DataGridViewRow2.Cells[sortColumnIndex].Value != null ? DataGridViewRow2.Cells[sortColumnIndex].Value.ToString().Replace(",", "") : "";
                int vx, vy;

                int CompareResult = 0;

                if (int.TryParse(sx, out vx))
                {
                    if (int.TryParse(sy, out vy))
                    {
                        if (vx < vy) CompareResult = -1;
                        else if (vx > vy) CompareResult = 1;
                    }
                    else
                    {
                        if (sy.Length <= 0) CompareResult = 1;
                        else CompareResult = -1;
                    }
                }
                else if (int.TryParse(sy, out vy))
                {
                    if (sx.Length <= 0) CompareResult = -1;
                    else CompareResult = 1;
                }
                else
                {
                    CompareResult = System.String.Compare(sx, sy);
                }

                if (CompareResult == 0)
                    CompareResult = String.Compare(DataGridViewRow1.Cells[0].Value.ToString(), DataGridViewRow2.Cells[0].Value.ToString());

                return CompareResult * sortOrderModifier;
            }
        }

        private class Double32Comparer : System.Collections.IComparer
        {
            private static int sortOrderModifier = 1;
            private static int sortColumnIndex = 13;

            public Double32Comparer(SortOrder sortOrder, int sortColumnIndex)
            {
                if (sortOrder == SortOrder.Descending)
                {
                    sortOrderModifier = -1;
                }
                else if (sortOrder == SortOrder.Ascending)
                {
                    sortOrderModifier = 1;
                }
                Double32Comparer.sortColumnIndex = sortColumnIndex;
            }

            public int Compare(object x, object y)
            {
                DataGridViewRow DataGridViewRow1 = (DataGridViewRow)x;
                DataGridViewRow DataGridViewRow2 = (DataGridViewRow)y;

                // Try to sort based on the Last Name column.
                string sx = DataGridViewRow1.Cells[sortColumnIndex].Value != null ? DataGridViewRow1.Cells[sortColumnIndex].Value.ToString().Replace(",", "") : "";
                string sy = DataGridViewRow2.Cells[sortColumnIndex].Value != null ? DataGridViewRow2.Cells[sortColumnIndex].Value.ToString().Replace(",", "") : "";
                double vx, vy;

                int CompareResult = 0;

                if (double.TryParse(sx, out vx))
                {
                    if (double.TryParse(sy, out vy))
                    {
                        if (vx < vy) CompareResult = -1;
                        else if (vx > vy) CompareResult = 1;
                    }
                    else
                    {
                        if (sy.Length <= 0) CompareResult = 1;
                        else CompareResult = -1;
                    }
                }
                else if (double.TryParse(sy, out vy))
                {
                    if (sx.Length <= 0) CompareResult = -1;
                    else CompareResult = 1;
                }
                else
                {
                    CompareResult = System.String.Compare(sx, sy);
                }

                if (CompareResult == 0)
                    CompareResult = String.Compare(DataGridViewRow1.Cells[0].Value.ToString(), DataGridViewRow2.Cells[0].Value.ToString());

                return CompareResult * sortOrderModifier;
            }
        }

        #endregion

        #region "User Mouse Click"

        private void ClearPreviousSelectedRow()
        {
            if (prevSelectedOrderNo[tabControlOrders.SelectedIndex] != "")
            {
                lock (GridMutex)
                {
                    DataGridViewRow row = null;
                    if (GetGridDict(currDGV).TryGetValue(prevSelectedOrderNo[tabControlOrders.SelectedIndex], out row) && row.Index >= 0)
                        RestoreRowColor(row);
                }
            }
        }

        private void dataGridView_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            //lock (GridClickMutex)
            {
                string strOrderNoSort = "";
                if (e.RowIndex < 0 || e.Button != MouseButtons.Left || currDGV == null || e.ColumnIndex < 0 || e.RowIndex < 0)// || currDGV.SelectedCells.Count == 0)
                    return;

                try
                {
                    labelTransacChargeVal.Text = "$0.0";
                    ClearDealGrid();
                    // Highlight selected row and clear high-light on previously selected row
                    strOrderNoSort = currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderNoSort].Value.ToString();
                    ClearPreviousSelectedRow();
                    for (int i = 1; i < (currDGV.Name == "dataGridViewAllOrder" ? currDGV.ColumnCount - 5 : currDGV.ColumnCount - 4); i++)
                    {
                        lock (GridMutex)
                        {
                            currDGV.Rows[e.RowIndex].Cells[i].Style.BackColor = Color.DarkBlue;
                            currDGV.Rows[e.RowIndex].Cells[i].Style.ForeColor = Color.White;
                        }
                    }
                    prevSelectedOrderNo[tabControlOrders.SelectedIndex] = strOrderNoSort;
                    lock (GridMutex)
                    {
                        currDGV.ClearSelection();
                        currDGV.Refresh();
                    }

                    string strOrderNo = currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderNo].Value.ToString();
                    if (currDGV.Rows[e.RowIndex].Cells[strOrderCol_StatusMessage].Value != null && currDGV.Rows[e.RowIndex].Cells[strOrderCol_StatusMessage].Value.ToString().Trim() != "")
                        labelRemark.Text = currDGV.Rows[e.RowIndex].Cells[strOrderCol_StatusMessage].Value.ToString();
                    else
                        labelRemark.Text = "";
                    if (currDGV.Rows[e.RowIndex].Cells[strOrderCol_OGMessage].Value != null &&
                        currDGV.Rows[e.RowIndex].Cells[strOrderCol_OGMessage].Value.ToString().Trim() != "" &&
                        (labelRemark.Text != currDGV.Rows[e.RowIndex].Cells[strOrderCol_OGMessage].Value.ToString()))
                        labelRemark.Text = labelRemark.Text + "\n" + currDGV.Rows[e.RowIndex].Cells[strOrderCol_OGMessage].Value.ToString();
                    int iOrderNo;
                    if (!int.TryParse(strOrderNo, out iOrderNo))
                        return;   // failed orders

                    string actionPlacedBy = null;
                    List<Order> Orders = TradeDB.GetOrderByOrderNo(new List<int> { iOrderNo });
                    if (Orders != null && Orders[0] != null)
                    {
                        actionPlacedBy = Orders[0].ActionPlacedBy.Trim();
                        if (actionPlacedBy != null)
                            labelPlacedByVal.Text = actionPlacedBy;
                    }

                    if (checkBoxShowSearch.Checked && checkBoxEnableSrch.Checked)
                    {
                        if (currDGV.Rows[e.RowIndex].Cells[strOrderCol_AccNo].Value != null)
                            comboBoxAccount.Text = currDGV.Rows[e.RowIndex].Cells[strOrderCol_AccNo].Value.ToString();
                        if (currDGV.Rows[e.RowIndex].Cells[strOrderCol_StkCode].Value != null)
                            textBoxStkCode.Text = currDGV.Rows[e.RowIndex].Cells[strOrderCol_StkCode].Value.ToString();
                        if (labelAccTarget.Text.Trim() == "")
                            comboBoxAccount.Focus();
                        else
                            textBoxStkCode.Focus();
                    }

                    // account # clicked, pass to account form to select the account
                    List<BaseForm> AccountBookFormList = BaseForm.GetFormByFormType(typeof(AccountForm));
                    if (AccountBookFormList != null && AccountBookFormList.Count > 0 && AccountBookFormList[0] != null && ((AccountForm)AccountBookFormList[0]).IsAccountComboEnabled())
                    {
                        AccountForm accForm = ((AccountForm)AccountBookFormList[0]);
                        accForm.AllowChangeOrderTicketAccount = true;
                        accForm.ListenedAccount = currDGV.Rows[e.RowIndex].Cells[strOrderCol_AccNo].Value.ToString();
                        //List<Order> Orders;
                        ExchangeTypeEnum ete = ExchangeTypeEnum.HKG;
                        Orders = TradeDB.GetOrderByOrderNo(new List<int> { iOrderNo });
                        if (Orders != null && Orders[0] != null)
                            ete = Orders[0].ExType;
                        int iStkCode;
                        int.TryParse(currDGV.Rows[e.RowIndex].Cells[strOrderCol_StkCode].Value.ToString(), out iStkCode);
                        accForm.ShowStock(Stock.GetSignature(ete, iStkCode.ToString()));
                    }

                    // Del button clicked
                    if (currDGV.Columns[e.ColumnIndex].Name == strOrderCol_DelBtn) 
                    {
                        DataGridViewDisableButtonCell buttonCell = (DataGridViewDisableButtonCell)currDGV.Rows[e.RowIndex].Cells[strOrderCol_DelBtn];

                        if (buttonCell.Enabled && currDGV.Rows[e.RowIndex].Cells[strOrderCol_AccNo].Value != null && currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderPrice].Value != null &&
                            currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderQty].Value != null)
                        {
                            ModifyOrder_ConfirmForm cancelConfirmForm = new ModifyOrder_ConfirmForm(this.Culture, null);
                            cancelConfirmForm.FormName = "CancelConfirmForm";
                            cancelConfirmForm.strStockCode = currDGV.Rows[e.RowIndex].Cells[strOrderCol_StkCode].Value.ToString();
                            cancelConfirmForm.strOrderNo = currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderNo].Value.ToString();
                            cancelConfirmForm.strExchangeCode = currDGV.Rows[e.RowIndex].Cells[strOrderCol_ExCode].Value.ToString();
                            cancelConfirmForm.AccountNo = currDGV.Rows[e.RowIndex].Cells[strOrderCol_AccNo].Value.ToString();
                            cancelConfirmForm.IsAmendOrder = false;
                            if (GetResxString("Buy") == currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderSide].Value.ToString())
                                cancelConfirmForm.IsBuyOrder = true;
                            else
                                cancelConfirmForm.IsBuyOrder = false;
                            //List<Order> Orders;
                            ExchangeTypeEnum ete = ExchangeTypeEnum.HKG;
                            Orders = TradeDB.GetOrderByOrderNo(new List<int> { iOrderNo });
                            if (Orders != null && Orders[0] != null)
                                ete = Orders[0].ExType;
                            if (ete == ExchangeTypeEnum.SHG || ete == ExchangeTypeEnum.SZE)
                                cancelConfirmForm.strStkPrice = "￥" + currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderPrice].Value.ToString();
                            else if (selectedDealStock != null && selectedDealStock.Currency != null)
                                cancelConfirmForm.strStkPrice = GetBestCurrencySign(selectedDealStock.Currency) + currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderPrice].Value.ToString();
                            else
                                cancelConfirmForm.strStkPrice = "$" + currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderPrice].Value.ToString();
                            cancelConfirmForm.strStkQty = currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderQty].Value.ToString();
                            if (cancelConfirmForm.ShowDialog(this) == DialogResult.OK)
                            {
                                bool success = PrepareCancel(strOrderNo);
                                if (success == true) 
                                    buttonCell.Enabled = false;
                            }
                        }
                        return;
                    }
                    else if (currDGV.Columns[e.ColumnIndex].Name == strOrderCol_ReplyChkBox)
                    {   // Reply check box clicked 
                        if (((DataGridViewDisableCheckBoxCell)currDGV.Rows[e.RowIndex].Cells[strOrderCol_ReplyChkBox]).Enabled)
                        {
                            currDGV.Rows[e.RowIndex].Cells[strOrderCol_ReplyChkBox].Value = !((bool)currDGV.Rows[e.RowIndex].Cells[strOrderCol_ReplyChkBox].Value);
                            if ((bool)currDGV.Rows[e.RowIndex].Cells[strOrderCol_ReplyChkBox].Value)
                            {
                                string errorMsg = "";
                                TradeDB.OrderReplied(iOrderNo, out errorMsg);
                                (currDGV.Rows[e.RowIndex].Cells[strOrderCol_ReplyChkBox] as DataGridViewDisableCheckBoxCell).Enabled = false;
                            }
                        }
                    }
                    // AAmend clicked Up
                    else if (currDGV.Columns[e.ColumnIndex].Name == strOrderCol_AAmendBtnUp)
                    {
                        if (AutoAmend == true)
                        {
                            //////if (CurrStock == null)
                            //////{
                            //////    SetCurrentStockCode(currDGV.Rows[e.RowIndex].Cells[strOrderCol_StkCode].Value.ToString());
                            //////} 

                            //////bool CanBuy, CanSell;
                            ////////Check Auction period
                            //////if (Utils.OrderTypeSpread.CheckSpread(0, 'A', 'B', 1, CurrStock, TradeDB, out CanBuy, out CanSell) == -1)
                            //////{
                                // Not Auction period

                                DataGridViewDisableButtonCell buttonCell = (DataGridViewDisableButtonCell)currDGV.Rows[e.RowIndex].Cells[strOrderCol_AAmendBtnUp];

                                if (buttonCell.Enabled && currDGV.Rows[e.RowIndex].Cells[strOrderCol_AccNo].Value != null && currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderPrice].Value != null &&
                                    currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderQty].Value != null)
                                {

                                    ModifyOrder_ConfirmForm modifyOrderConfirmForm = new ModifyOrder_ConfirmForm(this.Culture, null);
                                    modifyOrderConfirmForm.FormName = "ModifyOrder";
                                    if (currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderSide].Value.ToString().Trim() == GetResxString("Buy"))
                                    {
                                        modifyOrderConfirmForm.IsBuyOrder = true;
                                    }
                                    else if (currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderSide].Value.ToString().Trim() == GetResxString("Sell"))
                                    {
                                        modifyOrderConfirmForm.IsBuyOrder = false;
                                    }
                                    modifyOrderConfirmForm.strStockCode = currDGV.Rows[e.RowIndex].Cells[strOrderCol_StkCode].Value.ToString();
                                    modifyOrderConfirmForm.strOrderNo = currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderNo].Value.ToString();
                                    ExchangeTypeEnum ete = ExchangeTypeEnum.HKG;
                                    Orders = TradeDB.GetOrderByOrderNo(new List<int> { iOrderNo });
                                    if (Orders != null && Orders[0] != null)
                                        ete = Orders[0].ExType;
                                    modifyOrderConfirmForm.strExchangeCode = Stock.GetExchangeCode(ete);
                                    modifyOrderConfirmForm.strStkPrice = "$" + currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderPrice].Value.ToString() + " +1s";
                                    modifyOrderConfirmForm.strStkQty = currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderQty].Value.ToString();
                                    modifyOrderConfirmForm.StartPosition = FormStartPosition.CenterScreen;
                                    modifyOrderConfirmForm.AccountNo = currDGV.Rows[e.RowIndex].Cells[strOrderCol_AccNo].Value.ToString();
                                    if ((SettingsUserPreference["ConfirmBeforeQuickAmend"] != null && SettingsUserPreference["ConfirmBeforeQuickAmend"] == "0") || modifyOrderConfirmForm.ShowDialog(this) == DialogResult.OK)
                                    {
                                        //////PrepareAmend();

                                        AAmendOrder = new Order();
                                        AAmendOrder.StockCode = currDGV.Rows[e.RowIndex].Cells[strOrderCol_StkCode].Value.ToString();

                                        AAmendOrder.Side = ' ';
                                        if (currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderSide].Value.ToString().Trim() == GetResxString("Buy"))
                                        {
                                            AAmendOrder.Side = 'B';
                                        }
                                        else if (currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderSide].Value.ToString().Trim() == GetResxString("Sell"))
                                        {
                                            AAmendOrder.Side = 'A';
                                        }
                                        if (AAmendOrder.Side == ' ') return;

                                        int.TryParse(currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderNo].Value.ToString(), out AAmendOrder.OrderNo);
                                        decimal.TryParse(currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderPrice].Value.ToString(), out AAmendOrder.Price);
                                        int.TryParse(currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderQty].Value.ToString().Replace(",", ""), out AAmendOrder.Quantity);

                                        ete = ExchangeTypeEnum.HKG;
                                        Orders = TradeDB.GetOrderByOrderNo(new List<int> { AAmendOrder.OrderNo });
                                        if (Orders != null && Orders[0] != null)
                                            ete = Orders[0].ExType;
                                        AAmendOrder.ExType = ete;

                                        buttonCell.Enabled = false;
                                        IsAutoAmend = true;
                                        intAAmendSpread = 1;

                                        SetCurrentStockCode(AAmendOrder.StockCode, ete);

                                        AppendLog("1", "OrderBookForm:strOrderCol_AAmendBtnUp" + " Stockcode:" + AAmendOrder.StockCode + " Side:" + AAmendOrder.Side + " Stockcode:" + AAmendOrder.StockCode + " OrderNo:" + AAmendOrder.OrderNo + " Price:" + AAmendOrder.Price + " Quantity:" + AAmendOrder.Quantity, "dataGridView_CellMouseDown", false);
                                        //////this.Close();
                                    }

                                }
                            //////}
                            return;
                        }
                    }
                    // AAmend clicked Down
                    else if (currDGV.Columns[e.ColumnIndex].Name == strOrderCol_AAmendBtnDown)
                    {
                        if (AutoAmend == true)
                        {
                            //////bool CanBuy, CanSell;
                            ////////Check Auction period
                            //////if (Utils.OrderTypeSpread.CheckSpread(0, 'A', 'B', 1, CurrStock, TradeDB, out CanBuy, out CanSell) == -1)
                            //////{
                                // Not Auction period

                                DataGridViewDisableButtonCell buttonCell = (DataGridViewDisableButtonCell)currDGV.Rows[e.RowIndex].Cells[strOrderCol_AAmendBtnDown];

                                if (buttonCell.Enabled && currDGV.Rows[e.RowIndex].Cells[strOrderCol_AccNo].Value != null && currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderPrice].Value != null &&
                                    currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderQty].Value != null)
                                {

                                    ModifyOrder_ConfirmForm modifyOrderConfirmForm = new ModifyOrder_ConfirmForm(this.Culture, null);
                                    modifyOrderConfirmForm.FormName = "ModifyOrder";
                                    if (currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderSide].Value.ToString().Trim() == GetResxString("Buy"))
                                    {
                                        modifyOrderConfirmForm.IsBuyOrder = true;
                                    }
                                    else if (currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderSide].Value.ToString().Trim() == GetResxString("Sell"))
                                    {
                                        modifyOrderConfirmForm.IsBuyOrder = false;
                                    }
                                    modifyOrderConfirmForm.strStockCode = currDGV.Rows[e.RowIndex].Cells[strOrderCol_StkCode].Value.ToString();
                                    modifyOrderConfirmForm.strOrderNo = currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderNo].Value.ToString();
                                    ExchangeTypeEnum ete = ExchangeTypeEnum.HKG;
                                    Orders = TradeDB.GetOrderByOrderNo(new List<int> { iOrderNo });
                                    if (Orders != null && Orders[0] != null)
                                        ete = Orders[0].ExType;
                                    modifyOrderConfirmForm.strExchangeCode = Stock.GetExchangeCode(ete);
                                    modifyOrderConfirmForm.strStkPrice = "$" + currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderPrice].Value.ToString() + " -1s";
                                    modifyOrderConfirmForm.strStkQty = currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderQty].Value.ToString();
                                    modifyOrderConfirmForm.StartPosition = FormStartPosition.CenterScreen;
                                    modifyOrderConfirmForm.AccountNo = currDGV.Rows[e.RowIndex].Cells[strOrderCol_AccNo].Value.ToString();
                                    if ((SettingsUserPreference["ConfirmBeforeQuickAmend"] != null && SettingsUserPreference["ConfirmBeforeQuickAmend"] == "0") || modifyOrderConfirmForm.ShowDialog(this) == DialogResult.OK)
                                    {
                                        //////PrepareAmend();

                                        AAmendOrder = new Order();
                                        AAmendOrder.StockCode = currDGV.Rows[e.RowIndex].Cells[strOrderCol_StkCode].Value.ToString();

                                        AAmendOrder.Side = ' ';
                                        if (currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderSide].Value.ToString().Trim() == GetResxString("Buy"))
                                        {
                                            AAmendOrder.Side = 'B';
                                        }
                                        else if (currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderSide].Value.ToString().Trim() == GetResxString("Sell"))
                                        {
                                            AAmendOrder.Side = 'A';
                                        }
                                        if (AAmendOrder.Side == ' ') return;

                                        int.TryParse(currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderNo].Value.ToString(), out AAmendOrder.OrderNo);
                                        decimal.TryParse(currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderPrice].Value.ToString(), out AAmendOrder.Price);
                                        int.TryParse(currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderQty].Value.ToString().Replace(",", ""), out AAmendOrder.Quantity);

                                        ete = ExchangeTypeEnum.HKG;
                                        Orders = TradeDB.GetOrderByOrderNo(new List<int> { AAmendOrder.OrderNo });
                                        if (Orders != null && Orders[0] != null)
                                            ete = Orders[0].ExType;
                                        AAmendOrder.ExType = ete;

                                        buttonCell.Enabled = false;
                                        IsAutoAmend = true;
                                        intAAmendSpread = -1;
                                        SetCurrentStockCode(AAmendOrder.StockCode, ete);

                                        AppendLog("1", "OrderBookForm:strOrderCol_AAmendBtnDown" + " Stockcode:" + AAmendOrder.StockCode + " Side:" + AAmendOrder.Side + " Stockcode:" + AAmendOrder.StockCode + " OrderNo:" + AAmendOrder.OrderNo + " Price:" + AAmendOrder.Price + " Quantity:" + AAmendOrder.Quantity, "dataGridView_CellMouseDown", false);

                                        //////this.Close();
                                    }


                                }
                            //////}
                            return;
                        }
                    }
                    else
                    {   // Update deal grid
                        UpdateDealGrid(strOrderNo, currDGV.Rows[e.RowIndex].Cells[strOrderCol_ExCode].Value.ToString(), currDGV.Rows[e.RowIndex].Cells[strOrderCol_StkCode].Value.ToString(),
                            currDGV.Rows[e.RowIndex].Cells[strOrderCol_OrderSide].Value.ToString());
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "dataGridView_CellMouseDown");
                }
            }
        }

        private void ClearDealGrid()
        {
            selectedStockCode = selectedExchangeCode = selectedOrderSide = "";
            selectedDealStock = null;
            DealGridArrivedOrder = null;
            dataGridViewDeal.Rows.Clear();
            dataGridViewDeal.Refresh();
            labelCurrency.Text = "";
        }

        private void RefreshDealGrid(Order ord)
        { // new deal arrival
            if (DealGridArrivedOrder == null || DealGridArrivedOrder == null || ord.OrderNo != DealGridArrivedOrder.OrderNo || 
                ord.Filled == DealGridArrivedOrder.Filled)
                return;

            dataGridViewDeal.Rows.Clear();
            dataGridViewDeal.Refresh();
            dataGridViewDeal.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;    // performance reason
            foreach (KeyValuePair<int, Deal> kvpNewDeal in ord.Deals)
            {
                if (kvpNewDeal.Value != null)
                {
                    string[] row1 = new string[] { kvpNewDeal.Value.OrderNo.ToString(), selectedExchangeCode, selectedStockCode, 
                        selectedOrderSide, kvpNewDeal.Value.Price.ToString(), String.Format("{0:N0}", kvpNewDeal.Value.Quantity), 
                        kvpNewDeal.Value.Time.ToString("HH:mm:ss"), kvpNewDeal.Value.BrokerId };
                    dataGridViewDeal.Rows.Add(row1);
                }
            }
            DealGridSortedOrder = SortOrder.Descending; // Rest to sort by time descending when new deal arrived
            SortDealGrid(6);
            DealGridArrivedOrder = ord.Clone();
        }

        private void UpdateDealGrid(string strOrderNo, string exchangeCode, string strStkCode, string strOrderSide)
        { // when user first select an order
            if (checkBoxShowDeal.Checked)
            {
                try
                {
                    ClearDealGrid();
                    selectedDealStock = null;
                    selectedStockCode = strStkCode;
                    selectedExchangeCode = exchangeCode;
                    selectedOrderSide = strOrderSide;
                    GetStock(new List<string> { Stock.GetSignature(exchangeCode, strStkCode) } ); // ;;;

                    int orderNo;
                    if (!int.TryParse(strOrderNo, out orderNo))
                        return;
                    List<Order> Orders;
                    Orders = TradeDB.GetOrderByOrderNo(new List<int> { orderNo });
                    foreach (Order ord in Orders)
                    {
                        DealGridArrivedOrder = ord.Clone();
                        foreach (KeyValuePair<int, Deal> kvp in ord.Deals)
                        {
                            if (kvp.Value != null)
                            {
                                string[] row1 = new string[] { kvp.Value.OrderNo.ToString(), exchangeCode, strStkCode, strOrderSide, 
                                kvp.Value.Price.ToString(), String.Format("{0:N0}", kvp.Value.Quantity), kvp.Value.Time.ToString("HH:mm:ss"), kvp.Value.BrokerId };
                                dataGridViewDeal.Rows.Add(row1);
                            }
                        }
                    }

                    DealGridSortedColumnIndex = 6; // Reset to sort by time descending when new order is selected
                    DealGridSortedOrder = SortOrder.Descending;
                    SortDealGrid(6);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "DB Error");
                }
            }
        }

        private void dataGridView_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            //lock (GridClickMutex)
            {
                if (currDGV != null && currDGV.SelectedCells.Count != 0)
                {
                    currDGV.ClearSelection();
                    currDGV.Refresh();
                }
            }
        }

        private void dataGridViewOrder_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            //lock (GridClickMutex)
            {
                try
                {
                    if (e.RowIndex == -1 && currDGV.Columns[e.ColumnIndex].SortMode != DataGridViewColumnSortMode.NotSortable)  // && e.ColumnIndex == 0
                    {
                        if (OrderGridSortedColumnIndexList[tabControlOrders.SelectedIndex] == e.ColumnIndex)
                        {
                            if (OrderGridSortedOrderList[tabControlOrders.SelectedIndex] == SortOrder.Ascending)
                                OrderGridSortedOrderList[tabControlOrders.SelectedIndex] = SortOrder.Descending;
                            else
                                OrderGridSortedOrderList[tabControlOrders.SelectedIndex] = SortOrder.Ascending;
                        }
                        else
                        {
                            OrderGridSortedOrderList[tabControlOrders.SelectedIndex] = SortOrder.Ascending;
                            OrderGridSortedColumnIndexList[tabControlOrders.SelectedIndex] = e.ColumnIndex;
                        }

                        int i, n = currDGV.Columns.Count;
                        for (i = 0; i < n; i++)
                        {
                            if (i != e.ColumnIndex)
                            {
                                currDGV.Columns[i].HeaderCell.SortGlyphDirection = SortOrder.None;
                            }
                            else if (currDGV.Columns[i].SortMode == DataGridViewColumnSortMode.Programmatic)
                            {
                                if (e.ColumnIndex == iOrderNoColIdx)
                                    currDGV.Sort(new Int32Comparer(OrderGridSortedOrderList[tabControlOrders.SelectedIndex], iOrderNoSortColIdx));
                                else if (e.ColumnIndex == iOrderAvgPriceColIdx || e.ColumnIndex == iOrderPriceColIdx)
                                    currDGV.Sort(new Double32Comparer(OrderGridSortedOrderList[tabControlOrders.SelectedIndex], e.ColumnIndex));
                                else
                                    currDGV.Sort(new Int32Comparer(OrderGridSortedOrderList[tabControlOrders.SelectedIndex], e.ColumnIndex));
                                currDGV.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection = OrderGridSortedOrderList[tabControlOrders.SelectedIndex];
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "dataGridViewOrder_CellClick");
                }
            }
        }

        private void dataGridView_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
#if DEBUG
            List<int> listOrders = new List<int>();
            for (int xxx = 0; xxx < currDGV.RowCount; xxx++)
            {
                int tempOrderNo;
                if (int.TryParse(currDGV.Rows[xxx].Cells[strOrderCol_OrderNo].Value.ToString().Replace(",", ""), out tempOrderNo))
                    listOrders.Add(tempOrderNo);
            }
            List<Order> Orders2;
            Orders2 = TradeDB.GetOrderByOrderNo(listOrders);
            int iTtlDealCnt = 0;
            decimal dTtlDealPrice = 0;
            foreach (Order ord in Orders2)
            {
                foreach (KeyValuePair<int, Deal> kvp in ord.Deals)
                {
                    if (kvp.Value != null)
                    {
                        dTtlDealPrice += kvp.Value.Price;
                        iTtlDealCnt++;
                    }
                }
            }
            //MessageBox.Show("Ttl # of deal: " + iTtlDealCnt + "  StockCode total: " + dTtlDealPrice);
            label4.Visible = true;
            label2.Visible = true;
            labelTtlDealPrice.Visible = true;
            labelTtlDealNo.Visible = true;
            labelTtlDealPrice.Text = dTtlDealPrice.ToString();
            labelTtlDealNo.Text = iTtlDealCnt.ToString();
#endif
            //lock (GridClickMutex)
            {
                DataGridView DoubleClickGrid = currDGV;

                if (DoubleClickGrid == null || e.RowIndex < 0 || DoubleClickGrid.SelectedCells.Count == 0 || e.RowIndex != DoubleClickGrid.SelectedCells[0].RowIndex)   // avoid false alarm where user click the row divider
                    return;
                if (currDGV.Columns[e.ColumnIndex].Name == strOrderCol_DelBtn || currDGV.Columns[e.ColumnIndex].Name == strOrderCol_ReplyChkBox || currDGV.Columns[e.ColumnIndex].Name == strOrderCol_AAmendBtnUp || currDGV.Columns[e.ColumnIndex].Name == strOrderCol_AAmendBtnDown)
                    return;
                try
                {
                    List<BaseForm> amendFormList = (BaseForm.GetFormByFormType(typeof(AmendForm)));
                    bool IsNewAmendForm = amendFormList == null || amendFormList.Count == 0;// GetInstanceCount(typeof(AmendForm)) < 1;
                    DataGridViewRow row = DoubleClickGrid.Rows[DoubleClickGrid.SelectedCells[0].RowIndex];
                    ExchangeTypeEnum exType = Stock.GetExchangeType(row.Cells[strOrderCol_ExCode].Value.ToString());

                    // select stock code in stockQuoteForm
                    List<BaseForm> stockQuoteFormList = (BaseForm.GetFormByFormType(typeof(StockQuoteForm)));
                    if (stockQuoteFormList != null && stockQuoteFormList.Count > 0)
                    {
                        StockQuoteForm stockQuoteForm = (StockQuoteForm)stockQuoteFormList[0];
                        /*
                        stockQuoteForm.ListenedStockCode = row.Cells[strOrderCol_StkCode].Value.ToString();
                        stockQuoteForm.checkBoxSHG.Checked = exType == ExchangeTypeEnum.SHG;
                        stockQuoteForm.checkBoxSZE.Checked = exType == ExchangeTypeEnum.SZE;
                         */

                        stockQuoteForm.SetStock(exType, row.Cells[strOrderCol_StkCode].Value.ToString());
                    }

                    int iOrderNo;
                    if (!int.TryParse(row.Cells[strOrderCol_OrderNo].Value.ToString(), out iOrderNo))
                        return;
                    if (IsNewAmendForm)
                        amForm = new AmendForm(this.Culture, null);
                    else
                        amForm = (AmendForm)amendFormList[0];
                    amForm.OrderNumber = row.Cells[strOrderCol_OrderNo].Value.ToString();
                    if (GetResxString("Buy") == row.Cells[strOrderCol_OrderSide].Value.ToString())
                    {
                        amForm.BuySell = GetResxString("Buy");
                        amForm.IsBuyOrder = true;
                    }
                    else if (GetResxString("Sell") == row.Cells[strOrderCol_OrderSide].Value.ToString())
                    {
                        amForm.BuySell = GetResxString("Sell");
                        amForm.IsBuyOrder = false;
                    }
                    amForm.StockCode = row.Cells[strOrderCol_StkCode].Value.ToString();
                    amForm.OrderPrice = row.Cells[strOrderCol_OrderPrice].Value.ToString();
                    //if (row.Cells[strOrderCol_OrderType].Value.ToString().Trim() == "A")
                    decimal dOrderPrice = 0m;
                    decimal.TryParse(amForm.OrderPrice, out dOrderPrice);
                    if (dOrderPrice == 0m)
                        amForm.IsAuctionOrder = true;
                    else
                        amForm.IsAuctionOrder = false;
                    amForm.OrginalOrderQty = amForm.OrderQty = row.Cells[strOrderCol_OrderQty].Value.ToString();
                    amForm.FilledQty = row.Cells[strOrderCol_FilledQty].Value.ToString();
                    amForm.LastAction = row.Cells[strOrderCol_LastAction].Value.ToString();
                    int iOrderStatus = -1;
                    int.TryParse(row.Cells[strOrderCol_OrderStatusCode].Value.ToString(), out iOrderStatus);
                    amForm.OrderStatus = Utils.Utils.GetStatusType(iOrderStatus);
                    amForm.Accno = row.Cells[strOrderCol_AccNo].Value.ToString();
                    string strOrderNo = row.Cells[strOrderCol_OrderNo].Value.ToString();
                    List<Order> Orders;
                    ExchangeTypeEnum ete = ExchangeTypeEnum.HKG;
                    Orders = TradeDB.GetOrderByOrderNo(new List<int> { iOrderNo });
                    if (Orders != null && Orders[0] != null)
                        ete = Orders[0].ExType;
                    amForm.FormExType = ete;
                    if (IsNewAmendForm)
                        amForm.Show(dockPanelMain, DockState.Float, new Rectangle(300, 300, amForm.Width, amForm.Height));
                    else
                    {
                        amForm.Redraw();
                        amForm.Focus();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "dataGridView_CellMouseDoubleClick");
                }
            }
        }

        #endregion

        #region "Search"

        public void QuoteFormShowSearch(string stockCode, string AccNum)
        {
            if (TradeDB.UserType == UserTypeEnum.Client)
                return;

            labelStkCodeTarget.Text = stockCode;
            labelAccTarget.Text = AccNum;
            checkBoxShowSearch.Checked = true;
            ShowSearch();
            PerformSearch();
        }

        public void SetSearchAccount(string srchAccount)
        {
            if (SearchEnabled) { comboBoxAccount.Text = srchAccount; }
        }

        private void checkBoxEnableSrch_Click(object sender, EventArgs e)
        {
            EnableSearch();
        }

        private void ClearGridAndDict()
        {
            dataGridViewAllOrder.Rows.Clear();
            dataGridViewRepliedOrder.Rows.Clear();
            dataGridViewCompletedOrder.Rows.Clear();
            dataGridViewPendOrder.Rows.Clear();
            AllOrderGridDict.Clear();
            RepliedOrderGridDict.Clear();
            PendOrderGridDict.Clear();
            CompletedOrderGridDict.Clear();
            ClearDealGrid();
        }

        private void ClearRefreshGrid()
        {
            ClearGridAndDict();
            OnOrderStatus(TradeDB.GetOrder());
        }

        private void ClearStockSrchLabel()
        {
            labelBuyTtlQty.Text = "";
            labelSellTtlQty.Text = "";
            labelBuyFilled.Text = "";
            labelSellFilled.Text = "";
            labelBuyAvgPrice.Text = "";
            labelSellAvgPrice.Text = "";
            //ttlSellCost = 0;
            //ttlBuyCost = 0;
        }

        private void PerformSearch()
        {
            string strAE = labelAETarget.Text;
            if (comboBoxAE != null)
            {
                if (strAE == "")
                    currDGV.Columns[strOrderCol_AECode].HeaderCell.Style.BackColor = SystemColors.Control;
                else
                    currDGV.Columns[strOrderCol_AECode].HeaderCell.Style.BackColor = Color.Lime;
            }
            string strAcc = labelAccTarget.Text;
            if (comboBoxAccount != null)
            {
                if (strAcc == "")
                    currDGV.Columns[strOrderCol_AccNo].HeaderCell.Style.BackColor = SystemColors.Control;
                else
                    currDGV.Columns[strOrderCol_AccNo].HeaderCell.Style.BackColor = Color.Lime;
            }
            string stkCode = labelStkCodeTarget.Text;
            if (textBoxStkCode != null) // && textBoxStkCode.Text.Trim() != "")
            {
                if (stkCode == "")
                    currDGV.Columns[strOrderCol_StkCode].HeaderCell.Style.BackColor = SystemColors.Control;
                else
                    currDGV.Columns[strOrderCol_StkCode].HeaderCell.Style.BackColor = Color.Lime;
            }
            ClearGridAndDict();
            OnOrderStatus(TradeDB.GetOrder());
        }

        private void EnableSearch()
        {
            ClearDealGrid();

            if (checkBoxEnableSrch.Checked)
            {
                comboBoxAE.Enabled = true;
                comboBoxAccount.Enabled = true;
                textBoxStkCode.Enabled = true;
                labelAETarget.BackColor = Color.Cyan;
                labelAE.BackColor = Color.Cyan;
                labelAccTarget.BackColor = Color.Cyan;
                labelAcc.BackColor = Color.Cyan;
                labelStkCodeTarget.BackColor = Color.Cyan;
                labelStkCode.BackColor = Color.Cyan;
                //PerformSearch();
            }
            else
            {
                currDGV.Columns[strOrderCol_AECode].HeaderCell.Style.BackColor = currDGV.Columns[strOrderCol_AECode].DefaultCellStyle.BackColor;
                currDGV.Columns[strOrderCol_AccNo].HeaderCell.Style.BackColor = currDGV.Columns[strOrderCol_AccNo].DefaultCellStyle.BackColor;
                currDGV.Columns[strOrderCol_StkCode].HeaderCell.Style.BackColor = currDGV.Columns[strOrderCol_StkCode].DefaultCellStyle.BackColor;
                comboBoxAE.Enabled = false;
                comboBoxAE.Text = "";
                comboBoxAccount.Enabled = false;
                comboBoxAccount.Text = "";
                textBoxStkCode.Enabled = false;
                labelAETarget.BackColor = panelTop.BackColor;
                labelAE.BackColor = panelTop.BackColor;
                labelAccTarget.BackColor = panelTop.BackColor;
                labelAcc.BackColor = panelTop.BackColor;
                labelStkCodeTarget.BackColor = panelTop.BackColor;
                labelStkCode.BackColor = panelTop.BackColor;
                ClearStockSrchLabel();
                labelStkCodeTarget.Text = "";
                labelAETarget.Text = "";
                labelAccTarget.Text = "";
                textBoxStkCode.Text = "";
                ClearRefreshGrid();
            }

            if (checkBoxEnableSrch.Checked)
            {
                SearchOption so = new SearchOption();
                so.AECode = labelAETarget.Text.Trim();
                so.AccNo = labelAccTarget.Text.Trim();
                so.StkCode = labelStkCodeTarget.Text.Trim();
                if (SearchOptionChanged != null) SearchOptionChanged.Invoke(this, so);
            }
            else
            {
                if (SearchOptionChanged != null) SearchOptionChanged.Invoke(this, null);
            }
        }

        private void comboBoxAE_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar >= 97 && e.KeyChar <= 122)
                e.KeyChar = (char)(e.KeyChar & 223);
            else if (e.KeyChar != 8 && (e.KeyChar < 48 || (e.KeyChar > 57 && e.KeyChar < 65) || e.KeyChar > 90))
                e.Handled = true;
        }

        private void comboBoxAE_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;

            string strAE = comboBoxAE.Text;
            {
                labelAETarget.Text = strAE.Trim();
                LocalFormSettings["SearchAETaget"] = strAE.Trim();
                SearchOption so = new SearchOption();
                so.AECode = labelAETarget.Text;
                if (SearchOptionChanged != null) SearchOptionChanged.Invoke(this, so);
            }
            PerformSearch();
            comboBoxAE.Text = "";
            comboBoxAccount.Text = "";
            textBoxStkCode.Text = "";
        }

        private void comboBoxAE_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (comboBoxAE == null || comboBoxAE.SelectedItem == null || !comboBoxAE.DroppedDown)
                return;

            string strAE = comboBoxAE.SelectedItem.ToString();
            //if (comboBoxAE.Items.Contains(strAE) || strAE == "")
            {
                labelAETarget.Text = strAE.Trim();
                LocalFormSettings["SearchAETaget"] = strAE.Trim();
                SearchOption so = new SearchOption ();
                so.AECode = labelAETarget.Text;
                if (SearchOptionChanged != null) SearchOptionChanged.Invoke(this, so);
            }
            PerformSearch();
            comboBoxAE.SelectedIndex = -1;
        }

        private void comboBoxAccount_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;

            string strAcc = comboBoxAccount.Text;
            {
                labelAccTarget.Text = strAcc.Trim();
                LocalFormSettings["SearchAccountTaget"] = strAcc.Trim();
                SearchOption so = new SearchOption();
                so.AccNo = labelAccTarget.Text;
                if (SearchOptionChanged != null)
                    SearchOptionChanged.Invoke(this, so);
            }
            PerformSearch();
            comboBoxAccount.Text = "";
            textBoxStkCode.Focus();
        }

        private void comboBoxAccount_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar >= 97 && e.KeyChar <= 122)
                e.KeyChar = (char)(e.KeyChar & 223);
            else if (e.KeyChar != 8 && (e.KeyChar < 48 || (e.KeyChar > 57 && e.KeyChar < 65) || e.KeyChar > 90))
                e.Handled = true;
        }

        private void comboBoxAccount_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (comboBoxAccount == null || comboBoxAccount.SelectedItem == null || !comboBoxAccount.DroppedDown)
                return;

            string strAcc = comboBoxAccount.SelectedItem.ToString();
            //if (comboBoxAccount.Items.Contains(strAcc) || strAcc == "")
            {
                labelAccTarget.Text = strAcc.Trim();
                LocalFormSettings["SearchAccountTaget"] = strAcc.Trim();
                SearchOption so = new SearchOption();
                so.AccNo = labelAccTarget.Text;
                if (SearchOptionChanged != null) SearchOptionChanged.Invoke(this, so);
            }
            PerformSearch();
            comboBoxAccount.SelectedIndex = -1;
        }

        private void labelAETarget_TextChanged(object sender, EventArgs e)
        {
            AppendLog("StkSrch", Environment.NewLine + "AE: " + labelAETarget.Text + "  Acc.: " + labelAccTarget.Text + "  Stk Code: " + labelStkCodeTarget.Text, "StkSrch", false);
            comboBoxAccount.Items.Clear();
        }

        private void labelAccTarget_TextChanged(object sender, EventArgs e)
        {
            AppendLog("StkSrch", Environment.NewLine + "AE: " + labelAETarget.Text + "  Acc.: " + labelAccTarget.Text + "  Stk Code: " + labelStkCodeTarget.Text, "StkSrch", false);
            UpdatePanelChkBoxLocation();
        }

        private void SetPanelTopHeight()
        {
            if (labelAccTarget.Text.Trim() != "")
            {
                if (panelTop.Height != 102)
                {
                    panelTop.Width = 773;
                    panelTop.Height = 102;
                    if (textBoxStkCode != null)
                        currDGV.Columns[strOrderCol_StkCode].HeaderCell.Style.BackColor = SystemColors.Control;
                    labelStkCodeTarget.Text = "";
                    ClearStockSrchLabel();
                }
            }
            else
            {
                panelTop.Height = 38;
                currDGV.Columns[strOrderCol_StkCode].HeaderCell.Style.BackColor = SystemColors.Control;
            }
        }

        private void textBoxStkCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;

            int iStkCode = 0;
            labelStkCodeTarget.Text = "";
            ClearStockSrchLabel();
            string strStkCode = textBoxStkCode.Text.Trim();
            if (int.TryParse(strStkCode, out iStkCode) && strStkCode != "")
                labelStkCodeTarget.Text = textBoxStkCode.Text;
            else
            {
                if (textBoxStkCode != null)
                    currDGV.Columns[strOrderCol_StkCode].HeaderCell.Style.BackColor = SystemColors.Control;
                labelStkCodeTarget.Text = "";
            }
            //LocalFormSettings["SearchStkCodeTaget"] = strStkCode.Trim();
            SearchOption so = new SearchOption();
            so.StkCode = labelStkCodeTarget.Text;
            if (SearchOptionChanged != null)
                SearchOptionChanged.Invoke(this, so);
            PerformSearch();
            textBoxStkCode.Text = "";
        }

        private void textBoxStkCode_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar >= 97 && e.KeyChar <= 122)
                e.KeyChar = (char)(e.KeyChar & 223);
            else if (e.KeyChar != 8 && (e.KeyChar < 48 || (e.KeyChar > 57 && e.KeyChar < 65) || e.KeyChar > 90))
                e.Handled = true;
        }

        private void checkBoxShowSearch_Click(object sender, EventArgs e)
        {
            ShowSearch();
        }

        private void ShowSearch()
        {
            if (checkBoxShowSearch.Checked && !checkBoxEnableSrch.Checked)
            {
                checkBoxEnableSrch.Checked = true;
                EnableSearch();
            }
            UpdatePanelChkBoxLocation();
        }

        private void UpdatePanelChkBoxLocation()
        {
            if (checkBoxShowSearch.Checked)
                SetPanelTopHeight();
            panelTop.Visible = checkBoxShowSearch.Checked ? true : false;
            panelChkBox.Location = new Point(panelChkBox.Location.X, panelMiddle.Location.Y);
        }

        private void checkBoxShowInternetOrders_Click(object sender, EventArgs e)
        {
            EnableSearch();
            PerformSearch();
        }

        private void checkBoxShowAEOrders_Click(object sender, EventArgs e)
        {
            EnableSearch();
            PerformSearch();
        }

        private void checkBoxShowInternetOrders_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxShowInternetOrders.Checked)
            {
                currDGV.Columns[strOrderCol_Memo].HeaderCell.Style.BackColor = Color.Lime;
                checkBoxShowInternetOrders.BackColor = Color.Lime;
            }
            else
            {
                if (!checkBoxShowAEOrders.Checked)
                    currDGV.Columns[strOrderCol_Memo].HeaderCell.Style.BackColor = currDGV.Columns[strOrderCol_Memo].DefaultCellStyle.BackColor;
                checkBoxShowInternetOrders.BackColor = SystemColors.Control;
            }
        }

        private void checkBoxShowAEOrders_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxShowAEOrders.Checked)
            {
                currDGV.Columns[strOrderCol_Memo].HeaderCell.Style.BackColor = Color.Lime;
                checkBoxShowAEOrders.BackColor = Color.Lime;
            }
            else
            {
                if (!checkBoxShowInternetOrders.Checked)
                    currDGV.Columns[strOrderCol_Memo].HeaderCell.Style.BackColor = currDGV.Columns[strOrderCol_Memo].DefaultCellStyle.BackColor;
                checkBoxShowAEOrders.BackColor = SystemColors.Control;
            }
        }

        private void checkBoxEnableSrch_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxEnableSrch.Checked)
            {
                SearchEnabled = true;
                //checkBoxEnableSrch.BackColor = Color.Lime;
                //buttonApplySrchSetting.BackColor = Color.Lime;
                checkBoxShowSearch.BackColor = Color.Lime;
            }
            else
            {
                SearchEnabled = false;
                checkBoxShowSearch.Checked = false;
                UpdatePanelChkBoxLocation();
                //checkBoxEnableSrch.BackColor = SystemColors.Control;
                checkBoxShowSearch.BackColor = SystemColors.Control;
            }
        }

        #endregion

        #region "Handle Event form double click"

        private void OnEventFormDoubleClicked(SystemEvent TheSystemEvent)
        {
            SelectEventDoubleClickedRow(TheSystemEvent.OrderNoSort);
        }

        private void SelectEventDoubleClickedRow(string orderNoSort)
        {
            if (orderNoSort == null || orderNoSort.Trim() == "")
                return;
            // clear high-light on previously selected row
            if (prevSelectedOrderNo[tabControlOrders.SelectedIndex] != "")
            {
                lock (GridMutex)
                {
                    DataGridViewRow row = null;
                    GetGridDict(currDGV).TryGetValue(prevSelectedOrderNo[tabControlOrders.SelectedIndex], out row);
                    if (row != null && row.Index >= 0)
                        RestoreRowColor(row);
                }
            }
            DataGridViewRow eventFormDoubleClickedRow = null;
            if (!GetGridDict(currDGV).TryGetValue(orderNoSort, out eventFormDoubleClickedRow))
            {
                if ((!GetGridDict(dataGridViewAllOrder).TryGetValue(orderNoSort, out eventFormDoubleClickedRow) || eventFormDoubleClickedRow == null || eventFormDoubleClickedRow.Index < 0) && 
                    currDGV.Name == "dataGridViewAllOrder")
                    return;
                else
                {
                    tabControlOrders.SelectedIndex = 3;
                    tabControlOrders.SelectedTab = tabPage4;
                    OrderGridSelectRowAfterSort = true;
                    selectRowOrderNoSort = orderNoSort;
                }
            }
            else
            {
                // Highlight selected row 
                for (int i = 1; i < (currDGV.Name == "dataGridViewAllOrder" ? currDGV.ColumnCount - 5 : currDGV.ColumnCount - 4); i++)
                {
                    lock (GridMutex)
                    {
                        eventFormDoubleClickedRow.Cells[i].Style.BackColor = Color.DarkBlue;
                        eventFormDoubleClickedRow.Cells[i].Style.ForeColor = Color.White;
                    }
                }
                currDGV.FirstDisplayedCell = eventFormDoubleClickedRow.Cells[0];
                string strOrderNoSort = eventFormDoubleClickedRow.Cells[iOrderNoSortColIdx].Value.ToString();
                prevSelectedOrderNo[tabControlOrders.SelectedIndex] = strOrderNoSort;
                string statusMessage = "";
                DataGridViewRow row = null;
                if (GetGridDict(currDGV).TryGetValue(strOrderNoSort, out row) && row != null && row.Index >= 0)
                {
                    if (row.Cells[strOrderCol_StatusMessage].Value != null)
                        statusMessage = row.Cells[strOrderCol_StatusMessage].Value.ToString();
                    if (row.Cells[strOrderCol_OGMessage].Value != null && row.Cells[strOrderCol_OGMessage].Value.ToString().Trim() != "" &&
                        (statusMessage != row.Cells[strOrderCol_OGMessage].Value.ToString()))
                        statusMessage = statusMessage + "\n" + row.Cells[strOrderCol_OGMessage].Value.ToString();
                    if (row.Cells[strOrderCol_OrderNo].Value != null && row.Cells[strOrderCol_StkCode].Value != null && row.Cells[strOrderCol_OrderSide].Value != null)
                        UpdateDealGrid(row.Cells[strOrderCol_OrderNo].Value.ToString(), row.Cells[strOrderCol_ExCode].Value.ToString(), 
                            row.Cells[strOrderCol_StkCode].Value.ToString(), row.Cells[strOrderCol_OrderSide].Value.ToString());
                }
                labelRemark.Text = statusMessage;
                lock (GridMutex)
                {
                    currDGV.ClearSelection();
                    currDGV.Refresh();
                }
            }
        }

        protected override void OnFormListChange()
        {
            AddEventDoubleClickedDelegate();
        }

        private void AddEventDoubleClickedDelegate()
        {
            List<BaseForm> EventFormList = BaseForm.GetFormByFormType(typeof(EventForm));

            if (EventFormList == null || EventFormList.Count <= 0) return;

            Dictionary<BaseForm, SystemEvent> dict = new Dictionary<BaseForm, SystemEvent>(EventFormList.Count);
            SystemEvent so = null;

            foreach (BaseForm FormObject in EventFormList)
            {
                if (pSystemEventDict.TryGetValue(FormObject, out so))
                {
                    dict[FormObject] = so;
                }
                else
                {
                    (FormObject as EventForm).EventDoubleClicked += pEventDoubleClickedDelegate;
                    dict[FormObject] = null;
                }
            }
            pSystemEventDict = dict;
        }

        private void RemoveEventDoubleClickedDelegate()
        {
            // Remove event form double clicked delegate
            List<BaseForm> EventFormList = BaseForm.GetFormByFormType(typeof(EventForm));
            if (EventFormList != null && EventFormList.Count > 0)
            {
                Dictionary<BaseForm, SystemEvent> dict = new Dictionary<BaseForm, SystemEvent>(EventFormList.Count);
                foreach (BaseForm FormObject in EventFormList)
                {
                    (FormObject as EventForm).EventDoubleClicked -= pEventDoubleClickedDelegate;
                    dict[FormObject] = null;
                }
            }
        }

        #endregion

        private void splitter2_SplitterMoved(object sender, SplitterEventArgs e)
        {
            LocalFormSettings["DealGridWidth"] = dataGridViewDeal.Width.ToString();
        }

        private void splitter1_SplitterMoved(object sender, SplitterEventArgs e)
        {
            LocalFormSettings["PanelBottomHeight"] = panelBottom.Height.ToString();
        }

        #region Transaction Charge

        private void buttonGetTransacCharge_Click(object sender, EventArgs e)
        {
            try
            {
                DataGridViewRow row = null;
                char buySide;
                string strOrderNo = null;
                int orderNo;

                labelTransacChargeVal.Text = "$0.0";
                if (GetGridDict(currDGV).TryGetValue(prevSelectedOrderNo[tabControlOrders.SelectedIndex], out row))
                {
                    strOrderNo = row.Cells[strOrderCol_OrderNo].Value.ToString();
                    if (strOrderNo != null && int.TryParse(strOrderNo, out orderNo))
                    {
                        List<Deal> dealList = GetDealList(strOrderNo);
                        if (dealList == null || dealList.Count == 0)
                            return;

                        List<decimal> priceList = new List<decimal>(dealList.Count);
                        List<decimal> qtyList = new List<decimal>(dealList.Count);

                        if (dealList.Count <= 0)
                        {
                            foreach (Deal deal in dealList)
                            {
                                if (deal != null)
                                {
                                    priceList.Add(deal.Price);
                                    qtyList.Add(deal.Quantity);
                                }
                            }
                        }
                        else
                        {
                            decimal sumQty = 0;
                            decimal totalCost = 0.0M;
                            foreach (Deal deal in dealList)
                            {
                                if (deal != null)
                                {
                                    sumQty += deal.Quantity;
                                    totalCost = totalCost + (deal.Quantity * deal.Price);
                                }
                            }
                            decimal avgPrice = totalCost / sumQty;
                            decimal price1 = Math.Truncate(avgPrice * 1000) * 0.001m;
                            decimal qty1 = sumQty;
                            decimal price2 = (avgPrice - Math.Truncate(avgPrice)) * 1000;
                            price2 = price2 - Math.Truncate(price2);
                            decimal qty2 = sumQty * 0.001m;
                            priceList.Add(price1);
                            priceList.Add(price2);
                            qtyList.Add(qty1);
                            qtyList.Add(qty2);

                            //int maxDivider = GetMaxDivider(sumQty, (totalCost / sumQty));
                            //priceList.Add((totolCost / sumQty) * maxDivider);
                            //qtyList.Add(sumQty / maxDivider);
                        }

                        string strOrderSide = row.Cells[strOrderCol_OrderSide].Value.ToString();
                        string strAccountNo = row.Cells[strOrderCol_AccNo].Value.ToString();
                        int iStkCode;

                        if (strOrderSide == GetResxString("Buy"))
                            buySide = 'B';
                        else // if (strOrderSide == GetResxString("Sell"))
                            buySide = 'S'; // 'A'

                        int.TryParse(row.Cells[strOrderCol_StkCode].Value.ToString(), out iStkCode);

                        TransactionCharge tc;
                        List<Order> Orders;
                        ExchangeTypeEnum ete = ExchangeTypeEnum.HKG;
                        Orders = TradeDB.GetOrderByOrderNo(new List<int> { orderNo });
                        if (Orders != null && Orders[0] != null)
                            ete = Orders[0].ExType;

                        GetStock(new List<string> { Stock.GetSignature(ete, row.Cells[strOrderCol_StkCode].Value.ToString()) });

                        tc = new TransactionCharge(strAccountNo, buySide, ete, iStkCode.ToString(), priceList, qtyList);
                        if (tc.PriceList != null && tc.PriceList.Count > 0 && tc.QuantityList != null && tc.QuantityList.Count > 0)
                            GetTransactionCharge(tc);
                    }
                }
            }
            catch {}
        }

        private int GetMaxDivider(long sumQty, decimal avgPrice)
        {
            int maxDivider = 1;
            while ((sumQty / (maxDivider*10)) >= 10 && (avgPrice * (maxDivider*10)) <= 9999.999M)
            {
                maxDivider *= 10;
            }
            return maxDivider;
        }

        private List <Deal> GetDealList(string strOrderNo)
        {
            try
            {
                int orderNo;
                if (!int.TryParse(strOrderNo, out orderNo))
                    return null;

                List<Order> Orders;
                List<Deal> dealList = null;
                Orders = TradeDB.GetOrderByOrderNo(new List<int> { orderNo });
                if (Orders != null && Orders[0] != null)
                {
                    if (Orders[0].Deals != null && Orders[0].Deals.Count > 0)
                        dealList = new List<Deal> (Orders[0].Deals.Values);
                }
                return dealList;
            }
            catch {
                return null;
            }
        }

        protected override void OnTransactionCharge(TransactionCharge TxnCharge)
        {
            if (TxnCharge != null)
                labelTransacChargeVal.Text = String.Format("{0:N}", Math.Abs(TxnCharge.NetAmount));
            //labelTransacChargeVal.Text = "$" + String.Format("{0:N}", Math.Abs(TransactionCharges[0].NetAmount));
            labelCurrency.Location = new Point(buttonGetTransacCharge.Location.X + buttonGetTransacCharge.Width + 3, labelTransacChargeVal.Location.Y);
            labelTransacChargeVal.Location = new Point(labelCurrency.Location.X + labelCurrency.Width, labelCurrency.Location.Y);
        }

        #endregion

        private void labelStkCodeTarget_TextChanged(object sender, EventArgs e)
        {
            AppendLog("StkSrch", Environment.NewLine + "AE: " + labelAETarget.Text + "  Acc.: " + labelAccTarget.Text + "  Stk Code: " + labelStkCodeTarget.Text, "StkSrch", false);
        }

        private void ShowTradeStatus(Stock theStock)
        {
            if (theStock != null)
            {
                labelCurrency.Visible = true;
                if (theStock.SuspensionFlag == "Y")
                {
                    labelCurrency.Text = GetResxString("TradeStatusSuspended");
                    labelCurrency.ForeColor = Color.Red;
                }
                else
                {
                    labelCurrency.Text = GetGeneralResxString("Currency_" + theStock.Currency) + " " + GetBestCurrencySign(theStock.Currency);
                    labelCurrency.ForeColor = (theStock.Currency == null || theStock.Currency == "" || theStock.Currency == "HKD") ?
                        Color.FromKnownColor(KnownColor.ControlText) : Color.Red;
                }
                labelCurrency.Location = new Point(buttonGetTransacCharge.Location.X +  buttonGetTransacCharge.Width + 3, labelTransacChargeVal.Location.Y);
                labelTransacChargeVal.Location = new Point(labelCurrency.Location.X + labelCurrency.Width, labelCurrency.Location.Y);
            }
            else
            {
                labelCurrency.Text = "";
                labelCurrency.ForeColor = Color.FromKnownColor(KnownColor.ControlText);
            }
        }

        protected override void OnStock(Stock TheStock)
        {
            int iStkCode;

            if (TheStock != null) // && int.TryParse(selectedStockCode, out iStkCode))
            {
                int.TryParse(selectedStockCode, out iStkCode);
                if (TheStock != null && TheStock.ExchangeCode == selectedExchangeCode && TheStock.Code == iStkCode.ToString())
                {
                    selectedDealStock = TheStock;
                    ShowTradeStatus(selectedDealStock);
                }

                if (AAmendOrder != null && AAmendOrder.ExType == TheStock.ExchangeType && AAmendOrder.StockCode != null && TheStock != null && AAmendOrder.StockCode.Trim() == TheStock.Code)
                {
                    CurrStock = TheStock;
                    if (AutoAmend == true && IsAutoAmend == true) ProcessAAmend();
                }
            }
        }

        private void ProcessAAmend()
        {
            if (CurrStock == null) return;
            if (CurrStock.LotSize <= 0 || CurrStock.Code == null) return;
            if (CurrStock.Code != AAmendOrder.StockCode.Trim() || AAmendOrder.StockCode.Trim() == "") return;
            if (CurrStock.ExchangeType != AAmendOrder.ExType) return;            

            //////if (CurrStock.Nominal <= 0 || CurrStock.Bid < 0 || CurrStock.Ask < 0) return;
            if (CurrStock.SpreadTableCode <= 0) return;

            //////bool CanBuy, CanSell;
            //Check Auction period
            //////if (Utils.OrderTypeSpread.CheckSpread(0, 'A', 'B', 1, CurrStock, TradeDB, out CanBuy, out CanSell) == -1)
            //////{
                // Not Auction period

                IsAutoAmend = false;                
                bool success = PrepareAAmend(AAmendOrder.StockCode, AAmendOrder.Side, AAmendOrder.OrderNo, AAmendOrder.Price, AAmendOrder.Quantity);

                DeleteCurrentStockCodeAAmend();
                //if (success == true)
                //buttonCell.Enabled = false;
            //////}
        }

        private void comboBoxShowEx_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (comboBoxShowEx == null || comboBoxShowEx.SelectedItem == null)
                return;

            EnableSearch();
            PerformSearch();
            //comboBoxShowEx.SelectedIndex = -1;
        }

        private void buttonApplySrchSetting_Click(object sender, EventArgs e)
        {
            string strAcc = comboBoxAccount.Text;
            int iStkCode = 0;
            string strAE = comboBoxAE.Text;
            labelStkCodeTarget.Text = "";
            ClearStockSrchLabel();
            string strStkCode = textBoxStkCode.Text.Trim();

            SearchOption so = new SearchOption();
            so.AccNo = labelAccTarget.Text = LocalFormSettings["SearchAccountTaget"] = strAcc.Trim();
            if (int.TryParse(strStkCode, out iStkCode) && strStkCode != "")
                labelStkCodeTarget.Text = textBoxStkCode.Text;
            else
            {
                if (textBoxStkCode != null)
                    currDGV.Columns[strOrderCol_StkCode].HeaderCell.Style.BackColor = SystemColors.Control;
                labelStkCodeTarget.Text = "";
            }
            so.StkCode = labelStkCodeTarget.Text;
            if (SearchOptionChanged != null)
                SearchOptionChanged.Invoke(this, so);
            so.AECode = labelAETarget.Text = LocalFormSettings["SearchAETaget"] = strAE.Trim();
            PerformSearch();
            comboBoxAE.Text = "";
            comboBoxAccount.Text = "";
            textBoxStkCode.Text = "";
            textBoxStkCode.Focus();
        }

        private void textBoxStkCode_MouseClick(object sender, MouseEventArgs e)
        {
            textBoxStkCode.SelectAll();
        }

        private void buttonClrSrchSetting_Click(object sender, EventArgs e)
        {
            comboBoxAE.Text = "";
            comboBoxAccount.Text = "";
            textBoxStkCode.Text = "";

            if (labelAccTarget.Text.Trim().Length == 0 && labelStkCodeTarget.Text.Trim().Length == 0 && labelAETarget.Text.Trim().Length == 0)
                return;

            SearchOption so = new SearchOption();
            so.AccNo = labelAccTarget.Text = LocalFormSettings["SearchAccountTaget"] = comboBoxAccount.Text;
            so.StkCode = labelStkCodeTarget.Text = textBoxStkCode.Text;
            so.AECode = labelAETarget.Text = LocalFormSettings["SearchAETaget"] = comboBoxAE.Text;
            PerformSearch();
        }

        private void checkBoxGrayStock_CheckedChanged(object sender, EventArgs e)
        {
            ClearRefreshGrid();
            if (checkBoxGrayStock.Checked)
            {
                checkBoxGrayStock.BackColor = Color.Lime;
                comboBoxShowEx.Visible = false;
                labelXchg.Visible = false;
            }
            else
            {
                checkBoxGrayStock.BackColor = SystemColors.Control;
                comboBoxShowEx.Visible = true;
                labelXchg.Visible = true;
            }
            lock (GridMutex)
            {
                switch (tabControlOrders.SelectedIndex)
                {
                    case (0):
                        currDGV = dataGridViewPendOrder;
                        break;
                    case (1):
                        currDGV = dataGridViewCompletedOrder;
                        break;
                    case (2):
                        if (TradeDB.UserType == UserTypeEnum.AE)
                            currDGV = dataGridViewRepliedOrder;
                        else // no need to have reply tab in client edition
                            currDGV = dataGridViewAllOrder;
                        break;
                    case (3):
                        currDGV = dataGridViewAllOrder;
                        break;
                }
            }
            RefreshGrid(currDGV, TradeDB.GetOrder());
            SortGrid();
            ClearDealGrid();
        }

        private void SortDealGrid(int SortColumnIndex)
        {
            if (SortColumnIndex < 0)
                SortColumnIndex = DealGridSortedColumnIndex;

            int i, n = dataGridViewDeal.Columns.Count;
            for (i = 0; i < n; i++)
            {
                if (i != SortColumnIndex)
                {
                    dataGridViewDeal.Columns[i].HeaderCell.SortGlyphDirection = SortOrder.None;
                }
                else if (dataGridViewDeal.Columns[i].SortMode == DataGridViewColumnSortMode.Programmatic)
                {
                    if (SortColumnIndex == 4) // deal price
                        dataGridViewDeal.Sort(new Double32Comparer(DealGridSortedOrder, SortColumnIndex));
                    else
                        dataGridViewDeal.Sort(new Int32Comparer(DealGridSortedOrder, SortColumnIndex));
                    dataGridViewDeal.Columns[SortColumnIndex].HeaderCell.SortGlyphDirection = DealGridSortedOrder;
                }
            }

            dataGridViewDeal.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewDeal.ClearSelection();
            dataGridViewDeal.Refresh();
        }

        private void dataGridViewDeal_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            try
            {
                if (e.RowIndex == -1 && dataGridViewDeal.Columns[e.ColumnIndex].SortMode != DataGridViewColumnSortMode.NotSortable)  // && e.ColumnIndex == 0
                {
                    SortDealGrid(e.ColumnIndex);

                    if (DealGridSortedColumnIndex == e.RowIndex)
                    {
                        if (DealGridSortedOrder == SortOrder.Ascending)
                            DealGridSortedOrder = SortOrder.Descending;
                        else
                            DealGridSortedOrder = SortOrder.Ascending;
                    }
                    else
                    {
                        DealGridSortedOrder = SortOrder.Ascending;
                        DealGridSortedColumnIndex = e.RowIndex;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "dataGridViewDeal_CellClick");
            }
        }


    }
}
