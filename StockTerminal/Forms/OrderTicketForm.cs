using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using TradeDB;
using WeifenLuo.WinFormsUI.Docking;
using System.Collections;

namespace StockTerminal.Forms
{
    public partial class OrderTicketForm : BaseForm
    {
        public Dictionary<string, StockQuoteForm> StkQuoteDict = new Dictionary<string, StockQuoteForm>(5);

        private string StockCode = "";
        private string Price = "";
        private string Qty = "";
        private bool Auction = false;
        private bool BuyButton = true;

        private bool DisableHKG = false, DisableSZE = false, DisableSHG = false, DisableGray = false;
        //private bool EnablePMHKG = false;

        private EventHandler pHandlerCBSZE;
        private EventHandler pHandlerCBSHG;
        private EventHandler pHandlerCBGray;

        private DockPanel dockPanelMain;
        private string StockCodePending = "";
        //private decimal Spread = 1;
        //private Int64 lotSize = 100;
        private bool nonNumberEntered = false;
        private Stock CurrStock = null;
        private bool PriceFilled = false;
        private bool NameFilled = false;
        private bool LotSizeFilled = false;
        private bool SpreadTableCodeFilled = false;        
        private Account CurrAccount = null;
        //private bool noNeedToResetForm = false;
        private List<string> AccountList1 = new List<string>(20);
        private decimal pBuyLimit = 0;
        //private int BlockStatus = 0;        
        private ExchangeTypeEnum FormExType = ExchangeTypeEnum.HKG;
        private char DPGW = '\0'; // Discretion Product Group Warning
        private string OTP = "";

        private List<string> tempAccountList = new List<string>(400);
        private object tempAccountListMutex = new object();
        private SortedList accountSortedList = new SortedList();
        private bool accountComboSorted = false;
        private bool boolLoadAllStockQty = false;

        //private List<List<string>> AccountToBeFilledList = new List<List<string>>(20);
        //private object AccountToBeFilledListMutex = new object();
        //private const int AccountToBeFilledMaxCountPerInterval = 200;
        //private int AccountToBeFilledEmptyCount = 0;

        private string pListenedAccount = null;

        //private string strLastRequestStock = "";

        public string ListenedAccount
        {
            get { return pListenedAccount; }

            set
            {
                string pValue = value;
                if (pValue != null) pValue = pValue.Trim().ToUpper();

                if (pListenedAccount != pValue)
                {
                    if (pListenedAccount != null) UnListenAccount(new List<string> { pListenedAccount });
                    pListenedAccount = value;
                    if (AccountNoComboBox1 != null)
                        AccountNoComboBox1.Text = value;
                    CurrAccount = null;
                    labelAccountName.Text = "";
                    labelAccountName.ForeColor = Color.Black;
                    //FillAccount(null);

                    if (pListenedAccount != null && pListenedAccount.Length > 0)
                    {
                        ListenAccount(new List<string> { pListenedAccount });
                        //SetAccountForm(); // Remarked because ListenedAccount is not a direct user trigger and should not propagate to other form.
                    }
                }
            }
        }

        public bool AllowChangeAccountFormAccount = true;

        public decimal BuyLimit
        {
            get { return pBuyLimit; }

            set 
            {
                decimal pValue = value;
                if (pValue >= 0) pBuyLimit = value; 
            }
        }

        public OrderTicketForm()
            : this(null, null, null, ExchangeTypeEnum.Unassigned, null)
        {
            InitializeComponent();
        }

        public OrderTicketForm(CultureInfo Culture, string PersistString, DockPanel dockPanelMain, ExchangeTypeEnum FormExType, string AccountNo)
            : base(Culture, PersistString)
        {
            InitializeComponent();

            Utils.Utils.EnableDoubleBuffered(AccountNoComboBox1);

            this.dockPanelMain = dockPanelMain;

            checkBoxSZE.CheckedChanged += pHandlerCBSZE = new System.EventHandler(checkBoxSZE_CheckedChanged);
            checkBoxSHG.CheckedChanged += pHandlerCBSHG = new System.EventHandler(checkBoxSHG_CheckedChanged);
            checkBoxGray.CheckedChanged += pHandlerCBGray = new System.EventHandler(checkBoxGray_CheckedChanged);

            // central user setting
            DisableHKG = (SettingsForms["DisableHKG"] == "1");
            DisableSZE = (SettingsForms["DisableSZE"] == "1");
            DisableSHG = (SettingsForms["DisableSHG"] == "1");
            DisableGray = (SettingsForms["DisableGray"] == "1");

            if (FormExType == ExchangeTypeEnum.Unassigned)
            {
                if (LocalFormSettings["IsGrayStock"] == "1")
                {
                    if (!DisableGray)
                        this.FormExType = ExchangeTypeEnum.Gray;
                    else if (!DisableHKG)
                        this.FormExType = ExchangeTypeEnum.HKG;
                    else if (!DisableSZE)
                        this.FormExType = ExchangeTypeEnum.SZE;
                    else if (!DisableSHG)
                        this.FormExType = ExchangeTypeEnum.SHG;
                    else
                        this.FormExType = ExchangeTypeEnum.Unassigned;
                }
                else if (LocalFormSettings["IsSZE"] == "1")
                {
                    if (!DisableSZE)
                        this.FormExType = ExchangeTypeEnum.SZE;
                    else if (!DisableHKG)
                        this.FormExType = ExchangeTypeEnum.HKG;
                    else if (!DisableSHG)
                        this.FormExType = ExchangeTypeEnum.SHG;
                    else
                        this.FormExType = ExchangeTypeEnum.Unassigned;
                }
                else if (LocalFormSettings["IsSHG"] == "1")
                {
                    if (!DisableSHG)
                        this.FormExType = ExchangeTypeEnum.SHG;
                    else if (!DisableHKG)
                        this.FormExType = ExchangeTypeEnum.HKG;
                    else if (!DisableSZE)
                        this.FormExType = ExchangeTypeEnum.SZE;
                    else
                        this.FormExType = ExchangeTypeEnum.Unassigned;
                }
                else if (!DisableHKG)
                    this.FormExType = ExchangeTypeEnum.HKG;
                else if (!DisableSZE)
                    this.FormExType = ExchangeTypeEnum.SZE;
                else if (!DisableSHG)
                    this.FormExType = ExchangeTypeEnum.SHG;
                else
                    this.FormExType = ExchangeTypeEnum.Unassigned;
            }
            else
                this.FormExType = FormExType;

            if (AccountNo != null && AccountNo.Trim().Length > 0)
            {
                AccountNoComboBox1.Enabled = false;
                AccountNoComboBox1.Text = AccountNo.Trim();
            }
            else
            {
                AccountNoComboBox1.Enabled = true;
                AccountNoComboBox1.TradeDB = TradeDB;
            }
        }

        public void SetStock(ExchangeTypeEnum ExType, string StockCode)
        {
            FormExType = ExType;
            ExchangeCheckBoxChanged(null, null);
            SetCurrentStockCode(StockCode, false);
        }

        public void SetStockAndOrder(ExchangeTypeEnum ExType, string StockCode, string AccountNo, string Price, string Qty, bool Auction, bool BuyButton)
        {
            FormExType = ExType;
            ExchangeCheckBoxChanged(null, null);
            SetCurrentStockCode(StockCode, false);
            this.Price = Price;
            this.Qty = Qty;
            this.Auction = Auction;
            this.BuyButton = BuyButton;
            comboBoxAccountNo.Text = AccountNo;
            comboBoxAccountNo.Enabled = false;
        }

        private void OrderTicketForm_Load(object sender, EventArgs e)
        {
            if (comboBoxAccountNo.Text.Trim() != "" && !comboBoxAccountNo.Enabled)
                ListenedAccount = comboBoxAccountNo.Text;

            ExchangeCheckBoxChanged(null, null);

            /*
            // central user setting
            //if (SettingsForms["MaxGrayStockQuoteForm"] == null || SettingsForms["MaxGrayStockQuoteForm"].Trim() == "0")
            //    checkBoxPMHKG.Visible = false;
            DisableSZE = (SettingsForms["DisableSZE"] != null && SettingsForms["DisableSZE"] == "1");
            DisableSHG = (SettingsForms["DisableSHG"] != null && SettingsForms["DisableSHG"] == "1");
            EnablePMHKG = (SettingsForms["EnablePMHKG"] != null && SettingsForms["EnablePMHKG"] == "1");
            //EnablePMHKG = true;

            if (DisableHKG && DisableSZE && DisableSHG && EnablePMHKG)
            {
                this.Visible = false;
                return;
            }

            if (this.Visible == false)
                this.Visible = true;
            if (!DisableHKG && DisableSZE && DisableSHG) // HKG only
            {
                checkBoxSHG.Checked = false; checkBoxSZE.Checked = false;
                checkBoxSHG.Enabled = false; checkBoxSZE.Enabled = false;
                checkBoxSHG.Visible = false; checkBoxSZE.Visible = false;
            }
            else if (DisableHKG && !DisableSZE && !DisableSHG) // Disable HKG
            {
                checkBoxGray.Visible = false;
                //checkBoxASHR.Enabled = true; checkBoxSZE.Enabled = true;
                checkBoxSHG.Visible = true; checkBoxSZE.Visible = true;
                if (LocalFormSettings["IsSHG"] == "1")
                    checkBoxSHG.Checked = true;
                else if (LocalFormSettings["IsSZE"] == "1")
                    checkBoxSZE.Checked = true;
                else if (LocalFormSettings["IsPMHKG"] == "1")
                    checkBoxGray.Checked = true;
                else
                    checkBoxSHG.Checked = true;
            }
            else if (!DisableSZE && DisableSHG && DisableHKG) // SZE only
            {
                checkBoxGray.Checked = false;
                checkBoxSHG.Checked = false;
                checkBoxSZE.Checked = true;
                checkBoxSZE.Enabled = false;
                checkBoxSHG.Visible = false;
            }
            else if (!DisableSHG && DisableSZE && DisableHKG) // SHG only
            {
                checkBoxGray.Checked = false;
                checkBoxSZE.Checked = false;
                checkBoxSHG.Checked = true;
                checkBoxSHG.Enabled = false;
                checkBoxSZE.Visible = false;
            }
            else if (LocalFormSettings["IsSHG"] == "1") // load last saved setting
            {
                checkBoxSZE.Checked = false;
                checkBoxGray.Checked = false;
                checkBoxSHG.Checked = true;
            }
            else if (LocalFormSettings["IsSZE"] == "1")
            {
                checkBoxSHG.Checked = false;
                checkBoxGray.Checked = false;
                checkBoxSZE.Checked = true;
            }

            if (EnablePMHKG)
            {
                if (LocalFormSettings["IsPMHKG"] == "1")
                {
                    checkBoxSHG.Checked = false;
                    checkBoxSZE.Checked = false;
                    checkBoxGray.Visible = true;
                    checkBoxGray.Enabled = true;
                    checkBoxGray.Checked = true;
                }
            }
            else
            {
                checkBoxGray.Checked = false;
                checkBoxGray.Enabled = false;
                checkBoxGray.Visible = false;
            }
            */

            if (TradeDB.UserType == UserTypeEnum.AE)
            {
                panelAE.Top = myGroupBoxOrder.Top + myGroupBoxOrder.Height;
                panelAE.Visible = true;
                panelSpecialType.Top = panelAE.Top + panelAE.Height;
                panelPriceArrange.Visible = true;

                if (SettingsForms["AggOrderBtn"] != null && SettingsForms["AggOrderBtn"] == "1")
                    checkBoxAggOrder.Visible = true;
                else
                    checkBoxAggOrder.Visible = false;
                checkBoxAggOrder.Top = panelSpecialType.Top + panelSpecialType.Height;
            }
            else
            {
                panelPriceArrange.Visible = false;
                myGroupBoxOrder.Top = panelPriceArrange.Top;
                panelAE.Visible = false;
                checkBoxAggOrder.Visible = false;
            }

            checkBoxLock.BackgroundImage = Properties.Resources.blueUnlock as Image;
            Redraw();

            if (AccountNoComboBox1.Text == null || AccountNoComboBox1.Text.Trim().Length <= 0)
            {
                ListenAccountList();
                AccountNoComboBox1.Text = GetResxString("ComboBoxAccountNo_Loading");
            }

            LayoutLockable = false;

            //comboBoxAccountNo.AutoCompleteSource = AutoCompleteSource.None;
            //comboBoxAccountNo.AutoCompleteMode = AutoCompleteMode.None;
            //comboBoxAccountNo.Sorted = false;
            //comboBoxAccountNo.Text = GetResxString("ComboBoxAccountNo_Loading");
            //comboBoxAccountNo.SuspendLayout();

            AccountNoComboBox1.Enabled = false;
            AccountNoComboBox1.AutoCompleteSource = AutoCompleteSource.None;
            AccountNoComboBox1.AutoCompleteMode = AutoCompleteMode.None;
            AccountNoComboBox1.Sorted = false;

            AccountNoComboBox1.SuspendLayout();

            if (LocalFormSettings["preset_accno-1"] != null)
            {
                listBoxAccountno.Items.Add(LocalFormSettings["preset_accno-1"]);
            }

            SetVisibleSpecialType(booVisibleSpecialType);
        }

        public void Redraw()
        {
            if (!checkBoxLock.Checked) { if (textBoxStockCode.Text.Trim() != StockCode) this.textBoxStockCode.Text = StockCode; }
            this.textBoxStockPrice.Text = ""; // Price;
            if (!checkBoxLock.Checked) this.textBoxStockQty.Text = Qty;
            this.labelStockName.Text = "";
            this.labelLotSize.Text = "";
            this.checkBoxAuction.Enabled = Auction;
            this.buttonBuy.Visible = BuyButton;
            labelBidRange.Text = "";
            labelAskRange.Text = "";
            checkBoxCheckPrice.Checked = true;
            //checkBoxCheckPrice.Enabled = false;
            checkBoxAllOrNothing.Checked = false;
            checkBoxSpecialLimit.Checked = false;
            PriceFilled = false;
            NameFilled = false;
            LotSizeFilled = false;
            SpreadTableCodeFilled = false;            
            ShowTradeStatus(CurrStock);
            calAmount();
        }

        private void ResetForm(string NewStockCode)
        {
            if (NewStockCode != null)
            {
                if (CurrStock != null) UnListenStock(new List<string> { CurrStock.StockSignature });
                CurrStock = null;
                StockCode = NewStockCode.Trim();
            }
            else
                StockCode = "";
            //this.CurrStock = null;
            Qty = "";            
            this.checkBoxAuction.Checked = false;
            checkBoxStockOnHand.Checked = false;
            if (checkBoxAggOrder.Visible == true)
                checkBoxAggOrder.Checked = false;
            //Auction = false;
            Price = "";
            DPGW = '\0';
            OTP = "";
            EnableDisableBuySellBtn(true, true);
            Redraw();
        }

