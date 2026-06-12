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
    public partial class FastOrderTicketForm : BaseForm
    {
        private DockPanel dockPanelMain;
        private bool nonNumberEntered = false;
        protected Int64 lotSize = 100;
        public Dictionary<string, StockQuoteForm> StkQuoteDict = new Dictionary<string, StockQuoteForm>(5);
        public List<string> AccountList1 = new List<string>(20);
        private string pListenedAccount = null;        
        private Account CurrAccount = null;
        private Stock CurrStock = null;
        //private bool PriceFilled = false;
        private bool NameFilled = false;
        private bool LotSizeFilled = false;
        private bool QtyFilled = false;        
        public string StockCode = "";
        private string pListenedStockCode = null;
        private decimal pTrackPrice;
        private decimal pTrackPriceBid;
        private decimal pTrackPriceAsk;
        private string pTrackSpread = "0";
        private string pConfirmWaiting = "";
        private char DPGW = '\0'; // Discretion Product Group Warning

        public string ConfirmWaiting
        {
            get
            {
                return pConfirmWaiting;
            }

            set
            {
                pConfirmWaiting = value != null ? value.ToUpper().Trim() : "";
                if (pConfirmWaiting == "B")
                {
                    buttonResetBuy.Visible = true;
                    buttonResetSell.Visible = false;

                    textBoxPriceBid.Text = TrackPriceBid;
                    buttonChaseBuy.Text = textBoxPriceBid.Text + "\n" + GetResxString("Yes") + "?";

                    if (TradeDB.UserType != UserTypeEnum.AE) textBoxPriceAsk.Text = "";
                    buttonChaseSell.Text = textBoxPriceAsk.Text + "\n" + GetResxString("Sell");
                }
                else if (pConfirmWaiting == "A")
                {
                    buttonResetBuy.Visible = false;
                    buttonResetSell.Visible = true;

                    textBoxPriceAsk.Text = TrackPriceAsk;
                    buttonChaseSell.Text = textBoxPriceAsk.Text + "\n" + GetResxString("Yes") + "?";

                    if (TradeDB.UserType != UserTypeEnum.AE) textBoxPriceBid.Text = "";
                    buttonChaseBuy.Text = textBoxPriceBid.Text + "\n" + GetResxString("Buy");
                }
                else
                {
                    buttonResetBuy.Visible = false;
                    buttonResetSell.Visible = false;

                    if (TradeDB.UserType != UserTypeEnum.AE)
                    {
                        textBoxPriceBid.Text = "";
                        textBoxPriceAsk.Text = "";
                    }

                    buttonChaseBuy.Text = textBoxPriceBid.Text + "\n" + GetResxString("Buy");
                    buttonChaseSell.Text = textBoxPriceAsk.Text + "\n" + GetResxString("Sell");
                }
            }
        }


        public string TrackPriceAsk
        {
            get
            {
                pTrackPriceAsk = -1;
                if (pTrackSpread == "") return "";
                if (CurrStock == null) return "";

                int Spread;
                int.TryParse(pTrackSpread, out Spread);
                bool CanBuy = false, CanSell = false;
                if (Spread == 0)
                    pTrackPriceAsk = Utils.OrderTypeSpread.CheckSpread(CurrStock.Ask, 'X', ' ', -Spread, CurrStock, TradeDB, out CanBuy, out CanSell);
                else if (Spread > 0) // <=5
                    pTrackPriceAsk = Utils.OrderTypeSpread.CheckSpread(CurrStock.Bid, 'X', ' ', -Spread + 1, CurrStock, TradeDB, out CanBuy, out CanSell);
                else if (Spread < 0) // < 23
                    pTrackPriceAsk = Utils.OrderTypeSpread.CheckSpread(CurrStock.Ask, 'X', ' ', -Spread, CurrStock, TradeDB, out CanBuy, out CanSell);

                if (CanBuy || CanSell)
                    return String.Format("{0:N3}", pTrackPriceAsk);

                return "";
            }

            set
            {
                pTrackSpread = value != null ? value.ToUpper().Trim() : "";

                textBoxPriceAsk.Text = TrackPriceAsk;
                ConfirmWaiting = "";
            }
        }


        public string TrackPriceBid
        {
            get
            {
                if (CurrStock == null) return "";

                int Spread;
                int.TryParse(pTrackSpread, out Spread);

                bool CanBuy = false, CanSell = false;
                if (Spread == 0)
                    pTrackPriceBid = Utils.OrderTypeSpread.CheckSpread(CurrStock.Bid, 'X', ' ', Spread, CurrStock, TradeDB, out CanBuy, out CanSell);
                else if (Spread > 0)  // <=5
                    pTrackPriceBid = Utils.OrderTypeSpread.CheckSpread(CurrStock.Ask, 'X', ' ', Spread-1, CurrStock, TradeDB, out CanBuy, out CanSell);
                else if (Spread < 0)  // < 23
                    pTrackPriceBid = Utils.OrderTypeSpread.CheckSpread(CurrStock.Bid, 'X', ' ', Spread, CurrStock, TradeDB, out CanBuy, out CanSell);

                if (CanBuy || CanSell)
                    return String.Format("{0:N3}", pTrackPriceBid);

                return "";
            }

            set
            {
                pTrackSpread = value != null ? value.ToUpper().Trim() : "";

                textBoxPriceBid.Text = TrackPriceBid;
                ConfirmWaiting = "";
            }
        }

        public string TrackPrice
        {
            get
            {
                pTrackPrice = -1;
                if (pTrackSpread == "") return "";
                if (CurrStock == null) return "";
                
                bool CanBuy = false, CanSell = false;
                if (pTrackSpread == "CHECKBOXBID1")
                    pTrackPrice = Utils.OrderTypeSpread.CheckSpread(CurrStock.Bid, 'X', ' ', 0, CurrStock, TradeDB, out CanBuy, out CanSell);
                else if (pTrackSpread == "CHECKBOXBID2")
                    pTrackPrice = Utils.OrderTypeSpread.CheckSpread(CurrStock.Bid, 'X', ' ', -1, CurrStock, TradeDB, out CanBuy, out CanSell);
                else if (pTrackSpread == "CHECKBOXBID3")
                    pTrackPrice = Utils.OrderTypeSpread.CheckSpread(CurrStock.Bid, 'X', ' ', -2, CurrStock, TradeDB, out CanBuy, out CanSell);
                else if (pTrackSpread == "CHECKBOXBID4")
                    pTrackPrice = Utils.OrderTypeSpread.CheckSpread(CurrStock.Bid, 'X', ' ', -3, CurrStock, TradeDB, out CanBuy, out CanSell);
                else if (pTrackSpread == "CHECKBOXBID5")
                    pTrackPrice = Utils.OrderTypeSpread.CheckSpread(CurrStock.Bid, 'X', ' ', -4, CurrStock, TradeDB, out CanBuy, out CanSell);
                else if (pTrackSpread == "CHECKBOXASK1")
                    pTrackPrice = Utils.OrderTypeSpread.CheckSpread(CurrStock.Ask, 'X', ' ', 0, CurrStock, TradeDB, out CanBuy, out CanSell);
                else if (pTrackSpread == "CHECKBOXASK2")
                    pTrackPrice = Utils.OrderTypeSpread.CheckSpread(CurrStock.Ask, 'X', ' ', 1, CurrStock, TradeDB, out CanBuy, out CanSell);
                else if (pTrackSpread == "CHECKBOXASK3")
                    pTrackPrice = Utils.OrderTypeSpread.CheckSpread(CurrStock.Ask, 'X', ' ', 2, CurrStock, TradeDB, out CanBuy, out CanSell);
                else if (pTrackSpread == "CHECKBOXASK4")
                    pTrackPrice = Utils.OrderTypeSpread.CheckSpread(CurrStock.Ask, 'X', ' ', 3, CurrStock, TradeDB, out CanBuy, out CanSell);
                else if (pTrackSpread == "CHECKBOXASK5")
                    pTrackPrice = Utils.OrderTypeSpread.CheckSpread(CurrStock.Ask, 'X', ' ', 4, CurrStock, TradeDB, out CanBuy, out CanSell);

                if (CanBuy || CanSell)
                    return String.Format("{0:N3}", pTrackPrice);

                return "";
            }

            set
            {
                pTrackSpread = value != null ? value.ToUpper().Trim() : "";

                textBoxPrice.Text = TrackPrice;
            }
        }



        public string ListenedStockCode
        {
            get
            {
                return pListenedStockCode;
            }

            set
            {
                string code = value != null ? value.Trim() : "";

                if (code != (pListenedStockCode != null ? pListenedStockCode : ""))
                {
                    if (pListenedStockCode != null && pListenedStockCode.Length > 0)
                    {
                        UnListenStock(new List<string> { Stock.GetSignature(ExchangeTypeEnum.HKG, pListenedStockCode) });
                    }

                    //ClearForm();
                    ResetForm(code);

                    if (code.Length > 0)
                    {
                        pListenedStockCode = int.Parse(code).ToString();
                        ListenStock(new List<string> { Stock.GetSignature(ExchangeTypeEnum.HKG, pListenedStockCode) });

                        //PriceFilled = false;
                        NameFilled = false;
                        LotSizeFilled = false;
                        QtyFilled = false;
                    }
                    else
                    {
                        pListenedStockCode = "";
                    }
                    //this.textBoxStockCode.Text = pListenedStockCode;

                    // Reset Stock Quote Form
                    foreach (KeyValuePair<string, StockQuoteForm> kvp in StkQuoteDict)
                    {
                        if (kvp.Value != null)
                        {
                            /*
                            kvp.Value.textBoxCode.Text = code;
                            kvp.Value.ListenedStockCode = code;
                            kvp.Value.labelStockCode.Text = code;
                             */

                            kvp.Value.SetStock(ExchangeTypeEnum.HKG, code);
                        }
                    }
                }
            }
        }


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
                        SetAccountForm();
                    }
                }
            }
        }

        public bool AllowChangeAccountFormAccount = true;

        private List<string> tempAccountList = new List<string>(400);
        private object tempAccountListMutex = new object();
        private SortedList accountSortedList = new SortedList();
        private bool accountComboSorted = false;
        //private bool fillCompletedOnce = false;

        //private List<List<string>> AccountToBeFilledList = new List<List<string>>(20);
        //private object AccountToBeFilledListMutex = new object();
        //private const int AccountToBeFilledMaxCountPerInterval = 200;
        //private int AccountToBeFilledEmptyCount = 0;

        //public void Redraw()
        //{
        //    if (textBoxStockCode.Text.Trim() != StockCode) this.textBoxStockCode.Text = StockCode;

        //    this.textBoxChasePrice.Text = "";
        //    this.textBoxStkLot.Text = "";
        //    this.textBoxStkShares.Text = "";
        //    this.buttonChaseBuy.Enabled = true;
        //    this.buttonChaseSell.Enabled = true;
        //    //calAmount();
        //}


        private void ResetForm(string NewStockCode)
        {
            //StockCode = "";
            if (CurrStock != null) UnListenStock(new List<string> { CurrStock.StockSignature });
            CurrStock = null;            
            StockCode = NewStockCode.Trim();
            if (textBoxStockCode.Text.Trim() != StockCode) this.textBoxStockCode.Text = StockCode;
            labelLotSize.Text = "";
            labelStockName.Text = "";
            if (textBoxChasePrice.Text.Trim() == "")            
                textBoxChasePrice.Text = "0";
            else  //Make a text change trigger updating
                textBoxChasePrice.Text = textBoxChasePrice.Text;
            textBoxStkLot.Text = "1";
            textBoxStkShares.Text = "";
            buttonChaseBuy.Text = "Buy";
            buttonChaseBuy.Enabled = true;
            buttonChaseSell.Text = "Sell";
            buttonChaseSell.Enabled = true;
            buttonResetBuy.Visible = false;
            buttonResetSell.Visible = false;
            textBoxPriceAsk.Text = "";
            textBoxPriceBid.Text = "";
            ConfirmWaiting = "";
            DPGW = '\0';
            //PriceFilled = false;
            NameFilled = false;
            QtyFilled = false;
            LotSizeFilled = false;            
            ShowTradeStatus(CurrStock);
        }


        //private void ResetForm(string NewStockCode)
        //{
        //    if (NewStockCode != null)
        //    {
        //        if (CurrStock != null) UnListenStock(new List<string> { CurrStock.Code });
        //        CurrStock = null;
        //        StockCode = NewStockCode.Trim();
        //    }
        //    else
        //        StockCode = "";
        //    //Qty = "";
        //    //this.checkBoxAuction.Checked = false;
        //    Redraw();
        //}

        public FastOrderTicketForm() : this(null, null,null)
        {
            ////InitializeComponent();
        }

        public FastOrderTicketForm(CultureInfo Culture, string PersistString, DockPanel dockPanelMain)
            : base(Culture, PersistString)
        {
            InitializeComponent();

            Utils.Utils.EnableDoubleBuffered(AccountNoComboBox1);

            this.dockPanelMain = dockPanelMain;
            LayoutLockable = false;

            AccountNoComboBox1.TradeDB = TradeDB;
        }

        private void buttonChaseBuy_Click(object sender, EventArgs e)
        {
            //user click Buy now
            if (ConfirmWaiting == "A")
            {
                ConfirmWaiting = "B";
                return;
            }


            // Not the second click
            if (ConfirmWaiting != "")
            {
                if (!ChkBalanceQty('B')) return;
                if (!ChkOrderValue('B')) return;
            }

            //BuySellConfirmationForm bsForm = new BuySellConfirmationForm(this.Culture, null);
            //bsForm.IsBuyOrder = true;
            //bsForm.strAcc = AccountNoComboBox1.Text.Trim();
            //bsForm.strBuySell = buttonBuy.Text;
            //bsForm.strStkCode = textBoxStockCode.Text;
            //bsForm.strStkPrice = "$" + textBoxStockPrice.Text;
            //bsForm.strStkQty = textBoxStockQty.Text;
            //bsForm.strStkTtlAmt = "$" + labelTotal.Text;
            //bsForm.IsAuctionType = checkBoxAuction.Checked;
            //bsForm.StartPosition = FormStartPosition.CenterScreen;
            //if (bsForm.ShowDialog() == DialogResult.OK)        

            if (ConfirmWaiting == "B")
            {
                if (CurrAccount != null)
                {
                    switch (CurrAccount.DiscretionCheck(CurrStock))
                    {
                        case 1:
                            DiscretionConfirmForm dcForm = new DiscretionConfirmForm(this.Culture, null);
                            dcForm.StartPosition = FormStartPosition.CenterScreen;
                            if (dcForm.ShowDialog(this) != DialogResult.OK)
                                return;
                            DPGW = 'C'; // Order is from client directly
                            //bsForm.ShowInlineWithInvestPref = false;
                            break;

                        case 0:
                            DPGW = 'I'; // Order is inline with client investment preference
                            //bsForm.ShowInlineWithInvestPref = true;
                            break;

                        case -1:
                            DPGW = 'N'; // Client account is not discretion account
                            //bsForm.ShowInlineWithInvestPref = false;
                            break;
                    }
                }

                ////Check Warnning amount
                //bool WarnAmtok = true;
                //decimal total;
                //decimal.TryParse(labelTotal.Text, out total);
                //if (total > 300000)
                //{
                //    TotalAmtLimitForm limitForm = new TotalAmtLimitForm(this.Culture, null, labelTotal.Text, 300000);
                //    if (limitForm.ShowDialog(this) != DialogResult.OK)
                //    {
                //        WarnAmtok = false;
                //        return;
                //    }
                //}

                PrepareOrder('B');
                ConfirmWaiting = "";
                //ResetTradePwd();
                if (TradeDB.UserType == UserTypeEnum.AE && !KeepAccNoAfterPlaceOrder)
                    ListenedAccount = null;
                if (textBoxStockCode.Enabled)
                    textBoxStockCode.Focus();
                else if (textBoxStkLot.Enabled)
                    textBoxStkLot.Focus();
            }
            else
                ConfirmWaiting = "B";
            
        }


        private void buttonChaseSell_Click(object sender, EventArgs e)
        {
            //user click Buy now
            if (ConfirmWaiting == "B")
            {
                ConfirmWaiting = "A";
                return;
            }

            // Not the second click
            if (ConfirmWaiting != "")
            {
                if (!ChkBalanceQty('A')) return;
                if (!ChkOrderValue('A')) return;
            }

            if (ConfirmWaiting == "A")
            {
                PrepareOrder('A');
                ConfirmWaiting = "";
                //ResetTradePwd();
                if (TradeDB.UserType == UserTypeEnum.AE && !KeepAccNoAfterPlaceOrder)
                    ListenedAccount = null;
                if (textBoxStockCode.Enabled)
                    textBoxStockCode.Focus();
                else if (textBoxStkLot.Enabled)
                    textBoxStkLot.Focus();
            }
            else
                ConfirmWaiting = "A";

        }

        private void FastOrderTicketForm_Load(object sender, EventArgs e)
        {
            checkBoxLock.BackgroundImage = Properties.Resources.blueUnlock as Image;
            ResetForm("");
            ListenAccountList();
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
            AccountNoComboBox1.Text = GetResxString("ComboBoxAccountNo_Loading");
            AccountNoComboBox1.SuspendLayout();

            SetVisibleSpecialType(booVisibleSpecialType);
        }

        private void checkBoxLock_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxLock.Checked)
            {
                checkBoxLock.BackgroundImage = Properties.Resources.blueLock as Image;
                textBoxStockCode.Enabled = false;
                textBoxChasePrice.Enabled = false;
                vScrollBarSpread.Enabled = false;
                textBoxStkShares.Enabled = false;
                vScrollBarShares.Enabled = false;
                textBoxStkLot.Enabled = false;
                vScrollBarLot.Enabled = false;
                checkBoxHotKey.Enabled = false;
            }
            else
            {
                checkBoxLock.BackgroundImage = Properties.Resources.blueUnlock as Image;
                textBoxStockCode.Enabled = true;
                textBoxChasePrice.Enabled = true;
                vScrollBarSpread.Enabled = true;
                textBoxStkShares.Enabled = true;
                vScrollBarShares.Enabled = true;
                textBoxStkLot.Enabled = true;
                vScrollBarLot.Enabled = true;
                checkBoxHotKey.Enabled = true;
            }
        }

        private void vScrollBarSpread_Scroll(object sender, ScrollEventArgs e)
        {
            long stkQty = 0;
            if (textBoxChasePrice.Text != "")
                stkQty = Convert.ToInt64(textBoxChasePrice.Text.Replace(",", ""));
            if (e.Type == ScrollEventType.SmallDecrement && stkQty < 5)
                stkQty += 1;
            if (e.Type == ScrollEventType.SmallIncrement && stkQty - 1 >= -23)
                stkQty -= 1;
            //if (e.Type == ScrollEventType.SmallIncrement && stkQty - 1 >= 0)
            //    stkQty -= 1;
            textBoxChasePrice.Text = String.Format("{0:N0}", stkQty);
            textBoxChasePrice.SelectAll();
        }

        private void vScrollBarLotShare_Scroll(object sender, ScrollEventArgs e)
        {
            long stkLot = 0;
            long stkShares = 0;
            if (textBoxStkLot.Text != "")
                stkLot = Convert.ToInt64(labelLotSize.Text.Replace(",", ""));
            if (textBoxStkShares.Text != "")
                stkShares = Convert.ToInt64(textBoxStkShares.Text.Replace(",", ""));
            if (e.Type == ScrollEventType.SmallDecrement)
                stkLot += 1;
            if (e.Type == ScrollEventType.SmallIncrement && stkLot - 1 >= 0 && stkShares - lotSize >= 0)
                stkLot -= 1;
            stkShares = lotSize * stkLot;
            textBoxStkLot.Text = String.Format("{0:N0}", stkLot);
            textBoxStkShares.Text = String.Format("{0:N0}", stkShares);
            
        }

        private void focusNextControl(object sender, KeyPressEventArgs e)
        {
            Control currCtl = (Control)sender; //current control
            e.Handled = true;
            Control c = GetNextControl(currCtl, true);
            if (c != null)
                c.Focus();
        }

        public void setStk_Price_Code(string stkPrice, string stkCode, bool CanBuy, bool CanSell, bool setFocus)
        {

            //Price = stkPrice;
            SetCurrentStockCode(stkCode);
            if (setFocus)
            {
                if (TradeDB.UserType == UserTypeEnum.Client)
                {
                    if (textBoxStockCode.Enabled) textBoxStockCode.Focus();
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

        private void textBoxStockCode_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                if (textBoxStockCode.Text.Trim() != "")
                    this.AllowChangeAccountFormAccount = true;
                ListenedStockCode = textBoxStockCode.Text;
                SetCurrentStockCode(textBoxStockCode.Text);
                if (textBoxStockCode.Text.Trim() != "")
                {
                    List<BaseForm> QuoteBrowserFormList = GetFormByFormType(typeof(QuoteBrowserForm));
                    if (QuoteBrowserFormList != null && QuoteBrowserFormList.Count > 0 && QuoteBrowserFormList[0] != null)
                        ((QuoteBrowserForm)QuoteBrowserFormList[0]).ChangeStockCode(textBoxStockCode.Text);
                }
                e.Handled = true;
                //focusNextControl(sender, e);
                if (textBoxStkLot.Enabled)
                    textBoxStkLot.Focus();
                else if (textBoxStkShares.Enabled)
                    textBoxStkShares.Focus();

                return;
            }

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

        private void textBoxPriceQty_KeyDown(object sender, KeyEventArgs e)
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
                }
            }
            if (Control.ModifierKeys == Keys.Shift)
            {
                nonNumberEntered = true;
            }
        }

        private void textBoxChasePrice_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                focusNextControl(sender, e);
                return;
            }

            if (e.KeyChar.ToString() != "-" && e.KeyChar.ToString() != "+" && nonNumberEntered)
            {
                e.Handled = true;
                return;
            }

            if (e.KeyChar.ToString() == "-" || e.KeyChar.ToString() == "+")
            {
                if (textBoxChasePrice.Text != "")
                {
                    long chaseSpread = Convert.ToInt64(textBoxChasePrice.Text.Replace(",", ""));
                    if (e.KeyChar.ToString() == "-" && chaseSpread - 1 >= 0)
                        chaseSpread -= 1;
                    else if (e.KeyChar.ToString() == "+" && chaseSpread < 5)
                        chaseSpread += 1;
                    textBoxChasePrice.Text = String.Format("{0:N0}", chaseSpread);
                    textBoxChasePrice.SelectAll();
                }
                e.Handled = true;
                return;
            }
        }

        private void textBoxStkShares_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                //focusNextControl(sender, e);
                if (buttonChaseBuy.Enabled)
                    buttonChaseBuy.Focus();
                else if (buttonChaseSell.Enabled)
                    buttonChaseSell.Focus();
                return;
            }

            if (e.KeyChar.ToString() != "-" && e.KeyChar.ToString() != "+" && nonNumberEntered)
            {
                e.Handled = true;
                return;
            }

            if (e.KeyChar.ToString() == "-" || e.KeyChar.ToString() == "+")
            {
                if (textBoxStkShares.Text != "")
                {
                    decimal InputQty = 0;
                    InputQty = CheckLotSize(textBoxStkShares.Text);
                    if (InputQty < 0) return;
                    
                    long stkLotSize = (labelLotSize.Text.Trim() != "") ? long.Parse(labelLotSize.Text) : 0;
                    if (e.KeyChar.ToString() == "-" && InputQty - stkLotSize > 0)
                        InputQty -= stkLotSize;
                    else if (e.KeyChar.ToString() == "+")
                        InputQty += stkLotSize;
                    textBoxStkShares.Text = String.Format("{0:N0}", InputQty);
                    textBoxStkShares.SelectAll();
                }
                e.Handled = true;
                return;
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


        private void textBoxStkLot_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                focusNextControl(sender, e);
                return;
            }

            if (e.KeyChar.ToString() != "-" && e.KeyChar.ToString() != "+" && nonNumberEntered)
            {
                e.Handled = true;
                return;
            }

            if (e.KeyChar.ToString() == "-" || e.KeyChar.ToString() == "+")
            {
                if (textBoxStkLot.Text != "")
                {
                    long chaseLot = Convert.ToInt64(textBoxStkLot.Text.Replace(",", ""));
                    long stkLotSize = (labelLotSize.Text.Trim() != "") ? long.Parse(labelLotSize.Text) : 0;
                    long stkShares = 0;
                    if (textBoxStkShares.Text != "")
                        stkShares = Convert.ToInt64(textBoxStkShares.Text.Replace(",", ""));
                    if (e.KeyChar.ToString() == "-" && chaseLot - 1 >= 0 && stkShares - stkLotSize > 0)
                        chaseLot -= 1;
                    else if (e.KeyChar.ToString() == "+")
                        chaseLot += 1;
                    stkShares = stkLotSize * chaseLot;
                    textBoxStkShares.Text = String.Format("{0:N0}", stkShares);
                    textBoxStkLot.Text = String.Format("{0:N0}", chaseLot);
                    textBoxStkLot.SelectAll();
                }
                e.Handled = true;
                return;
            }
        }

        private void textBoxStkLot_KeyUp(object sender, KeyEventArgs e)
        {
            if (textBoxStkLot.Text == "")
                return;
            long chaseLot = Convert.ToInt64(textBoxStkLot.Text.Replace(",", ""));
            long stkShares;
            long stkLotSize = (labelLotSize.Text.Trim() != "") ? long.Parse(labelLotSize.Text) : 0;

            stkShares = (stkLotSize == 0) ? 0 : stkLotSize * chaseLot;
            textBoxStkShares.Text = String.Format("{0:N0}", stkShares);
            textBoxStkLot.Text = String.Format("{0:N0}", chaseLot);
        }

        private void textBoxStkShares_KeyUp(object sender, KeyEventArgs e)
        {
            if (textBoxStkShares.Text == "")
                return;
            long stkShares = Convert.ToInt64(textBoxStkShares.Text.Replace(",", ""));

            long chaseLot;

            chaseLot = (CurrStock == null || stkShares <= 0) ? 1 : stkShares / CurrStock.LotSize;            
            textBoxStkShares.Text = String.Format("{0:N0}", stkShares);
            textBoxStkLot.Text = String.Format("{0:N0}", chaseLot);
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBoxBid1_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        protected override void OnStock(Stock TheStock)
        {
            try
            {
                if (TheStock != null)
                {
                    this.CurrStock = TheStock;

                    if (this.CurrStock != null && this.CurrStock.Nominal <= 0 && this.CurrStock.PrevClose > 0)
                        this.CurrStock.Nominal = this.CurrStock.PrevClose;

                    ShowStock();
                }
            }
            catch
            {

            }
        }


        protected override void OnCultureChange(CultureInfo ci)
        {
            SetVisibleSpecialType(booVisibleSpecialType);
            ShowTradeStatus(CurrStock);            
        }


        private void textBoxStockCode_TextChanged(object sender, EventArgs e)
        {
            ResetForm(textBoxStockCode.Text.Trim());            
        }

        private void SetCurrentStockCode(string NewStockCode)
        {
            NewStockCode = NewStockCode == null ? "" : NewStockCode.Trim();
            try
            {
                ResetForm(NewStockCode);
                foreach (KeyValuePair<string, StockQuoteForm> kvp in StkQuoteDict)
                {
                    if (kvp.Value != null)
                    {
                        /*
                        kvp.Value.textBoxCode.Text = NewStockCode;
                        kvp.Value.ListenedStockCode = NewStockCode;
                        kvp.Value.labelStockCode.Text = NewStockCode;
                         */

                        kvp.Value.SetStock(ExchangeTypeEnum.HKG, NewStockCode);
                    }
                }
                int code;
                if (int.TryParse(NewStockCode, out code))
                {
                    ListenStock(new List<string> { Stock.GetSignature(ExchangeTypeEnum.HKG, code.ToString()) });
                    SetAccountForm();
                }
            }
            catch
            { }
        }

        private void ShowStock()
        {
            if (CurrStock == null) return;
            if (CurrStock.Nominal <= -1 || CurrStock.LotSize <= -1 || CurrStock.Code == null)
                return;

            if (CurrStock.Code != ListenedStockCode.Trim() && ListenedStockCode.Trim() != "") return;

            bool CanBuy, CanSell;
            //Check Auction period
            if (Utils.OrderTypeSpread.CheckSpread(0, 'A', 'B', 1, CurrStock, TradeDB, out CanBuy, out CanSell) == -1)
            {
                //Auction = false;
                if (checkBoxBid1.Enabled == false) checkBoxBid1.Enabled = true; 
                if (checkBoxBid2.Enabled == false) checkBoxBid2.Enabled = true; 
                if (checkBoxBid3.Enabled == false) checkBoxBid3.Enabled = true; 
                if (checkBoxBid4.Enabled == false) checkBoxBid4.Enabled = true; 
                if (checkBoxBid5.Enabled == false) checkBoxBid5.Enabled = true; 
                if (checkBoxAsk1.Enabled == false) checkBoxAsk1.Enabled = true; 
                if (checkBoxAsk2.Enabled == false) checkBoxAsk2.Enabled = true; 
                if (checkBoxAsk3.Enabled == false) checkBoxAsk3.Enabled = true; 
                if (checkBoxAsk4.Enabled == false) checkBoxAsk4.Enabled = true; 
                if (checkBoxAsk5.Enabled == false) checkBoxAsk5.Enabled = true;
                //if (textBoxChasePrice.Enabled == false) textBoxChasePrice.Enabled = true;
                if (buttonChaseBuy.Enabled == false) buttonChaseBuy.Enabled = true;
                if (buttonChaseSell.Enabled == false) buttonChaseSell.Enabled = true;
            }
            else
            {
                //if Auction is true, stop user place an order
                if (checkBoxBid1.Enabled == true) checkBoxBid1.Enabled = false; checkBoxBid1.Checked = false;
                if (checkBoxBid2.Enabled == true) checkBoxBid2.Enabled = false; checkBoxBid2.Checked = false;
                if (checkBoxBid3.Enabled == true) checkBoxBid3.Enabled = false; checkBoxBid3.Checked = false;
                if (checkBoxBid4.Enabled == true) checkBoxBid4.Enabled = false; checkBoxBid4.Checked = false;
                if (checkBoxBid5.Enabled == true) checkBoxBid5.Enabled = false; checkBoxBid5.Checked = false;
                if (checkBoxAsk1.Enabled == true) checkBoxAsk1.Enabled = false; checkBoxAsk1.Checked = false;
                if (checkBoxAsk2.Enabled == true) checkBoxAsk2.Enabled = false; checkBoxAsk2.Checked = false;
                if (checkBoxAsk3.Enabled == true) checkBoxAsk3.Enabled = false; checkBoxAsk3.Checked = false;
                if (checkBoxAsk4.Enabled == true) checkBoxAsk4.Enabled = false; checkBoxAsk4.Checked = false;
                if (checkBoxAsk5.Enabled == true) checkBoxAsk5.Enabled = false; checkBoxAsk5.Checked = false;
                //if (textBoxChasePrice.Enabled == true) textBoxChasePrice.Enabled = false;
                if (buttonChaseBuy.Enabled == true) buttonChaseBuy.Enabled = false;
                if (buttonChaseSell.Enabled == true) buttonChaseSell.Enabled = false;                             
                
            }

            // Set OrderPrice to Nominal   //only fill price if stockprice textbox empty
            if (CurrStock.SpreadTableCode > 0 && CurrStock.MarketBelong != null)
            {
                SetPriceRange(CurrStock.Nominal);
            }

            if (CheckIsBlockAcc(' '))
            {
                if (CurrStock != null)
                    CheckIsDiscretion();
            }
            ShowTradeStatus(CurrStock);

            if (LotSizeFilled == false && CurrStock.LotSize != -1)
            {
                labelLotSize.Text = CurrStock.LotSize.ToString();
                LotSizeFilled = true;
            }
            if (NameFilled == false && CurrStock.NameENShort != null)
            {
                //labelStockName.Text = CurrStock.NameENShort.Trim();
                labelStockName.Text = CurrStock.GetBestName(this.Culture);
                NameFilled = true;
            }
            if (QtyFilled == false && CurrStock.LotSize != -1)
            {
                long stkLotSize = (textBoxStkLot.Text.Trim() != "") ? long.Parse(textBoxStkLot.Text) : 0;
                textBoxStkShares.Text = (CurrStock.LotSize * stkLotSize).ToString();
                QtyFilled = true;                
            }

            //if (PriceFilled == true && LotSizeFilled == true)
            //{
            //    this.buttonBuyMax.Enabled = true;
            //    this.buttonSellMax.Enabled = true;
            //}

            if (ConfirmWaiting == "B")
            {
                if (TradeDB.UserType == UserTypeEnum.AE)
                {
                    // only update sell side
                    textBoxPriceAsk.Text = TrackPriceAsk;
                    buttonChaseSell.Text = textBoxPriceAsk.Text + "\n" + GetResxString("Sell");
                }
                else
                {
                    if (textBoxPriceBid.Text.Trim() == "")
                    {
                        textBoxPriceBid.Text = TrackPriceBid;
                        buttonChaseBuy.Text = textBoxPriceBid.Text + "\n" + GetResxString("Buy");
                    }
                }
                
            }
            else if (ConfirmWaiting == "A")
            {
                if (TradeDB.UserType == UserTypeEnum.AE)
                {
                    textBoxPriceBid.Text = TrackPriceBid;
                    buttonChaseBuy.Text = textBoxPriceBid.Text + "\n" + GetResxString("Buy");
                }
                else
                {
                    if (textBoxPriceAsk.Text.Trim() == "")
                    {
                        textBoxPriceAsk.Text = TrackPriceAsk;
                        buttonChaseSell.Text = textBoxPriceAsk.Text + "\n" + GetResxString("Sell");
                    }
                }
            }
            else if (ConfirmWaiting == "")  // didn't click buy or sell button
            {
                if (TradeDB.UserType == UserTypeEnum.AE)
                {
                    textBoxPriceBid.Text = TrackPriceBid;
                    buttonChaseBuy.Text = textBoxPriceBid.Text + "\n" + GetResxString("Buy");
                    textBoxPriceAsk.Text = TrackPriceAsk;
                    buttonChaseSell.Text = textBoxPriceAsk.Text + "\n" + GetResxString("Sell");
                }
                else
                {
                    textBoxPriceBid.Text = "";
                    buttonChaseBuy.Text = textBoxPriceBid.Text + "\n" + GetResxString("Buy");
                    textBoxPriceAsk.Text = "";
                    buttonChaseSell.Text = textBoxPriceAsk.Text + "\n" + GetResxString("Sell");
                }
            }
        }


        private void ShowTradeStatus(Stock theStock)
        {
            if (theStock != null)
            {
                if (theStock.SuspensionFlag == "Y")
                {
                    labelCurrency.Text = GetResxString("TradeStatusSuspended");
                    labelCurrency.ForeColor = Color.Red;
                }
                else
                {
                    labelCurrency.Text = GetGeneralResxString("Currency_" + theStock.Currency);
                    labelCurrency.ForeColor = (theStock.Currency == null || theStock.Currency == "" || theStock.Currency == "HKD") ?
                        Color.FromKnownColor(KnownColor.ControlText) :
                        Color.Red;
                }
            }
            else
            {
                labelCurrency.Text = "";
                labelCurrency.ForeColor = Color.FromKnownColor(KnownColor.ControlText);
            }
        }


        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void SetTrackPrice(object sender)
        {
            switch (sender.GetType().ToString())
            {
                case "StockTerminal.Utils.MyTextBox":
                    TextBox t = (TextBox)sender;

                    TrackPriceBid = t.Text.Trim();
                    TrackPriceAsk = t.Text.Trim();
                    return;
                    
                case "System.Windows.Forms.CheckBox":
                    CheckBox c = (CheckBox)sender;
                    if (c.Checked == false)
                    {
                        textBoxChasePrice.Enabled = true;
                        textBoxChasePrice.Text = "1";
                        TrackPrice = textBoxChasePrice.Name.ToUpper();
                        return;
                    }
                    else
                    {
                        textBoxChasePrice.Enabled = false;
                        textBoxChasePrice.Text = "";
                        TrackPrice = c.Name.ToUpper().Trim();
                    }

                    foreach (Control ctrl in Controls)
                    {
                        if (ctrl.Name.ToUpper() != c.Name.ToUpper())
                        {
                            if (ctrl.Name.ToUpper().Contains("CHECKBOXBID") || ctrl.Name.ToUpper().Contains("CHECKBOXASK"))
                                (ctrl as CheckBox).Checked = false;
                        }
                    }                    
                    return;
            }
        }

        //private void SetTrackPrice1(object sender)
        //{
        //    CheckBox c = (CheckBox)sender;
        //    if (c.Checked == false)
        //    {
        //        textBoxChasePrice.Enabled = true;
        //        textBoxChasePrice.Text = "1";
        //        TrackPrice = textBoxChasePrice.Name.ToUpper();
        //        return;
        //    }
        //    else
        //    {
        //        textBoxChasePrice.Enabled = false;
        //        textBoxChasePrice.Text = "";
        //        TrackPrice = c.Name.ToUpper().Trim();                
        //    }

        //    foreach (Control ctrl in Controls)
        //    {
        //        if (ctrl.Name.ToUpper() != c.Name.ToUpper())
        //        {
        //            if (ctrl.Name.ToUpper().Contains("CHECKBOXBID") || ctrl.Name.ToUpper().Contains("CHECKBOXASK"))
        //                (ctrl as CheckBox).Checked = false;
        //        }
        //    }
        //}


        private void checkBoxBid1_Click(object sender, EventArgs e)
        {
            SetTrackPrice(sender);    
        }

        private void checkBoxBid2_Click(object sender, EventArgs e)
        {
            SetTrackPrice(sender);    
        }

        private void checkBoxBid3_Click(object sender, EventArgs e)
        {
            SetTrackPrice(sender);    
        }

        private void checkBoxBid4_Click(object sender, EventArgs e)
        {
            SetTrackPrice(sender);    
        }

        private void checkBoxBid5_Click(object sender, EventArgs e)
        {
            SetTrackPrice(sender);    
        }

        private void checkBoxAsk1_Click(object sender, EventArgs e)
        {
            SetTrackPrice(sender);    
        }

        private void checkBoxAsk2_Click(object sender, EventArgs e)
        {
            SetTrackPrice(sender);    
        }

        private void checkBoxAsk3_Click(object sender, EventArgs e)
        {
            SetTrackPrice(sender);    
        }

        private void checkBoxAsk4_Click(object sender, EventArgs e)
        {
            SetTrackPrice(sender);    
        }

        private void checkBoxAsk5_Click(object sender, EventArgs e)
        {
            SetTrackPrice(sender);    
        }

        private void textBoxStockCode_Leave(object sender, EventArgs e)
        {
            ListenedStockCode = textBoxStockCode.Text;
        }

        private void vScrollBarShares_Scroll(object sender, ScrollEventArgs e)
        {
            if (e.Type == ScrollEventType.EndScroll) return;

            decimal stkLot = 0;
            long stkShares = 0;

            if (labelLotSize.Text.Trim() == "") return;

            long stkLotSize = long.Parse(labelLotSize.Text);
            if (textBoxStkShares.Text != "")
                stkShares = Convert.ToInt64(textBoxStkShares.Text.Replace(",", ""));
            stkLot = Math.Floor((decimal)stkShares / (decimal)stkLotSize);

            //decimal InputQty = 0;
            //long stkLotSize = (labelLotSize.Text.Trim() != "") ? long.Parse(labelLotSize.Text) : 0;
            //if (textBoxStkShares.Text != "")
            //    InputQty = CheckLotSize(textBoxStkShares.Text);
            //if (InputQty < 0) return;

            if (e.Type == ScrollEventType.SmallDecrement)
                stkLot += 1;
            if (e.Type == ScrollEventType.SmallIncrement && stkLot > 1)
                stkLot -= 1;
            stkShares = stkLotSize * (long)stkLot;
            textBoxStkShares.Text = String.Format("{0:N0}", stkShares);
            textBoxStkLot.Text = String.Format("{0:N0}", stkLot);
            textBoxStkShares.SelectAll();   
        }

        private void vScrollBarLot_Scroll(object sender, ScrollEventArgs e)
        {
            if (e.Type == ScrollEventType.EndScroll) return;

            long stkLot = 0;
            long stkShares = 0;
            long stkLotSize = (labelLotSize.Text.Trim() != "") ? long.Parse(labelLotSize.Text) : 0;
            if (textBoxStkLot.Text != "")
                stkLot = Convert.ToInt64(textBoxStkLot.Text.Replace(",", ""));
            //if (textBoxStkShares.Text != "")
            //    stkShares = Convert.ToInt64(textBoxStkShares.Text.Replace(",", ""));
            if (e.Type == ScrollEventType.SmallDecrement)
                stkLot += 1;
            if (e.Type == ScrollEventType.SmallIncrement && stkLot > 1)
                stkLot -= 1;
            stkShares = stkLotSize * stkLot;
            textBoxStkShares.Text = String.Format("{0:N0}", stkShares);            
            textBoxStkLot.Text = String.Format("{0:N0}", stkLot);
            textBoxStkLot.SelectAll();            
        }


        private bool ChkOrderValue(char side)
        {
            bool Valid = true;
            
            // checkacc
            if (CurrAccount == null) { VisibleIcon(2, AccountNoComboBox1); return false; }
            if (CurrAccount.AccountNo.Trim() == "") { VisibleIcon(2, AccountNoComboBox1); return false; }           

            if (!CheckCodeValue()) Valid = false;
            if (!CheckQtyValue()) Valid = false;
            //if (!CheckPriceValue()) Valid = false;

            if (AccountNoComboBox1.Text.Trim() != "123")
            {
                if (!CheckIsBlockAcc(side))
                    Valid = false;
                else
                    if (CurrStock != null)
                        CheckIsDiscretion();
            }

            if (Valid == false) return false;

            //set valid
            VisibleIcon(1, textBoxStockCode);            
            VisibleIcon(1, textBoxStkShares);

            //VisibleIcon(1, textBoxPrice);

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
            if (stkCode.ToString() != CurrStock.Code) { VisibleIcon(2, textBoxStockCode); return false; }

            return true;
        }

        private bool CheckQtyValue()
        {
            int stkQty;
            //qty
            int.TryParse(textBoxStkShares.Text.Replace(",", ""), out stkQty);
            if (stkQty <= 0) { VisibleIcon(2, textBoxStkShares); return false; }

            // check stock
            if (CurrStock == null) return false;
            if (CurrStock.LotSize <= 0) return false;
            if ((stkQty % CurrStock.LotSize) > 0)
            {
                VisibleIcon(2, textBoxStkShares);
                return false;
            }
            else
                VisibleIcon(1, textBoxStkShares);

            return true;
        }

        private bool CheckPriceValue()
        {
            ////price
            //decimal stkPrice;
            //decimal.TryParse(textBoxPrice.Text, out stkPrice);
            //if (stkPrice <= 0 && checkBoxAuction.Checked == false) { VisibleIcon(2, textBoxPrice); return false; }

            //// check stock
            //if (CurrStock == null) return false;
            //if (CurrStock.LotSize <= 0) return false;
            //bool CanBuy = false, CanSell = false;
            //if (checkBoxAuction.Checked == true)
            //    stkPrice = Utils.OrderTypeSpread.CheckSpread(stkPrice, 'A', ' ', 0, CurrStock, TradeDB, out CanBuy, out CanSell);
            //else
            //{
            //    stkPrice = Utils.OrderTypeSpread.CheckSpread(stkPrice, 'X', ' ', 0, CurrStock, TradeDB, out CanBuy, out CanSell);
            //    if (stkPrice < 0)
            //    {
            //        VisibleIcon(2, textBoxPrice);
            //        return false;
            //    }
            //    else
            //        VisibleIcon(1, textBoxPrice);
            //}

            return true;
        }


        // 0 = reset, 1 = true, 2 = false
        private void VisibleIcon(int isValid, Control t)
        {
            if (isValid == 1)
            {
                //if (t.Name.ToUpper().Trim() == "TEXTBOXSTOCKPRICE")
                //{
                //    pictureBoxStockPriceTick.Visible = true;
                //    pictureBoxStockPriceCross.Visible = false;
                //}
                if (t.Name.ToUpper().Trim() == "TEXTBOXSTKSHARES")
                {
                    pictureBoxStockQtyTick.Visible = true;
                    pictureBoxStockQtyCross.Visible = false;
                }
                else if (t.Name.ToUpper().Trim() == "TEXTBOXSTOCKCODE")
                {
                    pictureBoxStockCodeTick.Visible = true;
                    pictureBoxStockCodeCross.Visible = false;
                }
                //else if (t.Name.ToUpper().Trim() == "COMBOBOXACCOUNTNO")
                else if (t.Name.ToUpper().Trim() == "ACCOUNTNOCOMBOBOX1")
                {
                    pictureBoxAccountTick.Visible = true;
                    pictureBoxAccountCross.Visible = false;
                }
            }
            else if (isValid == 2)
            {
                //if (t.Name.ToUpper().Trim() == "TEXTBOXSTOCKPRICE")
                //{
                //    pictureBoxStockPriceTick.Visible = false;
                //    pictureBoxStockPriceCross.Visible = true;
                //}
                if (t.Name.ToUpper().Trim() == "TEXTBOXSTKSHARES")
                {
                    pictureBoxStockQtyTick.Visible = false;
                    pictureBoxStockQtyCross.Visible = true;
                }
                else if (t.Name.ToUpper().Trim() == "TEXTBOXSTOCKCODE")
                {
                    pictureBoxStockCodeTick.Visible = false;
                    pictureBoxStockCodeCross.Visible = true;
                }
                //else if (t.Name.ToUpper().Trim() == "COMBOBOXACCOUNTNO")
                else if (t.Name.ToUpper().Trim() == "ACCOUNTNOCOMBOBOX1")
                {
                    pictureBoxAccountTick.Visible = false;
                    pictureBoxAccountCross.Visible = true;
                }
            }
            else
            {
                //if (t.Name.ToUpper().Trim() == "TEXTBOXSTOCKPRICE")
                //{
                //    pictureBoxStockPriceTick.Visible = false;
                //    pictureBoxStockPriceCross.Visible = false;
                //}
                if (t.Name.ToUpper().Trim() == "TEXTBOXSTKSHARES")
                {
                    pictureBoxStockQtyTick.Visible = false;
                    pictureBoxStockQtyCross.Visible = false;
                }
                else if (t.Name.ToUpper().Trim() == "TEXTBOXSTOCKCODE")
                {
                    pictureBoxStockCodeTick.Visible = false;
                    pictureBoxStockCodeCross.Visible = false;
                }
                //else if (t.Name.ToUpper().Trim() == "COMBOBOXACCOUNTNO")
                else if (t.Name.ToUpper().Trim() == "ACCOUNTNOCOMBOBOX1")
                {
                    pictureBoxAccountTick.Visible = false;
                    pictureBoxAccountCross.Visible = false;
                }
            }
        }


        private void PrepareOrder(char BuySell)
        {
            Order order1 = new Order();
            order1.AccountNo = CurrAccount.AccountNo.ToUpper().Trim();
            order1.StockCode = textBoxStockCode.Text.Trim();
            order1.Side = BuySell;
            if (BuySell == 'B')
                decimal.TryParse(textBoxPriceBid.Text, out order1.Price);
            else if (BuySell == 'A')
                decimal.TryParse(textBoxPriceAsk.Text, out order1.Price);
            else
                return;

            int.TryParse(this.textBoxStkShares.Text.Replace(",", ""), out order1.Quantity);
            //if (checkBoxAuction.Checked == true)
            //    order1.OrderType = 'A';
            //else
                //order1.OrderType = 'X';

            if (checkBoxSpecialLimit.Checked == true)
            {
                order1.OrderType = 'S';  //order1.OrderType = 'X';
            }
            else
            {
                order1.OrderType = 'X';
            }

            bool shortonhand = false; //(checkBoxStockOnHand.Checked) ? true : false;
            bool allornothing = (checkBoxAllOrNothing.Checked) ? true : false;

            string Message;
            string actRef;
            bool reply = TradeDB.OrderPlace(order1.AccountNo, order1.Side, order1.StockCode, order1.Price, order1.Quantity, order1.OrderType,
                allornothing, shortonhand, 0, ExchangeTypeEnum.HKG, DPGW, false, null, out Message, out actRef);
            DPGW = '\0';
            if (reply == false)
            {
                ShowMessageBox(GetResxString("PlaceOrderError"), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                //(GetFormByFormType(typeof(MainForm))[0] as MainForm).RequestToLogout();
            }
        }

        private decimal GetTrackerBoxPrice(string side)
        {
            int TrackSpread = Convert.ToInt32(textBoxChasePrice.Text.Replace(",", ""));
            decimal NewPrice = 0;           
            
            if (TrackSpread < 0) return -1;

            bool CanBuy = false, CanSell = false;
            if (side.ToUpper().Trim() == "B")
                NewPrice = Utils.OrderTypeSpread.CheckSpread(CurrStock.Ask, 'X', ' ', TrackSpread, CurrStock, TradeDB, out CanBuy, out CanSell);
            else if (side.ToUpper().Trim() == "S")
                NewPrice = Utils.OrderTypeSpread.CheckSpread(CurrStock.Bid, 'X', ' ', -TrackSpread, CurrStock, TradeDB, out CanBuy, out CanSell);

            if (CanBuy || CanSell)
                textBoxPrice.Text = String.Format("{0:N3}", NewPrice);
            else
                textBoxPrice.Text = "";

            return NewPrice;
        }


        private void textBoxChasePrice_TextChanged(object sender, EventArgs e)
        {
            SetTrackPrice(sender);
        }

        private void textBoxStkLot_TextChanged(object sender, EventArgs e)
        {

        }

        private void checkBoxBid2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBoxBid3_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void buttonLayout_Click(object sender, EventArgs e)
        {
            OrderTicketForm orderTicketForm = new OrderTicketForm(this.Culture, null, this.dockPanelMain, ExchangeTypeEnum.HKG, null);
            if (this.Pane.FloatWindow != null)
                orderTicketForm.Show(dockPanelMain, DockState.Float, new Rectangle(this.Pane.FloatWindow.Bounds.X, this.Pane.FloatWindow.Bounds.Y, orderTicketForm.Width, orderTicketForm.Height));
            else
                orderTicketForm.Show(dockPanelMain, DockState.Float, new Rectangle(200, 200, orderTicketForm.Width, orderTicketForm.Height));
            this.IsProgramClosing = true;
            this.Close();
            this.Dispose();
        }

        private void checkBox1_CheckedChanged_1(object sender, EventArgs e)
        {

        }

        private void myGroupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void buttonResetBuy_Click(object sender, EventArgs e)
        {
            ConfirmWaiting = "";
        }

        private void buttonResetSell_Click(object sender, EventArgs e)
        {
            ConfirmWaiting = "";
        }

        private bool ChkBalanceQty(char Side)
        {
            // check Balance           
            if (CurrStock == null) { VisibleIcon(0, textBoxStkShares); return false; }

            if (CurrStock.LotSize <= 0) { VisibleIcon(0, textBoxStkShares); return false; }

            decimal stkPrice;
            if (Side == 'A')
            {
                decimal.TryParse(textBoxPriceAsk.Text, out stkPrice);
                if (stkPrice <= 0) { return false; }
            }
            else
            {
                //if (TradeDB.UserType == UserTypeEnum.AE)
                    decimal.TryParse(textBoxPriceBid.Text, out stkPrice);
                //else
                //    decimal.TryParse(TrackPriceBid, out stkPrice);
                
                if (stkPrice <= 0) { return false; }
            }
            //if (stkPrice <= 0) { VisibleIcon(2, textBoxStockPrice); return false; }

            decimal stkQty;
            decimal.TryParse(textBoxStkShares.Text, out stkQty);
            if (stkQty <= 0) { VisibleIcon(2, textBoxStkShares); return false; }
            int stkCode = 0, acStkCode;
            int.TryParse(textBoxStockCode.Text, out stkCode);
            if (stkCode <= 0) { VisibleIcon(2, textBoxStockCode); return false; }
            if (CurrAccount == null) { VisibleIcon(2, comboBoxAccountNo); return false; }

            if (Side == 'B')
            {
                //AE don't check for credit
                if (TradeDB.UserType == UserTypeEnum.AE)
                {
                    //set valid
                    VisibleIcon(1, textBoxStockCode);
                    //VisibleIcon(1, textBoxStockPrice);
                    VisibleIcon(1, textBoxStkShares);
                    return true;
                }

                //decimal BuyPower = CurrAccount.T2DayBal + CurrAccount.FundHold + CurrAccount.Interest;
                decimal BuyPower = 0; //CurrAccount.T2DayBal + CurrAccount.FundHold + CurrAccount.Interest;
                if (CurrAccount.Balances.ContainsKey(CurrStock.Currency))
                    BuyPower = CurrAccount.Balances[CurrStock.Currency].T2DayBal + CurrAccount.Balances[CurrStock.Currency].FundHold + CurrAccount.Balances[CurrStock.Currency].Interest;


                if (BuyPower <= 0) { VisibleIcon(2, textBoxStkShares); return false; }
                if (checkBoxAuction.Checked == false)
                {
                    if (BuyPower < (stkPrice * stkQty)) { VisibleIcon(2, textBoxStkShares); return false; }
                }
                else
                {
                    if (BuyPower < (CurrStock.Nominal * stkQty)) { VisibleIcon(2, textBoxStkShares); return false; }
                }

                //set valid
                VisibleIcon(1, textBoxStockCode);
                //VisibleIcon(1, textBoxStockPrice);
                VisibleIcon(1, textBoxStkShares);

                return true;
            }
            else if (Side == 'A')
            {

                //check net qty
                decimal QtyRemains = 0;
                decimal MaxSellQty = 0;

                foreach (KeyValuePair<string, AccountStock> kvp in CurrAccount.Stocks)
                {
                    if (kvp.Value != null && int.TryParse(kvp.Value.Code, out acStkCode))
                    {
                        if (stkCode == acStkCode)
                        {
                            QtyRemains = kvp.Value.QtyOnHand + kvp.Value.QtyInTransit;
                            break;
                        }
                    }
                }

                MaxSellQty = Math.Truncate(((decimal)QtyRemains / CurrStock.LotSize)) * CurrStock.LotSize;
                if (MaxSellQty >= stkQty)
                {
                    VisibleIcon(1, textBoxStkShares);
                    return true;
                }
                else
                {
                    VisibleIcon(2, textBoxStkShares);
                    return false;
                }

            }
            else
                return false;

        }

        #region Account No.

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
                if (CurrAccount.IsNoBuy) { return false; }
                if (CurrAccount.IsNoSell) { return false; }
            }

            return true;
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

        private void SetAccountForm()
        {
            // account # clicked, pass to account form to select the account
            List<BaseForm> AccountBookFormList = BaseForm.GetFormByFormType(typeof(AccountForm));
            if (AutoFillAccNo && AllowChangeAccountFormAccount && AccountBookFormList != null && AccountBookFormList.Count > 0 && AccountBookFormList[0] != null && TradeDB.UserType == UserTypeEnum.AE)
            {
                AccountForm accountBookForm = ((AccountForm)AccountBookFormList[0]);
                accountBookForm.AllowChangeOrderTicketAccount = false;
                accountBookForm.ListenedAccount = ListenedAccount;
                if (textBoxStockCode.Text.Trim() != "")
                    accountBookForm.ShowStock(Stock.GetSignature(ExchangeTypeEnum.HKG, textBoxStockCode.Text));
            }
            List<BaseForm> OrderBookFormList = BaseForm.GetFormByFormType(typeof(OrderBookForm));
            if (AllowChangeAccountFormAccount && OrderBookFormList != null && OrderBookFormList.Count > 0 && OrderBookFormList[0] != null &&
                ((OrderBookForm)OrderBookFormList[0]).SearchEnabled && TradeDB.UserType == UserTypeEnum.AE)
            {
                ((OrderBookForm)OrderBookFormList[0]).SetSearchAccount(ListenedAccount);
            }
        }

        private void textBoxStkShares_TextChanged(object sender, EventArgs e)
        {
            CheckQtyValue();
        }

        private void comboBoxAccountNo_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBoxAccountNo_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (comboBoxAccountNo.SelectedItem == null)
                ListenedAccount = null;
            else
            {
                this.AllowChangeAccountFormAccount = true;
                ListenedAccount = comboBoxAccountNo.SelectedItem.ToString();
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
        }

        private void comboBoxAccountNo_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }

        private void comboBoxAccountNo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string accountNo = comboBoxAccountNo.Text.ToUpper().Trim();
                if (comboBoxAccountNo.Items.Contains(accountNo) && ListenedAccount != accountNo)
                {
                    this.AllowChangeAccountFormAccount = true;
                    ListenedAccount = accountNo;
                }
                if (textBoxStockCode.Enabled)
                    textBoxStockCode.Focus();
                else if (textBoxStkLot.Enabled)
                    textBoxStkLot.Focus();
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
                e.KeyChar = (char)(e.KeyChar & 223);
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

        private void comboBoxAccountNo_Leave(object sender, EventArgs e)
        {
            string accountNo = comboBoxAccountNo.Text.ToUpper().Trim();
            if (comboBoxAccountNo.Items.Contains(accountNo) && ListenedAccount != accountNo)
            {
                ListenedAccount = accountNo;
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
            if (AccountNoComboBox1.Items.Contains(accountNo) && ListenedAccount != accountNo)
            {
                ListenedAccount = accountNo;
            }
        }

        private void AccountNoComboBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar >= 97 && e.KeyChar <= 122)
            {
                e.KeyChar = (char)(e.KeyChar & 223);
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

        private void AccountNoComboBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string accountNo = AccountNoComboBox1.Text.ToUpper().Trim();
                if (AccountNoComboBox1.Items.Contains(accountNo) && ListenedAccount != accountNo)
                {
                    this.AllowChangeAccountFormAccount = true;
                    ListenedAccount = accountNo;
                }
                if (textBoxStockCode.Enabled)
                    textBoxStockCode.Focus();
                else if (textBoxStkLot.Enabled)
                    textBoxStkLot.Focus();
            }
            else if (e.KeyCode == Keys.Delete)
            {
                ListenedAccount = null;
            }
        }

        #endregion

        private void FastOrderTicketForm_Activated(object sender, EventArgs e)
        {
            if (ListenedAccount != "" && ListenedAccount != null) SetAccountForm();
        }

        public void SetVisibleSpecialType(bool IsVisible)
        {
            booVisibleSpecialType = IsVisible;
            if (IsVisible == true)
            {
                checkBoxSpecialLimit.Visible = true;
                checkBoxAllOrNothing.Visible = true;
            }
            else
            {
                checkBoxSpecialLimit.Checked = false;
                checkBoxSpecialLimit.Visible = false;
                checkBoxAllOrNothing.Checked = false;
                checkBoxAllOrNothing.Visible = false;
            }
        }

        private void SetPriceRange(decimal stkPrice)
        {
            decimal BidRange = 0;
            decimal AskRange = 0;
            char OrderTypeNow = ' ';
            if (Utils.OrderTypeSpread.GetSpreadRange(stkPrice, 'X', CurrStock, TradeDB, out OrderTypeNow, out BidRange, out AskRange) > 0)
            {

            }

            //allow uncheck Checkorderprice
            if (OrderTypeNow == 'I' || OrderTypeNow == 'N')
            {                
                //////checkBoxAllOrNothing.Enabled = false;
                //////checkBoxSpecialLimit.Enabled = false;
                //////checkBoxAllOrNothing.Checked = false;
                //////checkBoxSpecialLimit.Checked = false;
            }
            else
            {
                //////checkBoxAllOrNothing.Enabled = true;
                //////checkBoxSpecialLimit.Enabled = true;
            }
        }

        private void checkBoxSpecialLimit_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxSpecialLimit.Checked == true)
                checkBoxAllOrNothing.Checked = false;
        }

        private void checkBoxAllOrNothing_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxAllOrNothing.Checked == true)
                checkBoxSpecialLimit.Checked = false;
        }
    }
}
