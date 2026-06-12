using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using TradeDB;
using System.Collections;


namespace StockTerminal.Forms
{
    public partial class AccountForm : StockTerminal.Forms.BaseForm
    {
        private class DgvStockSignatureComparer : IComparer
        {
            public SortOrder SortingOrder { get; set; }

            public int Compare(object X, object Y)
            {
                DataGridViewRow rX = (DataGridViewRow)X;
                DataGridViewRow rY = (DataGridViewRow)Y;

                int result = 0;

                if (rX == null)
                    result = rY == null ? 0 : -1;
                else if (rY == null)
                    result = 1;
                else
                {
                    string sX = rX.Cells[0].Value != null ? rX.Cells[0].Value.ToString() : null;
                    string sY = rY.Cells[0].Value != null ? rY.Cells[0].Value.ToString() : null;

                    if ((result = String.Compare(sX, sY, StringComparison.OrdinalIgnoreCase)) == 0)
                    {
                        sX = rX.Cells[1].Value != null ? rX.Cells[1].Value.ToString() : null;
                        sY = rY.Cells[1].Value != null ? rY.Cells[1].Value.ToString() : null;

                        int stockCodeX, stockCodeY;

                        if (int.TryParse(sX, out stockCodeX))
                            result = int.TryParse(sY, out stockCodeY) ? stockCodeX.CompareTo(stockCodeY) : -1;
                        else
                            result = int.TryParse(sY, out stockCodeY) ?
                                1 : String.Compare(sX, sY, StringComparison.OrdinalIgnoreCase);
                    }
                }

                return SortingOrder == SortOrder.Descending ? -result : result;
            }
        }
        
        private interface AccountStockComparer : IComparer<AccountStock>
        {
            SortOrder SortingOrder { get; set; }
        }

        private class AccountStockSignatureComparer : AccountStockComparer
        {
            public SortOrder SortingOrder { get; set; }

            public int Compare(AccountStock X, AccountStock Y)
            {
                int result = 0;

                if (X == null)
                    result = Y == null ? 0 : -1;
                else if (Y == null)
                    result = 1;
                else if ((result = X.ExchangeType.CompareTo(Y.ExchangeType)) == 0)
                {
                    int stockCodeX, stockCodeY;

                    if (int.TryParse(X.Code, out stockCodeX))
                        result = int.TryParse(Y.Code, out stockCodeY) ? stockCodeX.CompareTo(stockCodeY) : -1;
                    else
                        result = int.TryParse(Y.Code, out stockCodeY) ? 1 : String.Compare(X.Code, Y.Code, StringComparison.OrdinalIgnoreCase);
                }

                return SortingOrder == SortOrder.Descending ? -result : result;
            }
        }

        private class AccountStockBestNameComparer : AccountStockComparer
        {
            public SortOrder SortingOrder { get; set; }

            public int Compare(AccountStock X, AccountStock Y)
            {
                if (SortingOrder == SortOrder.Descending)
                    return X == null ? (Y == null ? 0 : 1) : (Y == null ? -1 : -String.Compare(X.BestName, Y.BestName, StringComparison.OrdinalIgnoreCase));
                else
                    return X == null ? (Y == null ? 0 : -1) : (Y == null ? 1 : String.Compare(X.BestName, Y.BestName, StringComparison.OrdinalIgnoreCase));
            }
        }

        private class AccountStockOpeningComparer : AccountStockComparer
        {
            public SortOrder SortingOrder { get; set; }

            public int Compare(AccountStock X, AccountStock Y)
            {
                if (SortingOrder == SortOrder.Descending)
                    return X == null ? (Y == null ? 0 : 1) : (Y == null ? -1 : -X.QtyOnHand.CompareTo(Y.QtyOnHand));
                else
                    return X == null ? (Y == null ? 0 : -1) : (Y == null ? 1 : X.QtyOnHand.CompareTo(Y.QtyOnHand));
            }
        }

        private class AccountStockInTransitComparer : AccountStockComparer
        {
            public SortOrder SortingOrder { get; set; }

            public int Compare(AccountStock X, AccountStock Y)
            {
                if (SortingOrder == SortOrder.Descending)
                    return X == null ? (Y == null ? 0 : 1) : (Y == null ? -1 : -X.QtyInTransit.CompareTo(Y.QtyInTransit));
                else
                    return X == null ? (Y == null ? 0 : -1) : (Y == null ? 1 : X.QtyInTransit.CompareTo(Y.QtyInTransit));
            }
        }

        private class AccountStockInTransitSoldComparer : AccountStockComparer
        {
            public SortOrder SortingOrder { get; set; }

            public int Compare(AccountStock X, AccountStock Y)
            {
                if (SortingOrder == SortOrder.Descending)
                    return X == null ? (Y == null ? 0 : 1) : (Y == null ? -1 : -X.QtyInTransitSold.CompareTo(Y.QtyInTransitSold));
                else
                    return X == null ? (Y == null ? 0 : -1) : (Y == null ? 1 : X.QtyInTransitSold.CompareTo(Y.QtyInTransitSold));
            }
        }

        private class AccountStockNetQtyComparer : AccountStockComparer
        {
            public SortOrder SortingOrder { get; set; }

            public int Compare(AccountStock X, AccountStock Y)
            {
                if (SortingOrder == SortOrder.Descending)
                    return X == null ? (Y == null ? 0 : 1) : (Y == null ? -1 : -(X.QtyOnHand + X.QtyInTransit).CompareTo(Y.QtyOnHand + Y.QtyInTransit));
                else
                    return X == null ? (Y == null ? 0 : -1) : (Y == null ? 1 : (X.QtyOnHand + X.QtyInTransit).CompareTo(Y.QtyOnHand + Y.QtyInTransit));
            }
        }

        private class AccountStockMarketValueComparer : AccountStockComparer
        {
            public SortOrder SortingOrder { get; set; }

            public int Compare(AccountStock X, AccountStock Y)
            {
                if (SortingOrder == SortOrder.Descending)
                    return X == null ? (Y == null ? 0 : 1) : (Y == null ? -1 : -X.MarketValue.CompareTo(Y.MarketValue));
                else
                    return X == null ? (Y == null ? 0 : -1) : (Y == null ? 1 : X.MarketValue.CompareTo(Y.MarketValue));
            }
        }

        private class AccountStockStatusComparer : AccountStockComparer
        {
            public SortOrder SortingOrder { get; set; }

            public int Compare(AccountStock X, AccountStock Y)
            {
                if (SortingOrder == SortOrder.Descending)
                    return X == null ? (Y == null ? 0 : 1) : (Y == null ? -1 : -String.Compare(X.SuspensionFlag, Y.SuspensionFlag, StringComparison.OrdinalIgnoreCase));
                else
                    return X == null ? (Y == null ? 0 : -1) : (Y == null ? 1 : String.Compare(X.SuspensionFlag, Y.SuspensionFlag, StringComparison.OrdinalIgnoreCase));
            }
        }

        private class Int32Comparer : System.Collections.IComparer
        {
            private static int sortOrderModifier = 1;
            private static int sortColumnIndex = 0;

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

        private DockPanel dockPanelMain;
        private OrderTicketForm OrderTicketForm1 = null;

        /*
#if DEBUG
        private string strAccNotIn = "";
        private long ttlAccountCounter = 0;
        private long ttlAccountCheckSum = 0;
#endif
         */