        private void checkBoxLmt_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxLmt.Checked)
                textBoxLmt.Enabled = true;
            else
                textBoxLmt.Enabled = false;
        }

        private void PrepareOrder(char BuySell)
        {
            Order order1 = new Order();
            order1.AccountNo = ListenedAccount.ToUpper().Trim(); // CurrAccount.AccountNo.ToUpper().Trim();
            order1.StockCode = textBoxStockCode.Text.Trim();
            order1.Side = BuySell;
            decimal.TryParse(textBoxStockPrice.Text , out order1.Price);
            int.TryParse(this.textBoxStockQty.Text.Replace(",",""), out order1.Quantity);
            if (checkBoxAuction.Checked == true)
                order1.OrderType = 'A';
            else if (checkBoxSpecialLimit.Checked == true)
            {
                order1.OrderType = 'S';  //order1.OrderType = 'X';
            }
            else
            {
                order1.OrderType = 'X';
            }

            if (FormExType == ExchangeTypeEnum.SHG || FormExType == ExchangeTypeEnum.SZE) { order1.OrderType = 'L'; }

            order1.Filled = 0;
            bool shortonhand = (checkBoxStockOnHand.Checked) ? true : false;
            bool allornothing = (checkBoxAllOrNothing.Checked) ? true : false;
            bool aggregateOrder = (checkBoxAggOrder.Checked) ? true : false;

            string Message;
            string actRef;
            bool reply = TradeDB.OrderPlace(order1.AccountNo, order1.Side, order1.StockCode, order1.Price, order1.Quantity, order1.OrderType, allornothing, shortonhand, 0, FormExType, DPGW, aggregateOrder, OTP, out Message, out actRef);
            DPGW = '\0';
            OTP = "";
            if (reply == false)
            {
                ShowMessageBox(GetResxString("PlaceOrderError"), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                //(GetFormByFormType(typeof(MainForm))[0] as MainForm).RequestToLogout();
            }
        }

        private void buttonBuy_Click(object sender, EventArgs e)
        {            
            if (!ChkOrderValue('B')) return;
            
            //no stockonhand for buying
            checkBoxStockOnHand.Checked = false;
            BuySellConfirmationForm bsForm = new BuySellConfirmationForm(this.Culture, null);
            bsForm.IsBuyOrder = true;
            bsForm.strAcc = AccountNoComboBox1.Text.Trim();
            bsForm.strBuySell = buttonBuy.Text;
            bsForm.strStkCode = textBoxStockCode.Text;
            bsForm.strExchangeCode = Stock.GetExchangeCode(FormExType);
            if (CurrStock != null && CurrStock.Currency != null)
                bsForm.strStkPrice = GetBestCurrencySign(CurrStock.Currency) + textBoxStockPrice.Text + "  ";
            else
                bsForm.strStkPrice = "$" + textBoxStockPrice.Text;
            bsForm.strStkQty = textBoxStockQty.Text;
            if (CurrStock != null && CurrStock.Currency != null)
                bsForm.strStkTtlAmt = GetBestCurrencySign(CurrStock.Currency) + labelTotal.Text + "  (" + GetGeneralResxString("Currency_" + CurrStock.Currency) + ")";
            else
                bsForm.strStkTtlAmt = "$" + labelTotal.Text;
            bsForm.IsAuctionType = checkBoxAuction.Checked;
            bsForm.RequireOTP = (SettingsForms["SkipOrderOTP"] != "1" && TradeDB.RequireOTP) ? true : false;
            bsForm.StartPosition = FormStartPosition.CenterScreen;

            if (CurrAccount != null)
            {
                switch (CurrAccount.DiscretionCheck(CurrStock))
                {
                    case 1:
                        DPGW = 'C'; // Order is from client directly
                        DiscretionConfirmForm dcForm = new DiscretionConfirmForm(this.Culture, null);
                        dcForm.StartPosition = FormStartPosition.CenterScreen;
                        if (dcForm.ShowDialog(this) != DialogResult.OK)
                            return;
                        bsForm.ShowInlineWithInvestPref = false;
                        break;

                    case 0:
                        DPGW = 'I'; // Order is inline with client investment preference
                        bsForm.ShowInlineWithInvestPref = true;
                        break;

                    case -1:
                        DPGW = 'N'; // Client account is not discretion account
                        bsForm.ShowInlineWithInvestPref = false;
                        break;
                }
            }

            /* ChiNext to be checked by Stock Server
            //Check SZ chiNext stock            
            int stkCode;            
            int.TryParse(textBoxStockCode.Text, out stkCode);

            if ((FormExType == ExchangeTypeEnum.SZE && stkCode >= 300000 && stkCode <= 399999 && (SettingsForms["AllowSZEChiNext"] == null || SettingsForms["AllowSZEChiNext"] == "1") &&
            CurrAccount != null && CurrAccount.AllowCscSzeChiNext != null))
            {
                if (CurrAccount.AllowCscSzeChiNext.Trim().ToUpper() == "Y" || SettingsForms["AllowSZEChiNext"] == "1")
                {
                    TotalAmtLimitForm limitForm;
                    limitForm = new TotalAmtLimitForm(this.Culture, null, "", 0, "CHINEXT");
                    if (limitForm.ShowDialog(this) != DialogResult.OK) return;
                }

            }*/

            //Check Warnning amount            
            decimal total;
            decimal.TryParse(labelTotal.Text, out total);
            if (total > 300000)
            {
                if (SettingsUserPreference["ConfirmBeforeOrder"] == null || SettingsUserPreference["ConfirmBeforeOrder"] != "0")
                {
                    TotalAmtLimitForm limitForm;
                    if (CurrStock != null && CurrStock.Currency != null)
                        limitForm = new TotalAmtLimitForm(this.Culture, null, labelTotal.Text, 300000, GetBestCurrencySign(CurrStock.Currency));
                    else
                        limitForm = new TotalAmtLimitForm(this.Culture, null, labelTotal.Text, 300000, "$");
                    if (limitForm.ShowDialog(this) != DialogResult.OK) return;
                    //////if ((SettingsUserPreference["ConfirmBeforeOrder"] != null && SettingsUserPreference["ConfirmBeforeOrder"] == "0") || limitForm.ShowDialog(this) != DialogResult.OK) return;
                }
            }

            if ((SettingsUserPreference["ConfirmBeforeOrder"] != null && SettingsUserPreference["ConfirmBeforeOrder"] == "0") || bsForm.ShowDialog(this) == DialogResult.OK)
            {
                OTP = bsForm.OTP;
                PrepareOrder('B');
                if (TradeDB.UserType == UserTypeEnum.AE && !KeepAccNoAfterPlaceOrder)
                {
                    ListenedAccount = null;
                    SetAccountForm();
                }

                if (TradeDB.UserType != UserTypeEnum.AE)
                    textBoxStockCode.Focus();
                else
                {
                    checkBoxStockOnHand.Checked = false;
                    //comboBoxAccountNo.Focus();
                    AccountNoComboBox1.Focus();
                }
                if (checkBoxResetForm.Checked == true) ResetForm("");
                //////checkBoxASHR.BackColor = Color.FromArgb(255, 255, 128);
                //////checkBoxASHR.Checked = false;
            }                      
        }

        private void buttonSell_Click(object sender, EventArgs e)
        {
            string remarks;

            if (!ChkBalanceQty('A', out remarks)) return;
            if (!ChkOrderValue('A')) return;

            BuySellConfirmationForm bsForm = new BuySellConfirmationForm(this.Culture, null);
            bsForm.IsBuyOrder = false;
            bsForm.strAcc = AccountNoComboBox1.Text.Trim();
            bsForm.strBuySell = buttonSell.Text;
            bsForm.strExchangeCode = Stock.GetExchangeCode(FormExType);
            bsForm.strStkCode = textBoxStockCode.Text;
            if (CurrStock != null && CurrStock.Currency != null)
                bsForm.strStkPrice = GetBestCurrencySign(CurrStock.Currency) + textBoxStockPrice.Text + "  ";
            else
                bsForm.strStkPrice = "$" + textBoxStockPrice.Text;
            bsForm.strStkQty = textBoxStockQty.Text;
            if (CurrStock != null && CurrStock.Currency != null)
                bsForm.strStkTtlAmt = GetBestCurrencySign(CurrStock.Currency) + labelTotal.Text + "  (" + GetGeneralResxString("Currency_" + CurrStock.Currency) + ")";
            else
                bsForm.strStkTtlAmt = "$" + labelTotal.Text;
            bsForm.IsAuctionType = checkBoxAuction.Checked;
            bsForm.RequireOTP = false;
            bsForm.Remarks = remarks;
            bsForm.StartPosition = FormStartPosition.CenterScreen;

            /* ChiNext to be checked by Stock Server
            //Check SZ chiNext stock            
            int stkCode;
            int.TryParse(textBoxStockCode.Text, out stkCode);

            if ((FormExType == ExchangeTypeEnum.SZE && stkCode >= 300000 && stkCode <= 399999 && (SettingsForms["AllowSZEChiNext"] == null || SettingsForms["AllowSZEChiNext"] == "1") &&
            CurrAccount != null && CurrAccount.AllowCscSzeChiNext != null))       
            {
                if (CurrAccount.AllowCscSzeChiNext.Trim().ToUpper() == "Y" || SettingsForms["AllowSZEChiNext"] == "1")                
                {
                    TotalAmtLimitForm limitForm;
                    limitForm = new TotalAmtLimitForm(this.Culture, null, "", 0, "CHINEXT");
                    if (limitForm.ShowDialog(this) != DialogResult.OK) return;
                }
            }*/

            if ((SettingsUserPreference["ConfirmBeforeOrder"] != null && SettingsUserPreference["ConfirmBeforeOrder"] == "0") || bsForm.ShowDialog(this) == DialogResult.OK)
            {
                OTP = bsForm.OTP;
                DPGW = 'C';
                PrepareOrder('A');
                if (TradeDB.UserType == UserTypeEnum.AE && !KeepAccNoAfterPlaceOrder)
                {
                    ListenedAccount = null;
                    SetAccountForm();
                }

                if (TradeDB.UserType != UserTypeEnum.AE)
                    textBoxStockCode.Focus();
                else
                {
                    checkBoxStockOnHand.Checked = false;
                    //comboBoxAccountNo.Focus();
                    AccountNoComboBox1.Focus();
                }
                if (checkBoxResetForm.Checked == true) ResetForm("");
                //////checkBoxASHR.BackColor = Color.FromArgb(255, 255, 128);
                //////checkBoxASHR.Checked = false;
            }

            if (this.buttonBuy.Visible == false) this.Close();
        }

        private void textBoxPriceQty_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            nonNumberEntered = false;

            if (e.KeyCode < Keys.D0 || e.KeyCode > Keys.D9)
            {
                if (e.KeyCode < Keys.NumPad0 || e.KeyCode > Keys.NumPad9)
                {
                    if (e.KeyCode != Keys.Back)
                    {
                        nonNumberEntered = true;
                    }
                    if (e.KeyCode == Keys.Up) { e.Handled = true; focusPrevControl(sender, e); }
                    if (e.KeyCode == Keys.Down) { e.Handled = true; focusNextControl(sender, e); }
                }
            }
            if (Control.ModifierKeys == Keys.Shift)
            {
                nonNumberEntered = true;
            }
        }

        private void textBoxStockQty_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter) // key press cannot capture arrow, page up/down
            {
                e.Handled = true;
                focusNextControl(sender, e);                
                return;
            }

            if (e.KeyChar.ToString() == "*")
            {
                textBoxStockQty.Text = textBoxStockQty.Text.Trim() + "0000";                
                e.Handled = true;                
                return;
            }            

            if (e.KeyChar.ToString() != "-" && e.KeyChar.ToString() != "+" && nonNumberEntered)
            {
                e.Handled = true;
                return;
            }

            if (e.KeyChar.ToString() == "-" || e.KeyChar.ToString() == "+")
            {
                if (textBoxStockQty.Text != "")
                {
                    decimal InputQty = 0;
                    //////if (FormExType == ExchangeTypeEnum.HKG)
                        InputQty = CheckLotSize(textBoxStockQty.Text);
                    //////else if (FormExType == ExchangeTypeEnum.SHG)
                    //////    decimal.TryParse(textBoxStockQty.Text, out InputQty);

                    if (InputQty < 0) { e.Handled = true; return; }

                    //long stkQty = Convert.ToInt64(textBoxStockQty.Text.Replace(",", ""));
                    long stkLotSize = (labelLotSize.Text.Trim() != "") ? long.Parse(labelLotSize.Text) : 0;
                    if (e.KeyChar.ToString() == "-" && InputQty - stkLotSize > 0)
                    {
                        if (labelLotSize.Text.Trim() != "") InputQty -= stkLotSize;
                    }
                    else if (e.KeyChar.ToString() == "+")
                    {
                        if (labelLotSize.Text.Trim() != "") InputQty += stkLotSize;
                    }
                    textBoxStockQty.Text = String.Format("{0:N0}", InputQty);
                    textBoxStockQty.SelectAll();
                }
                e.Handled = true;
                return;
            }
        }

        private void textBoxStockPrice_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                SetQty();
                focusNextControl(sender, e);                
                return;
            }

            if ((e.KeyChar.ToString() != "-" && e.KeyChar.ToString() != "+" && nonNumberEntered && e.KeyChar.ToString() != ".") ||
                (textBoxStockPrice.Text.IndexOf(".") >= 0 && e.KeyChar.ToString() == "."))
            {
                e.Handled = true;
                return;
            }

            if (e.KeyChar.ToString() == "-" || e.KeyChar.ToString() == "+")
            {
                if (textBoxStockPrice.Text != "" && textBoxStockPrice.Text != ".")
                {
                    decimal stkPrice = Convert.ToDecimal(textBoxStockPrice.Text.Replace(",", ""));                    
                    bool CanBuy = false, CanSell= false;
                    if (FormExType == ExchangeTypeEnum.HKG || FormExType == ExchangeTypeEnum.PMHKG || FormExType == ExchangeTypeEnum.FTHKG)
                    {
                        if (e.KeyChar.ToString() == "-")
                            stkPrice = Utils.OrderTypeSpread.CheckSpread(stkPrice, 'X', ' ', -1, CurrStock, TradeDB, out CanBuy, out CanSell);
                        else if (e.KeyChar.ToString() == "+")
                            stkPrice = Utils.OrderTypeSpread.CheckSpread(stkPrice, 'X', ' ', 1, CurrStock, TradeDB, out CanBuy, out CanSell);
                    }
                    else if (FormExType == ExchangeTypeEnum.SHG || FormExType == ExchangeTypeEnum.SZE)
                    {
                        if (e.KeyChar.ToString() == "-")
                            stkPrice = Utils.OrderTypeSpread.CheckSpread_ASHR(stkPrice, 'X', ' ', -1, CurrStock, TradeDB, out CanBuy, out CanSell);
                        else if (e.KeyChar.ToString() == "+")
                            stkPrice = Utils.OrderTypeSpread.CheckSpread_ASHR(stkPrice, 'X', ' ', 1, CurrStock, TradeDB, out CanBuy, out CanSell);
                    }
                    //EnableDisableBuySellBtn(CanBuy, CanSell);
                    if (stkPrice > 0) textBoxStockPrice.Text = String.Format("{0:N3}", stkPrice);
                    textBoxStockPrice.SelectionStart = 0;
                    textBoxStockPrice.SelectionLength = textBoxStockPrice.Text.Length;
                }
                e.Handled = true;
                return;
            }
        }

        private void EnableDisableBuySellBtn(bool CanBuy, bool CanSell)
        {
            if (checkBoxCheckPrice.Checked == false)
            {
                buttonBuy.Enabled = true;
                buttonBuy.BackColor = Color.SkyBlue;
                buttonSell.Enabled = true;
                buttonSell.BackColor = Color.LightPink;
            }
            else
            {
                if (CanBuy == true && buttonBuy.Enabled == false)
                {
                    buttonBuy.Enabled = true;
                    buttonBuy.BackColor = Color.SkyBlue;
                }
                else if (CanBuy == false && buttonBuy.Enabled == true)
                {
                    buttonBuy.Enabled = false;
                    buttonBuy.BackColor = Color.Silver;
                }

                if (CanSell == true && buttonSell.Enabled == false)
                {
                    buttonSell.Enabled = true;
                    buttonSell.BackColor = Color.LightPink;
                }
                else if (CanSell == false && buttonSell.Enabled == true)
                {
                    buttonSell.Enabled = false;
                    buttonSell.BackColor = Color.Silver;
                }
            }
        }


        private void focusNextControl(object sender, object o)
        {
            Control currCtl = (Control)sender; //current control
            if (o.GetType().Name == "KeyEventArgs")
            {
                KeyEventArgs e = (KeyEventArgs)o;
                e.Handled = true;
            }
            else if (o.GetType().Name == "KeyPressEventArgs")
            {
                KeyPressEventArgs r = (KeyPressEventArgs)o;
                r.Handled = true;
            }
                
            Control c = GetNextControl(currCtl, true);
            if (c != null)
            {
                if (c.Enabled == false || c.Visible == false)
                    c = GetNextControl(c, true);
                if (c == null)
                    return;
                c.Focus();
            }
        }


        private void focusPrevControl(object sender, object o)
        {
            Control currCtl = (Control)sender; //current control
            if (o.GetType().Name == "KeyEventArgs")
            {
                KeyEventArgs e = (KeyEventArgs)o;
                e.Handled = true;
            }
            else if (o.GetType().Name == "KeyPressEventArgs")
            {
                KeyPressEventArgs r = (KeyPressEventArgs)o;
                r.Handled = true;
            }
            
            Control c = GetNextControl(currCtl, false);
            if (c != null)
            {
                if (c.Enabled == false || c.Visible == false)
                    c = GetNextControl(c, false);
                if (c == null)
                    return;
                c.Focus();
            }
        }

        private void textBoxStockCode_KeyPress(object sender, KeyPressEventArgs e)
        {
            StockCodePending = "";  // to make leave event to call SetCurrentStockCode if not Enter is pressed

            if (e.KeyChar == (char)Keys.Enter)
            {

                //////checkBoxCheckPrice.Enabled = true;
                if (textBoxStockPrice.Text.Trim() == "")
                {
                    if (textBoxStockCode.Text.Trim() != "")
                        this.AllowChangeAccountFormAccount = true;

                    SetCurrentStockCode(textBoxStockCode.Text.Trim().TrimStart('0'), true);
                    if (textBoxStockCode.Text.Trim() != "")
                    {
                        CheckCodeValue();

                        List<BaseForm> StockQuoteFormList = GetFormByFormType(typeof(StockQuoteForm));
                        if (StockQuoteFormList != null && StockQuoteFormList.Count > 0 && StockQuoteFormList[0] != null)
                            ((StockQuoteForm)StockQuoteFormList[0]).SetStock(FormExType, textBoxStockCode.Text);
                    }
                }
                

                SetAccountForm();

                focusNextControl(sender, e);

                //checkBoxCheckPrice.Enabled = true; //for IPO listdate before 9am
                
                e.Handled = true;
                return;
            }

            if (FormExType == ExchangeTypeEnum.PMHKG || FormExType == ExchangeTypeEnum.FTHKG)
                FormExType = ExchangeTypeEnum.Gray;

            if (e.KeyChar.ToString() != "-" && e.KeyChar.ToString() != "+" && nonNumberEntered)
            {
                e.Handled = true;
                return;
            }

            if (e.KeyChar.ToString() == "-" || e.KeyChar.ToString() == "+")
            {
                if (textBoxStockCode.Text != "")
                {
                    long inputStkCode = Convert.ToInt64(textBoxStockCode.Text.Replace(",", ""));
                    if (e.KeyChar.ToString() == "-" && inputStkCode - 1 > 0)
                        inputStkCode -= 1;
                    else if (e.KeyChar.ToString() == "+")
                        inputStkCode += 1;
                    textBoxStockCode.Text = inputStkCode.ToString();
                }
                e.Handled = true;
                return;
            }
        }

        private void checkBoxLock_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxLock.Checked)
            {
                checkBoxLock.BackgroundImage = Properties.Resources.blueLock as Image;
                textBoxStockCode.Enabled = false;
                textBoxStockQty.Enabled = false;                
                vScrollBarQty.Enabled = false;
            }
            else
            {
                checkBoxLock.BackgroundImage = Properties.Resources.blueUnlock as Image;
                textBoxStockCode.Enabled = true;
                textBoxStockQty.Enabled = true;                
                vScrollBarQty.Enabled = true;
            }
        }

        private void checkBoxAuction_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxAuction.Checked)
            {
                textBoxStockPrice.Text = "0";
                textBoxStockPrice.Enabled = false;
                vScrollBarPrice.Enabled = false;
            }
            else
            {
                textBoxStockPrice.Enabled = true;
                vScrollBarPrice.Enabled = true;
            }
        }

        private void textBoxStockPriceQty_Leave(object sender, EventArgs e)
        {            
            calAmount();

            if (textBoxStockPrice.Text.Trim() != "")
                CheckPriceValue();

            if (textBoxStockQty.Text.Trim() != "")
                CheckQtyValue(' ');            
        }


        private string formatPrice(string textPrice)
        {
            try
            {
                if (textPrice != "" && textPrice != ".")
                    if (textPrice.IndexOf(".") >= 0)
                    {
                        string beforedec = textPrice.Substring(0, textPrice.IndexOf("."));
                        string afterdec = textPrice.Substring(textPrice.IndexOf("."));
                        if (afterdec.Length > 4)
                        {
                            textPrice = String.Format("{0:0}", beforedec) + afterdec.Substring(0, 4);
                        }
                        else
                        {
                            textPrice = String.Format("{0:0.000}", Convert.ToDecimal(textPrice.Replace(",", "")));
                        }
                    }
                return textPrice;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return "";
            }            
        }


        private void calAmount()
        {
            try
            {
                string textPrice = textBoxStockPrice.Text.Trim();
                string textQty = textBoxStockQty.Text.Trim();
                bool booPrevSelectedAll = false;
                
                
                if (textPrice != "" && textPrice != "." && textQty != "")
                    labelTotal.Text = String.Format("{0:N0}", Convert.ToDecimal(textPrice.Replace(",", "")) * Convert.ToInt64(textQty.Replace(",", "")));
                else
                    labelTotal.Text = "";

                if (textBoxStockPrice.SelectionLength == textBoxStockPrice.Text.Length && textBoxStockPrice.Text.Length > 0)
                {
                    booPrevSelectedAll = true;
                }
                textBoxStockPrice.Text = formatPrice(textPrice);
                if (booPrevSelectedAll == true)
                    textBoxStockPrice.SelectAll();

                if (textQty != "")
                    textBoxStockQty.Text = String.Format("{0:N0}", Convert.ToInt64(textQty.Replace(",", "")));
                
                this.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void vScrollBarPrice_Scroll(object sender, ScrollEventArgs e)
        {
            if (e.Type == ScrollEventType.EndScroll) return;

            decimal stkPrice = 0;
            if (textBoxStockPrice.Text == "" || textBoxStockPrice.Text == "." || !decimal.TryParse(textBoxStockPrice.Text.Replace(",", ""), out stkPrice) || stkPrice < 0)
                return;

            bool CanBuy = false, CanSell = false;
            if (FormExType == ExchangeTypeEnum.HKG || FormExType == ExchangeTypeEnum.FTHKG || FormExType == ExchangeTypeEnum.PMHKG)
            {
                if (e.Type == ScrollEventType.SmallDecrement)
                    stkPrice = Utils.OrderTypeSpread.CheckSpread(stkPrice, 'X', ' ', 1, CurrStock, TradeDB, out CanBuy, out CanSell);
                if (e.Type == ScrollEventType.SmallIncrement)
                    stkPrice = Utils.OrderTypeSpread.CheckSpread(stkPrice, 'X', ' ', -1, CurrStock, TradeDB, out CanBuy, out CanSell);
            }
            else if (FormExType == ExchangeTypeEnum.SHG || FormExType == ExchangeTypeEnum.SZE)
            {
                if (e.Type == ScrollEventType.SmallDecrement)
                    stkPrice = Utils.OrderTypeSpread.CheckSpread_ASHR(stkPrice, 'X', ' ', 1, CurrStock, TradeDB, out CanBuy, out CanSell);
                if (e.Type == ScrollEventType.SmallIncrement)
                    stkPrice = Utils.OrderTypeSpread.CheckSpread_ASHR(stkPrice, 'X', ' ', -1, CurrStock, TradeDB, out CanBuy, out CanSell);
            }
            EnableDisableBuySellBtn(CanBuy, CanSell);
            if (stkPrice >= 0) textBoxStockPrice.Text = String.Format("{0:N3}", stkPrice);
            textBoxStockPrice.SelectionStart = 0;
            textBoxStockPrice.SelectionLength = textBoxStockPrice.Text.Length;
            calAmount();
        }

        private void vScrollBarQty_Scroll(object sender, ScrollEventArgs e)
        {
            if (e.Type == ScrollEventType.EndScroll) return;

            decimal InputQty = 0;
            long stkLotSize = (labelLotSize.Text.Trim() != "") ? long.Parse(labelLotSize.Text) : 0;
            if (textBoxStockQty.Text != "")
                InputQty = CheckLotSize(textBoxStockQty.Text);            
            if (InputQty < 0) return;

            if (e.Type == ScrollEventType.SmallDecrement)
                if (labelLotSize.Text.Trim() != "") InputQty += stkLotSize;
            if (e.Type == ScrollEventType.SmallIncrement && InputQty - stkLotSize > 0)
                if (labelLotSize.Text.Trim() != "") InputQty -= stkLotSize;
            if (InputQty >= 0) textBoxStockQty.Text = String.Format("{0:N0}", InputQty);
            textBoxStockQty.SelectionStart = 0;
            textBoxStockQty.SelectionLength = textBoxStockQty.Text.Length;
            calAmount();

        }

        private void textBoxStockPrice_KeyUp(object sender, KeyEventArgs e)
        {
            int oldLen = textBoxStockPrice.SelectionStart;
            int oldSelStart = textBoxStockPrice.SelectionStart;
            bool hasCommaBefore = textBoxStockPrice.Text.IndexOf(",") >= 0 ? true : false;
            bool hasDotBefore = textBoxStockPrice.Text.IndexOf(".") == 0 ? true : false;
            calAmount();
            bool hasCommaAfter = textBoxStockPrice.Text.IndexOf(",") >= 0 ? true : false;
            bool hasDotAfter = textBoxStockPrice.Text.IndexOf("0.") == 0 ? true : false;
            if (!hasCommaBefore && hasCommaAfter)
                oldLen++;
            else if (hasCommaBefore && !hasCommaAfter)
                oldLen--;
            if (hasDotBefore && hasDotAfter)
                oldLen++;
            if (oldLen < 0)
                oldLen = 0;
            if (e.KeyValue == 110 || e.KeyValue == 190)  //.
            {
                string oldprice = textBoxStockPrice.Text.Trim();
                int dotpos = oldprice.IndexOf(".");
                if (oldSelStart == 0) // input "." at the pos 0
                {                    
                    textBoxStockPrice.Text = "0.000";
                    oldLen = 2;
                }
                else if (dotpos > 0 && dotpos == oldLen) // input "." same pos as old "."
                {
                    oldLen = dotpos + 1;
                    textBoxStockPrice.Text = String.Format("{0:0.000}", Convert.ToDecimal(textBoxStockPrice.Text.Replace(",", "")));
                }
                else if (dotpos > 0) // input "." not same pos as old "."
                {
                    string beforedec = textBoxStockPrice.Text.Substring(0, textBoxStockPrice.Text.IndexOf("."));
                    string afterdec = textBoxStockPrice.Text.Substring(textBoxStockPrice.Text.IndexOf("."));                    
                    textBoxStockPrice.Text = String.Format("{0:0}", beforedec) + ".000";
                    oldLen = textBoxStockPrice.Text.IndexOf(".") + 1;                    
                }                
            }
            if (e.KeyValue == 46)  //Del
            {
                textBoxStockPrice.Text = "0.000";
                oldLen = 0;
                calAmount();
                textBoxStockPrice.SelectAll();
            }
            else if (e.KeyValue == 13)  //enter
            {
                textBoxStockPrice.SelectAll();
            }
            else
            {
                textBoxStockPrice.SelectionStart = oldLen;
            }
        }

        private void textBoxStockQty_KeyUp(object sender, KeyEventArgs e)
        {
            int oldLen = textBoxStockQty.SelectionStart;
            bool hasCommaBefore = textBoxStockQty.Text.IndexOf(",") >= 0 ? true : false;
            calAmount();
            bool hasCommaAfter = textBoxStockQty.Text.IndexOf(",") >= 0 ? true : false;
            if (!hasCommaBefore && hasCommaAfter)
                oldLen++;
            else if (hasCommaBefore && !hasCommaAfter)
                oldLen--;
            if (oldLen < 0)
                oldLen = 0;
            if (e.KeyValue == 106)
                textBoxStockQty.SelectionStart = textBoxStockQty.Text.Length;
            else
                textBoxStockQty.SelectionStart = oldLen;
        }

        protected override void OnStock(Stock TheStock)
        {
            ExchangeTypeEnum exType;

            //Console.WriteLine("OTF OnStock: " + TheStock.StockSignature + " - Alternatives: " + TheStock.StockSignatureAlternative);

            if (TheStock != null &&
                TheStock.Code.TrimStart('0') == StockCodePending &&
                (TheStock.ExchangeType == FormExType || TheStock.ExchangeType == ExchangeTypeEnum.Unassigned && FormExType == ExchangeTypeEnum.Gray))
            {
                if (TheStock.StockSignatureAlternative != null)
                {
                    if (FormExType == ExchangeTypeEnum.Gray && (TheStock.ExchangeType == ExchangeTypeEnum.Unassigned || TheStock.ExchangeType == ExchangeTypeEnum.HKG || TheStock.ExchangeType == ExchangeTypeEnum.FTHKG || TheStock.ExchangeType == ExchangeTypeEnum.PMHKG) ||
                        TheStock.ExchangeType == FormExType)
                    {
                        string[] signatureArr = TheStock.StockSignatureAlternative.Split('|');
                        string altExCode, mainExCode, exCode, stkCode, qtyPersist;

                        for (int i = 0; i < signatureArr.Length; i++)
                        {
                            Stock.GetExchangeCodeStockCodeShort(signatureArr[i], out exCode, out stkCode);
                            exType = Stock.GetExchangeType(exCode);

                            if (exType != ExchangeTypeEnum.Unassigned && exType == FormExType)
                            {
                                qtyPersist = Qty;
                                SetCurrentStockCode(stkCode.TrimStart('0'), true);

                                if (StockCodePending == stkCode)    // preserve the Qty if same stock code and that the exchange type has just become clear
                                {
                                    Qty = qtyPersist;
                                    Redraw();
                                }

                                return;
                            }
                        }

                        for (int i = 0; i < signatureArr.Length; i++)
                        {
                            Stock.GetAltExchangeCodeStockCodeShort(signatureArr[i], out altExCode, out mainExCode, out exCode, out stkCode);
                            exType = Stock.GetExchangeType(mainExCode);

                            if (exType != ExchangeTypeEnum.Unassigned &&
                                (exType == FormExType || FormExType == ExchangeTypeEnum.Gray && (altExCode == "FT" || altExCode == "PM")))
                            {
                                exType = Stock.GetExchangeType(exCode);
                                if (exType != FormExType)
                                {
                                    FormExType = exType;
                                    ExchangeCheckBoxChanged(null, null);
                                }

                                qtyPersist = Qty;
                                SetCurrentStockCode(stkCode.TrimStart('0'), true);

                                if (StockCodePending == stkCode)    // preserve the Qty if same stock code and that the exchange type has just become clear
                                {
                                    Qty = qtyPersist;
                                    Redraw();
                                }

                                return;
                            }

                            /*
                            //if (exType == ExchangeTypeEnum.PMHKG || exType == ExchangeTypeEnum.FTHKG)
                            {
                                if (exType != FormExType)
                                {
                                    FormExType = exType;
                                    ExchangeCheckBoxChanged(null, null);
                                }

                                if (stkCode != null && stkCode.Length > 0)
                                {
                                    qtyPersist = Qty;
                                    SetCurrentStockCode(stkCode.TrimStart('0'), true);

                                    if (StockCodePending == stkCode)    // preserve the Qty if same stock code and that the exchange type has just become clear
                                    {
                                        Qty = qtyPersist;
                                        Redraw();
                                    }
                                }

                                break;
                            }*/
                        }
                    }
                }
                else
                {
                    this.CurrStock = TheStock;
                    ShowStock();
                }
            }
        }


        private void SetPriceRange_ASHR(decimal stkPrice)
        {
            decimal BidRange = 0;
            decimal AskRange = 0;

            if (stkPrice > 0)
            {
                BidRange = Math.Round(stkPrice * 0.9M, 2, MidpointRounding.AwayFromZero);
                AskRange = Math.Round(stkPrice * 1.1M, 2, MidpointRounding.AwayFromZero);
                labelBidRange.Text = String.Format("{0:N2}", BidRange);
                labelAskRange.Text = String.Format("{0:N2}", AskRange);
            }
            else
            {
                labelBidRange.Text = "";
                labelAskRange.Text = "";
            }

            //checkBoxCheckPrice.Checked = true;
            //checkBoxCheckPrice.Enabled = true;
            //////checkBoxAllOrNothing.Enabled = false;
            //////checkBoxAllOrNothing.Checked = false;
            //////checkBoxSpecialLimit.Enabled = false;
            //////checkBoxSpecialLimit.Checked = false;
            
        }

        private void SetPriceRange(decimal stkPrice)
        {            
            decimal BidRange = 0;
            decimal AskRange = 0;
            char OrderTypeNow = ' ';
            if (Utils.OrderTypeSpread.GetSpreadRange(stkPrice, 'X', CurrStock, TradeDB,out OrderTypeNow, out BidRange,out AskRange) > 0)
            {
                labelBidRange.Text = String.Format("{0:N3}", BidRange);
                labelAskRange.Text = String.Format("{0:N3}", AskRange);
            }
            else
            {
                labelBidRange.Text = String.Format("{0:N3}", BidRange);
                labelAskRange.Text = String.Format("{0:N3}", AskRange);
            }

            //allow uncheck Checkorderprice
            if (OrderTypeNow == 'I' || OrderTypeNow == 'N')
            {
                //checkBoxCheckPrice.Enabled = true;
                //////checkBoxAllOrNothing.Enabled = false;
                //////checkBoxAllOrNothing.Checked = false;
                //////checkBoxSpecialLimit.Enabled = false;
                //////checkBoxSpecialLimit.Checked = false;
            }
                else
            {
                //checkBoxCheckPrice.Enabled = false;
                //checkBoxCheckPrice.Checked = true;
                //////checkBoxAllOrNothing.Enabled = true;
                //////checkBoxSpecialLimit.Enabled = true;
            }
        }


        private void ShowStock()
        {
            //if (CurrStock.Nominal == 0 || CurrStock.LotSize == 0 || CurrStock.Code == null)
            if (CurrStock == null) return;
            if (CurrStock.LotSize == -1 || CurrStock.Code == null) return;
            if (CurrStock.Code != StockCode.Trim() && StockCode.Trim() != "") return;

            VisibleIcon(1, textBoxStockCode);

            AppendLog("1", "Stock:" + CurrStock.Code + " Bid:" + CurrStock.Bid + " Ask:" + CurrStock.Ask + " Nominal:" + CurrStock.Nominal + " PrevClose:" + CurrStock.PrevClose + " LotSize:" + CurrStock.LotSize + " SpreadTableCode:" + CurrStock.SpreadTableCode, "ShowStock", false);

            bool CanBuy, CanSell;
            //Check Auction period
            if (FormExType == ExchangeTypeEnum.SHG || FormExType == ExchangeTypeEnum.SZE || 
                Utils.OrderTypeSpread.CheckSpread(0, 'A', 'B', 1, CurrStock, TradeDB, out CanBuy, out CanSell) == -1)
            {
                Auction = false;
                if (checkBoxAuction.Enabled == true) checkBoxAuction.Enabled = false;
                if (checkBoxAuction.Checked == true) checkBoxAuction.Checked = false;
            }
            else
            {
                Auction = true;
                if (checkBoxAuction.Enabled == false) checkBoxAuction.Enabled = true;
            }

            if (SpreadTableCodeFilled == false && CurrStock.SpreadTableCode > 0)
            {
                AppendLog("7", "Stock:" + CurrStock.Code + " Bid:" + CurrStock.Bid + " Ask:" + CurrStock.Ask + " Nominal:" + CurrStock.Nominal + " PrevClose:" + CurrStock.PrevClose + " LotSize:" + CurrStock.LotSize + " SpreadTableCode:" + CurrStock.SpreadTableCode, "ShowStock", false);
                SpreadTableCodeFilled = true;
                if (PriceFilled) ChkOrderValue(' ');
            }

            if (CheckIsBlockAcc(' '))
            {
                if (CurrStock != null)
                    CheckIsDiscretion();
            }
            ShowTradeStatus(CurrStock);

            // Set OrderPrice to Nominal   //only fill price if stockprice textbox empty
            
            if (PriceFilled == false && CurrStock.Nominal > 0 && textBoxStockPrice.Text.Trim() == "" && (Price == null || Price.Trim() == "") && SpreadTableCodeFilled == true && CurrStock.MarketBelong != null)
            {
                AppendLog("2a", "Stock:" + CurrStock.Code + " Bid:" + CurrStock.Bid + " Ask:" + CurrStock.Ask + " Nominal:" + CurrStock.Nominal + " PrevClose:" + CurrStock.PrevClose + " LotSize:" + CurrStock.LotSize + " SpreadTableCode:" + CurrStock.SpreadTableCode + " Price:" + Price, "ShowStock", false);

                textBoxStockPrice.Text = CurrStock.Nominal.ToString();
                textBoxStockPrice.SelectAll();
                PriceFilled = true;
                Price = "";
                SetQty();
/*
                if (FormExType == ExchangeTypeEnum.HKG || FormExType == ExchangeTypeEnum.PMHKG || FormExType == ExchangeTypeEnum.FTHKG)
                    SetPriceRange(CurrStock.Nominal);
                else if (FormExType == ExchangeTypeEnum.SHG || FormExType == ExchangeTypeEnum.SZE)
                    SetPriceRange_ASHR(CurrStock.PrevClose);
*/
                if (LotSizeFilled) ChkOrderValue(' ');
                AppendLog("2b", "Stock:" + CurrStock.Code + " Bid:" + CurrStock.Bid + " Ask:" + CurrStock.Ask + " Nominal:" + CurrStock.Nominal + " PrevClose:" + CurrStock.PrevClose + " LotSize:" + CurrStock.LotSize + " SpreadTableCode:" + CurrStock.SpreadTableCode + " Price:" + Price, "ShowStock", false);
            }

            // Set OrderPrice by User clicked in Stock Quote form
            string strCode = "";
            int intCode = 0;
            int.TryParse(StockCode, out intCode);
            strCode = intCode.ToString();
            if (PriceFilled == false && CurrStock.Code.Trim() != "" && Price != null && Price.Trim() != "" && CurrStock.Code.Trim() == strCode.Trim() && SpreadTableCodeFilled == true && CurrStock.MarketBelong != null)
            {
                AppendLog("3a", "Stock:" + CurrStock.Code + " Bid:" + CurrStock.Bid + " Ask:" + CurrStock.Ask + " Nominal:" + CurrStock.Nominal + " PrevClose:" + CurrStock.PrevClose + " LotSize:" + CurrStock.LotSize + " SpreadTableCode:" + CurrStock.SpreadTableCode + " Price:" + Price + " strcode:" + strCode, "ShowStock", false);
                decimal decPrice = 0;
                decimal.TryParse(Price, out decPrice);
                textBoxStockPrice.Text = String.Format("{0:N3}", decPrice);
                Price = "";
                textBoxStockPrice.SelectAll();
                PriceFilled = true;
                SetQty();
                /*
                                if (FormExType == ExchangeTypeEnum.HKG || FormExType == ExchangeTypeEnum.PMHKG || FormExType == ExchangeTypeEnum.FTHKG)
                                    SetPriceRange(CurrStock.Nominal);
                                else if (FormExType == ExchangeTypeEnum.SHG || FormExType == ExchangeTypeEnum.SZE)
                                    SetPriceRange_ASHR(CurrStock.PrevClose);
                */
                if (LotSizeFilled) ChkOrderValue(' ');
                AppendLog("3b", "Stock:" + CurrStock.Code + " Bid:" + CurrStock.Bid + " Ask:" + CurrStock.Ask + " Nominal:" + CurrStock.Nominal + " PrevClose:" + CurrStock.PrevClose + " LotSize:" + CurrStock.LotSize + " SpreadTableCode:" + CurrStock.SpreadTableCode + " Price:" + Price + " strcode:" + strCode, "ShowStock", false);
            }
            else
                CheckPriceValue();

            if (FormExType == ExchangeTypeEnum.HKG || FormExType == ExchangeTypeEnum.PMHKG || FormExType == ExchangeTypeEnum.FTHKG)
            {
                if (CurrStock.Nominal > 0)
                    SetPriceRange(CurrStock.Nominal);
            }
            else if (FormExType == ExchangeTypeEnum.SHG || FormExType == ExchangeTypeEnum.SZE)
            {
                if (CurrStock.PrevClose > 0)
                    SetPriceRange_ASHR(CurrStock.PrevClose);
            }

            if (LotSizeFilled == false && CurrStock.LotSize != -1)
            {
                AppendLog("4", "Stock:" + CurrStock.Code + " Bid:" + CurrStock.Bid + " Ask:" + CurrStock.Ask + " Nominal:" + CurrStock.Nominal + " PrevClose:" + CurrStock.PrevClose + " LotSize:" + CurrStock.LotSize + " SpreadTableCode:" + CurrStock.SpreadTableCode, "ShowStock", false);
                labelLotSize.Text = CurrStock.LotSize.ToString();

                decimal decLotQty = 0;
                if (FormExType == ExchangeTypeEnum.HKG || FormExType == ExchangeTypeEnum.FTHKG || FormExType == ExchangeTypeEnum.PMHKG) { decLotQty = CheckLotSize(Qty); }
                else if (FormExType == ExchangeTypeEnum.SHG || FormExType == ExchangeTypeEnum.SZE) { decimal.TryParse(Qty, out decLotQty); }

                if (decLotQty > 0) { textBoxStockQty.Text = String.Format("{0:N0}", decLotQty); }

                LotSizeFilled = true;
                if (PriceFilled) ChkOrderValue(' ');
            }

            if (NameFilled == false && CurrStock.GetBestName(this.Culture) != null) //CurrStock.NameENShort != null)
            {
                AppendLog("5", "Stock:" + CurrStock.Code + " Bid:" + CurrStock.Bid + " Ask:" + CurrStock.Ask + " Nominal:" + CurrStock.Nominal + " PrevClose:" + CurrStock.PrevClose + " LotSize:" + CurrStock.LotSize + " SpreadTableCode:" + CurrStock.SpreadTableCode, "ShowStock", false);
                //labelStockName.Text = CurrStock.NameENShort.Trim();
                labelStockName.Text = CurrStock.GetBestName(this.Culture);                
                NameFilled = true;
            }
            if (NameFilled == false)
            {
                AppendLog("6", "Stock:" + CurrStock.Code + " Bid:" + CurrStock.Bid + " Ask:" + CurrStock.Ask + " Nominal:" + CurrStock.Nominal + " PrevClose:" + CurrStock.PrevClose + " LotSize:" + CurrStock.LotSize + " SpreadTableCode:" + CurrStock.SpreadTableCode, "ShowStock", false);
                NameFilled = true;
            }
            /*
            if (PriceFilled == true && LotSizeFilled == true)
            {

            }
             */
        }


        private void ShowTradeStatus(Stock theStock)
        {
            if (theStock != null)
            {
                if (theStock.SuspensionFlag == "Y")
                {
                    labelCurrency.Text = GetResxString("TradeStatusSuspended");
                    labelCurrency.ForeColor = Color.Red;
                    labelCurrency2.Text = GetResxString("TradeStatusSuspended");
                    labelCurrency2.ForeColor = Color.Red;
                }
                else
                {
                    labelCurrency.Text = GetGeneralResxString("Currency_" + theStock.Currency);
                    labelCurrency.ForeColor = (theStock.Currency == null || theStock.Currency == "" || theStock.Currency == "HKD") ?
                        Color.FromKnownColor(KnownColor.ControlText) :
                        Color.Red;

                    labelCurrency2.Text = GetGeneralResxString("Currency_" + theStock.Currency);
                    labelCurrency2.ForeColor = (theStock.Currency == null || theStock.Currency == "" || theStock.Currency == "HKD") ?
                        Color.FromKnownColor(KnownColor.ControlText) :
                        Color.Red;
                }
            }
            else
            {
                labelCurrency.Text = "";
                labelCurrency.ForeColor = Color.FromKnownColor(KnownColor.ControlText);
                labelCurrency2.Text = "";
                labelCurrency2.ForeColor = Color.FromKnownColor(KnownColor.ControlText);
            }
        }


        private decimal CheckLotSize(string tempQty)
        {
            if (CurrStock == null) return -1;
            if (CurrStock.LotSize == 0) return -1;

            decimal decQtyOnHand = 0;
            decimal.TryParse(tempQty, out decQtyOnHand);
            return Math.Truncate(decQtyOnHand / CurrStock.LotSize) * CurrStock.LotSize;
        }

        private void SetQty()
        {
            if (CurrStock == null) return;

            if (textBoxStockQty.Enabled == false) return;

            // sell max
            if (checkBoxSellMax.Checked == true)
            {
                SellMax();
            }
            else if (boolLoadAllStockQty == true)
            {
                boolLoadAllStockQty = false;
                SellMax();
            }
            else
            {
                if (checkBoxBuyMax.Checked == true)
                {
                    BuyMax();
                }
                else
                {
                    if (textBoxStockQty.Text.Trim() == "") BuyMax();
                }
            }
        }


        //////private void SetQty()
        //////{
        //////    if (CurrStock == null) return;

        //////    if (textBoxStockQty.Enabled == false) return;

        //////    // sell max
        //////    if (checkBoxSellMax.Checked == true)
        //////    {
        //////        SellMax();
        //////    }
        //////    else
        //////    {
        //////        if (checkBoxBuyMax.Checked == true)
        //////        {
        //////            BuyMax();
        //////        }
        //////        else
        //////        {
        //////            if (textBoxStockQty.Text.Trim() == "") BuyMax();
        //////        }
        //////    }
        //////}

        private void SetCurrentStockCode(string NewStockCode, bool PassBackToQuoteForm)
        {
            NewStockCode = NewStockCode == null ? "" : NewStockCode.Trim();                        
            try
            {                
                ResetForm(NewStockCode);
                foreach (KeyValuePair<string, StockQuoteForm> kvp in StkQuoteDict)
                {
                    /*
                    if (kvp.Value == null || (FormExType == ExchangeTypeEnum.PMHKG && kvp.Value.FormExType != ExchangeTypeEnum.PMHKG) ||
                        (FormExType != ExchangeTypeEnum.PMHKG && kvp.Value.FormExType == ExchangeTypeEnum.PMHKG))
                        continue;
                     */

                    if (kvp.Value != null)
                    {
                        kvp.Value.SetStock(FormExType, NewStockCode);
                        
                        /*
                        if (FormExType == ExchangeTypeEnum.HKG)
                        {
                            kvp.Value.checkBoxSHG.Checked = false;
                            kvp.Value.checkBoxSZE.Checked = false;
                            kvp.Value.checkBoxGray.Checked = false;
                        }
                        else if (FormExType == ExchangeTypeEnum.SHG)
                            kvp.Value.checkBoxSHG.Checked = true;
                        else if (FormExType == ExchangeTypeEnum.SZE)
                            kvp.Value.checkBoxSZE.Checked = true;
                        else if (FormExType == ExchangeTypeEnum.PMHKG)
                            kvp.Value.checkBoxGray.Checked = true;

                        kvp.Value.textBoxCode.Text = NewStockCode;
                        kvp.Value.ListenedStockCode = NewStockCode;
                        kvp.Value.labelStockCode.Text = NewStockCode;
                        */
                    }
                }
                int code;
                if (int.TryParse(NewStockCode, out code))
                {
                    ListenStock(new List<string> { Stock.GetSignature(FormExType, code.ToString()) });
                    //GetStock(new List<string> { Stock.GetSignature(FormExType, code.ToString()) });
                    StockCodePending = NewStockCode;
                }
            }
            catch
            { }
        }

        private void textBoxStockPrice_TextChanged(object sender, EventArgs e)
        {            
            //SetQty();
            CheckPriceValue();
        }

        private void textBoxStockCode_Leave(object sender, EventArgs e)
        {
            textBoxStockCode.Text = textBoxStockCode.Text.TrimStart('0');

            if (textBoxStockCode.Text.Trim().Length > 0 && 
                textBoxStockCode.Text.Trim() != StockCodePending && 
                (textBoxStockCode.Text.Trim() != StockCode.Trim() || labelStockName.Text.Trim() == ""))
                SetCurrentStockCode(textBoxStockCode.Text.Trim().TrimStart('0'), true);

            if (textBoxStockCode.Text.Trim() != "")
                CheckCodeValue();

            //checkBoxCheckPrice.Enabled = true;  //for IPO listdate before 9am
        }

        private void BuyMax()
        {
            if (CurrStock == null)                
                return;

            if (CurrStock.LotSize <= 0) return;

            decimal stkPrice;
            decimal.TryParse(textBoxStockPrice.Text, out stkPrice);
            if (stkPrice <= 0) return;            

            decimal MaxBuyQty = 0;
            decimal BuyPower = 0;

            if (CurrAccount == null)
            {
                if (ListenedAccount != "123") VisibleIcon(2, AccountNoComboBox1);
            }
            else
            {
                if (CurrAccount.Balances.ContainsKey(CurrStock.Currency))
                    BuyPower = CurrAccount.Balances[CurrStock.Currency].T2DayBal + CurrAccount.Balances[CurrStock.Currency].FundHold;

                //BuyPower = CurrAccount.T2DayBal + CurrAccount.FundHold;
            }

            // No Credit
            if (BuyPower <= 0 && TradeDB.UserType != UserTypeEnum.AE) return;
            
            //Buy Max Checked
            if (checkBoxBuyMax.Checked == true)
            {
                MaxBuyQty = Math.Truncate(BuyPower / (CurrStock.LotSize * stkPrice)) * CurrStock.LotSize;
                MaxBuyQty = MaxBuyQty >= 0 ? MaxBuyQty : 0;
            }
            else // 1 Lot
            {
                if (TradeDB.UserType == UserTypeEnum.AE)                
                    MaxBuyQty = CurrStock.LotSize;
                else
                    MaxBuyQty = (CurrStock.LotSize * stkPrice) > BuyPower ? 0 : CurrStock.LotSize;                
            }

            textBoxStockQty.Text = String.Format("{0:N0}", MaxBuyQty);
            calAmount();
        }

        private bool ChkBalanceQty(char Side, out string Remarks)
        {
            Remarks = null;

            // check Balance           
            if (CurrStock == null) { VisibleIcon(0, textBoxStockQty); return false; }

            if (CurrStock.LotSize <= 0) { VisibleIcon(0, textBoxStockQty); return false; }

            decimal stkPrice;
            decimal.TryParse(textBoxStockPrice.Text, out stkPrice);
            //if (stkPrice <= 0) { VisibleIcon(2, textBoxStockPrice); return false; }
            decimal stkQty;
            decimal.TryParse(textBoxStockQty.Text, out stkQty);
            if (stkQty <= 0) { VisibleIcon(2, textBoxStockQty); return false; }
            int stkCode = 0;
            int.TryParse(textBoxStockCode.Text, out stkCode);
            if (stkCode <= 0) { VisibleIcon(2, textBoxStockCode); return false; }
            if (CurrAccount == null) {
                if (ListenedAccount == "123") { VisibleIcon(0, AccountNoComboBox1); return true; }
                else { VisibleIcon(2, AccountNoComboBox1); return false; }
            }
            //if (CurrAccount == null) { VisibleIcon(2, comboBoxAccountNo); return false; }            

            if (Side == 'B')
            {
                //AE don't check for credit
                if (TradeDB.UserType == UserTypeEnum.AE)
                {
                    //set valid
                    VisibleIcon(1, textBoxStockCode);
                    VisibleIcon(1, textBoxStockPrice);
                    VisibleIcon(1, textBoxStockQty);
                    return true;
                }

                decimal BuyPower = 0; //CurrAccount.T2DayBal + CurrAccount.FundHold + CurrAccount.Interest;
                if (CurrAccount.Balances.ContainsKey(CurrStock.Currency))
                    BuyPower = CurrAccount.Balances[CurrStock.Currency].T2DayBal + CurrAccount.Balances[CurrStock.Currency].FundHold + CurrAccount.Balances[CurrStock.Currency].Interest;

                if (BuyPower <= 0) { VisibleIcon(2, textBoxStockQty); return false; }
                if (checkBoxAuction.Checked == false)
                {
                    if (BuyPower < (stkPrice * stkQty)) { VisibleIcon(2, textBoxStockQty); return false; }
                }
                else
                {
                    if (BuyPower < (CurrStock.Nominal * stkQty)) { VisibleIcon(2, textBoxStockQty); return false; }
                }

                //set valid
                VisibleIcon(1, textBoxStockCode);
                VisibleIcon(1, textBoxStockPrice);
                VisibleIcon(1, textBoxStockQty);

                return true;
            }
            else if (Side == 'A')
            {
                StockRelationBook srBook = TradeDB.StockRelationBook;

                CurrAccount.UpdateOnHoldByRelated(srBook);
                if (CurrAccount.IsQtyEnough(srBook, FormExType, stkCode.ToString(), stkQty))
                {
                    VisibleIcon(1, textBoxStockQty);
                    return true;
                }
                else
                {
                    if (FormExType != ExchangeTypeEnum.SHG && FormExType != ExchangeTypeEnum.SZE && checkBoxStockOnHand.Checked == true)
                    {
                        VisibleIcon(1, textBoxStockQty);
                        return true;
                    }
                    else
                    {
                        VisibleIcon(2, textBoxStockQty);
                        return false;
                    }
                }

                /*
                //check net qty
                decimal qty;
                decimal QtyRemains = 0;
                decimal MaxSellQty = 0;
                StockRelation stockRelation;
                StockRelation.StockRelationStock stockFrom;
                AccountStock acStock;
                string stockSignature;
                StringBuilder sb;
                string sep = null;

                stockSignature = Stock.GetSignature(FormExType, stkCode.ToString());

                if (CurrAccount.Stocks.TryGetValue(stockSignature, out acStock))
                {
                    if (FormExType == ExchangeTypeEnum.HKG || FormExType == ExchangeTypeEnum.PMHKG || FormExType == ExchangeTypeEnum.FTHKG)
                        QtyRemains = acStock.QtyOnHand + acStock.QtyInTransit;
                    else if (FormExType == ExchangeTypeEnum.SHG || FormExType == ExchangeTypeEnum.SZE)
                        QtyRemains = acStock.QtyOnHand + acStock.QtyInTransitSold;
                }

                if (QtyRemains < stkQty &&
                    (FormExType == ExchangeTypeEnum.HKG || FormExType == ExchangeTypeEnum.PMHKG || FormExType == ExchangeTypeEnum.FTHKG))
                {
                    stockRelation = TradeDB.GetStockRelationBook(stockSignature);

                    if (stockRelation != null)
                    {
                        sb = new StringBuilder(1000);

                        for (int i = 0; i < stockRelation.From.Count; i++)
                        {
                            stockFrom = stockRelation.From[i];

                            if (CurrAccount.Stocks.TryGetValue(stockFrom.StockSignature, out acStock))
                            {
                                qty = acStock.QtyOnHand + acStock.QtyInTransit;

                                if (qty > 0 && stockFrom.Weight > 0)
                                {
                                    qty = Math.Truncate(qty / stockFrom.LotSize) * stockFrom.LotSize / stockFrom.Weight;

                                    if (qty > 0)
                                    {
                                        QtyRemains += qty;
                                        sb.Append(sep + stockRelation.Relation);
                                        sep = ", ";
                                    }
                                }
                            }
                        }

                        if (sb.Length > 0)
                            Remarks = sb.ToString();
                    }
                }
                */
                /*
                foreach (KeyValuePair<string, AccountStock> kvp in CurrAccount.Stocks)
                {
                    if (kvp.Value != null && int.TryParse(kvp.Value.Code, out acStkCode))
                    {
                        if (stkCode == acStkCode && kvp.Value.ExchangeType == FormExType)
                        {
                            if (FormExType == ExchangeTypeEnum.HKG || FormExType == ExchangeTypeEnum.PMHKG || FormExType == ExchangeTypeEnum.FTHKG)
                                QtyRemains = kvp.Value.QtyOnHand + kvp.Value.QtyInTransit;
                            else if (FormExType == ExchangeTypeEnum.SHG || FormExType == ExchangeTypeEnum.SZE)
                                QtyRemains = kvp.Value.QtyOnHand + kvp.Value.QtyInTransitSold;
                            break;
                        }
                    }
                }
                 */
                /*
                if (FormExType == ExchangeTypeEnum.HKG || FormExType == ExchangeTypeEnum.PMHKG || FormExType == ExchangeTypeEnum.FTHKG)
                    MaxSellQty = Math.Floor(QtyRemains / CurrStock.LotSize) * CurrStock.LotSize;
                else if (FormExType == ExchangeTypeEnum.SHG)
                {
                    MaxSellQty = QtyRemains;
                    if (SettingsForms["SHGStockonhand"] != null && SettingsForms["SHGStockonhand"] == "1")
                    {
                        VisibleIcon(1, textBoxStockQty);
                        return true;
                    }
                }
                else if (FormExType == ExchangeTypeEnum.SZE)
                {
                    MaxSellQty = QtyRemains;
                    if (SettingsForms["SZEStockonhand"] != null && SettingsForms["SZEStockonhand"] == "1")
                    {
                        VisibleIcon(1, textBoxStockQty);
                        return true;
                    }
                }

                if (MaxSellQty >= stkQty)
                {
                    VisibleIcon(1, textBoxStockQty);
                    return true;
                }
                else
                {
                    if (checkBoxStockOnHand.Checked == true)
                    {
                        VisibleIcon(1, textBoxStockQty);
                        return true;
                    }
                    else
                    {
                        VisibleIcon(2, textBoxStockQty);
                        return false;
                    }
                }
                 */
            }
            else
                return false;

        }




        private void textBoxStockCode_TextChanged_1(object sender, EventArgs e)
        {
            //if (noNeedToResetForm)
            //    return;
            ResetForm(textBoxStockCode.Text.Trim());

            VisibleIcon(0, textBoxStockCode);
        }

        public void setStk_Price_Code(string stkPrice, string stkCode, bool CanBuy, bool CanSell, bool setFocus, ExchangeTypeEnum exType)
        {
            if (CurrStock != null && stkCode == CurrStock.Code && FormExType == exType)
            {
                double dStkPrice;
                if (double.TryParse(stkPrice, out dStkPrice) && dStkPrice > 0)
                    textBoxStockPrice.Text = String.Format("{0:0.000}", Convert.ToDecimal(stkPrice.Replace(",", "")));
                else
                    textBoxStockPrice.Text = "";
                stkPrice = "";
            }
            else
            {
                FormExType = exType;
                ExchangeCheckBoxChanged(null, null);

                /*
                switch (exType)
                {
                    case ExchangeTypeEnum.SZE:
                        if (!checkBoxSZE.Checked)
                            checkBoxSZE.Checked = true;
                        if (checkBoxSHG.Checked)
                            checkBoxSHG.Checked = false;
                        if (checkBoxGray.Checked)
                            checkBoxGray.Checked = false;
                        break;
                    case ExchangeTypeEnum.SHG:
                        if (!checkBoxSHG.Checked)
                            checkBoxSHG.Checked = true;
                        if (checkBoxSZE.Checked)
                            checkBoxSZE.Checked = false;
                        if (checkBoxGray.Checked)
                            checkBoxGray.Checked = false;
                        break;
                    case ExchangeTypeEnum.HKG:
                        if (checkBoxSHG.Checked)
                            checkBoxSHG.Checked = false;
                        if (checkBoxSZE.Checked)
                            checkBoxSZE.Checked = false;
                        if (checkBoxGray.Checked)
                            checkBoxGray.Checked = false;
                        break;
                    case ExchangeTypeEnum.PMHKG:
                        if (!checkBoxGray.Checked)
                            checkBoxGray.Checked = true;
                        if (checkBoxSHG.Checked)
                            checkBoxSHG.Checked = false;
                        if (checkBoxSZE.Checked)
                            checkBoxSZE.Checked = false;
                        break;
                }
                 */
                SetCurrentStockCode(stkCode, false);
                double dStkPrice;
                if (double.TryParse(stkPrice, out dStkPrice) && dStkPrice > 0)
                    Price = stkPrice ?? "";
                else
                    Price = "";
            }

            if (setFocus)
            {
                if (TradeDB.UserType == UserTypeEnum.Client)
                {
                    if (textBoxStockPrice.Enabled) textBoxStockPrice.Focus();
                }
                else
                {
                    if (AccountNoComboBox1.Enabled) AccountNoComboBox1.Focus();
                }
            }            
            //noNeedToResetForm = true;
            //decimal decPrice = 0;
            //decimal.TryParse(stkPrice, out decPrice);
            //textBoxStockPrice.Text = String.Format("{0:N3}", decPrice);
            //textBoxStockCode.Text = stkCode;
            //EnableDisableBuySellBtn(CanBuy, CanSell);
            //textBoxStockQty.Focus();
            //noNeedToResetForm = false;
        }

        protected override void OnCultureChange(CultureInfo ci)
        {
            if (TradeDB.UserType == UserTypeEnum.AE)
            {
                panelAE.Top = myGroupBoxOrder.Top + myGroupBoxOrder.Height;
                panelAE.Visible = true;
                panelSpecialType.Top = panelAE.Top + panelAE.Height;
                panelPriceArrange.Visible = true;
                if (SettingsForms["AggOrderBtn"] != null && SettingsForms["AggOrderBtn"] == "1")
                    checkBoxAggOrder.Visible = true;
                else
                    checkBoxAggOrder.Visible = false;
                checkBoxAggOrder.Top = panelSpecialType.Top + panelSpecialType.Height;
            }
            else
            {
                panelPriceArrange.Visible = false;
                myGroupBoxOrder.Top = panelPriceArrange.Top;
                panelAE.Visible = false;
                checkBoxAggOrder.Visible = false;
            }
            SetVisibleSpecialType(booVisibleSpecialType);
            ShowTradeStatus(CurrStock);

            ExchangeCheckBoxChanged(null, null);
        }

        private void buttonSellMax_Click(object sender, EventArgs e)
        {
            SellMax();
        }

        private void SellMax()
        {
            if (CurrAccount == null) return;
            if (CurrStock== null || CurrStock.LotSize <= 0) return;

            decimal QtyRemains = 0;
            decimal MaxSellQty = 0;
            int stkCode = 0, acStkCode;
            int.TryParse(textBoxStockCode.Text, out stkCode);

            if (stkCode == 0) return;

            foreach (KeyValuePair<string, AccountStock> kvp in CurrAccount.Stocks)
            {
                if (kvp.Value != null && int.TryParse(kvp.Value.Code, out acStkCode))
                {
                    if (stkCode == acStkCode && kvp.Value.ExchangeType == FormExType)
                    {
                        if (FormExType == ExchangeTypeEnum.HKG || FormExType == ExchangeTypeEnum.PMHKG || FormExType == ExchangeTypeEnum.FTHKG)
                            QtyRemains = kvp.Value.QtyOnHand + kvp.Value.QtyInTransit;
                        else if (FormExType == ExchangeTypeEnum.SHG || FormExType == ExchangeTypeEnum.SZE)
                            QtyRemains = kvp.Value.QtyOnHand + kvp.Value.QtyInTransitSold;
                        break;
                    }
                }
            }

            if (QtyRemains >= 0)
            {
                if (FormExType == ExchangeTypeEnum.HKG || FormExType == ExchangeTypeEnum.PMHKG || FormExType == ExchangeTypeEnum.FTHKG)
                    MaxSellQty = Math.Truncate(((decimal)QtyRemains / CurrStock.LotSize)) * CurrStock.LotSize;
                else if (FormExType == ExchangeTypeEnum.SHG || FormExType == ExchangeTypeEnum.SZE)
                    MaxSellQty = QtyRemains;
            }
            textBoxStockQty.Text = String.Format("{0:N0}", MaxSellQty);
            calAmount();
        }

        private void CheckIsDiscretion()
        {
            if (CurrAccount == null)
                return;

            if (CurrAccount.DiscretionCheck(CurrStock) == 1)
            {
                string message = GetResxString("DiscretionConfirm");
                if (CurrAccount.AccountName != null)
                    labelAccountName.Text = message + " " + CurrAccount.AccountName.Trim();
                else
                    labelAccountName.Text = message;

                labelAccountName.ForeColor = Color.LightCoral;
            }
            else
            {
                labelAccountName.ForeColor = Color.Black;
                labelAccountName.Text = CurrAccount.AccountName;
            }
        }

        private bool CheckIsBlockAcc(char side)
        {
            if (CurrAccount == null)
                return false;

            string message = "";

            if (CurrAccount.IsNoBuy) message = "No Buy:";
            if (CurrAccount.IsNoSell) message += "No Sell:";
            if (message != "")
            {
                if (CurrAccount.AccountName != null)
                    labelAccountName.Text = message + " " + CurrAccount.AccountName.Trim();
                else
                    labelAccountName.Text = message;

                labelAccountName.ForeColor = Color.LightCoral;
            }

            if (side == 'B')
            {
                if (CurrAccount.IsNoBuy) { VisibleIcon(2, AccountNoComboBox1); return false; }
                else VisibleIcon(1, AccountNoComboBox1);
            }
            else if (side == 'A')
            {
                if (CurrAccount.IsNoSell) { VisibleIcon(2, AccountNoComboBox1); return false; }
                else VisibleIcon(1, AccountNoComboBox1);
            }
            else
            {
                if (CurrAccount.IsNoBuy) {return false; }
                if (CurrAccount.IsNoSell) { return false; }
            }            

            return true;
        }

        private bool ChkOrderValue(char side)
        {
            bool Valid = true;
            // check acc
            if (CurrAccount == null && AccountNoComboBox1.Text.Trim() != "123") { VisibleIcon(2, AccountNoComboBox1); return false; }
            //if (CurrAccount != null && CurrAccount.AccountNo.Trim() == "") { VisibleIcon(2, AccountNoComboBox1); return false; }
            //if (CurrAccount.IsNoBuy) { VisibleIcon(2, AccountNoComboBox1); return false; }
            //if (CurrAccount == null) { VisibleIcon(2, comboBoxAccountNo); return false; }
            //if (CurrAccount.AccountNo.Trim() == "") { VisibleIcon(2, comboBoxAccountNo); return false; }           

            if (AccountNoComboBox1.Text.Trim() != "123")
            {
                if (!CheckIsBlockAcc(side))
                    Valid = false;
                else
                    if (CurrStock != null)
                        CheckIsDiscretion();
            }
            if (!CheckCodeValue()) Valid = false;
            if (!CheckQtyValue(side)) Valid = false;
            if (checkBoxCheckPrice.Checked)
            {
                if (!CheckPriceValue())
                    Valid = false;  //dont check order price
            }                

            if (Valid == false) return false;

            //set valid
            VisibleIcon(1, textBoxStockCode);
            VisibleIcon(1, textBoxStockPrice);
            VisibleIcon(1, textBoxStockQty);

            return true;
        }

        private bool CheckCodeValue()
        {
            int stkCode;
            //code
            int.TryParse(textBoxStockCode.Text, out stkCode);
            if (stkCode <= 0) { VisibleIcon(2, textBoxStockCode); return false; }
             
            // check stock
            if (CurrStock == null) return false;                        
            if (CurrStock.LotSize <= 0) return false;

            if (FormExType == ExchangeTypeEnum.HKG || FormExType == ExchangeTypeEnum.PMHKG || FormExType == ExchangeTypeEnum.FTHKG)
            {
                if (stkCode.ToString() != CurrStock.Code) { VisibleIcon(2, textBoxStockCode); return false; }
            }
            else if (FormExType == ExchangeTypeEnum.SHG || FormExType == ExchangeTypeEnum.SZE)
            {
                if (stkCode.ToString() != CurrStock.Code) { VisibleIcon(2, textBoxStockCode); return false; }

                /* ChiNext to be checked by Stock Server
                if (stkCode.ToString() != CurrStock.Code ||
                    (FormExType == ExchangeTypeEnum.SZE && stkCode >= 300000 && stkCode <= 399999))
                {
                    // This login can't trade Chinext
                    if (SettingsForms["AllowSZEChiNext"] == null || SettingsForms["AllowSZEChiNext"] != "1") { VisibleIcon(2, textBoxStockCode); return false; }

                    if (CurrAccount != null && CurrAccount.AllowCscSzeChiNext != null && CurrAccount.AllowCscSzeChiNext.Trim().ToUpper() == "Y")
                    { }
                    else
                    {
                        VisibleIcon(2, textBoxStockCode);
                        return false;
                    }
                }
                 */
            }

            return true;
        }

        private bool CheckQtyValue(char side)
        {
            int stkQty;
            //qty
            int.TryParse(textBoxStockQty.Text.Replace(",", ""), out stkQty);
            if (stkQty <= 0)
            {
                if (CurrStock == null)
                { VisibleIcon(0, textBoxStockQty); return false; }
                else
                { VisibleIcon(2, textBoxStockQty); return false; }
            }

            // check stock
            if (CurrStock == null) return false;
            if (CurrStock.LotSize <= 0) return false;
            if (FormExType == ExchangeTypeEnum.HKG || FormExType == ExchangeTypeEnum.PMHKG || FormExType == ExchangeTypeEnum.FTHKG)
            {
                if ((stkQty % CurrStock.LotSize) > 0)
                {
                    VisibleIcon(2, textBoxStockQty);
                    return false;
                }
                else
                    VisibleIcon(1, textBoxStockQty);
            }
            else if (FormExType == ExchangeTypeEnum.SHG || FormExType == ExchangeTypeEnum.SZE)
            {
                if (side == 'B')
                {
                    if ((stkQty % CurrStock.LotSize) > 0 || stkQty > 1000000)
                    {
                        VisibleIcon(2, textBoxStockQty);
                        return false;
                    }
                    else
                        VisibleIcon(1, textBoxStockQty);
                }
            }

            return true;
        }

        private bool CheckPriceValue()
        {
            bool isValid = false;
            bool canBuy = false, canSell = false;
            int iconStatus = 0;

            decimal stkPrice;

            if (decimal.TryParse(textBoxStockPrice.Text, out stkPrice))
            {
                if (stkPrice <= 0 && checkBoxAuction.Checked == false)  // Non auction with zero or negative price
                {
                    iconStatus = 2;
                }
                else
                {
                    // check stock
                    if (CurrStock != null && CurrStock.LotSize > 0)
                    {
                        if (checkBoxAuction.Checked == true)    // Auction
                        {
                            Utils.OrderTypeSpread.CheckSpread(stkPrice, 'A', ' ', 0, CurrStock, TradeDB, out canBuy, out canSell);
                            isValid = true;
                        }
                        else  // Non-Auction
                        {
                            if (FormExType == ExchangeTypeEnum.HKG || FormExType == ExchangeTypeEnum.PMHKG || FormExType == ExchangeTypeEnum.FTHKG)
                                stkPrice = Utils.OrderTypeSpread.CheckSpread(stkPrice, 'X', ' ', 0, CurrStock, TradeDB, out canBuy, out canSell);
                            else if (FormExType == ExchangeTypeEnum.SHG || FormExType == ExchangeTypeEnum.SZE)
                                stkPrice = Utils.OrderTypeSpread.CheckSpread_ASHR(stkPrice, 'X', ' ', 0, CurrStock, TradeDB, out canBuy, out canSell);

                            if (canBuy || canSell)
                            {
                                iconStatus = 1;
                                isValid = true;
                            }
                            else
                                iconStatus = 2;     // Price out of range
                        }
                    }
                }
            }
            else    // Parse price string failed
            {
                if (textBoxStockPrice.Text.Trim() == "")
                    canBuy = canSell = true;    // Show normal buy sell when empty
                else
                    iconStatus = 2;             // Invalid and not empty
            }

            VisibleIcon(iconStatus, textBoxStockPrice);
            EnableDisableBuySellBtn(canBuy, canSell);
            return isValid;
        }


        /// <summary>
        /// Set status of tick/cross icon
        /// </summary>
        /// <param name="isValid">0 = hidden, 1 = tick, 2 = cross</param>
        /// <param name="t">The control that the tick/cross icon belongs to</param>
        private void VisibleIcon(int isValid, Control t)
        {
            if (isValid == 1)
            {
                if (t.Name.ToUpper().Trim() == "TEXTBOXSTOCKPRICE")
                {
                    if (pictureBoxStockPriceTick.Visible == false) { pictureBoxStockPriceTick.Visible = true; }
                    if (pictureBoxStockPriceCross.Visible == true) { pictureBoxStockPriceCross.Visible = false; }
                }
                else if (t.Name.ToUpper().Trim() == "TEXTBOXSTOCKQTY")
                {
                    if (pictureBoxStockQtyTick.Visible == false) { pictureBoxStockQtyTick.Visible = true; }
                    if (pictureBoxStockQtyCross.Visible == true) { pictureBoxStockQtyCross.Visible = false; }
                }
                else if (t.Name.ToUpper().Trim() == "TEXTBOXSTOCKCODE")
                {
                    if (pictureBoxStockCodeTick.Visible == false) { pictureBoxStockCodeTick.Visible = true; }
                    if (pictureBoxStockCodeCross.Visible == true) { pictureBoxStockCodeCross.Visible = false; }
                }
                else if (t.Name.ToUpper().Trim() == "ACCOUNTNOCOMBOBOX1")
                //else if (t.Name.ToUpper().Trim() == "COMBOBOXACCOUNTNO")
                {
                    if (pictureBoxAccountTick.Visible == false) { pictureBoxAccountTick.Visible = true; }
                    if (pictureBoxAccountCross.Visible == true) { pictureBoxAccountCross.Visible = false; }
                }
            }
            else if (isValid == 2)
            {
                if (t.Name.ToUpper().Trim() == "TEXTBOXSTOCKPRICE")
                {
                    pictureBoxStockPriceTick.Visible = false;
                    pictureBoxStockPriceCross.Visible = true;
                }
                else if (t.Name.ToUpper().Trim() == "TEXTBOXSTOCKQTY")
                {
                    pictureBoxStockQtyTick.Visible = false;
                    pictureBoxStockQtyCross.Visible = true;
                }
                else if (t.Name.ToUpper().Trim() == "TEXTBOXSTOCKCODE")
                {
                    pictureBoxStockCodeTick.Visible = false;
                    pictureBoxStockCodeCross.Visible = true;
                }
                else if (t.Name.ToUpper().Trim() == "ACCOUNTNOCOMBOBOX1")
                //else if (t.Name.ToUpper().Trim() == "COMBOBOXACCOUNTNO")
                {
                    pictureBoxAccountTick.Visible = false;
                    pictureBoxAccountCross.Visible = true;
                }
            }
            else
            {
                if (t.Name.ToUpper().Trim() == "TEXTBOXSTOCKPRICE")
                {
                    pictureBoxStockPriceTick.Visible = false;
                    pictureBoxStockPriceCross.Visible = false;
                }
                else if (t.Name.ToUpper().Trim() == "TEXTBOXSTOCKQTY")
                {
                    pictureBoxStockQtyTick.Visible = false;
                    pictureBoxStockQtyCross.Visible = false;
                }
                else if (t.Name.ToUpper().Trim() == "TEXTBOXSTOCKCODE")
                {
                    pictureBoxStockCodeTick.Visible = false;
                    pictureBoxStockCodeCross.Visible = false;
                }
                else if (t.Name.ToUpper().Trim() == "ACCOUNTNOCOMBOBOX1")
                //else if (t.Name.ToUpper().Trim() == "COMBOBOXACCOUNTNO")
                {
                    pictureBoxAccountTick.Visible = false;
                    pictureBoxAccountCross.Visible = false;
                }
            }
        }

        private void textBoxStockQty_TextChanged(object sender, EventArgs e)
        {
            //VisibleIcon(0, textBoxStockQty);            
            CheckQtyValue(' ');
        }

        private void checkBoxAutoBuy_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBoxAutoSell_CheckedChanged(object sender, EventArgs e)
        {

        }


        private void checkBoxBuyMax_Click(object sender, EventArgs e)
        {
            checkBoxSellMax.Checked = false;

            SetQty();                        
        }

        private void checkBoxSellMax_Click(object sender, EventArgs e)
        {
            checkBoxBuyMax.Checked = false;

            SetQty();
        }


        private void textBoxBuyLimit_KeyPress(object sender, KeyPressEventArgs e)
        {
            //if (e.KeyChar == (char)Keys.Enter)
            //{
            //    e.Handled = true;
            //    decimal limit;
            //    decimal.TryParse(textBoxBuyLimit.Text.Replace(",", ""), out limit);
            //    BuyLimit = limit;
            //    textBoxBuyLimit.Text = "";
            //    return;
            //}
        }

        private void buttonLayout_Click(object sender, EventArgs e)
        {
            FastOrderTicketForm orderTicketForm = new FastOrderTicketForm(this.Culture, null, this.dockPanelMain);
            if (this.Pane.FloatWindow != null)
                orderTicketForm.Show(dockPanelMain, DockState.Float, new Rectangle(this.Pane.FloatWindow.Bounds.X, this.Pane.FloatWindow.Bounds.Y, orderTicketForm.Width, orderTicketForm.Height));
            else
                orderTicketForm.Show(dockPanelMain, DockState.Float, new Rectangle(200, 200, orderTicketForm.Width, orderTicketForm.Height));
            this.IsProgramClosing = true;
            this.Close();
            this.Dispose();
        }

        private void OrderTicketForm_Activated(object sender, EventArgs e)
        {
            if (ListenedAccount != "" && ListenedAccount != null) SetAccountForm();
        }

        private void textBoxStockCode_KeyDown(object sender, KeyEventArgs e)
        {
            nonNumberEntered = false;

            if (e.KeyCode < Keys.D0 || e.KeyCode > Keys.D9)
            {
                if (e.KeyCode < Keys.NumPad0 || e.KeyCode > Keys.NumPad9)
                {
                    if (e.KeyCode != Keys.Back)
                    {
                        nonNumberEntered = true;
                    }
                    if (e.KeyCode == Keys.Up)
                    {
                        if (AccountNoComboBox1.Enabled == true)
                        {
                            e.Handled = true;
                            //focusPrevControl(sender, e);
                            AccountNoComboBox1.Focus();
                        }
                    }
                    if (e.KeyCode == Keys.Down) { e.Handled = true; focusNextControl(sender, e); }
                }
            }
            if (Control.ModifierKeys == Keys.Shift)
            {
                nonNumberEntered = true;
            }

            //if (e.KeyCode == Keys.Up) 
            //{
            //    if (AccountNoComboBox1.Enabled == true)
            //    {
            //        e.Handled = true;
            //        //comboBoxAccountNo.Focus();
            //        AccountNoComboBox1.Focus();
            //    }
            //}
            //if (e.KeyCode == Keys.Down)
            //{ 
            //    e.Handled = true; 
            //    focusNextControl(sender, e); 
            //}
        }


        private void buttonSell_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                if (buttonLayout.Enabled == true)
                    buttonLayout.Focus();
            }
        }

        private void buttonBuy_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                if (buttonSell.Enabled == false && buttonLayout.Enabled)
                    buttonLayout.Focus();
            }
        }

        private void textBoxStockQty_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                textBoxStockQty.Text = "";                
            }
        }

        private void textBoxStockCode_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                textBoxStockCode.Text = "";
            }
        }

        #region Account No. Combo

        protected override void OnAccount(Account TheAccount)
        {
            if (TheAccount != null && TheAccount.AccountNo == ListenedAccount)
            {
                CurrAccount = TheAccount;

                labelAccountName.Text = CurrAccount.AccountName;
                if (CheckIsBlockAcc(' '))
                {
                    if (CurrStock != null)
                        CheckIsDiscretion();
                }
                VisibleIcon(1, AccountNoComboBox1);                        
            }
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

            AccountNoComboBox1.SuspendLayout();
            AccountNoComboBox1.Items.AddRange(tempAccountList.ToArray());
            //AccountNoComboBox1.AddSuggest(tempAccountList);

            if (!AccountNoComboBox1.Enabled)
            {
                if (AccountNoComboBox1.Items.Count == 1)
                {
                    AccountNoComboBox1.SelectedIndex = 0;
                    ListenedAccount = AccountNoComboBox1.SelectedItem.ToString();
                    SetAccountForm();
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

        private void SetAccountForm()
        {
            List<BaseForm> AccountBookFormList = BaseForm.GetFormByFormType(typeof(AccountForm));
            if (AutoFillAccNo && AllowChangeAccountFormAccount && AccountBookFormList != null && AccountBookFormList.Count > 0 && AccountBookFormList[0] != null && TradeDB.UserType == UserTypeEnum.AE)
            {
                AccountForm accountBookForm = ((AccountForm)AccountBookFormList[0]);
                accountBookForm.AllowChangeOrderTicketAccount = false;
                accountBookForm.ListenedAccount = ListenedAccount;
                if (textBoxStockCode.Text.Trim() != "")
                    accountBookForm.ShowStock(Stock.GetSignature(FormExType, textBoxStockCode.Text));
            }
            List<BaseForm> OrderBookFormList = BaseForm.GetFormByFormType(typeof(OrderBookForm));
            if (AllowChangeAccountFormAccount && OrderBookFormList != null && OrderBookFormList.Count > 0 && OrderBookFormList[0] != null &&
                ((OrderBookForm)OrderBookFormList[0]).SearchEnabled && TradeDB.UserType == UserTypeEnum.AE)
            {
                ((OrderBookForm)OrderBookFormList[0]).SetSearchAccount(ListenedAccount);
            }
        }

        //protected override void OnAccount(List<Account> AccountList)
        //{
        //    if (AccountList != null && AccountList.Count >= 1)
        //    {
        //        string theAccountNo = ListenedAccount;

        //        foreach (Account account in AccountList)
        //        {
        //            if (account != null && account.AccountNo == theAccountNo)
        //            {
        //                CurrAccount = account;

        //                labelAccountName.Text = CurrAccount.AccountName;
        //                VisibleIcon(1, AccountNoComboBox1);
        //                //VisibleIcon(1, comboBoxAccountNo);
        //                break;
        //            }
        //        }
        //    }
        //}

        //protected override void OnAccountList(List<string> AccountList)
        //{
        //    if (AccountList == null || AccountList.Count <= 0) return;

        //    lock (AccountToBeFilledListMutex)
        //    {
        //        List<string> subAccountList = null;

        //        if (AccountToBeFilledList.Count > 0)
        //        {
        //            subAccountList = AccountToBeFilledList[AccountToBeFilledList.Count - 1];
        //        }
        //        else
        //        {
        //            subAccountList = new List<string>(AccountToBeFilledMaxCountPerInterval);
        //            AccountToBeFilledList.Add(subAccountList);
        //        }

        //        int i = 0;
        //        int n = AccountList.Count;
        //        int m;
        //        int j;

        //        while (i < n)
        //        {
        //            if (subAccountList.Count >= AccountToBeFilledMaxCountPerInterval)
        //                AccountToBeFilledList.Add(subAccountList = new List<string>(AccountToBeFilledMaxCountPerInterval));

        //            m = i + AccountToBeFilledMaxCountPerInterval;
        //            if (m > n) m = n;

        //            for (j = subAccountList.Count; i < m; i++, j++)
        //            {
        //                subAccountList.Add(AccountList[i]);
        //            }
        //        }
        //    }

        //    if (timerFillCombo.Interval != 20) timerFillCombo.Interval = 20;
        //    if (timerFillCombo.Enabled != true) timerFillCombo.Enabled = true;

        //    ////AccountList1 = AccountList;
        //    ////if (AccountList1 != null && AccountList1.Count > 0 && AccountList1[0].Trim() != "" && CurrAccount == null)
        //    ////{
        //    ////    ListenAccount(new List<string> { AccountList1[0].ToUpper().Trim() });
        //    ////    //Accountno = AccountList1[0].ToUpper().Trim();
        //    ////    //OnAccount(TradeDB.GetAccountByAccountNo(AccountList1[0].ToUpper().Trim()));
        //    ////}                                    
        //    //int comboCount = comboBoxAccountNo.Items.Count;
        //    //AppendAccountCombo(AccountList, comboBoxAccountNo.Text);
        //    //if (comboCount <= 0 && comboBoxAccountNo.Items.Count == 1 &&
        //    //    comboBoxAccountNo.Text.Trim() == "")
        //    //{
        //    //    comboBoxAccountNo.SelectedIndex = 0;
        //    //    ListenedAccount = comboBoxAccountNo.SelectedItem.ToString();
        //    //}

        //}

        //private void timerFillCombo_Tick(object sender, EventArgs e)
        //{
        //    timerFillCombo.Enabled = false;

        //    List<string> subAccountList = new List<string>(400);
        //    int listCount = 0;

        //    lock (AccountToBeFilledListMutex)
        //    {
        //        listCount = AccountToBeFilledList.Count;

        //        if (listCount > 0)
        //        {
        //            do
        //            {
        //                subAccountList.AddRange(AccountToBeFilledList[0]);
        //                AccountToBeFilledList.RemoveAt(0);
        //                listCount--;
        //            } while (subAccountList.Count < 200 && listCount > 0);
        //        }
        //    }

        //    if (subAccountList.Count > 0)
        //    {
        //        AppendAccountCombo(subAccountList, comboBoxAccountNo.Text);
        //        AccountToBeFilledEmptyCount = 0;
        //    }

        //    if (listCount > 0)
        //    {
        //        timerFillCombo.Interval = 20;
        //        timerFillCombo.Enabled = true;
        //    }
        //    else if (AccountToBeFilledEmptyCount < 5)
        //    {
        //        if (!comboBoxAccountNo.Sorted)
        //        {
        //            comboBoxAccountNo.Sorted = true;
        //            comboBoxAccountNo.AutoCompleteSource = AutoCompleteSource.ListItems;
        //            comboBoxAccountNo.AutoCompleteMode = AutoCompleteMode.Suggest;
        //        }
        //        AccountToBeFilledEmptyCount++;
        //        timerFillCombo.Interval = 300;
        //        timerFillCombo.Enabled = true;
        //    }
        //    else if (!comboBoxAccountNo.Enabled)
        //    {
        //        if (comboBoxAccountNo.Items.Count == 1)
        //        {
        //            comboBoxAccountNo.SelectedIndex = 0;
        //            ListenedAccount = comboBoxAccountNo.SelectedItem.ToString();
        //        }
        //        else if (comboBoxAccountNo.SelectedIndex <= 0)
        //        {
        //            comboBoxAccountNo.Text = "";
        //        }

        //        comboBoxAccountNo.Enabled = true;
        //        comboBoxAccountNo.ResumeLayout();
        //    }
        //}

        //private void AppendAccountCombo(List<string> AccountNoList, string SelectedAccountNo)
        //{
        //    if (AccountNoList != null && AccountNoList.Count > 0)
        //    {
        //        List<string> newList = new List<string>(AccountNoList.Count);

        //        foreach (string accountNo in AccountNoList)
        //        {
        //            if (!comboBoxAccountNo.Items.Contains(accountNo)) newList.Add(accountNo);
        //        }

        //        if (newList.Count > 0) comboBoxAccountNo.Items.AddRange(newList.ToArray());

        //        comboBoxAccountNo.SelectedItem = SelectedAccountNo;

        //        if (comboBoxAccountNo.Items.Count < 2)
        //        {
        //            comboBoxAccountNo.DropDownStyle = ComboBoxStyle.DropDownList;
        //        }
        //        else
        //        {
        //            comboBoxAccountNo.DropDownStyle = ComboBoxStyle.DropDown;
        //        }
        //    }
        //}

        private void comboBoxAccountNo_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (comboBoxAccountNo.SelectedItem == null)
                ListenedAccount = null;
            else
            {
                this.AllowChangeAccountFormAccount = true;
                ListenedAccount = comboBoxAccountNo.SelectedItem.ToString();
            }
            SetAccountForm();
        }

        private void comboBoxAccountNo_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void comboBoxAccountNo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string accountNo = comboBoxAccountNo.Text.ToUpper().Trim();
                if (comboBoxAccountNo.Items.Contains(accountNo) && ListenedAccount != accountNo)
                {
                    AllowChangeAccountFormAccount = true;
                    e.Handled = true; 
                    ListenedAccount = accountNo;
                    SetAccountForm();
                }
                if (textBoxStockCode.Enabled)
                {
                    e.Handled = true; textBoxStockCode.Focus();
                }
                else if (textBoxStockPrice.Enabled)
                {
                    e.Handled = true; textBoxStockPrice.Focus();
                }

            }
            else if (e.KeyCode == Keys.Delete)
            {
                e.Handled = true; ListenedAccount = null;
                SetAccountForm();
            }
            else if (e.KeyCode == Keys.Up)
            {
                if (buttonSell.Enabled)
                { e.Handled = true; buttonSell.Focus(); }
                else if (buttonBuy.Enabled)
                { e.Handled = true; buttonBuy.Focus(); }
            }
            else if (e.KeyCode == Keys.Down)
            {
                if (textBoxStockCode.Enabled)
                    e.Handled = true; textBoxStockCode.Focus();
            }
        }

        private void comboBoxAccountNo_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar >= 97 && e.KeyChar <= 122)
            {
                e.KeyChar = (char)(e.KeyChar & 223);
                ListenedAccount = null;
                SetAccountForm();
            }
            else if (e.KeyChar != 8 && (e.KeyChar < 48 || (e.KeyChar > 57 && e.KeyChar < 65) || e.KeyChar > 90))
            {
                e.Handled = true;
            }
            else
            {
                ListenedAccount = null;
                SetAccountForm();
            }
        }

        private void comboBoxAccountNo_Leave(object sender, EventArgs e)
        {
            string accountNo = comboBoxAccountNo.Text.ToUpper().Trim();
            if (comboBoxAccountNo.Items.Contains(accountNo) && ListenedAccount != accountNo)
            {
                ListenedAccount = accountNo;
                SetAccountForm();
            }
        }

        private void comboBoxAccountNo_TextChanged(object sender, EventArgs e)
        {
            VisibleIcon(0, comboBoxAccountNo);
        }

        private void AccountNoComboBox1_TextChanged(object sender, EventArgs e)
        {
            VisibleIcon(0, AccountNoComboBox1);
        }

        private void AccountNoComboBox1_Leave(object sender, EventArgs e)
        {
            string accountNo = AccountNoComboBox1.Text.ToUpper().Trim();
            //if (AccountNoComboBox1.Items.Contains(accountNo) && ListenedAccount != accountNo)
            if ((AccountNoComboBox1.Items.Contains(accountNo) && ListenedAccount != accountNo) || accountNo == "123")
            {
                ListenedAccount = accountNo;
                SetAccountForm();
            }
        }

        private void AccountNoComboBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar >= 97 && e.KeyChar <= 122)
            {
                e.KeyChar = (char)(e.KeyChar & 223);
                ListenedAccount = null;
                SetAccountForm();
            }
            else if (e.KeyChar != 8 && (e.KeyChar < 48 || (e.KeyChar > 57 && e.KeyChar < 65) || e.KeyChar > 90))
            {
                e.Handled = true;
            }
            else
            {
                ListenedAccount = null;
                SetAccountForm();
            }
        }

        private void AccountNoComboBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string accountNo = AccountNoComboBox1.Text.ToUpper().Trim();
                if ((AccountNoComboBox1.Items.Contains(accountNo) && ListenedAccount != accountNo) || accountNo == "123")
                {
                    this.AllowChangeAccountFormAccount = true;
                    e.Handled = true; 
                    ListenedAccount = accountNo;
                    SetAccountForm();
                }
                if (textBoxStockCode.Enabled)
                {
                    e.Handled = true; textBoxStockCode.Focus();
                }
                else if (textBoxStockPrice.Enabled)
                {
                    e.Handled = true; textBoxStockPrice.Focus();
                }

            }
            else if (e.KeyCode == Keys.Delete)
            {
                e.Handled = true; ListenedAccount = null;
                SetAccountForm();
            }
            else if (e.KeyCode == Keys.Up && !AccountNoComboBox1.ShowSuggestList)
            {
                if (buttonSell.Enabled)
                { e.Handled = true; buttonSell.Focus(); }
                else if (buttonBuy.Enabled)
                { e.Handled = true; buttonBuy.Focus(); }
            }
            else if (e.KeyCode == Keys.Down)
            {
                if (textBoxStockCode.Enabled && !AccountNoComboBox1.ShowSuggestList)
                {
                    e.Handled = true; 
                    textBoxStockCode.Focus();
                }
            }
        }

        private void AccountNoComboBox1_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (AccountNoComboBox1.SelectedItem == null)
                ListenedAccount = null;
            else
            {
                this.AllowChangeAccountFormAccount = true;
                ListenedAccount = AccountNoComboBox1.SelectedItem.ToString();
            }
            SetAccountForm();
        }

        private void checkBoxCheckPrice_CheckedChanged(object sender, EventArgs e)
        {
            //if (!checkBoxCheckPrice.Checked)
            //    EnableDisableBuySellBtn(true, true);
            CheckPriceValue();
        }

        #endregion


        public void SetVisibleSpecialType(bool IsVisible)
        {
            booVisibleSpecialType = IsVisible;
            if (IsVisible == true)
            {
                panelSpecialType.Visible = true;
            }
            else
            {
                checkBoxSpecialLimit.Checked = false;
                checkBoxAllOrNothing.Checked = false;
                panelSpecialType.Visible = false;
            }
        }

        private void checkBoxBuyMax_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void myGroupBoxOrder_Enter(object sender, EventArgs e)
        {

        }

        private void ExchangeCheckBoxChanged(object sender, EventArgs e)
        {
            this.checkBoxSZE.CheckedChanged -= pHandlerCBSZE;
            this.checkBoxSHG.CheckedChanged -= pHandlerCBSHG;
            this.checkBoxGray.CheckedChanged -= pHandlerCBGray;

            CheckBox TheCheckBox = sender as CheckBox;

            checkBoxSZE.Enabled = checkBoxSZE.Visible = !DisableSZE;
            checkBoxSHG.Enabled = checkBoxSHG.Visible = !DisableSHG;
            checkBoxGray.Enabled = checkBoxGray.Visible = !DisableGray;

            if (!DisableGray &&
                (TheCheckBox == checkBoxGray || TheCheckBox == null && (FormExType == ExchangeTypeEnum.Gray || FormExType == ExchangeTypeEnum.PMHKG || FormExType == ExchangeTypeEnum.FTHKG)))
            {
                LocalFormSettings["IsSZE"] = "0";
                LocalFormSettings["IsSHG"] = "0";

                textBoxStockCode.Enabled = textBoxStockCode.Visible = true;
                checkBoxSZE.Checked = false;
                checkBoxSHG.Checked = false;

                if (TheCheckBox == null || TheCheckBox.Checked || DisableHKG)
                {
                    if (FormExType == ExchangeTypeEnum.FTHKG)
                    {
                        LocalFormSettings["IsGrayStock"] = "1";
                        checkBoxGray.Checked = true;
                        FormExType = ExchangeTypeEnum.FTHKG;
                        myGroupBoxOrder.BackColor = Color.FromArgb(124, 214, 255);
                    }
                    else if (FormExType == ExchangeTypeEnum.PMHKG)
                    {
                        LocalFormSettings["IsGrayStock"] = "1";
                        checkBoxGray.Checked = true;
                        FormExType = ExchangeTypeEnum.PMHKG;
                        myGroupBoxOrder.BackColor = Color.FromArgb(255, 214, 124);
                    }
                    else
                    {
                        LocalFormSettings["IsGrayStock"] = "1";
                        checkBoxGray.Checked = true;
                        FormExType = ExchangeTypeEnum.Gray;
                        myGroupBoxOrder.BackColor = Color.FromArgb(255, 214, 124);
                    }
                }
                else
                {
                    LocalFormSettings["IsGrayStock"] = "0";
                    checkBoxGray.Checked = false;
                    FormExType = ExchangeTypeEnum.HKG;
                    myGroupBoxOrder.BackColor = Color.FromArgb(255, 255, 128);
                }
            }
            else if (!DisableSZE &&
                (TheCheckBox == checkBoxSZE || TheCheckBox == null && (FormExType == ExchangeTypeEnum.SZE)))
            {
                LocalFormSettings["IsSHG"] = "0";
                LocalFormSettings["IsGrayStock"] = "0";

                textBoxStockCode.Enabled = textBoxStockCode.Visible = true;
                checkBoxSHG.Checked = false;
                checkBoxGray.Checked = false;

                if (TheCheckBox == null || TheCheckBox.Checked || DisableHKG)
                {
                    LocalFormSettings["IsSZE"] = "1";
                    checkBoxSZE.Checked = true;
                    FormExType = ExchangeTypeEnum.SZE;
                    myGroupBoxOrder.BackColor = Color.FromArgb(218, 174, 232);
                }
                else
                {
                    LocalFormSettings["IsSZE"] = "0";
                    checkBoxSZE.Checked = false;
                    FormExType = ExchangeTypeEnum.HKG;
                    myGroupBoxOrder.BackColor = Color.FromArgb(255, 255, 128);
                }
            }
            else if (!DisableSHG &&
                (TheCheckBox == checkBoxSHG || TheCheckBox == null && (FormExType == ExchangeTypeEnum.SHG)))
            {
                LocalFormSettings["IsSZE"] = "0";
                LocalFormSettings["IsGrayStock"] = "0";

                textBoxStockCode.Enabled = textBoxStockCode.Visible = true;
                checkBoxSZE.Checked = false;
                checkBoxGray.Checked = false;

                if (TheCheckBox == null || TheCheckBox.Checked || DisableHKG)
                {
                    LocalFormSettings["IsSHG"] = "1";
                    checkBoxSHG.Checked = true;
                    FormExType = ExchangeTypeEnum.SHG;
                    myGroupBoxOrder.BackColor = Color.FromArgb(170, 255, 170);
                }
                else
                {
                    LocalFormSettings["IsSHG"] = "0";
                    checkBoxSHG.Checked = false;
                    FormExType = ExchangeTypeEnum.HKG;
                    myGroupBoxOrder.BackColor = Color.FromArgb(255, 255, 128);
                }
            }
            else if (!DisableHKG &&
                (TheCheckBox != null || FormExType == ExchangeTypeEnum.HKG))
            {
                LocalFormSettings["IsSZE"] = "0";
                LocalFormSettings["IsSHG"] = "0";
                LocalFormSettings["IsGrayStock"] = "0";

                textBoxStockCode.Enabled = textBoxStockCode.Visible = true;
                checkBoxSZE.Checked = false;
                checkBoxSHG.Checked = false;
                checkBoxGray.Checked = false;

                FormExType = ExchangeTypeEnum.HKG;
                myGroupBoxOrder.BackColor = Color.FromArgb(255, 255, 128);
            }
            else
            {
                LocalFormSettings["IsSZE"] = "0";
                LocalFormSettings["IsSHG"] = "0";
                LocalFormSettings["IsGrayStock"] = "0";

                textBoxStockCode.Enabled = textBoxStockCode.Visible = false;
                checkBoxSZE.Checked = false;
                checkBoxSHG.Checked = false;
                checkBoxGray.Checked = false;

                FormExType = ExchangeTypeEnum.Unassigned;
                myGroupBoxOrder.BackColor = Color.FromArgb(255, 255, 128);
            }

            this.checkBoxSZE.CheckedChanged += pHandlerCBSZE;
            this.checkBoxSHG.CheckedChanged += pHandlerCBSHG;
            this.checkBoxGray.CheckedChanged += pHandlerCBGray;
        }

        private void checkBoxSZE_CheckedChanged(object sender, EventArgs e)
        {
            ExchangeCheckBoxChanged(sender, e);
            SetCurrentStockCode(textBoxStockCode.Text.Trim().TrimStart('0'), true);
        }

        private void checkBoxSHG_CheckedChanged(object sender, EventArgs e)
        {
            ExchangeCheckBoxChanged(sender, e);
            SetCurrentStockCode(textBoxStockCode.Text.Trim().TrimStart('0'), true);
        }

        private void checkBoxGray_CheckedChanged(object sender, EventArgs e)
        {
            ExchangeCheckBoxChanged(sender, e);
            SetCurrentStockCode(textBoxStockCode.Text.Trim().TrimStart('0'), true);
        }

        /*
        private void checkBoxSHG_CheckedChanged(object sender, EventArgs e)
        { // prior to check state actually changed
            DisableHKG = (SettingsForms["DisableHKG"] != null && SettingsForms["DisableHKG"] == "1");
            DisableSZE = (SettingsForms["DisableSZE"] != null && SettingsForms["DisableSZE"] == "1");
            DisableSHG = (SettingsForms["DisableSHG"] != null && SettingsForms["DisableSHG"] == "1");
            EnablePMHKG = (SettingsForms["EnablePMHKG"] != null && SettingsForms["EnablePMHKG"] == "1");
            //EnablePMHKG = true;

            CheckBox tempCheckBox = (CheckBox)sender;
            if (tempCheckBox.CheckState == CheckState.Unchecked && (checkBoxSZE.Checked || checkBoxSZE.Checked))
                return;

            if ((tempCheckBox == checkBoxSHG) && checkBoxSHG.Checked)
            {
                if (DisableSHG)
                {
                    checkBoxSHG.Checked = false;
                    return;
                }
                if (DisableHKG)
                {
                    checkBoxSHG.Enabled = false;
                    checkBoxSZE.Enabled = true;
                }
                LocalFormSettings["IsSHG"] = "1";
                LocalFormSettings["IsSZE"] = "0";
                LocalFormSettings["IsPMHKG"] = "0";
                FormExType = ExchangeTypeEnum.SHG;
                checkBoxSHG.BackColor = Color.FromArgb(111, 168, 111);
                myGroupBoxOrder.BackColor = Color.FromArgb(170, 255, 170);
                checkBoxSZE.BackColor = Color.FromArgb(170, 255, 170);
                checkBoxGray.BackColor = Color.FromArgb(170, 255, 170);
                checkBoxStockOnHand.Checked = false;
                checkBoxStockOnHand.Enabled = false;
                checkBoxSZE.Checked = false;
                checkBoxGray.Checked = false;
            }
            else if ((tempCheckBox == checkBoxSZE) && checkBoxSZE.Checked)
            {
                if (DisableSZE)
                {
                    checkBoxSZE.Checked = false;
                    return;
                }
                if (DisableHKG)
                {
                    checkBoxSZE.Enabled = false;
                    checkBoxSHG.Enabled = true;
                }
                LocalFormSettings["IsSZE"] = "1";
                LocalFormSettings["IsSHG"] = "0";
                LocalFormSettings["IsPMHKG"] = "0";
                FormExType = ExchangeTypeEnum.SZE;
                checkBoxSZE.BackColor = Color.FromArgb(157, 89, 179);
                checkBoxSHG.BackColor = Color.FromArgb(218, 174, 232);
                myGroupBoxOrder.BackColor = Color.FromArgb(218, 174, 232);
                checkBoxGray.BackColor = Color.FromArgb(218, 174, 232);
                checkBoxStockOnHand.Checked = false;
                checkBoxStockOnHand.Enabled = false;
                checkBoxSHG.Checked = false;
                checkBoxGray.Checked = false;
            }
            else if ((tempCheckBox == checkBoxGray) && checkBoxGray.Checked)
            {
                if (!EnablePMHKG)
                {
                    checkBoxGray.Checked = false;
                    return;
                }
                if (DisableHKG)
                {
                    checkBoxGray.Enabled = false;
                    checkBoxSHG.Enabled = true;
                }
                LocalFormSettings["IsPMHKG"] = "1";
                LocalFormSettings["IsSZE"] = "0";
                LocalFormSettings["IsSHG"] = "0";
                FormExType = ExchangeTypeEnum.PMHKG;
                checkBoxGray.BackColor = Color.FromArgb(247, 184, 46);
                checkBoxSHG.BackColor = Color.FromArgb(255, 214, 124);
                checkBoxSZE.BackColor = Color.FromArgb(255, 214, 124);
                myGroupBoxOrder.BackColor = Color.FromArgb(255, 214, 124);
                checkBoxSZE.ForeColor = Color.Black;
                checkBoxSHG.Checked = false;
                checkBoxSZE.Checked = false;
            }
            else if (!checkBoxSHG.Checked && !checkBoxSZE.Checked && !checkBoxGray.Checked)
            {
                if (DisableHKG)
                {
                    if (!DisableSZE)
                        checkBoxSZE.Checked = true;
                    else if (!DisableSHG)
                        checkBoxSHG.Checked = true;
                    return;
                }
                LocalFormSettings["IsSHG"] = "0";
                LocalFormSettings["IsSZE"] = "0";
                LocalFormSettings["IsPMHKG"] = "0";
                FormExType = ExchangeTypeEnum.HKG;
                checkBoxSHG.BackColor = Color.FromArgb(255, 255, 128);
                myGroupBoxOrder.BackColor = Color.FromArgb(255, 255, 128);
                checkBoxSZE.BackColor = Color.FromArgb(255, 255, 128);
                checkBoxGray.BackColor = Color.FromArgb(255, 255, 128);
                checkBoxStockOnHand.Checked = false;
                checkBoxStockOnHand.Enabled = true;
            }
            ResetForm(textBoxStockCode.Text);
            SetCurrentStockCode(textBoxStockCode.Text, true);
        }
         */

        private void buttonTransactionCharge_Click(object sender, EventArgs e)
        {
            if (AccountNoComboBox1.Text.Trim() == "") return;
            labelTotal.Text = "";

            List<decimal> priceList = new List<decimal>(1);
            List<decimal> qtyList = new List<decimal>(1);

            decimal decStockPrice = 0; long lngQty = 0; int intCode = 0;
            decimal.TryParse(textBoxStockPrice.Text, out decStockPrice);
            priceList.Add(decStockPrice);
            long.TryParse(textBoxStockQty.Text.Replace(",", ""), out lngQty);
            qtyList.Add(lngQty);
            int.TryParse(textBoxStockCode.Text, out intCode);
            if (decStockPrice > 1000 || lngQty > 100000000)
                return;

            TransactionCharge tc;
            ExchangeTypeEnum ete = ExchangeTypeEnum.HKG;
            if (checkBoxSHG.Checked) { ete = ExchangeTypeEnum.SHG; }
            else if (checkBoxSZE.Checked) { ete = ExchangeTypeEnum.SZE; }
            else if (checkBoxGray.Checked) { ete = ExchangeTypeEnum.PMHKG; }

            tc = new TransactionCharge(AccountNoComboBox1.Text, BuyButton ? 'B' : 'S', ete, intCode.ToString(), priceList, qtyList);

            if (tc.PriceList != null && tc.PriceList.Count > 0 && tc.QuantityList != null && tc.QuantityList.Count > 0)
                GetTransactionCharge(tc);
            return;
        }

        protected override void OnTransactionCharge(TransactionCharge TxnCharge)
        {
            if (TxnCharge != null)
                labelTotal.Text = String.Format("{0:N}", Math.Abs(TxnCharge.NetAmount));
        }

        private void checkBoxSellMax_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBoxAllOrNothing_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxAllOrNothing.Checked == true)
                checkBoxSpecialLimit.Checked = false;
        }

        private void checkBoxSpecialLimit_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxSpecialLimit.Checked == true)
                checkBoxAllOrNothing.Checked = false;
        }

        private void listBoxAccountno_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void listBoxAccountno_DoubleClick(object sender, EventArgs e)
        {                 
            if (listBoxAccountno.SelectedItem != null && listBoxStockcode.SelectedItem!= null)            
            {
                if (listBoxAccountno.SelectedItem.ToString().Trim() != "" && listBoxStockcode.SelectedItem.ToString().Trim() != "")
                {
                    AccountNoComboBox1.Text = listBoxAccountno.SelectedItem.ToString().Trim();
                    ListenedAccount = AccountNoComboBox1.Text;
                    SetAccountForm();
                    textBoxStockCode.Text = listBoxStockcode.SelectedItem.ToString().Trim();
                    SetCurrentStockCode(textBoxStockCode.Text.Trim().TrimStart('0'), true);


                    if (checkBoxLoadAllStockQty.Checked)
                    {
                        boolLoadAllStockQty = true;
                    }
                }

            }
        }

        private void buttonPreset_Click(object sender, EventArgs e)
        {
            myGroupBoxPreset.Visible = true;
            if (AccountNoComboBox1.Text.Trim() != "")
            {
                if (!listBoxAccountno.Items.Contains(AccountNoComboBox1.Text.Trim()))
                {
                    listBoxAccountno.Items.Add(AccountNoComboBox1.Text.Trim());
                    //////LocalFormSettings["preset_accno-1"] = AccountNoComboBox1.Text.Trim();
                }
            }            
        }

        private void buttonDelAcc_Click(object sender, EventArgs e)
        {
            if (listBoxAccountno.SelectedItem != null)
            { listBoxAccountno.Items.Remove(listBoxAccountno.SelectedItem); }
        }

        private void buttonDelStock_Click(object sender, EventArgs e)
        {
            if (listBoxStockcode.SelectedItem != null)
            { listBoxStockcode.Items.Remove(listBoxStockcode.SelectedItem); }
        }

        private void buttonStockAdd_Click(object sender, EventArgs e)
        {
            myGroupBoxPreset.Visible = true;

            if (textBoxStockCode.Text.Trim() != "")
            {
                if (!listBoxStockcode.Items.Contains(textBoxStockCode.Text.Trim()))
                {
                    listBoxStockcode.Items.Add(textBoxStockCode.Text.Trim());
                }
            }            
        }

        private void listBoxStockcode_SelectedIndexChanged(object sender, EventArgs e)
        {
        
        }

        private void listBoxStockcode_DoubleClick(object sender, EventArgs e)
        {
            if (listBoxAccountno.SelectedItem != null && listBoxStockcode.SelectedItem != null)
            {
                if (listBoxAccountno.SelectedItem.ToString().Trim() != "" && listBoxStockcode.SelectedItem.ToString().Trim() != "")
                {
                    AccountNoComboBox1.Text = listBoxAccountno.SelectedItem.ToString().Trim();
                    ListenedAccount = AccountNoComboBox1.Text;
                    SetAccountForm();
                    textBoxStockCode.Text = listBoxStockcode.SelectedItem.ToString().Trim();
                    SetCurrentStockCode(textBoxStockCode.Text.Trim().TrimStart('0'), true);


                    if (checkBoxLoadAllStockQty.Checked)
                    {
                        boolLoadAllStockQty = true;
                    }
                }

            }

        }

        private void myGroupBoxPreset_Enter(object sender, EventArgs e)
        {

        }

        private void checkBoxLoadAllStockQty_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxLoadAllStockQty.Checked)
                boolLoadAllStockQty = true;
            else
                boolLoadAllStockQty = false;
        }
    }
}
 