        private readonly List<string> tempAccountList = new List<string>(400);
        private readonly object tempAccountListMutex = new object();
        private readonly SortedList accountSortedList = new SortedList();
        private bool accountComboSorted = false;
        //private bool fillCompletedOnce = false;

        private readonly Dictionary<string, DataGridViewRow> StockGridDict = new Dictionary<string, DataGridViewRow>(500);
        private readonly Dictionary<string, DataGridViewRow> StockGridUpdateDict = new Dictionary<string, DataGridViewRow>(500);
        private readonly DgvStockSignatureComparer dgvStockSignatureComparer = new DgvStockSignatureComparer();
        private AccountStockComparer[] SortComparers;
        private Account ShownAccount = null;

        private bool ShowAECodeDetermined = false;
        private bool ShowAECode = false;

        private string pListenedAccount = null;
        private string pShowStock = null;

        //private int orderTicketStartX = 80;
        //private int orderTicketStartY = 80;

        private string CurrencyLoaded = "***";

        private readonly OrderedDictionary CurrencyRBtnDict = new OrderedDictionary(20);
        private readonly List<RadioButton> CurrencyRBtnList = new List<RadioButton>(20);

        public string ListenedAccount
        {
            get { return pListenedAccount; }

            set
            {
                string pValue = value;
                if (pValue != null) pValue = pValue.Trim().ToUpper();

                if (pListenedAccount != pValue)
                {
                    if (pListenedAccount != null)
                    {
                        UnListenAccount(new List<string> { pListenedAccount });
                        GetStock(null);
                    }
                    pListenedAccount = value;
                    if (AccountNoComboBox1 != null)
                        AccountNoComboBox1.Text = value;
                    ShownAccount = null;
                    FillAccount(null);

                    if (pListenedAccount != null && pListenedAccount.Length > 0)
                    {
                        ListenAccount(new List<string> { pListenedAccount });
                        // set quote form
                        List<BaseForm> StockQuoteFormList = BaseForm.GetFormByFormType(typeof(StockQuoteForm));
                        if (StockQuoteFormList != null)
                        {
                            foreach (StockQuoteForm Quoteform in StockQuoteFormList)
                            { Quoteform.SetAccountno(pListenedAccount); }
                        }
                    }
                    else
                    {
                        // set quote form
                        List<BaseForm> StockQuoteFormList = BaseForm.GetFormByFormType(typeof(StockQuoteForm));
                        if (StockQuoteFormList != null)
                        {
                            foreach (StockQuoteForm Quoteform in StockQuoteFormList)
                            { Quoteform.SetAccountno(""); }
                        }
                    }
                }
            }
        }
        public bool AllowChangeOrderTicketAccount = true;

        public void ShowStock(string StockSignature)
        {
            if (StockSignature == null) return;

            DataGridViewRow stockRow = null;

            if (StockGridDict.TryGetValue(StockSignature, out stockRow) && stockRow != null)
            {
                dataGridViewPortfolio.FirstDisplayedCell = stockRow.Cells[0];
                dataGridViewPortfolio.ClearSelection();
                stockRow.Selected = true;
                pShowStock = null;
            }
            else
            {
                pShowStock = StockSignature;
            }
        }

        public AccountForm() : this(null, null, null)
        {
        }

        public AccountForm(CultureInfo Culture, string PersistString, DockPanel dockPanelMain) : base(Culture, PersistString)
        {
            InitializeComponent();

            Utils.Utils.EnableDoubleBuffered(dataGridViewPortfolio);
            Utils.Utils.EnableDoubleBuffered(AccountNoComboBox1);

            this.dockPanelMain = dockPanelMain;

            AccountNoComboBox1.TradeDB = TradeDB;
        }

        private void AccountForm_Load(object sender, EventArgs e)
        {
            CurrencyRBtnDict.Add("HKD", "HKD");
            CurrencyRBtnDict.Add("CNY", "CNY");
            CurrencyRBtnDict.Add("USD", "USD");
            CurrencyRBtnDict.Add("SGD", "SGD");
            CurrencyRBtnDict.Add("JPY", "JPY");
            CurrencyRBtnDict.Add("MYR", "MYR");
            CurrencyRBtnDict.Add("TWD", "TWD");
            ChangeCurrencyColor();

            ListenAccountList();

            RefreshStockGridHeader(dataGridViewPortfolio);

            panelDBalance.Top = 0;

            labelValueAECode.Visible = false;
            labelValueAEName.Visible = false;           

            AccountNoComboBox1.Enabled = false;
            AccountNoComboBox1.AutoCompleteSource = AutoCompleteSource.None;
            AccountNoComboBox1.AutoCompleteMode = AutoCompleteMode.None;
            AccountNoComboBox1.Sorted = false;
            AccountNoComboBox1.Text = GetResxString("ComboBoxAccountNo_Loading");
            AccountNoComboBox1.SuspendLayout();
        }

        private void AccountForm_Resize(object sender, EventArgs e)
        {
            ResizeControls();
        }

        private void AccountForm_Shown(object sender, EventArgs e)
        {
            if (SettingsForms["ShowBalanceDetails"] == "1" || TradeDB.UserType == UserTypeEnum.AE)
            {
                checkBoxDetails.Visible = true;
                ShowBalanceDetails(
                    checkBoxDetails.Checked = LocalFormSettings["ShowBalanceDetails"] == "1" ||
                    LocalFormSettings["ShowBalanceDetails"] == null);
            }
            else
            {
                checkBoxDetails.Visible = false;
                ShowBalanceDetails(false);
            }
        }

        private void checkBoxDetails_CheckedChanged(object sender, EventArgs e)
        {
            LocalFormSettings["ShowBalanceDetails"] = checkBoxDetails.Checked ? "1" : "0";
            ShowBalanceDetails(checkBoxDetails.Checked);
        }

        private void comboBoxAccountNo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string accountNo = comboBoxAccountNo.Text.ToUpper().Trim();
                if (comboBoxAccountNo.Items.Contains(accountNo) && ListenedAccount != accountNo)
                {
                    ListenedAccount = accountNo;
                }
            }
            else if (e.KeyCode == Keys.Delete)
            {
                ListenedAccount = null;
            }
        }

        private void comboBoxAccountNo_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar >= 97 && e.KeyChar <= 122)
            {
                e.KeyChar = (char) (e.KeyChar & 223);
                ListenedAccount = null;
            }
            else if (e.KeyChar != 8 && (e.KeyChar < 48 || (e.KeyChar > 57 && e.KeyChar < 65) || e.KeyChar > 90))
            {
                e.Handled = true;
            }
            else
            {
                ListenedAccount = null;
            }
        }

        private void comboBoxAccountNo_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (comboBoxAccountNo.SelectedItem != null)
            {
                ListenedAccount = comboBoxAccountNo.SelectedItem.ToString();
            }
        }

        private void radioButtonCurrency_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rBtn = sender as RadioButton;

            if (rBtn.Checked)
            {
                CurrencyLoaded = rBtn.Tag.ToString();
                ChangeCurrencyColor();
                //FillAccount(null);
                FillAccount(ShownAccount);
            }
        }

        private void RefreshStockGridHeader(DataGridView DataGridView1)
        {
            if (DataGridView1.Columns.Count < 8) DataGridView1.ColumnCount = 8;

            DataGridView1.BackgroundColor = Color.AliceBlue;

            int colCount = DataGridView1.ColumnCount;
            for (int i = 0; i < colCount; i++)
            {
                if (Culture.Name == "en-US" || i != 2)
                    DataGridView1.Columns[i].DefaultCellStyle.Font = new System.Drawing.Font("Arial", 9F, FontStyle.Regular);
                else
                    DataGridView1.Columns[i].DefaultCellStyle.Font = new System.Drawing.Font("MingLiU", 9F, FontStyle.Regular);
            }

            SortComparers = new AccountStockComparer[colCount];
            SortComparers[0] = new AccountStockSignatureComparer();
            SortComparers[1] = new AccountStockSignatureComparer();
            SortComparers[2] = new AccountStockBestNameComparer();
            SortComparers[3] = new AccountStockOpeningComparer();
            SortComparers[4] = new AccountStockInTransitComparer();
            SortComparers[5] = new AccountStockInTransitSoldComparer();
            SortComparers[6] = new AccountStockNetQtyComparer();
            SortComparers[7] = new AccountStockMarketValueComparer();
            SortComparers[8] = new AccountStockStatusComparer();

            DataGridView1.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopCenter;
            DataGridView1.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopCenter;
            DataGridView1.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopLeft;
            DataGridView1.Columns[3].DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopRight;
            DataGridView1.Columns[4].DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopRight;
            DataGridView1.Columns[5].DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopRight;
            DataGridView1.Columns[6].DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopRight;
            DataGridView1.Columns[7].DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopRight;
            DataGridView1.Columns[8].DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopRight;

            DataGridView1.Columns[0].Name = "ExchangeCode";
            DataGridView1.Columns[1].Name = "StockCode";
            DataGridView1.Columns[2].Name = "StockName";
            DataGridView1.Columns[3].Name = "OnHand";
            DataGridView1.Columns[4].Name = "InTransit";
            DataGridView1.Columns[5].Name = "InTransitSold";
            DataGridView1.Columns[6].Name = "StockRemains";
            DataGridView1.Columns[7].Name = "MarketValue";
            DataGridView1.Columns[8].Name = "Status";

            DataGridView1.Columns[0].SortMode = DataGridViewColumnSortMode.Programmatic;
            DataGridView1.Columns[1].SortMode = DataGridViewColumnSortMode.Programmatic;
            DataGridView1.Columns[2].SortMode = DataGridViewColumnSortMode.Automatic;
            DataGridView1.Columns[3].SortMode = DataGridViewColumnSortMode.Automatic;
            DataGridView1.Columns[4].SortMode = DataGridViewColumnSortMode.Automatic;
            DataGridView1.Columns[5].SortMode = DataGridViewColumnSortMode.Automatic;
            DataGridView1.Columns[6].SortMode = DataGridViewColumnSortMode.Automatic;
            DataGridView1.Columns[7].SortMode = DataGridViewColumnSortMode.Automatic;
            DataGridView1.Columns[8].SortMode = DataGridViewColumnSortMode.Automatic;

            DataGridView1.Columns["ExchangeCode"].HeaderText = GetResxString("Port_ExchangeCode");
            DataGridView1.Columns["StockCode"].HeaderText = GetResxString("Port_StockCode");
            DataGridView1.Columns["StockName"].HeaderText = GetResxString("Port_StockName");
            DataGridView1.Columns["OnHand"].HeaderText = GetResxString("Port_OnHand");
            DataGridView1.Columns["InTransit"].HeaderText = GetResxString("Port_InTransit");
            DataGridView1.Columns["InTransitSold"].HeaderText = GetResxString("Port_InTransitSold");
            DataGridView1.Columns["StockRemains"].HeaderText = GetResxString("Port_StockRemains");
            DataGridView1.Columns["MarketValue"].HeaderText = GetResxString("Port_MarketValue");
            DataGridView1.Columns["Status"].HeaderText = GetResxString("Port_Status");

            DataGridView1.Columns["ExchangeCode"].Width = 40;
            DataGridView1.Columns["ExchangeCode"].FillWeight = 40;
            DataGridView1.Columns["StockCode"].Width = 45;
            DataGridView1.Columns["StockCode"].FillWeight = 55;
            DataGridView1.Columns["StockName"].Width = 100;
            DataGridView1.Columns["StockName"].FillWeight = 100;
            DataGridView1.Columns["OnHand"].Width = 75;
            DataGridView1.Columns["OnHand"].FillWeight = 75;
            DataGridView1.Columns["InTransit"].Width = 75;
            DataGridView1.Columns["InTransit"].FillWeight = 75;
            DataGridView1.Columns["InTransitSold"].Width = 75;
            DataGridView1.Columns["InTransitSold"].FillWeight = 75;
            DataGridView1.Columns["StockRemains"].Width = 75;
            DataGridView1.Columns["StockRemains"].FillWeight = 75;
            DataGridView1.Columns["MarketValue"].Width = 100;
            DataGridView1.Columns["MarketValue"].FillWeight = 100;
            DataGridView1.Columns["Status"].Width = 100;
            DataGridView1.Columns["Status"].FillWeight = 100;
        }

        private void timerFillCombo_Tick(object sender, EventArgs e)
        {
            timerFillCombo.Enabled = false;
            if (!accountComboSorted)
            {
                accountComboSorted = true;
                lock (tempAccountListMutex)
                {
                    tempAccountList.Sort();
                }
            }
            //while (tempAccountList.Count > 0)
            {
                //lock (tempAccountListMutex)
                {
                    //int loopCount = 500;  // not too much to avoid UI freeze up
                    //int count = tempAccountList.Count < loopCount ? tempAccountList.Count : loopCount;

                    AccountNoComboBox1.SuspendLayout();
                    AccountNoComboBox1.Items.AddRange(tempAccountList.ToArray());
                    //AccountNoComboBox1.AddSuggest(tempAccountList);

                    /*
                    for (int i = 0; i < count; i++)
                    {
                        string currAcc = tempAccountList[i];
                        //if (!AccountNoComboBox1.Items.Contains(currAcc))
                        {
                            //accountSortedList.Add(currAcc, currAcc);
                            AccountNoComboBox1.AddSuggest(currAcc);
                            //if (fillCompletedOnce)
                                //AccountNoComboBox1.Items.Insert(accountSortedList.IndexOfKey(currAcc), currAcc);
                            //else
                            //    AccountNoComboBox1.Items.Add(currAcc);
#if DEBUG
                                strAccNotIn = strAccNotIn + currAcc + "\r\n";
                                ttlAccountCounter++;
                                int tempInt;
                                if (currAcc.Length >= 2)
                                {
                                    int.TryParse(currAcc.Substring(currAcc.Length - 2, 2), out tempInt);
                                    ttlAccountCheckSum = ttlAccountCheckSum + tempInt;
                                }
                                labelAccTtl.Text = ttlAccountCounter.ToString();
                                labelAccChkSum.Text = ttlAccountCheckSum.ToString();
                                labelAccTtl.Visible = true;
                                labelAccChkSum.Visible = true;
#endif
                        }
                    }

                    tempAccountList.RemoveRange(0, count);
                    if (count == loopCount && tempAccountList.Count > 0)
                    {
                        timerFillCombo.Interval = 1; // to avoid UI freeze up
                        timerFillCombo.Enabled = true;
                        return;
                    }
                     */
                }
            }

            if (!AccountNoComboBox1.Enabled)
            {
//#if DEBUG
//                System.IO.FileStream file = new System.IO.FileStream(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\tempTable.txt", System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write);
//                System.IO.StreamWriter sw = new System.IO.StreamWriter(file);
//                sw.Write(strAccNotIn);
//                sw.Close();
//#endif
                if (AccountNoComboBox1.Items.Count == 1)
                {
                    AccountNoComboBox1.SelectedIndex = 0;
                    ListenedAccount = AccountNoComboBox1.SelectedItem.ToString();
                }   
                else if (AccountNoComboBox1.SelectedIndex <= 0)
                {
                    AccountNoComboBox1.Text = "";
                }

                AccountNoComboBox1.Enabled = true;
            }

            AccountNoComboBox1.ResumeLayout();
            AccountNoComboBox1.DropDownStyle = AccountNoComboBox1.Items.Count < 2 ? ComboBoxStyle.DropDownList : ComboBoxStyle.DropDown;
        }

        private void ResizeControls()
        {
            SuspendLayout();

            int v = 0;

            panelCurrency.Width = this.Width;

            splitContainer1.Width = this.Width;
            
            v = this.Height - splitContainer1.Top;
            if (v < 0) v = 0;
            splitContainer1.Height = v;

            //dataGridViewPortfolio.Width = splitContainer1.Panel2.Width;
            //dataGridViewPortfolio.Height = splitContainer1.Panel2.Height;

            //int v = this.Height - panelStock.Top;
            //if (v < 0) v = 0;
            //panelStock.Height = v;

            ResumeLayout();
        }

        private void ShowBalanceDetails(bool Show)
        {
            SuspendLayout();

            if (Show)
            {
                dataGridViewPortfolio.Columns["OnHand"].Visible = true;
                dataGridViewPortfolio.Columns["InTransit"].Visible = true;
                panelSBalance.Hide();
                panelDBalance.Show();
                splitContainer1.Panel1.AutoScrollMinSize = new Size(0, panelDBalance.Size.Height);
                splitContainer1.Panel1MinSize = 75;
                splitContainer1.SplitterDistance = panelDBalance.Size.Height + 2;
            }
            else
            {
                dataGridViewPortfolio.Columns["OnHand"].Visible = false;
                dataGridViewPortfolio.Columns["InTransit"].Visible = false;
                panelSBalance.Show();
                panelDBalance.Hide();
                splitContainer1.Panel1.AutoScrollMinSize = new Size(0, panelSBalance.Size.Height);
                splitContainer1.Panel1MinSize = panelSBalance.Size.Height;
                splitContainer1.SplitterDistance = panelSBalance.Size.Height + 2;
            }
            ResumeLayout();

            ResizeControls();
        }

        private void FillAccount(Account TheAccount)
        {
            bool generalInfoShown = false;
            bool balanceShown = false;
            bool stockShown = false;
            decimal totalMarketValue = 0;

            if (TheAccount != null)
            {
                AppendLog("FillAccount", Environment.NewLine + TheAccount.ToString(), "FillAccount", false);

                string AccountTypeName = null;
                switch (TheAccount.AccountType)
                {
                    case 'B': AccountTypeName = GetResxString("AccountTypeB"); break;
                    case 'C': AccountTypeName = GetResxString("AccountTypeC"); break;
                    case 'M': AccountTypeName = GetResxString("AccountTypeM"); break;
                    case 'T': AccountTypeName = GetResxString("AccountTypeT"); break;
                    default: AccountTypeName = ""; break;
                }
                if (!ShowAECodeDetermined)
                {
                    if (TradeDB.UserType == UserTypeEnum.Client || TradeDB.GetAECount() > 1)
                    {
                        ShowAECode = true;
                        ShowAECodeDetermined = true;
                    }
                }

                if (labelValueAECode.Visible != ShowAECode) labelValueAECode.Visible = ShowAECode;
                if (labelValueAEName.Visible != ShowAECode) labelValueAEName.Visible = ShowAECode;

                labelValueAECode.Text = TheAccount.AECode != null ? ("AE " + TheAccount.AECode) : "";
                List<AccountExecutive> aeList = GetAEByCode(TheAccount.AECode);
                labelValueAEName.Text = (aeList != null && aeList.Count > 0) ? aeList[0].Name : "";
                labelValueAccType.Text = AccountTypeName;
                labelValueAccName.Text = TheAccount.AccountName;
                toolTipAccName.SetToolTip(labelValueAccName, labelValueAccName.Text);
                if (TradeDB.UserType == UserTypeEnum.AE)
                {
                    labelValueContact.Text = (TheAccount.Phone == null || TheAccount.Phone == "." ? "" : TheAccount.Phone.Trim()) + "   " + (TheAccount.Email == null ? "" : TheAccount.Email.Trim());
                    toolTipAccName.SetToolTip(labelValueContact, labelValueContact.Text);
                }
                else
                {
                    labelValueContact.Text = "";
                    toolTipAccName.SetToolTip(labelValueContact, "");
                }

                generalInfoShown = true;

                if (TheAccount.Balances != null)
                {
                    //Dictionary<string, int> currencyRBtnListed = new Dictionary<string, int>(20);
                    string currencyRBtnCode = null;
                    int currencyRBtnIdx = 0, currencyRBtnLeft = 95;
                    RadioButton currencyRBtn = null;

                    foreach (KeyValuePair<string, AccountBalance> kvp in TheAccount.Balances)
                    {
                        if (kvp.Key != "***" && !CurrencyRBtnDict.Contains(kvp.Key))
                            CurrencyRBtnDict.Add(kvp.Key, kvp.Key);
                    }

                    for (int i = 0; i < CurrencyRBtnDict.Count; i++)
                    {
                        if ((currencyRBtnCode = (string)CurrencyRBtnDict[i]) != null &&
                            TheAccount.Balances.ContainsKey(currencyRBtnCode))
                        {
                            if (currencyRBtnIdx < CurrencyRBtnList.Count)
                                currencyRBtn = CurrencyRBtnList[currencyRBtnIdx];
                            else
                            {
                                CurrencyRBtnList.Add(currencyRBtn = new RadioButton());
                                currencyRBtn.Appearance = Appearance.Button;
                                currencyRBtn.CheckedChanged += new System.EventHandler(radioButtonCurrency_CheckedChanged);
                                currencyRBtn.AutoSize = false;
                                currencyRBtn.Left = currencyRBtnLeft;
                                currencyRBtn.Top = 3;
                                currencyRBtn.Width = 52;
                                currencyRBtn.Height = 23;
                                currencyRBtn.Margin = new Padding(0, 0, 0, 0);
                                currencyRBtn.Font = new Font(currencyRBtn.Font.FontFamily, 8.0f);
                                currencyRBtn.BackColor = Color.FromKnownColor(KnownColor.Control);
                                currencyRBtn.ForeColor = Color.FromKnownColor(KnownColor.ControlText);
                                panelCurrency.Controls.Add(currencyRBtn);
                            }

                            currencyRBtn.Visible = true;
                            currencyRBtn.Text = GetGeneralResxString("Currency_" + currencyRBtnCode) ?? currencyRBtnCode;
                            currencyRBtn.Tag = currencyRBtnCode;

                            currencyRBtnLeft += 53;
                            currencyRBtnIdx++;
                        }
                    }

                    for (; currencyRBtnIdx < CurrencyRBtnList.Count; currencyRBtnIdx++)
                    {
                        if (CurrencyRBtnList[currencyRBtnIdx].Checked)
                        {
                            CurrencyRBtnList[currencyRBtnIdx].Checked = false;  // selected currency not exist for this account
                            radioButtonCurrencyTotal.Checked = true;            // switch to total currency
                        }

                        CurrencyRBtnList[currencyRBtnIdx].Visible = false;
                    }

                    if (TheAccount.Balances.ContainsKey(CurrencyLoaded))
                    {
                        labelValueSBalance.Text = String.Format("{0:#,##0.00}", TheAccount.Balances[CurrencyLoaded].T2DayBal + TheAccount.Balances[CurrencyLoaded].FundHold + TheAccount.Balances[CurrencyLoaded].Interest);
                        labelValueSMarketValue.Text = "0";
                        labelValueSUnclearChq.Text = String.Format("{0:#,##0.00}", TheAccount.Balances[CurrencyLoaded].UnclearChequeAmount);
                        labelValueSTotalStock.Text = TheAccount.TotalStock.ToString();
                        if (TheAccount.Balances[CurrencyLoaded].AvailableCredit < 0)
                            labelValueSAvlCdt.Text = labelValueDAvlCdt.Text = "";
                        else
                            labelValueSAvlCdt.Text = labelValueDAvlCdt.Text = String.Format("{0:#,##0.00}", TheAccount.Balances[CurrencyLoaded].AvailableCredit);
                        buttonSCdtClassDescWeb.Text = buttonDCdtClassDescWeb.Text = TheAccount.CreditClass;

                        labelValueDBalT0.Text = String.Format("{0:#,##0.00}", TheAccount.Balances[CurrencyLoaded].T0DayBal);
                        labelValueDBalT1.Text = String.Format("{0:#,##0.00}", TheAccount.Balances[CurrencyLoaded].T1DayBal);
                        labelValueDBalT2.Text = String.Format("{0:#,##0.00}", TheAccount.Balances[CurrencyLoaded].T2DayBal);
                        labelValueDOutT0.Text = String.Format("{0:#,##0.00}", TheAccount.Balances[CurrencyLoaded].T0DayOut);
                        labelValueDOutT1.Text = String.Format("{0:#,##0.00}", TheAccount.Balances[CurrencyLoaded].T1DayOut);
                        labelValueDOutT2.Text = String.Format("{0:#,##0.00}", TheAccount.Balances[CurrencyLoaded].T2DayOut);
                        labelValueDInt.Text = String.Format("{0:#,##0.00}", TheAccount.Balances[CurrencyLoaded].Interest);
                        labelValueDUCA.Text = String.Format("{0:#,##0.00}", TheAccount.Balances[CurrencyLoaded].UnclearChequeAmount);
                        labelValueDFundHold.Text = String.Format("{0:#,##0.00}", TheAccount.Balances[CurrencyLoaded].FundHold);
                        labelValueDSellQ.Text = String.Format("{0:#,##0.00}", TheAccount.Balances[CurrencyLoaded].SellQueue);
                        labelValueDMarginCall.Text = String.Format("{0:#,##0.00}", TheAccount.Balances[CurrencyLoaded].MarginCall);
                        labelValueDCreditIdx.Text = String.Format("{0:#,##0}", TheAccount.Balances[CurrencyLoaded].CreditIndex);
                        labelValueDCreditLimit.Text = String.Format("{0:#,##0}", TheAccount.Balances[CurrencyLoaded].CreditLimit);
                        labelValueDMarketValue.Text = "0";
                        labelValueDAcpMarketValue.Text = String.Format("{0:#,##0}", TheAccount.Balances[CurrencyLoaded].AcceptableMarketValue);
                        labelValueDTotalStock.Text = "0";

                        balanceShown = true;
                    }
                }

                if (TheAccount.Stocks != null && TheAccount.Stocks.Count > 0)
                {
                    AccountStock accStock;
                    Stock stock;
                    int gridRowCount = dataGridViewPortfolio.Rows.Count;
                    DataGridViewRow row;
                    Dictionary<string, Stock> stockDict = null;
                    List<string> stockNeeded = new List<string>(TheAccount.Stocks.Count);
                    int intStockCode;

                    List<AccountStock> accStockList = new List<AccountStock>(TheAccount.Stocks.Count);

                    int sortColIdx = Math.Max(LastDgvPortfolioSortColIdx, 0);
                    SortOrder sortOrder = dataGridViewPortfolio.Columns[sortColIdx].HeaderCell.SortGlyphDirection;

                    StockGridDict.Clear();
                    StockGridUpdateDict.Clear();

                    if (CurrencyLoaded == "***")
                    {
                        foreach (KeyValuePair<string, AccountStock> kvp in TheAccount.Stocks)
                        {
                            if ((accStock = kvp.Value) != null)
                            {
                                accStockList.Add(accStock);
                                stockNeeded.Add(accStock.StockSignature);

                                totalMarketValue += Currency.ExchangeToBase(accStock.Currency, accStock.MarketValue);
                            }
                        }
                    }
                    else 
                    {
                        foreach (KeyValuePair<string, AccountStock> kvp in TheAccount.Stocks)
                        {
                            if ((accStock = kvp.Value) != null && accStock.Currency == CurrencyLoaded)
                            {
                                accStockList.Add(accStock);
                                stockNeeded.Add(accStock.StockSignature);

                                totalMarketValue += Currency.Exchange(accStock.Currency, CurrencyLoaded, accStock.MarketValue);
                            }
                        }
                    }

                    if (stockNeeded.Count > 0)
                    {
                        if ((stockDict = GetStockLocal(stockNeeded)) == null)
                            stockDict = new Dictionary<string, Stock>(accStockList.Count);

                        for (int i = 0; i < accStockList.Count; i++)
                        {
                            accStock = accStockList[i];
                            if (stockDict.TryGetValue(accStock.StockSignature, out stock) && stock != null)
                            {
                                if ((accStock.BestName = stock.GetBestName(Culture)) != null && accStock.BestName.Length > 0)
                                    stockDict.Remove(accStock.StockSignature);
                                else
                                    accStock.BestName = null;
                            }
                            else
                            {
                                stockDict[accStockList[i].StockSignature] = null;
                            }
                        }

                        if (SortComparers[sortColIdx] != null)
                        {
                            SortComparers[sortColIdx].SortingOrder = sortOrder;
                            accStockList.Sort(SortComparers[sortColIdx]);
                        }
                    }

                    if (gridRowCount < accStockList.Count)
                        dataGridViewPortfolio.Rows.Add(accStockList.Count - gridRowCount);
                    else if (gridRowCount > accStockList.Count)
                    {
                        if (accStockList.Count > 0)
                        {
                            for (int i = gridRowCount - 1; i >= accStockList.Count; i--)
                                dataGridViewPortfolio.Rows.RemoveAt(i);
                        }
                        else
                            dataGridViewPortfolio.Rows.Clear();
                    }

                    for (int i = 0; i < accStockList.Count; i++)
                    {
                        accStock = accStockList[i];

                        row = dataGridViewPortfolio.Rows[i];
                        row.Cells[0].Value = accStock.ExchangeCode;
                        row.Cells[1].Value = int.TryParse(accStock.Code, out intStockCode) ? (object)intStockCode : accStock.Code;
                        row.Cells[2].Value = accStock.BestName;
                        row.Cells[3].Value = accStock.QtyOnHand;
                        row.Cells[4].Value = accStock.QtyInTransit;
                        row.Cells[5].Value = accStock.ExchangeType == ExchangeTypeEnum.HKG ? null : (object)accStock.QtyInTransitSold;
                        row.Cells[6].Value = accStock.QtyOnHand + accStock.QtyInTransit;
                        row.Cells[7].Value = Math.Floor(CurrencyLoaded == "***" ? Currency.ExchangeToBase(accStock.Currency, accStock.MarketValue) : accStock.MarketValue);

                        if (accStock.SuspensionFlag == "Y")
                        {
                            row.Cells[8].Value = GetResxString("TradeStatusSuspended");
                            row.DefaultCellStyle.ForeColor = Color.Red;
                        }
                        else
                        {
                            row.Cells[8].Value = null;

                            if (accStock.Currency == "HKD")
                                row.DefaultCellStyle.ForeColor = Color.FromKnownColor(KnownColor.ControlText);
                            else
                                row.DefaultCellStyle.ForeColor = Color.SeaGreen;
                        }

                        StockGridDict.Add(accStock.StockSignature, row);

                        if (accStock.BestName == null)
                            StockGridUpdateDict.Add(accStock.StockSignature, row);
                    }

                    labelValueDTotalStock.Text = dataGridViewPortfolio.Rows.Count.ToString();

                    string strTotalMarketValue = totalMarketValue.ToString("N0");
                    labelValueDMarketValue.Text = strTotalMarketValue;

                    if (stockDict != null && stockDict.Count > 0)
                    {
                        GetStock(new List<string>(stockDict.Keys));
                        //System.Threading.Thread.Sleep(5000);
                        //GetStock(new List<string>(stockDict.Keys));
                    }

                    ShowStock(pShowStock);

                    dataGridViewPortfolio.ResumeLayout();

                    stockShown = true;
                }
            }
            
            if (!generalInfoShown)
            {
                labelValueAccType.Text = "";
                labelValueAECode.Text = "";
                labelValueAEName.Text = "";
                labelValueAccName.Text = "";
                toolTipAccName.SetToolTip(labelValueAccName, "");
                labelValueContact.Text = "";
                toolTipAccName.SetToolTip(labelValueContact, "");
            }

            if (!balanceShown)
            {
                labelValueSBalance.Text = "";
                labelValueSMarketValue.Text = "";
                labelValueSUnclearChq.Text = "";
                labelValueSTotalStock.Text = "";
                buttonSCdtClassDescWeb.Text = "";
                labelValueSAvlCdt.Text = "";
                labelValueDAvlCdt.Text = "";

                labelValueDBalT0.Text = "";
                labelValueDBalT1.Text = "";
                labelValueDBalT2.Text = "";
                labelValueDOutT0.Text = "";
                labelValueDOutT1.Text = "";
                labelValueDOutT2.Text = "";
                labelValueDInt.Text = "";
                labelValueDUCA.Text = "";
                labelValueDFundHold.Text = "";
                labelValueDSellQ.Text = "";
                labelValueDMarginCall.Text = "";
                labelValueDCreditIdx.Text = "";
                labelValueDCreditLimit.Text = "";
                labelValueDMarketValue.Text = "";
                labelValueDAcpMarketValue.Text = "";
                labelValueDTotalStock.Text = "";
                buttonDCdtClassDescWeb.Text = "";

                radioButtonCurrencyTotal.Checked = true;
            }

            if (!stockShown)
            {
                dataGridViewPortfolio.Rows.Clear();
                StockGridDict.Clear();
                StockGridUpdateDict.Clear();
            }
        }

        protected override void OnCultureChange(CultureInfo ci)
        {
            RefreshStockGridHeader(dataGridViewPortfolio);
            FillAccount(ShownAccount);
            ResizeControls();
        }

        protected override void OnAccountList(List<string> AccountList)
        {
            if (AccountList == null || AccountList.Count <= 0) 
                return;

            if (timerFillCombo.Enabled)
                timerFillCombo.Enabled = false;

            lock (tempAccountListMutex)
            {
                tempAccountList.AddRange(AccountList);
            }
            AccountNoComboBox1.SelectedItem = AccountNoComboBox1.Text;
            timerFillCombo.Interval = 1;
            timerFillCombo.Enabled = true;
        }

        protected override void OnAccount(Account TheAccount)
        {
            if (TheAccount != null && TheAccount.AccountNo == ListenedAccount)
            {
                ShownAccount = TheAccount;
                // pass the account to order ticket if stock quote form selected a order ticket
                List<BaseForm> StockQuoteFormList = GetFormByFormType(typeof(StockQuoteForm));
                if (AutoFillAccNo && AllowChangeOrderTicketAccount && StockQuoteFormList != null && StockQuoteFormList.Count > 0 && StockQuoteFormList[0] != null)
                {
                    string orderTicketInstanceID = ((StockQuoteForm)StockQuoteFormList[0]).GetOrderTicketInstanceID();
                    if (orderTicketInstanceID != "")
                    {
                        if (Utils.Utils.IsTargetForm("OrderTicketForm_simple", orderTicketInstanceID))
                        {
                            OrderTicketForm_simple orderTicketForm = (OrderTicketForm_simple)BaseForm.GetFormByInstanceId(orderTicketInstanceID);
                            if (orderTicketForm != null)
                            {
                                orderTicketForm.AllowChangeAccountFormAccount = false;
                                orderTicketForm.ListenedAccount = TheAccount.AccountNo;
                            }
                        }
                        else if (Utils.Utils.IsTargetForm("FastOrderTicketForm", orderTicketInstanceID))
                        {
                            FastOrderTicketForm orderTicketForm = (FastOrderTicketForm)BaseForm.GetFormByInstanceId(orderTicketInstanceID);
                            if (orderTicketForm != null)
                            {
                                orderTicketForm.AllowChangeAccountFormAccount = false;
                                orderTicketForm.ListenedAccount = TheAccount.AccountNo;
                            }
                        }
                        else if (Utils.Utils.IsTargetForm("OrderTicketForm", orderTicketInstanceID))
                        {
                            OrderTicketForm orderTicketForm = (OrderTicketForm)BaseForm.GetFormByInstanceId(orderTicketInstanceID);
                            if (orderTicketForm != null)
                            {
                                orderTicketForm.AllowChangeAccountFormAccount = false;
                                orderTicketForm.ListenedAccount = TheAccount.AccountNo;
                            }
                        }
                    }
                }
            }

            FillAccount(ShownAccount);
        }

        protected override void OnStock(Stock TheStock)
        {
            if (TheStock != null)
            {
                dataGridViewPortfolio.SuspendLayout();

                DataGridViewRow row;
                if (StockGridUpdateDict.TryGetValue(TheStock.StockSignature, out row) && row != null)
                {
                    // No need checking signature as the StockGridUpdateDict should be accurate

                    timerSortGrid.Enabled = false;

                    row.Cells[2].Value = TheStock.GetBestName(Culture);

                    StockGridUpdateDict.Remove(TheStock.StockSignature);

                    if (StockGridUpdateDict.Count == 0 && dataGridViewPortfolio.SortedColumn != null)
                    {
                        dataGridViewPortfolio.Sort(dataGridViewPortfolio.SortedColumn,
                            dataGridViewPortfolio.SortOrder == SortOrder.Ascending ? ListSortDirection.Ascending : ListSortDirection.Descending);
                    }
                    else
                        timerSortGrid.Enabled = true;
                }

                dataGridViewPortfolio.ResumeLayout();
            }
        }

        private void dataGridViewPortfolio_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                List<BaseForm> StockQuoteFormList;
                StockQuoteForm quoteForm;

                DataGridViewRow row = dataGridViewPortfolio.Rows[dataGridViewPortfolio.SelectedCells[0].RowIndex];
                string strStockCode = Convert.ToInt32(row.Cells["StockCode"].Value.ToString()).ToString();
                ExchangeTypeEnum exType = Stock.GetExchangeType(row.Cells[0].Value.ToString());

                switch (exType)
                {
                    case ExchangeTypeEnum.HKG:
                    case ExchangeTypeEnum.SHG:
                    case ExchangeTypeEnum.SZE:
                    case ExchangeTypeEnum.FTHKG:
                    case ExchangeTypeEnum.PMHKG:
                        StockQuoteFormList = GetFormByFormType(typeof(StockQuoteForm));
                        if (StockQuoteFormList != null && StockQuoteFormList.Count > 0 && StockQuoteFormList[0] != null)
                        {
                            quoteForm = (StockQuoteForm)StockQuoteFormList[0];

                            /*
                            if (!quoteForm.checkBoxSHG.Checked && exType == ExchangeTypeEnum.SHG)
                                quoteForm.checkBoxSHG.Checked = true;
                            else if (quoteForm.checkBoxSHG.Checked && exType != ExchangeTypeEnum.SHG)
                                quoteForm.checkBoxSHG.Checked = false;
                            if (!quoteForm.checkBoxSZE.Checked && exType == ExchangeTypeEnum.SZE)
                                quoteForm.checkBoxSZE.Checked = true;
                            else if (quoteForm.checkBoxSZE.Checked && exType != ExchangeTypeEnum.SZE)
                                quoteForm.checkBoxSZE.Checked = false;

                            quoteForm.ListenedStockCode = strStockCode;
                             */

                            quoteForm.SetStock(exType, strStockCode);
                        }
                        break;

                    /*
                    case ExchangeTypeEnum.PMHKG:
                        StockQuoteFormList = GetFormByFormType(typeof(SnapShotQuoteForm));
                        if (StockQuoteFormList != null && StockQuoteFormList.Count > 0 && StockQuoteFormList[0] != null)
                        {
                            // To be implemented
                        }
                        break;
                     */
                }
            }
            catch (Exception ex)
            {
                Debug.Print(DateTime.Now.ToString() + ": " + this.GetType().ToString() + ": " + Utils.Utils.GetFunctionName() + ": " + ex.Message);
                Console.WriteLine(ex.ToString());
            }
        }

        private void dataGridViewPortfolio_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex != this.dataGridViewPortfolio.SelectedCells[0].RowIndex)   // avoid false alarm where user click the row divider
                return;

            //List<BaseForm> StockQuoteFormList = null;
            //List<BaseForm> QuoteBrowserFormList = null;
            //StockQuoteForm quoteForm;

            DataGridViewRow row;
            row = dataGridViewPortfolio.Rows[dataGridViewPortfolio.SelectedCells[0].RowIndex];
            string strStockCode;
            ExchangeTypeEnum exType;
            long Onhand = 0, InTransitSold = 0;

            try
            {
                strStockCode = row.Cells["StockCode"].Value.ToString();
                exType = Stock.GetExchangeType(row.Cells[0].Value.ToString());

                /* No need to show stock upon cell double click since this has already been done in cell single click
                switch (exType)
                {
                    case ExchangeTypeEnum.HKG:
                    case ExchangeTypeEnum.SHG:
                    case ExchangeTypeEnum.SZE:
                    case ExchangeTypeEnum.FTHKG:
                    case ExchangeTypeEnum.PMHKG:
                        StockQuoteFormList = GetFormByFormType(typeof(StockQuoteForm));
                        QuoteBrowserFormList = GetFormByFormType(typeof(QuoteBrowserForm));
                        if (StockQuoteFormList != null && StockQuoteFormList.Count > 0 && StockQuoteFormList[0] != null)
                        {
                            quoteForm = (StockQuoteForm)StockQuoteFormList[0];

                            / *
                            if (!quoteForm.checkBoxSHG.Checked && exType == ExchangeTypeEnum.SHG)
                                quoteForm.checkBoxSHG.Checked = true;
                            else if (quoteForm.checkBoxSHG.Checked && exType != ExchangeTypeEnum.SHG)
                                quoteForm.checkBoxSHG.Checked = false;
                            if (!quoteForm.checkBoxSZE.Checked && exType == ExchangeTypeEnum.SZE)
                                quoteForm.checkBoxSZE.Checked = true;
                            else if (quoteForm.checkBoxSZE.Checked && exType != ExchangeTypeEnum.SZE)
                                quoteForm.checkBoxSZE.Checked = false;

                            ((StockQuoteForm)StockQuoteFormList[0]).ListenedStockCode = strStockCode;
                             * /

                            quoteForm.SetStock(exType, strStockCode);
                        }
                        if (QuoteBrowserFormList != null && QuoteBrowserFormList.Count > 0 && QuoteBrowserFormList[0] != null)
                            ((QuoteBrowserForm)QuoteBrowserFormList[0]).ChangeStockCode(strStockCode);
                        break;
                    / *
                    case ExchangeTypeEnum.PMHKG:
                        StockQuoteFormList = GetFormByFormType(typeof(SnapShotQuoteForm));
                        if (StockQuoteFormList != null && StockQuoteFormList.Count > 0 && StockQuoteFormList[0] != null)
                        {
                            //((SnapShotQuoteForm)StockQuoteFormList[0]).ListenedStockCode = strStockCode;
                        }
                        break;
                     * /
                }*/

                if (exType == ExchangeTypeEnum.FTHKG || exType == ExchangeTypeEnum.PMHKG)
                    exType = ExchangeTypeEnum.Gray;

                // Sell order only
                OrderTicketForm1 = new OrderTicketForm(this.Culture, null, this.dockPanelMain, exType, AccountNoComboBox1.Text.Trim());

                switch (exType)
                {
                    /*
                    case ExchangeTypeEnum.HKG:
                        OrderTicketForm1.checkBoxASHR.Checked = false;
                        OrderTicketForm1.checkBoxSZE.Checked = false;
                        OrderTicketForm1.checkBoxPMHKG.Checked = false;
                        OrderTicketForm1.Qty = row.Cells["StockRemains"].Value.ToString();
                        break;
                     */

                    case ExchangeTypeEnum.SHG:
                        if (long.TryParse(row.Cells["OnHand"].Value.ToString().Replace(",", ""), out Onhand) &&
                            long.TryParse(row.Cells["InTransitSold"].Value.ToString().Replace(",", ""), out InTransitSold) &&
                            Onhand + InTransitSold > 0)
                        {
                            OrderTicketForm1.SetStockAndOrder(exType, strStockCode, AccountNoComboBox1.Text.Trim(), null, (Onhand + InTransitSold).ToString(), false, false);
                        }
                        else
                            OrderTicketForm1.SetStockAndOrder(exType, strStockCode, AccountNoComboBox1.Text.Trim(), null, "0", false, false);
                        break;

                    case ExchangeTypeEnum.SZE:
                        if (long.TryParse(row.Cells["OnHand"].Value.ToString().Replace(",", ""), out Onhand) &&
                            long.TryParse(row.Cells["InTransitSold"].Value.ToString().Replace(",", ""), out InTransitSold) &&
                            Onhand + InTransitSold > 0)
                        {
                            OrderTicketForm1.SetStockAndOrder(exType, strStockCode, AccountNoComboBox1.Text.Trim(), null, (Onhand + InTransitSold).ToString(), false, false);
                        }
                        else
                            OrderTicketForm1.SetStockAndOrder(exType, strStockCode, AccountNoComboBox1.Text.Trim(), null, "0", false, false);
                        break;

                    /*
                    case ExchangeTypeEnum.PMHKG:
                        OrderTicketForm1.checkBoxASHR.Checked = false;
                        OrderTicketForm1.checkBoxSZE.Checked = false;
                        OrderTicketForm1.checkBoxPMHKG.Checked = true;
                        OrderTicketForm1.Qty = row.Cells["StockRemains"].Value.ToString();
                        break;
                     */

                    default:
                        OrderTicketForm1.SetStockAndOrder(exType, strStockCode, AccountNoComboBox1.Text.Trim(), null, row.Cells["StockRemains"].Value.ToString(), false, false);
                        break;
                }

                //OrderTicketForm1.StockCode = strStockCode;
                //OrderTicketForm1.Auction = false;
                //OrderTicketForm1.BuyButton = false;
                //OrderTicketForm1.IsLayoutLock = false;
                //OrderTicketForm1.AccountNo = AccountNoComboBox1.Text.Trim();

                OrderTicketForm1.Show(dockPanelMain, DockState.Float, new Rectangle(200, 70, OrderTicketForm1.Width, OrderTicketForm1.Height));
                OrderTicketForm1.Focus();
            }
            catch (Exception ex)
            {
                Debug.Print(DateTime.Now.ToString() + ": " + this.GetType().ToString() + ": " + Utils.Utils.GetFunctionName() + ": " + ex.Message);
                Console.WriteLine(ex.ToString());
            }
        }

        int LastDgvPortfolioSortColIdx = -1;

        private void dataGridViewPortfolio_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (LastDgvPortfolioSortColIdx >= 0 && e.ColumnIndex != LastDgvPortfolioSortColIdx)
                dataGridViewPortfolio.Columns[LastDgvPortfolioSortColIdx].HeaderCell.SortGlyphDirection = SortOrder.None;

            if (dataGridViewPortfolio.Columns[e.ColumnIndex].SortMode == DataGridViewColumnSortMode.Programmatic)
            {
                DataGridViewColumn col = dataGridViewPortfolio.Columns[e.ColumnIndex];

                if (col.HeaderCell.SortGlyphDirection == SortOrder.Ascending)
                    col.HeaderCell.SortGlyphDirection = SortOrder.Descending;
                else
                    col.HeaderCell.SortGlyphDirection = SortOrder.Ascending;

                dgvStockSignatureComparer.SortingOrder = col.HeaderCell.SortGlyphDirection;
                dataGridViewPortfolio.Sort(dgvStockSignatureComparer);
            }

            LastDgvPortfolioSortColIdx = e.ColumnIndex;
        }

        private void enhanceComboBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string accountNo = AccountNoComboBox1.Text.ToUpper().Trim();
                if (AccountNoComboBox1.Items.Contains(accountNo) && ListenedAccount != accountNo)
                {
                    AllowChangeOrderTicketAccount = true;
                    ListenedAccount = accountNo;
                }
            }
            else if (e.KeyCode == Keys.Delete)
            {
                ListenedAccount = null;
            }
        }

        private void enhanceComboBox1_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (AccountNoComboBox1.SelectedItem != null)
            {
                this.AllowChangeOrderTicketAccount = true;
                ListenedAccount = AccountNoComboBox1.SelectedItem.ToString();

            }
        }

        private void enhanceComboBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar >= 97 && e.KeyChar <= 122)
            {
                e.KeyChar = (char)(e.KeyChar & 223);
                ListenedAccount = null;

            }
            else if (e.KeyChar != 8 && (e.KeyChar < 48 || (e.KeyChar > 57 && e.KeyChar < 65) || e.KeyChar > 90))
                e.Handled = true;
            else
            {
                ListenedAccount = null;
            }
        }

        private void splitContainer1_SplitterMoving(object sender, SplitterCancelEventArgs e)
        {
            SplitContainer sc = (SplitContainer)sender;
            if (sc.SplitterDistance > sc.Panel1.AutoScrollMinSize.Height)
            {
                //sc.SplitterDistance = sc.Panel1.AutoScrollMinSize.Height;
            }
        }

        public bool IsAccountComboEnabled()
        {
            return AccountNoComboBox1.Enabled;
        }

        private void AccountForm_Activated(object sender, EventArgs e)
        {
            AccountNoComboBox1.Focus();
            if (AccountNoComboBox1.Enabled)
                AccountNoComboBox1.SelectAll();
        }

        private void AccountForm_MouseClick(object sender, MouseEventArgs e)
        {
            AccountNoComboBox1.Focus();
            if (AccountNoComboBox1.Enabled)
                AccountNoComboBox1.SelectAll();
        }

        public void ChangeCurrencyColor()
        {
            RadioButton rBtn = null;

            for (int i = 0; i < panelCurrency.Controls.Count; i++)
            {
                if ((rBtn = (panelCurrency.Controls[i] as RadioButton)) != null)
                {
                    if (rBtn.Checked)
                    {
                        rBtn.BackColor = Color.FromKnownColor(KnownColor.Highlight);
                        rBtn.ForeColor = Color.FromKnownColor(KnownColor.Control);
                    }
                    else
                    {
                        rBtn.BackColor = Color.FromKnownColor(KnownColor.Control);
                        rBtn.ForeColor = Color.FromKnownColor(KnownColor.ControlText);
                    }
                }
            }
        }

        private void timerSortGrid_Tick(object sender, EventArgs e)
        {
            timerSortGrid.Enabled = false;

            if (dataGridViewPortfolio.SortedColumn != null)
            {
                dataGridViewPortfolio.Sort(dataGridViewPortfolio.SortedColumn,
                    dataGridViewPortfolio.SortOrder == SortOrder.Ascending ? ListSortDirection.Ascending : ListSortDirection.Descending);
            }
        }

        private void buttonCreditClassDescWeb_Click(object sender, EventArgs e)
        {
            string host = SettingsTradeDB["LoginHost01"];
            string port = SettingsTradeDB["LoginPort01"];
            string destWeb = null;
            int iPort;
            int.TryParse(port, out iPort);

            if (host == null || host.Trim() == "") // for the case if no ini file
                host = "www.pru.hk";

            if (iPort == 80)
                destWeb = "http://" + host;
            else // if (port == 443)
                destWeb = "https://" + host;
            destWeb += "/InternetStock/Help/Default.asp?Topic=Credit";

            if (Culture != null)
            {
                switch (Culture.Name)
                {
                    case "zh-CHS":
                        destWeb += "&LangID=SC";
                        break;
                    case "zh-CHT":
                        destWeb += "&LangID=TC";
                        break;
                    default:
                        break;
                }
            }
            System.Diagnostics.Process.Start(destWeb);
            //if (GetInstanceCount(typeof(CreditClassHelpBrowserForm)) < 1)
            //{
            //    CreditClassHelpBrowserForm form = new CreditClassHelpBrowserForm(this.Culture, null);
            //    form.Show(dockPanelMain, DockState.Float, new Rectangle(200, 200, form.Width, form.Height));
            //}
        }

        public string GetCurrentAccountno()
        {
            return ListenedAccount;            
        }

        private void dataGridViewPortfolio_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
