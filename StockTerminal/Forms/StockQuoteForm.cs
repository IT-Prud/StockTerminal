using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using TradeDB;
using StockTerminal.Utils;

namespace StockTerminal.Forms
{
    public partial class StockQuoteForm : StockTerminal.Forms.BaseForm
    {
        private EventHandler pHandlerCBSZE;
        private EventHandler pHandlerCBSHG;
        private EventHandler pHandlerCBGray;

        private bool DisableHKG = false, DisableSZE = false, DisableSHG = false, DisableGray = false;
        //private Stock theStock = null;
        private bool enterKeyPressed = false;
        //private bool IsStratup = true;
        private Stock CurrStock = null;
        private string oldOrderTicketInstanceID = "";

        private string pListenedStockCode = null;

        ContextMenuStrip mnuBuy = new ContextMenuStrip();
        ContextMenuStrip mnuSell = new ContextMenuStrip();
        ToolStripMenuItem mnuBuyRightClick = new ToolStripMenuItem("Buy");
        ToolStripMenuItem mnuSellRightClick = new ToolStripMenuItem("Sell");
        
        Order OneClickOrder = new Order();

        private Account CurrAccount = null;

        private ExchangeTypeEnum FormExType = ExchangeTypeEnum.HKG;

        //private int pTestState = 0;

        private string ListenedStockCode
        {
            get
            {
                return pListenedStockCode;
            }

            set
            {
                string code = value != null ? value.Trim().TrimStart('0') : "";
                //if (code != (pListenedStockCode != null ? pListenedStockCode : ""))
                {
                    ClearForm();
                    CurrStock = null;

                    if (code.Length > 0)
                    {
                        /*
                        if (checkBoxSHG.Checked)
                            pListenedExType = ExchangeTypeEnum.SHG;
                        else if (checkBoxSZE.Checked)
                            pListenedExType = ExchangeTypeEnum.SZE;
                        else if (!checkBoxGray.Checked)
                            pListenedExType = ExchangeTypeEnum.HKG;

                        //if (!checkBoxPMHKG.Checked)
                            //pListenedExType = ExchangeTypeEnum.PMHKG;
                         */

                        pListenedStockCode = int.Parse(code).ToString();

                        ListenStock(new List<string> { Stock.GetSignature(FormExType, pListenedStockCode) });

                        /*
                        // for testing
                        switch (pTestState)
                        {
                            case 0:
                                GetStock(new List<string> { Stock.GetSignature(pListenedExType, "5") });
                                pTestState++;
                                break;

                            case 1:
                                pTestState++;
                                ListenStock(new List<string> { Stock.GetSignature(pListenedExType, "5") }, true);
                                break;

                            case 2:
                                pTestState++;
                                GetStock(new List<string> { Stock.GetSignature(pListenedExType, "5") });
                                break;
                        }
                         */
                    }
                    else
                    {
                        if (pListenedStockCode != null && pListenedStockCode.Length > 0 && FormExType != ExchangeTypeEnum.Unassigned)
                        {
                            UnListenStock(new List<string> { Stock.GetSignature(FormExType, pListenedStockCode) });
                        }

                        pListenedStockCode = "";
                    }
                    textBoxCode.Text = pListenedStockCode;
                }
            }
        }

        private string pListenedAccount = null;
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
                    //////if (buttonAccno.Text != null)
                    //////    buttonAccno.Text = value;
                    buttonAccno.Text = "";
                    CurrAccount = null;
                    //////labelAccountName.Text = "";
                    //////labelAccountName.ForeColor = Color.Black;
                    //FillAccount(null);

                    if (pListenedAccount != null && pListenedAccount.Length > 0)
                    {
                        GetAccount(new List<string> { pListenedAccount });
                        //////SetAccountForm();
                    }
                }
            }
        }

        public void SetStock(ExchangeTypeEnum ExType, string StockCode)
        {
            if (ListenedStockCode != StockCode || ExType != ExchangeTypeEnum.Gray || FormExType != ExchangeTypeEnum.FTHKG && FormExType != ExchangeTypeEnum.PMHKG)
                FormExType = ExType;

            ExchangeCheckBoxChanged(null, null);
            ListenedStockCode = StockCode;
            LocalFormSettings["StockCode"] = ListenedStockCode;
        }

        protected override void OnAccount(Account TheAccount)
        {
            if (TheAccount != null && TheAccount.AccountNo == ListenedAccount)
            {
                CurrAccount = TheAccount;
                buttonAccno.Text = CurrAccount.AccountNo;

                if (checkBoxBuyAllCredit.Checked == true) { SetAllin(); }

                //////labelAccountName.Text = CurrAccount.AccountName;
                //////CheckIsBlockAcc(' ');
                //////VisibleIcon(1, AccountNoComboBox1);
            }
        }

        protected override void OnAccountList(List<string> AccountList)
        {
            if (AccountList == null || AccountList.Count <= 0)
                return;

            //////if (timerFillCombo.Enabled)
            //////    timerFillCombo.Enabled = false;

            //////lock (tempAccountListMutex)
            //////{
            //////    tempAccountList.AddRange(AccountList);
            //////}
            //////AccountNoComboBox1.SelectedItem = AccountNoComboBox1.Text;
            //////timerFillCombo.Interval = 1;
            //////timerFillCombo.Enabled = true;
        }

        /*
        public StockQuoteForm() : this(null, null)
        {
            Utils.Utils.EnableDoubleBuffered(labelValueHigh);
            Utils.Utils.EnableDoubleBuffered(labelValueLow);
            Utils.Utils.EnableDoubleBuffered(labelValuePrevious);
            Utils.Utils.EnableDoubleBuffered(labelValueNominal);
            Utils.Utils.EnableDoubleBuffered(labelValueChange);
            Utils.Utils.EnableDoubleBuffered(labelValueChangePC);
            Utils.Utils.EnableDoubleBuffered(labelValueVolume);
            Utils.Utils.EnableDoubleBuffered(labelValueTurnover);
            Utils.Utils.EnableDoubleBuffered(labelValueLotSize);

            Utils.Utils.EnableDoubleBuffered(labelValueTickerTime0);    Utils.Utils.EnableDoubleBuffered(labelValueTickerRemark0);
            Utils.Utils.EnableDoubleBuffered(labelValueTickerQty0);     Utils.Utils.EnableDoubleBuffered(labelValueTickerPrice0);

            Utils.Utils.EnableDoubleBuffered(labelValueTickerTime1);    Utils.Utils.EnableDoubleBuffered(labelValueTickerRemark1);
            Utils.Utils.EnableDoubleBuffered(labelValueTickerQty1);     Utils.Utils.EnableDoubleBuffered(labelValueTickerPrice1);

            Utils.Utils.EnableDoubleBuffered(labelValueTickerTime2);    Utils.Utils.EnableDoubleBuffered(labelValueTickerRemark2);
            Utils.Utils.EnableDoubleBuffered(labelValueTickerQty2);     Utils.Utils.EnableDoubleBuffered(labelValueTickerPrice2);

            Utils.Utils.EnableDoubleBuffered(labelValueTickerTime3);    Utils.Utils.EnableDoubleBuffered(labelValueTickerRemark3);
            Utils.Utils.EnableDoubleBuffered(labelValueTickerQty3);     Utils.Utils.EnableDoubleBuffered(labelValueTickerPrice3);

            Utils.Utils.EnableDoubleBuffered(labelValueTradeStatus);

            Utils.Utils.EnableDoubleBuffered(labelBid);         Utils.Utils.EnableDoubleBuffered(labelAsk);
            Utils.Utils.EnableDoubleBuffered(labelValueBid);    Utils.Utils.EnableDoubleBuffered(labelValueAsk);

            Utils.Utils.EnableDoubleBuffered(labelBidVol0); Utils.Utils.EnableDoubleBuffered(labelBidCount0);
            Utils.Utils.EnableDoubleBuffered(labelBidVol1); Utils.Utils.EnableDoubleBuffered(labelBidCount1);
            Utils.Utils.EnableDoubleBuffered(labelBidVol2); Utils.Utils.EnableDoubleBuffered(labelBidCount2);
            Utils.Utils.EnableDoubleBuffered(labelBidVol3); Utils.Utils.EnableDoubleBuffered(labelBidCount3);
            Utils.Utils.EnableDoubleBuffered(labelBidVol4); Utils.Utils.EnableDoubleBuffered(labelBidCount4);
            Utils.Utils.EnableDoubleBuffered(labelBidVol5); Utils.Utils.EnableDoubleBuffered(labelBidCount5);
            Utils.Utils.EnableDoubleBuffered(labelBidVol6); Utils.Utils.EnableDoubleBuffered(labelBidCount6);
            Utils.Utils.EnableDoubleBuffered(labelBidVol7); Utils.Utils.EnableDoubleBuffered(labelBidCount7);
            Utils.Utils.EnableDoubleBuffered(labelBidVol8); Utils.Utils.EnableDoubleBuffered(labelBidCount8);
            Utils.Utils.EnableDoubleBuffered(labelBidVol9); Utils.Utils.EnableDoubleBuffered(labelBidCount9);

            Utils.Utils.EnableDoubleBuffered(labelAskVol0); Utils.Utils.EnableDoubleBuffered(labelAskCount0);
            Utils.Utils.EnableDoubleBuffered(labelAskVol1); Utils.Utils.EnableDoubleBuffered(labelAskCount1);
            Utils.Utils.EnableDoubleBuffered(labelAskVol2); Utils.Utils.EnableDoubleBuffered(labelAskCount2);
            Utils.Utils.EnableDoubleBuffered(labelAskVol3); Utils.Utils.EnableDoubleBuffered(labelAskCount3);
            Utils.Utils.EnableDoubleBuffered(labelAskVol4); Utils.Utils.EnableDoubleBuffered(labelAskCount4);
            Utils.Utils.EnableDoubleBuffered(labelAskVol5); Utils.Utils.EnableDoubleBuffered(labelAskCount5);
            Utils.Utils.EnableDoubleBuffered(labelAskVol6); Utils.Utils.EnableDoubleBuffered(labelAskCount6);
            Utils.Utils.EnableDoubleBuffered(labelAskVol7); Utils.Utils.EnableDoubleBuffered(labelAskCount7);
            Utils.Utils.EnableDoubleBuffered(labelAskVol8); Utils.Utils.EnableDoubleBuffered(labelAskCount8);
            Utils.Utils.EnableDoubleBuffered(labelAskVol9); Utils.Utils.EnableDoubleBuffered(labelAskCount9);

            Utils.Utils.EnableDoubleBuffered(labelValueBidBQ0); Utils.Utils.EnableDoubleBuffered(labelValueBidBQ1);
            Utils.Utils.EnableDoubleBuffered(labelValueBidBQ2); Utils.Utils.EnableDoubleBuffered(labelValueBidBQ3);

            Utils.Utils.EnableDoubleBuffered(labelValueAskBQ0); Utils.Utils.EnableDoubleBuffered(labelValueAskBQ1);
            Utils.Utils.EnableDoubleBuffered(labelValueAskBQ2); Utils.Utils.EnableDoubleBuffered(labelValueAskBQ3);
        }
         */

        public StockQuoteForm(CultureInfo Culture, string PersistString, ExchangeTypeEnum FormExType) : base(Culture, PersistString)
        {
            InitializeComponent();
            this.Enter += new System.EventHandler(this.SQForm_Enter);
            ListenFormListChange();

            checkBoxSZE.CheckedChanged += pHandlerCBSZE = new System.EventHandler(checkBoxSZE_CheckedChanged);
            checkBoxSHG.CheckedChanged += pHandlerCBSHG = new System.EventHandler(checkBoxSHG_CheckedChanged);
            checkBoxGray.CheckedChanged += pHandlerCBGray = new System.EventHandler(checkBoxGray_CheckedChanged);

            mnuBuyRightClick.BackColor = Color.LightPink;
            //////mnuBuyRightClick.ForeColor = Color.White;            
            mnuBuyRightClick.Font = new Font(this.Font, FontStyle.Bold);
            mnuBuyRightClick.Click += new EventHandler(mnuBuyRightClick_Click);
            mnuSellRightClick.BackColor = Color.SkyBlue;
            //////mnuSellRightClick.ForeColor = Color.White;
            mnuSellRightClick.Font = new Font(this.Font, FontStyle.Bold);
            mnuSellRightClick.Click += new EventHandler(mnuSellRightClick_Click);

            //Add to main context menu
            mnuBuy.Items.AddRange(new ToolStripItem[] { mnuBuyRightClick });
            mnuSell.Items.AddRange(new ToolStripItem[] { mnuSellRightClick });
            //Assign to datagridview
            labelBidVol0.ContextMenuStrip = mnuBuy;
            labelBidVol1.ContextMenuStrip = mnuBuy;
            labelBidVol2.ContextMenuStrip = mnuBuy;
            labelBidVol3.ContextMenuStrip = mnuBuy;
            labelBidVol4.ContextMenuStrip = mnuBuy;
            labelBidVol5.ContextMenuStrip = mnuBuy;
            labelBidVol6.ContextMenuStrip = mnuBuy;
            labelBidVol7.ContextMenuStrip = mnuBuy;
            labelBidVol8.ContextMenuStrip = mnuBuy;
            labelBidVol9.ContextMenuStrip = mnuBuy;
            labelAskVol0.ContextMenuStrip = mnuSell;
            labelAskVol1.ContextMenuStrip = mnuSell;
            labelAskVol2.ContextMenuStrip = mnuSell;
            labelAskVol3.ContextMenuStrip = mnuSell;
            labelAskVol4.ContextMenuStrip = mnuSell;
            labelAskVol5.ContextMenuStrip = mnuSell;
            labelAskVol6.ContextMenuStrip = mnuSell;
            labelAskVol7.ContextMenuStrip = mnuSell;
            labelAskVol8.ContextMenuStrip = mnuSell;
            labelAskVol9.ContextMenuStrip = mnuSell;
            labelBidCount0.ContextMenuStrip = mnuBuy;
            labelBidCount1.ContextMenuStrip = mnuBuy;
            labelBidCount2.ContextMenuStrip = mnuBuy;
            labelBidCount3.ContextMenuStrip = mnuBuy;
            labelBidCount4.ContextMenuStrip = mnuBuy;
            labelBidCount5.ContextMenuStrip = mnuBuy;
            labelBidCount6.ContextMenuStrip = mnuBuy;
            labelBidCount7.ContextMenuStrip = mnuBuy;
            labelBidCount8.ContextMenuStrip = mnuBuy;
            labelBidCount9.ContextMenuStrip = mnuBuy;
            labelAskCount0.ContextMenuStrip = mnuSell;
            labelAskCount1.ContextMenuStrip = mnuSell;
            labelAskCount2.ContextMenuStrip = mnuSell;
            labelAskCount3.ContextMenuStrip = mnuSell;
            labelAskCount4.ContextMenuStrip = mnuSell;
            labelAskCount5.ContextMenuStrip = mnuSell;
            labelAskCount6.ContextMenuStrip = mnuSell;
            labelAskCount7.ContextMenuStrip = mnuSell;
            labelAskCount8.ContextMenuStrip = mnuSell;
            labelAskCount9.ContextMenuStrip = mnuSell;

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
        }

        protected void SQForm_Enter(object sender, EventArgs e)
        {
            textBoxCode.Focus();
        }

        private void textBoxCode_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (FormExType == ExchangeTypeEnum.PMHKG || FormExType == ExchangeTypeEnum.FTHKG)
                    FormExType = ExchangeTypeEnum.Gray;

                ListenedStockCode = textBoxCode.Text.Trim();
                LocalFormSettings["StockCode"] = ListenedStockCode;
                PassToOrderTicket(false);
                textBoxCode.SelectAll();
                enterKeyPressed = true;
                
                List<BaseForm> AccountBookFormList = BaseForm.GetFormByFormType(typeof(AccountForm));
                if (AccountBookFormList != null && AccountBookFormList.Count > 0 && AccountBookFormList[0] != null && ((AccountForm)AccountBookFormList[0]).IsAccountComboEnabled())
                {
                    AccountForm accForm = ((AccountForm)AccountBookFormList[0]);
                    accForm.ShowStock(Stock.GetSignature(FormExType, pListenedStockCode));
                }
                e.Handled = true;
            }
            else if ((e.KeyChar < 48 || e.KeyChar > 57) && e.KeyChar != 8)
            {
                e.Handled = true;
                enterKeyPressed = false;
            }
            else
            {
                enterKeyPressed = false;
            }
        }

        public string GetOrderTicketInstanceID()
        {
            if (comboBoxPlaceOrderTo == null || comboBoxPlaceOrderTo.SelectedIndex < 0)
                return "";
            return ((ComboBoxItem)comboBoxPlaceOrderTo.Items[comboBoxPlaceOrderTo.SelectedIndex]).Value;
        }

        protected override void OnStock(Stock TheStock)
        {
            if (TheStock != null)
            {
                string code = ListenedStockCode != null ? ListenedStockCode : "";
                ExchangeTypeEnum exType;

                if (TheStock.Code.TrimStart('0') == code &&
                    (TheStock.ExchangeType == FormExType || TheStock.ExchangeType == ExchangeTypeEnum.Unassigned && FormExType == ExchangeTypeEnum.Gray))
                {
                    if (TheStock.StockSignatureAlternative != null)
                    {
                        if (FormExType == ExchangeTypeEnum.Gray && (TheStock.ExchangeType == ExchangeTypeEnum.Unassigned || TheStock.ExchangeType == ExchangeTypeEnum.HKG || TheStock.ExchangeType == ExchangeTypeEnum.FTHKG || TheStock.ExchangeType == ExchangeTypeEnum.PMHKG) ||
                            TheStock.ExchangeType == FormExType)
                        {
                            string[] signatureArr = TheStock.StockSignatureAlternative.Split('|');
                            string altExCode, mainExCode, exCode, stkCode;

                            for (int i = 0; i < signatureArr.Length; i++)
                            {
                                Stock.GetExchangeCodeStockCodeShort(signatureArr[i], out exCode, out stkCode);
                                exType = Stock.GetExchangeType(exCode);

                                if (exType != ExchangeTypeEnum.Unassigned && exType == FormExType)
                                {
                                    ListenedStockCode = stkCode;

                                    LocalFormSettings["StockCode"] = stkCode;
                                    PassToOrderTicket(false);

                                    /* Remark because OnStock is not a direct user trigger and should not propagate to another form.
                                    List<BaseForm> AccountBookFormList = BaseForm.GetFormByFormType(typeof(AccountForm));
                                    if (AccountBookFormList != null && AccountBookFormList.Count > 0 && AccountBookFormList[0] != null && ((AccountForm)AccountBookFormList[0]).IsAccountComboEnabled())
                                    {
                                        AccountForm accForm = ((AccountForm)AccountBookFormList[0]);
                                        accForm.ShowStock(Stock.GetSignature(FormExType, pListenedStockCode));
                                    }
                                     */

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

                                    ListenedStockCode = stkCode;

                                    LocalFormSettings["StockCode"] = stkCode;
                                    PassToOrderTicket(false);

                                    /* Remark because OnStock is not a direct user trigger and should not propagate to another form.
                                    List<BaseForm> AccountBookFormList = BaseForm.GetFormByFormType(typeof(AccountForm));
                                    if (AccountBookFormList != null && AccountBookFormList.Count > 0 && AccountBookFormList[0] != null && ((AccountForm)AccountBookFormList[0]).IsAccountComboEnabled())
                                    {
                                        AccountForm accForm = ((AccountForm)AccountBookFormList[0]);
                                        accForm.ShowStock(Stock.GetSignature(FormExType, pListenedStockCode));
                                    }
                                     */

                                    return;
                                }
                            }
                        }
                    }
                    else
                    {
                        FillForm(TheStock);
                        Console.WriteLine(TheStock.StockSignature);

                        if (checkBoxAutoBuy.Checked == true) { AutoBuy(this.CurrStock); }

                        if (enterKeyPressed)
                        {
                            enterKeyPressed = false;
                            bool CanBuy = false, CanSell = false;
                            decimal stkPrice = Utils.OrderTypeSpread.GetSpreadNoStkPrice('B', 0, CurrStock, TradeDB, out CanBuy, out CanSell);
                            passInfoToStockQuoteForm(CurrStock.Nominal.ToString(), CanBuy, CanSell, false);
                        }
                    }
                }
            }
        }

        protected override void OnCultureChange(CultureInfo Culture)
        {
            if (CurrStock != null)
            {
                SetDocumentName(CurrStock.GetBestName(this.Culture), CurrStock.GetBestName(CultureInfo.GetCultureInfo("en-US")));
                //labelExchange.Text = theStock.GetBestExCodeName(this.Culture, theStock.ExchangeType);
                //labelValueStockName.Text = theStock.GetBestName(Culture);
                for (int i = 0; i < comboBoxPlaceOrderTo.Items.Count; i++)
                {
                    if (((ComboBoxItem)comboBoxPlaceOrderTo.Items[i]).Value == LocalFormSettings["OrderTicketInstanceID"])
                    {
                        comboBoxPlaceOrderTo.SelectedIndex = i;
                        break;
                    }
                }
                SetVisibleSpecialType(booVisibleSpecialType);
                ShowTradeStatus(CurrStock);
            }

            ExchangeCheckBoxChanged(null, null);
        }

        private void FillForm(Stock TheStock)
        {
            if (TheStock == null)
            {
                if (CurrStock != null)
                {
                    ClearForm();
                    CurrStock = null;
                }
            }
            else
            {
                bool fillStatics, fillDynamics;
                SpreadTableSet.SpreadTable spreadTable = TradeDB.SpreadTables[TheStock.SpreadTableCode];

                if (CurrStock == null || CurrStock.StockSignature != TheStock.StockSignature)
                    fillStatics = fillDynamics = true;
                else
                {
                    fillStatics = CurrStock.StaticVersion != TheStock.StaticVersion ? true : false;
                    fillDynamics = CurrStock.DynamicVersion != TheStock.DynamicVersion ? true : false;
                }

                if (fillStatics)
                {
                    int stockCodeInt;

                    SetDocumentName(TheStock.GetBestName(this.Culture), TheStock.GetBestName(CultureInfo.GetCultureInfo("en-US")));

                    labelStockCode.Text = int.TryParse(TheStock.Code, out stockCodeInt) ? stockCodeInt.ToString() : TheStock.Code;
                    spreadTable.Format(TheStock.PrevClose);

                    labelValuePrevious.Text = TheStock.PrevClose > 0 ? spreadTable.Format(TheStock.PrevClose) : "";

                    if (TheStock.Change > 0)
                        labelValueNominal.ForeColor = Color.FromArgb(0xff0000);
                    else if (TheStock.Change < 0)
                        labelValueNominal.ForeColor = Color.FromArgb(0x40a040);
                    else
                        labelValueNominal.ForeColor = Color.FromArgb(0x000000);

                    labelValueChange.Text = Stock.GetDispChange(TheStock.Change);
                    labelValueChangePC.Text = Stock.GetDispChangePC(TheStock.ChangePC);

                    labelValueLotSize.Text = TheStock.LotSize.ToString();
                }

                if (fillDynamics)
                {
                    StringBuilder sb = new StringBuilder(200);

                    labelValueHigh.Text = TheStock.High > 0 ? spreadTable.Format(TheStock.High) : "";
                    labelValueLow.Text = TheStock.Low > 0 ? spreadTable.Format(TheStock.Low) : "";
                    labelValueNominal.Text = TheStock.Nominal > 0 ? spreadTable.Format(TheStock.Nominal) : "";

                    if (TheStock.Change > 0)
                        labelValueNominal.ForeColor = Color.FromArgb(0xff0000);
                    else if (TheStock.Change < 0)
                        labelValueNominal.ForeColor = Color.FromArgb(0x40a040);
                    else
                        labelValueNominal.ForeColor = Color.FromArgb(0x000000);

                    labelValueChange.Text = Stock.GetDispChange(TheStock.Change);
                    labelValueChangePC.Text = Stock.GetDispChangePC(TheStock.ChangePC);
                    labelValueVolume.Text = TheStock.Volume >= 0 ? Stock.GetShortValue(TheStock.Volume, 6, 3) : "";
                    labelValueTurnover.Text = TheStock.Turnover >= 0 ? Stock.GetShortValue(TheStock.Turnover, 6, 3) : "";

                    ShowTradeStatus(TheStock);

                    #region Fill Bid

                    labelValueBid.Text = TheStock.Bid > 0 ? spreadTable.Format(TheStock.Bid) : "";

                    labelBidVol0.Text = TheStock.BidVol[0] > 0 ? Stock.FormatQtyValue(TheStock.BidVol[0]) : "";
                    if (TheStock.BidCount[0] > 0) { labelBidCount0.Text = Stock.FormatQtyValue(TheStock.BidCount[0]); }
                    else if (TheStock.BidCount[0] < 0) { labelBidCount0.Text = "***"; }
                    else { labelBidCount0.Text = ""; }

                    labelBidVol1.Text = TheStock.BidVol[1] > 0 ? Stock.FormatQtyValue(TheStock.BidVol[1]) : "";
                    if (TheStock.BidCount[1] > 0) { labelBidCount1.Text = Stock.FormatQtyValue(TheStock.BidCount[1]); }
                    else if (TheStock.BidCount[1] < 0) { labelBidCount1.Text = "***"; }
                    else { labelBidCount1.Text = ""; }

                    labelBidVol2.Text = TheStock.BidVol[2] > 0 ? Stock.FormatQtyValue(TheStock.BidVol[2]) : "";
                    if (TheStock.BidCount[2] > 0) { labelBidCount2.Text = Stock.FormatQtyValue(TheStock.BidCount[2]); }
                    else if (TheStock.BidCount[2] < 0) { labelBidCount2.Text = "***"; }
                    else { labelBidCount2.Text = ""; }

                    labelBidVol3.Text = TheStock.BidVol[3] > 0 ? Stock.FormatQtyValue(TheStock.BidVol[3]) : "";
                    if (TheStock.BidCount[3] > 0) { labelBidCount3.Text = Stock.FormatQtyValue(TheStock.BidCount[3]); }
                    else if (TheStock.BidCount[3] < 0) { labelBidCount3.Text = "***"; }
                    else { labelBidCount3.Text = ""; }

                    labelBidVol4.Text = TheStock.BidVol[4] > 0 ? Stock.FormatQtyValue(TheStock.BidVol[4]) : "";
                    if (TheStock.BidCount[4] > 0) { labelBidCount4.Text = Stock.FormatQtyValue(TheStock.BidCount[4]); }
                    else if (TheStock.BidCount[4] < 0) { labelBidCount4.Text = "***"; }
                    else { labelBidCount4.Text = ""; }

                    labelBidVol5.Text = TheStock.BidVol[5] > 0 ? Stock.FormatQtyValue(TheStock.BidVol[5]) : "";
                    if (TheStock.BidCount[5] > 0) { labelBidCount5.Text = Stock.FormatQtyValue(TheStock.BidCount[5]); }
                    else if (TheStock.BidCount[5] < 0) { labelBidCount5.Text = "***"; }
                    else { labelBidCount5.Text = ""; }

                    labelBidVol6.Text = TheStock.BidVol[6] > 0 ? Stock.FormatQtyValue(TheStock.BidVol[6]) : "";
                    if (TheStock.BidCount[6] > 0) { labelBidCount6.Text = Stock.FormatQtyValue(TheStock.BidCount[6]); }
                    else if (TheStock.BidCount[6] < 0) { labelBidCount6.Text = "***"; }
                    else { labelBidCount6.Text = ""; }

                    labelBidVol7.Text = TheStock.BidVol[7] > 0 ? Stock.FormatQtyValue(TheStock.BidVol[7]) : "";
                    if (TheStock.BidCount[7] > 0) { labelBidCount7.Text = Stock.FormatQtyValue(TheStock.BidCount[7]); }
                    else if (TheStock.BidCount[7] < 0) { labelBidCount7.Text = "***"; }
                    else { labelBidCount7.Text = ""; }

                    labelBidVol8.Text = TheStock.BidVol[8] > 0 ? Stock.FormatQtyValue(TheStock.BidVol[8]) : "";
                    if (TheStock.BidCount[8] > 0) { labelBidCount8.Text = Stock.FormatQtyValue(TheStock.BidCount[8]); }
                    else if (TheStock.BidCount[8] < 0) { labelBidCount8.Text = "***"; }
                    else { labelBidCount8.Text = ""; }

                    labelBidVol9.Text = TheStock.BidVol[9] > 0 ? Stock.FormatQtyValue(TheStock.BidVol[9]) : "";
                    if (TheStock.BidCount[9] > 0) { labelBidCount9.Text = Stock.FormatQtyValue(TheStock.BidCount[9]); }
                    else if (TheStock.BidCount[9] < 0) { labelBidCount9.Text = "***"; }
                    else { labelBidCount9.Text = ""; }

                    #endregion

                    #region Fill Ask

                    labelValueAsk.Text = TheStock.Ask > 0 ? spreadTable.Format(TheStock.Ask) : "";

                    labelAskVol0.Text = TheStock.AskVol[0] > 0 ? Stock.FormatQtyValue(TheStock.AskVol[0]) : "";
                    if (TheStock.AskCount[0] > 0) { labelAskCount0.Text = Stock.FormatQtyValue(TheStock.AskCount[0]); }
                    else if (TheStock.AskCount[0] < 0) { labelAskCount0.Text = "***"; }
                    else { labelAskCount0.Text = ""; }

                    labelAskVol1.Text = TheStock.AskVol[1] > 0 ? Stock.FormatQtyValue(TheStock.AskVol[1]) : "";
                    if (TheStock.AskCount[1] > 0) { labelAskCount1.Text = Stock.FormatQtyValue(TheStock.AskCount[1]); }
                    else if (TheStock.AskCount[1] < 0) { labelAskCount1.Text = "***"; }
                    else { labelAskCount1.Text = ""; }

                    labelAskVol2.Text = TheStock.AskVol[2] > 0 ? Stock.FormatQtyValue(TheStock.AskVol[2]) : "";
                    if (TheStock.AskCount[2] > 0) { labelAskCount2.Text = Stock.FormatQtyValue(TheStock.AskCount[2]); }
                    else if (TheStock.AskCount[2] < 0) { labelAskCount2.Text = "***"; }
                    else { labelAskCount2.Text = ""; }

                    labelAskVol3.Text = TheStock.AskVol[3] > 0 ? Stock.FormatQtyValue(TheStock.AskVol[3]) : "";
                    if (TheStock.AskCount[3] > 0) { labelAskCount3.Text = Stock.FormatQtyValue(TheStock.AskCount[3]); }
                    else if (TheStock.AskCount[3] < 0) { labelAskCount3.Text = "***"; }
                    else { labelAskCount3.Text = ""; }

                    labelAskVol4.Text = TheStock.AskVol[4] > 0 ? Stock.FormatQtyValue(TheStock.AskVol[4]) : "";
                    if (TheStock.AskCount[4] > 0) { labelAskCount4.Text = Stock.FormatQtyValue(TheStock.AskCount[4]); }
                    else if (TheStock.AskCount[4] < 0) { labelAskCount4.Text = "***"; }
                    else { labelAskCount4.Text = ""; }

                    labelAskVol5.Text = TheStock.AskVol[5] > 0 ? Stock.FormatQtyValue(TheStock.AskVol[5]) : "";
                    if (TheStock.AskCount[5] > 0) { labelAskCount5.Text = Stock.FormatQtyValue(TheStock.AskCount[5]); }
                    else if (TheStock.AskCount[5] < 0) { labelAskCount5.Text = "***"; }
                    else { labelAskCount5.Text = ""; }

                    labelAskVol6.Text = TheStock.AskVol[6] > 0 ? Stock.FormatQtyValue(TheStock.AskVol[6]) : "";
                    if (TheStock.AskCount[6] > 0) { labelAskCount6.Text = Stock.FormatQtyValue(TheStock.AskCount[6]); }
                    else if (TheStock.AskCount[6] < 0) { labelAskCount6.Text = "***"; }
                    else { labelAskCount6.Text = ""; }

                    labelAskVol7.Text = TheStock.AskVol[7] > 0 ? Stock.FormatQtyValue(TheStock.AskVol[7]) : "";
                    if (TheStock.AskCount[7] > 0) { labelAskCount7.Text = Stock.FormatQtyValue(TheStock.AskCount[7]); }
                    else if (TheStock.AskCount[7] < 0) { labelAskCount7.Text = "***"; }
                    else { labelAskCount7.Text = ""; }

                    labelAskVol8.Text = TheStock.AskVol[8] > 0 ? Stock.FormatQtyValue(TheStock.AskVol[8]) : "";
                    if (TheStock.AskCount[8] > 0) { labelAskCount8.Text = Stock.FormatQtyValue(TheStock.AskCount[8]); }
                    else if (TheStock.AskCount[8] < 0) { labelAskCount8.Text = "***"; }
                    else { labelAskCount8.Text = ""; }

                    labelAskVol9.Text = TheStock.AskVol[9] > 0 ? Stock.FormatQtyValue(TheStock.AskVol[9]) : "";
                    if (TheStock.AskCount[9] > 0) { labelAskCount9.Text = Stock.FormatQtyValue(TheStock.AskCount[9]); }
                    else if (TheStock.AskCount[9] < 0) { labelAskCount9.Text = "***"; }
                    else { labelAskCount9.Text = ""; }

                    #endregion

                    #region Fill Broker Bid

                    sb = new StringBuilder(49);
                    sb.Append(TheStock.BrokerBid[0] + "\n"); sb.Append(TheStock.BrokerBid[1] + "\n"); sb.Append(TheStock.BrokerBid[2] + "\n"); sb.Append(TheStock.BrokerBid[3] + "\n"); sb.Append(TheStock.BrokerBid[4] + "\n");
                    sb.Append(TheStock.BrokerBid[5] + "\n"); sb.Append(TheStock.BrokerBid[6] + "\n"); sb.Append(TheStock.BrokerBid[7] + "\n"); sb.Append(TheStock.BrokerBid[8] + "\n"); sb.Append(TheStock.BrokerBid[9]);
                    labelValueBidBQ0.Text = sb.ToString();
                    sb = new StringBuilder(49);
                    sb.Append(TheStock.BrokerBid[10] + "\n"); sb.Append(TheStock.BrokerBid[11] + "\n"); sb.Append(TheStock.BrokerBid[12] + "\n"); sb.Append(TheStock.BrokerBid[13] + "\n"); sb.Append(TheStock.BrokerBid[14] + "\n");
                    sb.Append(TheStock.BrokerBid[15] + "\n"); sb.Append(TheStock.BrokerBid[16] + "\n"); sb.Append(TheStock.BrokerBid[17] + "\n"); sb.Append(TheStock.BrokerBid[18] + "\n"); sb.Append(TheStock.BrokerBid[19]);
                    labelValueBidBQ1.Text = sb.ToString();
                    sb = new StringBuilder(49);
                    sb.Append(TheStock.BrokerBid[20] + "\n"); sb.Append(TheStock.BrokerBid[21] + "\n"); sb.Append(TheStock.BrokerBid[22] + "\n"); sb.Append(TheStock.BrokerBid[23] + "\n"); sb.Append(TheStock.BrokerBid[24] + "\n");
                    sb.Append(TheStock.BrokerBid[25] + "\n"); sb.Append(TheStock.BrokerBid[26] + "\n"); sb.Append(TheStock.BrokerBid[27] + "\n"); sb.Append(TheStock.BrokerBid[28] + "\n"); sb.Append(TheStock.BrokerBid[29]);
                    labelValueBidBQ2.Text = sb.ToString();
                    sb = new StringBuilder(49);
                    sb.Append(TheStock.BrokerBid[30] + "\n"); sb.Append(TheStock.BrokerBid[31] + "\n"); sb.Append(TheStock.BrokerBid[32] + "\n"); sb.Append(TheStock.BrokerBid[33] + "\n"); sb.Append(TheStock.BrokerBid[34] + "\n");
                    sb.Append(TheStock.BrokerBid[35] + "\n"); sb.Append(TheStock.BrokerBid[36] + "\n"); sb.Append(TheStock.BrokerBid[37] + "\n"); sb.Append(TheStock.BrokerBid[38] + "\n"); sb.Append(TheStock.BrokerBid[39]);
                    labelValueBidBQ3.Text = sb.ToString();

                    #endregion

                    #region Fill Broker Ask

                    sb = new StringBuilder(49);
                    sb.Append(TheStock.BrokerAsk[0] + "\n"); sb.Append(TheStock.BrokerAsk[1] + "\n"); sb.Append(TheStock.BrokerAsk[2] + "\n"); sb.Append(TheStock.BrokerAsk[3] + "\n"); sb.Append(TheStock.BrokerAsk[4] + "\n");
                    sb.Append(TheStock.BrokerAsk[5] + "\n"); sb.Append(TheStock.BrokerAsk[6] + "\n"); sb.Append(TheStock.BrokerAsk[7] + "\n"); sb.Append(TheStock.BrokerAsk[8] + "\n"); sb.Append(TheStock.BrokerAsk[9]);
                    labelValueAskBQ0.Text = sb.ToString();
                    sb = new StringBuilder(49);
                    sb.Append(TheStock.BrokerAsk[10] + "\n"); sb.Append(TheStock.BrokerAsk[11] + "\n"); sb.Append(TheStock.BrokerAsk[12] + "\n"); sb.Append(TheStock.BrokerAsk[13] + "\n"); sb.Append(TheStock.BrokerAsk[14] + "\n");
                    sb.Append(TheStock.BrokerAsk[15] + "\n"); sb.Append(TheStock.BrokerAsk[16] + "\n"); sb.Append(TheStock.BrokerAsk[17] + "\n"); sb.Append(TheStock.BrokerAsk[18] + "\n"); sb.Append(TheStock.BrokerAsk[19]);
                    labelValueAskBQ1.Text = sb.ToString();
                    sb = new StringBuilder(49);
                    sb.Append(TheStock.BrokerAsk[20] + "\n"); sb.Append(TheStock.BrokerAsk[21] + "\n"); sb.Append(TheStock.BrokerAsk[22] + "\n"); sb.Append(TheStock.BrokerAsk[23] + "\n"); sb.Append(TheStock.BrokerAsk[24] + "\n");
                    sb.Append(TheStock.BrokerAsk[25] + "\n"); sb.Append(TheStock.BrokerAsk[26] + "\n"); sb.Append(TheStock.BrokerAsk[27] + "\n"); sb.Append(TheStock.BrokerAsk[28] + "\n"); sb.Append(TheStock.BrokerAsk[29]);
                    labelValueAskBQ2.Text = sb.ToString();
                    sb = new StringBuilder(49);
                    sb.Append(TheStock.BrokerAsk[30] + "\n"); sb.Append(TheStock.BrokerAsk[31] + "\n"); sb.Append(TheStock.BrokerAsk[32] + "\n"); sb.Append(TheStock.BrokerAsk[33] + "\n"); sb.Append(TheStock.BrokerAsk[34] + "\n");
                    sb.Append(TheStock.BrokerAsk[35] + "\n"); sb.Append(TheStock.BrokerAsk[36] + "\n"); sb.Append(TheStock.BrokerAsk[37] + "\n"); sb.Append(TheStock.BrokerAsk[38] + "\n"); sb.Append(TheStock.BrokerAsk[39]);
                    labelValueAskBQ3.Text = sb.ToString();

                    #endregion

                    #region Fill Ticker

                    int TickIdx = 14;
                    for (; TickIdx > 3; TickIdx--)
                    {
                        if (TheStock.Ticker[TickIdx] != null && TheStock.Ticker[TickIdx].Time != null && TheStock.Ticker[TickIdx].Time.Length > 0 && TheStock.Ticker[TickIdx].Time != "00:00") 
                            break;
                    }

                    if (TheStock.Ticker[TickIdx] != null && TheStock.Ticker[TickIdx].Time != "00:00")
                    {
                        labelValueTickerTime3.Text = TheStock.Ticker[TickIdx].Time == null ? "" : (TheStock.Ticker[TickIdx].Time.Length > 5 ? TheStock.Ticker[TickIdx].Time.Substring(0, 5) : TheStock.Ticker[TickIdx].Time);
                        labelValueTickerRemark3.Text = TheStock.Ticker[TickIdx].Remarks ?? "";
                        labelValueTickerQty3.Text = TheStock.Ticker[TickIdx].Quantity > 0 ? Stock.FormatQtyValue(TheStock.Ticker[TickIdx].Quantity) : "";
                        if (TheStock.Ticker[TickIdx].Price == 0)
                            labelValueTickerPrice3.Text = "";
                        else if (TheStock.Ticker[TickIdx].Price.ToString("0.#####").Length > spreadTable.Format(TheStock.Ticker[TickIdx].Price).Length)
                            labelValueTickerPrice3.Text = TheStock.Ticker[TickIdx].Price.ToString("0.#####");
                        else
                            labelValueTickerPrice3.Text = spreadTable.Format(TheStock.Ticker[TickIdx].Price);
                    }
                    else
                    {
                        labelValueTickerTime3.Text = "";
                        labelValueTickerRemark3.Text = "";
                        labelValueTickerQty3.Text = "";
                        labelValueTickerPrice3.Text = "";
                    }

                    TickIdx--;
                    if (TheStock.Ticker[TickIdx] != null && TheStock.Ticker[TickIdx].Time != "00:00")
                    {
                        labelValueTickerTime2.Text = TheStock.Ticker[TickIdx].Time == null ? "" : (TheStock.Ticker[TickIdx].Time.Length > 5 ? TheStock.Ticker[TickIdx].Time.Substring(0, 5) : TheStock.Ticker[TickIdx].Time);
                        labelValueTickerRemark2.Text = TheStock.Ticker[TickIdx].Remarks ?? "";
                        labelValueTickerQty2.Text = TheStock.Ticker[TickIdx].Quantity > 0 ? Stock.FormatQtyValue(TheStock.Ticker[TickIdx].Quantity) : "";
                        if (TheStock.Ticker[TickIdx].Price == 0)
                            labelValueTickerPrice2.Text = "";
                        else if (TheStock.Ticker[TickIdx].Price.ToString("0.#####").Length > spreadTable.Format(TheStock.Ticker[TickIdx].Price).Length)
                            labelValueTickerPrice2.Text = TheStock.Ticker[TickIdx].Price.ToString("0.#####");
                        else
                            labelValueTickerPrice2.Text = spreadTable.Format(TheStock.Ticker[TickIdx].Price);
                    }
                    else
                    {
                        labelValueTickerTime2.Text = "";
                        labelValueTickerRemark2.Text = "";
                        labelValueTickerQty2.Text = "";
                        labelValueTickerPrice2.Text = "";
                    }

                    TickIdx--;
                    if (TheStock.Ticker[TickIdx] != null && TheStock.Ticker[TickIdx].Time != "00:00")
                    {
                        labelValueTickerTime1.Text = TheStock.Ticker[TickIdx].Time == null ? "" : (TheStock.Ticker[TickIdx].Time.Length > 5 ? TheStock.Ticker[TickIdx].Time.Substring(0, 5) : TheStock.Ticker[TickIdx].Time);
                        labelValueTickerRemark1.Text = TheStock.Ticker[TickIdx].Remarks ?? "";
                        labelValueTickerQty1.Text = TheStock.Ticker[TickIdx].Quantity > 0 ? Stock.FormatQtyValue(TheStock.Ticker[TickIdx].Quantity) : "";
                        if (TheStock.Ticker[TickIdx].Price == 0)
                            labelValueTickerPrice1.Text = "";
                        else if (TheStock.Ticker[TickIdx].Price.ToString("0.#####").Length > spreadTable.Format(TheStock.Ticker[TickIdx].Price).Length)
                            labelValueTickerPrice1.Text = TheStock.Ticker[TickIdx].Price.ToString("0.#####");
                        else
                            labelValueTickerPrice1.Text = spreadTable.Format(TheStock.Ticker[TickIdx].Price);
                    }
                    else
                    {
                        labelValueTickerTime1.Text = "";
                        labelValueTickerRemark1.Text = "";
                        labelValueTickerQty1.Text = "";
                        labelValueTickerPrice1.Text = "";
                    }

                    TickIdx--;
                    if (TheStock.Ticker[TickIdx] != null && TheStock.Ticker[TickIdx].Time != "00:00")
                    {
                        labelValueTickerTime0.Text = TheStock.Ticker[TickIdx].Time == null ? "" : (TheStock.Ticker[TickIdx].Time.Length > 5 ? TheStock.Ticker[TickIdx].Time.Substring(0, 5) : TheStock.Ticker[TickIdx].Time);
                        labelValueTickerRemark0.Text = TheStock.Ticker[TickIdx].Remarks ?? "";
                        labelValueTickerQty0.Text = TheStock.Ticker[TickIdx].Quantity > 0 ? Stock.FormatQtyValue(TheStock.Ticker[TickIdx].Quantity) : "";
                        if (TheStock.Ticker[TickIdx].Price == 0)
                            labelValueTickerPrice0.Text = "";
                        else if (TheStock.Ticker[TickIdx].Price.ToString("0.#####").Length > spreadTable.Format(TheStock.Ticker[TickIdx].Price).Length)
                            labelValueTickerPrice0.Text = TheStock.Ticker[TickIdx].Price.ToString("0.#####");
                        else
                            labelValueTickerPrice0.Text = spreadTable.Format(TheStock.Ticker[TickIdx].Price);
                    }
                    else
                    {
                        labelValueTickerTime0.Text = "";
                        labelValueTickerRemark0.Text = "";
                        labelValueTickerQty0.Text = "";
                        labelValueTickerPrice0.Text = "";
                    }

                    #endregion
                }

                CurrStock = TheStock;
            }
        }

        private void ClearForm()
        {
            SetDocumentName(null, null);

            labelStockCode.Text = "";
            //labelExchange.Text = "";
            //labelValueStockName.Text = "";
            //labelValueOpen.Text = "";
            labelValueHigh.Text = "";
            labelValueLow.Text = "";
            labelValuePrevious.Text = "";
            labelValueNominal.Text = "";
            labelValueLotSize.Text = "";
            labelValueChange.Text = "";
            labelValueChangePC.Text = "";
            labelValueVolume.Text = "";
            labelValueTurnover.Text = "";
            ShowTradeStatus(null);

            labelValueBid.Text = "";
            labelBidVol0.Text = "";
            labelBidVol1.Text = "";
            labelBidVol2.Text = "";
            labelBidVol3.Text = "";
            labelBidVol4.Text = "";
            labelBidVol5.Text = "";
            labelBidVol6.Text = "";
            labelBidVol7.Text = "";
            labelBidVol8.Text = "";
            labelBidVol9.Text = "";
            //labelValueBidVol.Text = "";
            labelBidCount0.Text = "";
            labelBidCount1.Text = "";
            labelBidCount2.Text = "";
            labelBidCount3.Text = "";
            labelBidCount4.Text = "";
            labelBidCount5.Text = "";
            labelBidCount6.Text = "";
            labelBidCount7.Text = "";
            labelBidCount8.Text = "";
            labelBidCount9.Text = "";
            //labelValueBidCount.Text = "";

            labelValueAsk.Text = "";
            labelAskVol0.Text = "";
            labelAskVol1.Text = "";
            labelAskVol2.Text = "";
            labelAskVol3.Text = "";
            labelAskVol4.Text = "";
            labelAskVol5.Text = "";
            labelAskVol6.Text = "";
            labelAskVol7.Text = "";
            labelAskVol8.Text = "";
            labelAskVol9.Text = "";
            //labelValueAskVol.Text = "";
            labelAskCount0.Text = "";
            labelAskCount1.Text = "";
            labelAskCount2.Text = "";
            labelAskCount3.Text = "";
            labelAskCount4.Text = "";
            labelAskCount5.Text = "";
            labelAskCount6.Text = "";
            labelAskCount7.Text = "";
            labelAskCount8.Text = "";
            labelAskCount9.Text = "";
            //labelValueAskCount.Text = "";

            //labelValueBidBQ.Text = "";
            labelValueBidBQ0.Text = "";
            labelValueBidBQ1.Text = "";
            labelValueBidBQ2.Text = "";
            labelValueBidBQ3.Text = "";

            //labelValueAskBQ.Text = "";
            labelValueAskBQ0.Text = "";
            labelValueAskBQ1.Text = "";
            labelValueAskBQ2.Text = "";
            labelValueAskBQ3.Text = "";

            //labelValueTickerTime.Text = "";
            //labelValueTickerRemark.Text = "";
            //labelValueTickerQty.Text = "";
            //labelValueTickerPrice.Text = "";

            labelValueTickerTime0.Text = "";
            labelValueTickerRemark0.Text = "";
            labelValueTickerQty0.Text = "";
            labelValueTickerPrice0.Text = "";
            labelValueTickerTime1.Text = "";
            labelValueTickerRemark1.Text = "";
            labelValueTickerQty1.Text = "";
            labelValueTickerPrice1.Text = "";
            labelValueTickerTime2.Text = "";
            labelValueTickerRemark2.Text = "";
            labelValueTickerQty2.Text = "";
            labelValueTickerPrice2.Text = "";
            labelValueTickerTime3.Text = "";
            labelValueTickerRemark3.Text = "";
            labelValueTickerQty3.Text = "";
            labelValueTickerPrice3.Text = "";

            //////checkBoxBuyAllCredit.Checked = false;
            //////checkBoxSpecialLimit.Checked = false;
            checkBoxAutoBuy.Checked = false;
            textBoxAutoBuy.Text = "";
        }

        private void ShowTradeStatus(Stock theStock)
        {
            if (theStock != null)
            {
                SpreadTableSet.SpreadTable spreadTable = TradeDB.SpreadTables[theStock.SpreadTableCode];

                bool isToday = (theStock.dtVCMCoolOffEndTime.Year == TradeDB.ServerTime.Year && theStock.dtVCMCoolOffEndTime.Month == TradeDB.ServerTime.Month && theStock.dtVCMCoolOffEndTime.Day == TradeDB.ServerTime.Day);
                bool isCAS = ((theStock.CASLowerPrice > 0m || theStock.CASUpperPrice > 0m) && theStock.MarketBelong != null && theStock.MarketBelong.Status >= 10 && theStock.MarketBelong.Status <= 14);
                bool isVCM = ((theStock.VCMReferencePrice > 0m || theStock.VCMLowerPrice > 0m || theStock.VCMUpperPrice > 0m) && (theStock.dtVCMCoolOffEndTime.CompareTo(TradeDB.ServerTime) > 0));
                if (isToday && (isCAS || isVCM))
                {
                    if (isCAS)
                    {
                        labelValueTradeStatus.Text = GetResxString("CAS");
                        //if (theStock.OrderImbalanceDirection != null && theStock.OrderImbalanceDirection.Trerim().Length > 0)
                        //    labelValueTradeStatus.Text += theStock.OrderImbalanceDirection + " " + theStock.OrderImbalanceQuantity; 
                        labelValueTradeStatus.ForeColor = Color.Red;
                        if (theStock.CASLowerPrice > 0m)
                        {
                            labelBid.Text = spreadTable.Format(theStock.CASLowerPrice);
                            labelBid.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold);
                            labelBid.BackColor = Color.SkyBlue;
                        }
                        if (theStock.CASUpperPrice > 0m)
                        {
                            labelAsk.Text = spreadTable.Format(theStock.CASUpperPrice);
                            labelAsk.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold);
                            labelAsk.BackColor = Color.LightPink;
                        }
                    }
                    else if (isVCM)
                    {
                        System.Diagnostics.Debug.Print("VCMCoolOffEndTime: " + theStock.dtVCMCoolOffEndTime.ToString("yyyy/MM/dd HH:mm:ss") +
                            " ServerTime: " + TradeDB.ServerTime.ToString("yyyy/MM/dd HH:mm:ss") + " Compare result: " + theStock.dtVCMCoolOffEndTime.CompareTo(TradeDB.ServerTime));
                        labelValueTradeStatus.Text = GetResxString("VCMCooling") + (Math.Abs(theStock.dtVCMCoolOffEndTime.Subtract(TradeDB.ServerTime).Minutes) + "").PadLeft(2, '0') + ":" +
                             (Math.Abs(theStock.dtVCMCoolOffEndTime.Subtract(TradeDB.ServerTime).Seconds) + "").PadLeft(2, '0');
                        labelValueTradeStatus.ForeColor = Color.Red;
                        if (theStock.VCMLowerPrice > 0)
                        {
                            labelBid.Text = spreadTable.Format(theStock.VCMLowerPrice);
                            labelBid.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold);
                            labelBid.BackColor = Color.SkyBlue;
                        }
                        if (theStock.VCMUpperPrice > 0)
                        {
                            labelAsk.Text = spreadTable.Format(theStock.VCMUpperPrice);
                            labelAsk.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold);
                            labelAsk.BackColor = Color.LightPink;
                        }
                    }
                }
                else // not VCM, not CAS
                {
                    if (theStock.SuspensionFlag == "Y")
                    {
                        labelValueTradeStatus.Text = GetResxString("TradeStatusSuspended");
                        labelValueTradeStatus.ForeColor = Color.Red;
                    }
                    else if (theStock.FusingFlag == "Y")
                    {
                        labelValueTradeStatus.Text = GetResxString("TradeStatusFusing");
                        labelValueTradeStatus.ForeColor = Color.Red;
                    }
                    else
                    {
                        labelValueTradeStatus.Text = GetGeneralResxString("Currency_" + theStock.Currency);
                        labelValueTradeStatus.ForeColor = (theStock.Currency == null || theStock.Currency == "" || theStock.Currency == "HKD") ? Color.FromKnownColor(KnownColor.ControlText) : Color.Red;
                    }
                    labelBid.Text = GetResxString("Bid");
                    labelAsk.Text = GetResxString("Ask");
                    labelBid.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Regular);
                    labelAsk.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Regular);
                    labelBid.BackColor = Color.Transparent;
                    labelAsk.BackColor = Color.Transparent;
                    /*
                    if (checkBoxSHG.Checked == true)
                        labelAsk.BackColor = labelBid.BackColor = Color.FromArgb(170, 255, 170);
                    else if (checkBoxSZE.Checked == true)
                        labelAsk.BackColor = labelBid.BackColor = Color.FromArgb(218, 174, 232);
                    else if (checkBoxGray.Checked == true)
                        labelAsk.BackColor = labelBid.BackColor = Color.FromArgb(255, 214, 124);
                    else
                        labelBid.BackColor = labelBid.BackColor = Color.FromArgb(255, 255, 205);
                     */
                    //if (theStock != null)
                    //{
                    //    labelValueTradeStatus.Text = GetResxString("VCMCooling") + (Math.Abs(theStock.dtVCMCoolOffEndTime.Subtract(TradeDB.ServerTime).Minutes) + "").PadLeft(2, '0') + ":" + 
                    //     (Math.Abs(theStock.dtVCMCoolOffEndTime.Subtract(TradeDB.ServerTime).Seconds) + "").PadLeft(2, '0');
                    //}
                }
            }
            else // no or invalid stock, clear form
            {
                labelBid.Text = GetResxString("Bid");
                labelAsk.Text = GetResxString("Ask");
                labelBid.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Regular);
                labelAsk.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Regular);
                labelBid.BackColor = Color.Transparent;
                labelAsk.BackColor = Color.Transparent;
                /*
                if (checkBoxSHG.Checked == true)
                    labelAsk.BackColor = labelBid.BackColor = Color.FromArgb(170, 255, 170);
                else if (checkBoxSZE.Checked == true)
                    labelAsk.BackColor = labelBid.BackColor = Color.FromArgb(218, 174, 232);
                else if (checkBoxGray.Checked == true)
                    labelAsk.BackColor = labelBid.BackColor = Color.FromArgb(255, 214, 124);
                else
                    labelAsk.BackColor = labelBid.BackColor = Color.FromArgb(255, 255, 205);
                 */
                labelValueTradeStatus.Text = "";
                labelValueTradeStatus.ForeColor = Color.FromKnownColor(KnownColor.ControlText);
            }
        }

        private void labelBid_DoubleClick(object sender, EventArgs e)
        {
            PassToOrderTicket(true);
        }

        public void PassToOrderTicket(bool SetOrderTicketFocus)
        {
            bool CanBuy = false, CanSell = false;
            decimal stkPrice = Utils.OrderTypeSpread.GetSpreadNoStkPrice(' ', 0, CurrStock, TradeDB, out CanBuy, out CanSell);
            passInfoToStockQuoteForm(stkPrice.ToString(), CanBuy, CanSell, SetOrderTicketFocus);
        }

        private void labelAsk_DoubleClick(object sender, EventArgs e)
        {
            bool CanBuy = false, CanSell = false;
            decimal stkPrice = Utils.OrderTypeSpread.GetSpreadNoStkPrice('S', 0, CurrStock, TradeDB, out CanBuy, out CanSell);
            passInfoToStockQuoteForm(stkPrice.ToString(), CanBuy, CanSell, true);
        }

        private void labelBid1_DoubleClick(object sender, EventArgs e)
        {
            bool CanBuy = false, CanSell = false;
            decimal stkPrice = Utils.OrderTypeSpread.GetSpreadNoStkPrice('B', 0, CurrStock, TradeDB, out CanBuy, out CanSell);
            passInfoToStockQuoteForm(stkPrice.ToString(), CanBuy, CanSell, true);
        }

        private void labelBid2_DoubleClick(object sender, EventArgs e)
        {
            bool CanBuy = false, CanSell = false;
            decimal stkPrice = Utils.OrderTypeSpread.GetSpreadNoStkPrice('B', -1, CurrStock, TradeDB, out CanBuy, out CanSell);
            passInfoToStockQuoteForm(stkPrice.ToString(), CanBuy, CanSell, true);
        }

        private void labelBid3_DoubleClick(object sender, EventArgs e)
        {
            bool CanBuy = false, CanSell = false;
            decimal stkPrice = Utils.OrderTypeSpread.GetSpreadNoStkPrice('B', -2, CurrStock, TradeDB, out CanBuy, out CanSell);
            passInfoToStockQuoteForm(stkPrice.ToString(), CanBuy, CanSell, true);
        }

        private void labelBid4_DoubleClick(object sender, EventArgs e)
        {
            bool CanBuy = false, CanSell = false;
            decimal stkPrice = Utils.OrderTypeSpread.GetSpreadNoStkPrice('B', -3, CurrStock, TradeDB, out CanBuy, out CanSell);
            passInfoToStockQuoteForm(stkPrice.ToString(), CanBuy, CanSell, true);
        }

        private void labelBid5_DoubleClick(object sender, EventArgs e)
        {
            bool CanBuy = false, CanSell = false;
            decimal stkPrice = Utils.OrderTypeSpread.GetSpreadNoStkPrice('B', -4, CurrStock, TradeDB, out CanBuy, out CanSell);
            passInfoToStockQuoteForm(stkPrice.ToString(), CanBuy, CanSell, true);
        }

        private void labelBid6_DoubleClick(object sender, EventArgs e)
        {
            bool CanBuy = false, CanSell = false;
            decimal stkPrice = Utils.OrderTypeSpread.GetSpreadNoStkPrice('B', -5, CurrStock, TradeDB, out CanBuy, out CanSell);
            passInfoToStockQuoteForm(stkPrice.ToString(), CanBuy, CanSell, true);
        }

        private void labelBid7_DoubleClick(object sender, EventArgs e)
        {
            bool CanBuy = false, CanSell = false;
            decimal stkPrice = Utils.OrderTypeSpread.GetSpreadNoStkPrice('B', -6, CurrStock, TradeDB, out CanBuy, out CanSell);
            passInfoToStockQuoteForm(stkPrice.ToString(), CanBuy, CanSell, true);
        }

        private void labelBid8_DoubleClick(object sender, EventArgs e)
        {
            bool CanBuy = false, CanSell = false;
            decimal stkPrice = Utils.OrderTypeSpread.GetSpreadNoStkPrice('B', -7, CurrStock, TradeDB, out CanBuy, out CanSell);
            passInfoToStockQuoteForm(stkPrice.ToString(), CanBuy, CanSell, true);
        }

        private void labelBid9_DoubleClick(object sender, EventArgs e)
        {
            bool CanBuy = false, CanSell = false;
            decimal stkPrice = Utils.OrderTypeSpread.GetSpreadNoStkPrice('B', -8, CurrStock, TradeDB, out CanBuy, out CanSell);
            passInfoToStockQuoteForm(stkPrice.ToString(), CanBuy, CanSell, true);
        }

        private void labelBid10_DoubleClick(object sender, EventArgs e)
        {
            bool CanBuy = false, CanSell = false;
            decimal stkPrice = Utils.OrderTypeSpread.GetSpreadNoStkPrice('B', -9, CurrStock, TradeDB, out CanBuy, out CanSell);
            passInfoToStockQuoteForm(stkPrice.ToString(), CanBuy, CanSell, true);
        }

        private void labelAsk1_DoubleClick(object sender, EventArgs e)
        {
            bool CanBuy = false, CanSell = false;
            decimal stkPrice = Utils.OrderTypeSpread.GetSpreadNoStkPrice('S', 0, CurrStock, TradeDB, out CanBuy, out CanSell);
            passInfoToStockQuoteForm(stkPrice.ToString(), CanBuy, CanSell, true);
        }

        private void labelAsk2_DoubleClick(object sender, EventArgs e)
        {
            bool CanBuy = false, CanSell = false;
            decimal stkPrice = Utils.OrderTypeSpread.GetSpreadNoStkPrice('S', 1, CurrStock, TradeDB, out CanBuy, out CanSell);
            passInfoToStockQuoteForm(stkPrice.ToString(), CanBuy, CanSell, true);
        }

        private void labelAsk3_DoubleClick(object sender, EventArgs e)
        {
            bool CanBuy = false, CanSell = false;
            decimal stkPrice = Utils.OrderTypeSpread.GetSpreadNoStkPrice('S', 2, CurrStock, TradeDB, out CanBuy, out CanSell);
            passInfoToStockQuoteForm(stkPrice.ToString(), CanBuy, CanSell, true);
        }

        private void labelAsk4_DoubleClick(object sender, EventArgs e)
        {
            bool CanBuy = false, CanSell = false;
            decimal stkPrice = Utils.OrderTypeSpread.GetSpreadNoStkPrice('S', 3, CurrStock, TradeDB, out CanBuy, out CanSell);
            passInfoToStockQuoteForm(stkPrice.ToString(), CanBuy, CanSell, true);
        }

        private void labelAsk5_DoubleClick(object sender, EventArgs e)
        {
            bool CanBuy = false, CanSell = false;
            decimal stkPrice = Utils.OrderTypeSpread.GetSpreadNoStkPrice('S', 4, CurrStock, TradeDB, out CanBuy, out CanSell);
            passInfoToStockQuoteForm(stkPrice.ToString(), CanBuy, CanSell, true);
        }

        private void labelAsk6_DoubleClick(object sender, EventArgs e)
        {
            bool CanBuy = false, CanSell = false;
            decimal stkPrice = Utils.OrderTypeSpread.GetSpreadNoStkPrice('S', 5, CurrStock, TradeDB, out CanBuy, out CanSell);
            passInfoToStockQuoteForm(stkPrice.ToString(), CanBuy, CanSell, true);
        }

        private void labelAsk7_DoubleClick(object sender, EventArgs e)
        {
            bool CanBuy = false, CanSell = false;
            decimal stkPrice = Utils.OrderTypeSpread.GetSpreadNoStkPrice('S', 6, CurrStock, TradeDB, out CanBuy, out CanSell);
            passInfoToStockQuoteForm(stkPrice.ToString(), CanBuy, CanSell, true);
        }

        private void labelAsk8_DoubleClick(object sender, EventArgs e)
        {
            bool CanBuy = false, CanSell = false;
            decimal stkPrice = Utils.OrderTypeSpread.GetSpreadNoStkPrice('S', 7, CurrStock, TradeDB, out CanBuy, out CanSell);
            passInfoToStockQuoteForm(stkPrice.ToString(), CanBuy, CanSell, true);
        }

        private void labelAsk9_DoubleClick(object sender, EventArgs e)
        {
            bool CanBuy = false, CanSell = false;
            decimal stkPrice = Utils.OrderTypeSpread.GetSpreadNoStkPrice('S', 8, CurrStock, TradeDB, out CanBuy, out CanSell);
            passInfoToStockQuoteForm(stkPrice.ToString(), CanBuy, CanSell, true);
        }

        private void labelAsk10_DoubleClick(object sender, EventArgs e)
        {
            bool CanBuy = false, CanSell = false;
            decimal stkPrice = Utils.OrderTypeSpread.GetSpreadNoStkPrice('S', 9, CurrStock, TradeDB, out CanBuy, out CanSell);
            passInfoToStockQuoteForm(stkPrice.ToString(), CanBuy, CanSell, true);
        }

        private void StockQuoteForm_Load(object sender, EventArgs e)
        {
            ExchangeCheckBoxChanged(null, null);

            OnFormListChange();
        }

        private void StockQuoteForm_Shown(object sender, EventArgs e)
        {
            for (int i = 0; i < comboBoxPlaceOrderTo.Items.Count; i++)
            {
                if (((ComboBoxItem)comboBoxPlaceOrderTo.Items[i]).Value == LocalFormSettings["OrderTicketInstanceID"])
                {
                    comboBoxPlaceOrderTo.SelectedIndex = i;
                    break;
                }
            }

            AddStkQuoteDict();

            comboBoxPlaceOrderTo.Location = new Point(labelLotSize.Location.X + 2, labelLotSize.Location.Y + 23);
            //IsStratup = false;

            List<BaseForm> AccountBookFormList = BaseForm.GetFormByFormType(typeof(AccountForm));
            if (AccountBookFormList != null && AccountBookFormList.Count > 0 && AccountBookFormList[0] != null && ((AccountForm)AccountBookFormList[0]).IsAccountComboEnabled())
            {
                AccountForm accForm = ((AccountForm)AccountBookFormList[0]);
                string accno = accForm.GetCurrentAccountno();
                if (accno != null) { SetAccountno(accno); }
            }

            if (textBoxCode.Visible)
            {
                ListenedStockCode = LocalFormSettings["StockCode"];
                textBoxCode.Focus();
            }
        }

        private void passInfoToStockQuoteForm(string stkPrice, bool CanBuy, bool CanSell, bool SetOrderTicketFocus)
        {
            //decimal dStkPrice;
            if (comboBoxPlaceOrderTo.SelectedIndex <= 0)// || !decimal.TryParse(stkPrice, out dStkPrice) || dStkPrice < 0)
                return;
            string strSelectedInstanceID = ((ComboBoxItem)comboBoxPlaceOrderTo.SelectedItem).Value;
            ExchangeTypeEnum exType;
            if (checkBoxSHG.Checked)
                exType = ExchangeTypeEnum.SHG;
            else if (checkBoxSZE.Checked)
                exType = ExchangeTypeEnum.SZE;
            else if (checkBoxGray.Checked)
                exType = ExchangeTypeEnum.Gray;
            else
                exType = ExchangeTypeEnum.HKG;
            if (Utils.Utils.IsTargetForm("OrderTicketForm_simple", strSelectedInstanceID))
            {
                OrderTicketForm_simple OTFrm = BaseForm.GetFormByInstanceId(strSelectedInstanceID) as OrderTicketForm_simple;
                if (OTFrm != null)
                {
                    OTFrm.setStk_Price_Code(stkPrice, ListenedStockCode, CanBuy, CanSell, SetOrderTicketFocus, exType); 
                    if (SetOrderTicketFocus)
                        OTFrm.Focus(); 
                }
            }
            else if (Utils.Utils.IsTargetForm("FastOrderTicketForm", strSelectedInstanceID))
            {
                FastOrderTicketForm Fast_OT_Form = BaseForm.GetFormByInstanceId(strSelectedInstanceID) as FastOrderTicketForm;
                if (Fast_OT_Form != null)
                {
                    Fast_OT_Form.setStk_Price_Code(stkPrice, ListenedStockCode, CanBuy, CanSell, SetOrderTicketFocus);
                    if (SetOrderTicketFocus)
                        Fast_OT_Form.Focus(); 
                }
            }
            else if (Utils.Utils.IsTargetForm("OrderTicketForm", strSelectedInstanceID))
            {
                OrderTicketForm OTFrm = BaseForm.GetFormByInstanceId(strSelectedInstanceID) as OrderTicketForm;
                if (OTFrm != null)
                {
                    OTFrm.setStk_Price_Code(stkPrice, ListenedStockCode, CanBuy, CanSell, SetOrderTicketFocus, exType);
                    if (SetOrderTicketFocus)
                        OTFrm.Focus(); 
                }
            }
        }

        protected override void OnFormListChange()
        {
            int count = 0;
            int counter2 = 0;
            List <BaseForm> OTS_FormList = BaseForm.GetFormByFormType(typeof(OrderTicketForm_simple));
            List<BaseForm> OT_FormList = BaseForm.GetFormByFormType(typeof(OrderTicketForm));
            List<BaseForm> FastOT_FormList = BaseForm.GetFormByFormType(typeof(FastOrderTicketForm));

            string oldInstanceID = "";
            if (comboBoxPlaceOrderTo.SelectedIndex >= 0)
            {   // save old selection before clear
                oldInstanceID = ((ComboBoxItem)comboBoxPlaceOrderTo.Items[comboBoxPlaceOrderTo.SelectedIndex]).Value;
            }
            comboBoxPlaceOrderTo.SelectedIndex = -1;    // clear selection before clear all items
            comboBoxPlaceOrderTo.Items.Clear();
            comboBoxPlaceOrderTo.Items.Add(new ComboBoxItem("", ""));
            if (OTS_FormList != null)
            {
                foreach (BaseForm baseForm in OTS_FormList)
                {
                    count++;
                    comboBoxPlaceOrderTo.Items.Add(new ComboBoxItem(baseForm.DocumentName, baseForm.InstanceId));
                }
            }
            if (OT_FormList != null)
            {
                foreach (BaseForm baseForm in OT_FormList)
                {
                    count++;
                    comboBoxPlaceOrderTo.Items.Add(new ComboBoxItem(baseForm.DocumentName, baseForm.InstanceId));
                }
            }
            if (FastOT_FormList != null)
            {
                foreach (BaseForm baseForm in FastOT_FormList)
                {
                    count++;
                    comboBoxPlaceOrderTo.Items.Add(new ComboBoxItem(baseForm.DocumentName, baseForm.InstanceId));
                }
            }
            if (count > 0)
            {
                comboBoxPlaceOrderTo.SelectedIndex = 0; // default set to first order ticket
                if (oldInstanceID != "")
                {   // restore original selection
                    foreach (ComboBoxItem CBItem in comboBoxPlaceOrderTo.Items)
                    {
                        if (oldInstanceID == CBItem.Value)
                            comboBoxPlaceOrderTo.SelectedIndex = counter2;
                        counter2++;
                    }
                }
            }
        }

        private void comboBoxPlaceOrderTo_SelectionChangeCommitted(object sender, EventArgs e)
        {
            RemoveStkQuoteDict();
            AddStkQuoteDict();
        }

        private void RemoveStkQuoteDict()
        {
            if (comboBoxPlaceOrderTo == null || oldOrderTicketInstanceID == "")
                return;

            if (Utils.Utils.IsTargetForm("OrderTicketForm_simple", oldOrderTicketInstanceID))
            {
                OrderTicketForm_simple OldOrderTicketForm = (OrderTicketForm_simple)BaseForm.GetFormByInstanceId(oldOrderTicketInstanceID);
                StockQuoteForm outSQForm = null;
                if (OldOrderTicketForm != null && OldOrderTicketForm.StkQuoteDict.TryGetValue(this.InstanceId, out outSQForm))
                    OldOrderTicketForm.StkQuoteDict.Remove(this.InstanceId);
            }
            else if (Utils.Utils.IsTargetForm("FastOrderTicketForm", oldOrderTicketInstanceID))
            {
                FastOrderTicketForm OldOrderTicketForm = (FastOrderTicketForm)BaseForm.GetFormByInstanceId(oldOrderTicketInstanceID);
                StockQuoteForm outSQForm = null;
                if (OldOrderTicketForm != null && OldOrderTicketForm.StkQuoteDict.TryGetValue(this.InstanceId, out outSQForm))
                    OldOrderTicketForm.StkQuoteDict.Remove(this.InstanceId);
            }
            else if (Utils.Utils.IsTargetForm("OrderTicketForm", oldOrderTicketInstanceID))
            {
                OrderTicketForm OldOrderTicketForm = (OrderTicketForm)BaseForm.GetFormByInstanceId(oldOrderTicketInstanceID);
                StockQuoteForm outSQForm = null;
                if (OldOrderTicketForm != null && OldOrderTicketForm.StkQuoteDict.TryGetValue(this.InstanceId, out outSQForm))
                    OldOrderTicketForm.StkQuoteDict.Remove(this.InstanceId);
            }
        }

        private void AddStkQuoteDict()
        {
            if (comboBoxPlaceOrderTo == null || comboBoxPlaceOrderTo.SelectedIndex < 0)
                return;

            string orderTicketInstanceID = ((ComboBoxItem)comboBoxPlaceOrderTo.Items[comboBoxPlaceOrderTo.SelectedIndex]).Value;
            LocalFormSettings["OrderTicketInstanceID"] = orderTicketInstanceID;
            if (Utils.Utils.IsTargetForm("OrderTicketForm_simple", orderTicketInstanceID))
            {
                OrderTicketForm_simple orderTicketForm = (OrderTicketForm_simple)BaseForm.GetFormByInstanceId(orderTicketInstanceID);
                if (orderTicketForm != null)
                    orderTicketForm.StkQuoteDict.Add(this.InstanceId, this);
            }
            else if (Utils.Utils.IsTargetForm("FastOrderTicketForm", orderTicketInstanceID))
            {
                FastOrderTicketForm orderTicketForm = (FastOrderTicketForm)BaseForm.GetFormByInstanceId(orderTicketInstanceID);
                if (orderTicketForm != null)
                    orderTicketForm.StkQuoteDict.Add(this.InstanceId, this);
            }
            else if (Utils.Utils.IsTargetForm("OrderTicketForm", orderTicketInstanceID))
            {
                OrderTicketForm orderTicketForm = (OrderTicketForm)BaseForm.GetFormByInstanceId(orderTicketInstanceID);
                if (orderTicketForm != null)
                    orderTicketForm.StkQuoteDict.Add(this.InstanceId, this);
            }
            oldOrderTicketInstanceID = orderTicketInstanceID;
        }

        private void StockQuoteForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            RemoveStkQuoteDict();
        }

        private void StockQuoteForm_Activated(object sender, EventArgs e)
        {
            textBoxCode.Focus();
            textBoxCode.SelectAll();
        }

        private void StockQuoteForm_MouseClick(object sender, MouseEventArgs e)
        {
            textBoxCode.Focus();
            textBoxCode.SelectAll();
        }

        private void comboBoxPlaceOrderTo_MouseEnter(object sender, EventArgs e)
        {

        }

        private void checkBoxSZE_CheckedChanged(object sender, EventArgs e)
        {
            ExchangeCheckBoxChanged(sender, e);
            ListenedStockCode = textBoxCode.Text.Trim();
            LocalFormSettings["StockCode"] = ListenedStockCode;
            PassToOrderTicket(false);
        }

        private void checkBoxSHG_CheckedChanged(object sender, EventArgs e)
        {
            ExchangeCheckBoxChanged(sender, e);
            ListenedStockCode = textBoxCode.Text.Trim();
            LocalFormSettings["StockCode"] = ListenedStockCode;
            PassToOrderTicket(false);
        }

        private void checkBoxGray_CheckedChanged(object sender, EventArgs e)
        {
            ExchangeCheckBoxChanged(sender, e);
            ListenedStockCode = textBoxCode.Text.Trim();
            LocalFormSettings["StockCode"] = ListenedStockCode;
            PassToOrderTicket(false);
        }

        private void ExchangeCheckBoxChanged(object sender, EventArgs e)
        {
            this.checkBoxSZE.CheckedChanged -= pHandlerCBSZE;
            this.checkBoxSHG.CheckedChanged -= pHandlerCBSHG;
            this.checkBoxGray.CheckedChanged -= pHandlerCBGray;

            CheckBox TheCheckBox = sender as CheckBox;

            checkBoxSZE.Visible = !DisableSZE;
            checkBoxSHG.Visible = !DisableSHG;
            checkBoxGray.Visible = !DisableGray;
            textBoxCode.Visible = !DisableSZE || !DisableSHG || !DisableGray || !DisableHKG;

            if (TheCheckBox == null)
            {
                if (FormExType == ExchangeTypeEnum.SZE)
                {
                    TheCheckBox = checkBoxSZE;
                    TheCheckBox.Checked = true;
                }
                else if (FormExType == ExchangeTypeEnum.SHG)
                {
                    TheCheckBox = checkBoxSHG;
                    TheCheckBox.Checked = true;
                }
                else if (FormExType == ExchangeTypeEnum.Gray || FormExType == ExchangeTypeEnum.FTHKG || FormExType == ExchangeTypeEnum.PMHKG)
                {
                    TheCheckBox = checkBoxGray;
                    TheCheckBox.Checked = true;
                }
            }
            else if (DisableHKG && !TheCheckBox.Checked)
            {
                TheCheckBox.Checked = true;
            }

            if (TheCheckBox == null || !TheCheckBox.Checked)
            {
                LocalFormSettings["IsSZE"] = "0";
                LocalFormSettings["IsSHG"] = "0";
                LocalFormSettings["IsGrayStock"] = "0";
                checkBoxSZE.Checked = false;
                checkBoxSHG.Checked = false;
                checkBoxGray.Checked = false;

                if (DisableHKG)
                    FormExType = ExchangeTypeEnum.Unassigned;
                else
                    FormExType = ExchangeTypeEnum.HKG;

                this.BackColor = Color.FromArgb(255, 255, 205);
            }
            else if (TheCheckBox == checkBoxSZE)
            {
                LocalFormSettings["IsSHG"] = "0";
                LocalFormSettings["IsGrayStock"] = "0";
                checkBoxSHG.Checked = false;
                checkBoxGray.Checked = false;

                if (DisableSZE)
                {
                    LocalFormSettings["IsSZE"] = "0";
                    checkBoxSZE.Checked = false;
                    FormExType = ExchangeTypeEnum.Unassigned;
                    this.BackColor = Color.FromArgb(255, 255, 205);
                }
                else
                {
                    LocalFormSettings["IsSZE"] = "1";
                    FormExType = ExchangeTypeEnum.SZE;
                    this.BackColor = Color.FromArgb(218, 174, 232);
                }
            }
            else if (TheCheckBox == checkBoxSHG)
            {
                LocalFormSettings["IsSZE"] = "0";
                LocalFormSettings["IsGrayStock"] = "0";
                checkBoxSZE.Checked = false;
                checkBoxGray.Checked = false;

                if (DisableSHG)
                {
                    LocalFormSettings["IsSHG"] = "0";
                    checkBoxSHG.Checked = false;
                    FormExType = ExchangeTypeEnum.Unassigned;
                    this.BackColor = Color.FromArgb(255, 255, 205);
                }
                else
                {
                    LocalFormSettings["IsSHG"] = "1";
                    FormExType = ExchangeTypeEnum.SHG;
                    this.BackColor = Color.FromArgb(170, 255, 170);
                }
            }
            else if (TheCheckBox == checkBoxGray)
            {
                LocalFormSettings["IsSZE"] = "0";
                LocalFormSettings["IsSHG"] = "0";

                checkBoxSZE.Checked = false;
                checkBoxSHG.Checked = false;

                if (DisableGray)
                {
                    LocalFormSettings["IsGrayStock"] = "0";
                    checkBoxGray.Checked = false;
                    FormExType = ExchangeTypeEnum.Unassigned;
                    this.BackColor = Color.FromArgb(255, 255, 205);
                }
                else
                {
                    LocalFormSettings["IsGrayStock"] = "1";
                    if (FormExType == ExchangeTypeEnum.FTHKG)
                    {
                        FormExType = ExchangeTypeEnum.FTHKG;
                        this.BackColor = Color.FromArgb(124, 214, 255);
                    }
                    else if (FormExType == ExchangeTypeEnum.PMHKG)
                    {
                        FormExType = ExchangeTypeEnum.PMHKG;
                        this.BackColor = Color.FromArgb(255, 214, 124);
                    }
                    else
                    {
                        FormExType = ExchangeTypeEnum.Gray;
                        this.BackColor = Color.FromArgb(255, 214, 124);
                    }
                }
            }

            this.checkBoxSZE.CheckedChanged += pHandlerCBSZE;
            this.checkBoxSHG.CheckedChanged += pHandlerCBSHG;
            this.checkBoxGray.CheckedChanged += pHandlerCBGray;
        }

        /*
        private void checkBoxASHR_CheckedChanged(object sender, EventArgs e)
        {
            // central user setting
            DisableHKG = (SettingsForms["DisableHKG"] != null && SettingsForms["DisableHKG"] == "1");
            DisableSZE = (SettingsForms["DisableSZE"] != null && SettingsForms["DisableSZE"] == "1");
            DisableSHG = (SettingsForms["DisableSHG"] != null && SettingsForms["DisableSHG"] == "1");
            DisablePMHKG = (SettingsForms["DisablePMHKG"] != null && SettingsForms["DisablePMHKG"] == "1");
            string tempStockCode = textBoxCode.Text.Trim();
            ListenedStockCode = "";
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
                this.BackColor = Color.FromArgb(170, 255, 170);
                if (!checkBoxBuyAllCredit.Checked)
                    checkBoxBuyAllCredit.BackColor = Color.FromArgb(170, 255, 170);
                checkBoxSHG.BackColor = Color.FromArgb(111, 168, 111);
                checkBoxSHG.ForeColor = Color.White;
                checkBoxSZE.BackColor = Color.FromArgb(170, 255, 170);
                checkBoxSZE.ForeColor = Color.Black;
                checkBoxGray.BackColor = Color.FromArgb(170, 255, 170);
                checkBoxGray.ForeColor = Color.Black;
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
                this.BackColor = Color.FromArgb(218, 174, 232);
                if (!checkBoxBuyAllCredit.Checked)
                    checkBoxBuyAllCredit.BackColor = Color.FromArgb(218, 174, 232);
                checkBoxSZE.BackColor = Color.FromArgb(157, 89, 179);
                checkBoxSZE.ForeColor = Color.White;
                checkBoxSHG.BackColor = Color.FromArgb(218, 174, 232);
                checkBoxSHG.ForeColor = Color.Black;
                checkBoxGray.BackColor = Color.FromArgb(218, 174, 232);
                checkBoxGray.ForeColor = Color.Black;
                checkBoxSHG.Checked = false;
                checkBoxGray.Checked = false;
            }
            else if ((tempCheckBox == checkBoxGray) && checkBoxGray.Checked)
            {
                //if (DisablePMHKG)
                //{
                //    checkBoxPMHKG.Checked = false;
                //    return;
                //}
                if (DisableHKG)
                {
                    checkBoxGray.Enabled = false;
                    checkBoxSHG.Enabled = true;
                }
                LocalFormSettings["IsPMHKG"] = "1";
                LocalFormSettings["IsSZE"] = "0";
                LocalFormSettings["IsSHG"] = "0";
                FormExType = ExchangeTypeEnum.PMHKG;
                this.BackColor = Color.FromArgb(255, 214, 124);
                if (!checkBoxBuyAllCredit.Checked)
                    checkBoxBuyAllCredit.BackColor = Color.FromArgb(255, 214, 124);
                checkBoxGray.BackColor = Color.FromArgb(247, 184, 46);
                checkBoxGray.ForeColor = Color.White;
                checkBoxSHG.BackColor = Color.FromArgb(255, 214, 124);
                checkBoxSHG.ForeColor = Color.Black;
                checkBoxSZE.BackColor = Color.FromArgb(255, 214, 124);
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
                this.BackColor = Color.FromArgb(255, 255, 205);
                if (!checkBoxBuyAllCredit.Checked)
                    checkBoxBuyAllCredit.BackColor = Color.FromArgb(255, 255, 205);
                checkBoxSHG.BackColor = Color.FromArgb(255, 255, 205);
                checkBoxSHG.ForeColor = Color.Black;
                checkBoxSZE.BackColor = Color.FromArgb(255, 255, 205);
                checkBoxSZE.ForeColor = Color.Black;
                checkBoxGray.BackColor = Color.FromArgb(255, 255, 205);
                checkBoxGray.ForeColor = Color.Black;
            }
            ListenedStockCode = tempStockCode;
            textBoxCode.Focus();
            textBoxCode.SelectAll();
            if (!IsStratup)
                PassToOrderTicket(false);
        }
         */

        private void mnuBuyRightClick_Click(object sender, EventArgs e)
        {
            string Sellinfo = sender.ToString();

            if (Sellinfo.Length > 3 && Sellinfo.Substring(0, 3) == "---") { return; }

            if (OneClickOrder.StockCode == null || labelStockCode.Text.Trim() != OneClickOrder.StockCode.Trim()) { return; }
            if (OneClickOrder.Price <= 0) { return; }
            if (OneClickOrder.Quantity <= 0) { return; }

            BuySellConfirmationForm bsForm = new BuySellConfirmationForm(this.Culture, null);
            bsForm.IsBuyOrder = false;
            bsForm.strAcc = buttonAccno.Text; //"1888"; // AccountNoComboBox1.Text.Trim();
            bsForm.strBuySell = GetResxString("Sell").Trim();  //buttonBuy.Text;
            bsForm.strExchangeCode = Stock.GetExchangeCode(FormExType);
            bsForm.strStkCode = OneClickOrder.StockCode.Trim();
            //////if (CurrStock != null && CurrStock.Currency != null)
            //////    bsForm.strStkPrice = GetBestCurrencySign(CurrStock.Currency) + textBoxStockPrice.Text + "  ";
            //////else
            bsForm.strStkPrice = "$" + OneClickOrder.Price.ToString(); // Sellinfo.Substring(0, Sellinfo.IndexOf(" ")).Trim(); // "$91"; // + textBoxStockPrice.Text;
            bsForm.strStkQty = OneClickOrder.Quantity.ToString("#,##,##0"); // Sellinfo.Substring(Sellinfo.IndexOf(" ", Sellinfo.IndexOf(" ") + 1)).Trim();  //"1,000"; //textBoxStockQty.Text;
            //////if (CurrStock != null && CurrStock.Currency != null)
            //////    bsForm.strStkTtlAmt = GetBestCurrencySign(CurrStock.Currency) + labelTotal.Text + "  (" + GetGeneralResxString("Currency_" + CurrStock.Currency) + ")";
            //////else
            bsForm.strStkTtlAmt = "$" + (OneClickOrder.Price * OneClickOrder.Quantity).ToString("#,###,##0"); // (decimal.Parse(bsForm.strStkPrice.Replace("$", "")) * int.Parse(bsForm.strStkQty.Replace(",", ""))).ToString("#,###,##0"); // "$2000";// + labelTotal.Text;
            bsForm.IsAuctionType = false; // checkBoxAuction.Checked;
            bsForm.RequireOTP = (SettingsForms["SkipOrderOTP"] != "1" && TradeDB.RequireOTP) ? true : false;
            bsForm.StartPosition = FormStartPosition.CenterScreen;

            //Check Warnning amount            
            //////decimal decTotal = 2000;
            //////decimal.TryParse(labelTotal.Text, out total);
            //////decimal decTotal = OneClickOrder.Price * OneClickOrder.Quantity;
            //////if (decTotal > 300000)
            //////{
            //////    TotalAmtLimitForm limitForm;
            //////    if (CurrStock != null && CurrStock.Currency != null)
            //////        limitForm = new TotalAmtLimitForm(this.Culture, null, decTotal.ToString("#,###,##0"), 300000, GetBestCurrencySign(CurrStock.Currency));
            //////    else
            //////        limitForm = new TotalAmtLimitForm(this.Culture, null, decTotal.ToString("#,###,##0"), 300000, "$");
            //////    if (limitForm.ShowDialog(this) != DialogResult.OK) return;
            //////}


            if ((SettingsUserPreference["ConfirmBeforeOrder"] != null && SettingsUserPreference["ConfirmBeforeOrder"] == "0") || bsForm.ShowDialog(this) == DialogResult.OK)
            {
                PrepareOrder('A');

                AppendLog("1", "StockQuoteForm:" + bsForm.strBuySell + " Acc:" + bsForm.strAcc + " Stockcode:" + bsForm.strStkCode + " Price:" + bsForm.strStkPrice + " Qty:" + bsForm.strStkQty + " Amt:" + bsForm.strStkTtlAmt, "mnuBuyRightClick_Click_ok", false);


            }     

        }

        private void mnuSellRightClick_Click(object sender, EventArgs e)
        {
            string Buyinfo = sender.ToString();
            
            if (Buyinfo.Length > 3 && Buyinfo.Substring(0, 3) == "---") { return; }

            if (OneClickOrder.StockCode == null || labelStockCode.Text.Trim() != OneClickOrder.StockCode.Trim()) { return; }
            if (OneClickOrder.Price <= 0) { return; }
            if (OneClickOrder.Quantity <= 0) { return; }

            BuySellConfirmationForm bsForm = new BuySellConfirmationForm(this.Culture, null);
            bsForm.IsBuyOrder = true;
            bsForm.strAcc = buttonAccno.Text; //"1888"; // AccountNoComboBox1.Text.Trim();
            bsForm.strBuySell = GetResxString("Buy").Trim();  //buttonBuy.Text;
            bsForm.strExchangeCode = Stock.GetExchangeCode(FormExType);
            bsForm.strStkCode = OneClickOrder.StockCode.Trim();
            //////if (CurrStock != null && CurrStock.Currency != null)
            //////    bsForm.strStkPrice = GetBestCurrencySign(CurrStock.Currency) + textBoxStockPrice.Text + "  ";
            //////else
            bsForm.strStkPrice = "$" + OneClickOrder.Price.ToString(); // Buyinfo.Substring(0, Buyinfo.IndexOf(" ")).Trim(); // "$91"; // + textBoxStockPrice.Text;
            bsForm.strStkQty = OneClickOrder.Quantity.ToString("#,##,##0"); // Buyinfo.Substring(Buyinfo.IndexOf(" ", Buyinfo.IndexOf(" ") + 1)).Trim();  //"1,000"; //textBoxStockQty.Text;
            //////if (CurrStock != null && CurrStock.Currency != null)
            //////    bsForm.strStkTtlAmt = GetBestCurrencySign(CurrStock.Currency) + labelTotal.Text + "  (" + GetGeneralResxString("Currency_" + CurrStock.Currency) + ")";
            //////else
            bsForm.strStkTtlAmt = "$" + (OneClickOrder.Price * OneClickOrder.Quantity).ToString("#,###,##0"); // (decimal.Parse(bsForm.strStkPrice.Replace("$", "")) * int.Parse(bsForm.strStkQty.Replace(",", ""))).ToString("#,###,##0"); // "$2000";// + labelTotal.Text;
            bsForm.IsAuctionType = false; // checkBoxAuction.Checked;
            bsForm.RequireOTP = false;
            bsForm.StartPosition = FormStartPosition.CenterScreen;

            //Check Warnning amount            
            //////decimal decTotal = 2000;
            //////decimal.TryParse(labelTotal.Text, out total);
            decimal decTotal = OneClickOrder.Price * OneClickOrder.Quantity;
            if (decTotal > 300000)
            {
                if (SettingsUserPreference["ConfirmBeforeOrder"] == null || SettingsUserPreference["ConfirmBeforeOrder"] != "0")
                {
                    TotalAmtLimitForm limitForm;
                    if (CurrStock != null && CurrStock.Currency != null)
                        limitForm = new TotalAmtLimitForm(this.Culture, null, decTotal.ToString("#,###,##0"), 300000, GetBestCurrencySign(CurrStock.Currency));
                    else
                        limitForm = new TotalAmtLimitForm(this.Culture, null, decTotal.ToString("#,###,##0"), 300000, "$");
                    if (limitForm.ShowDialog(this) != DialogResult.OK) return;                    
                }
            }


            if ((SettingsUserPreference["ConfirmBeforeOrder"] != null && SettingsUserPreference["ConfirmBeforeOrder"] == "0") || bsForm.ShowDialog(this) == DialogResult.OK)
            {

                PrepareOrder('B');

                AppendLog("2", "StockQuoteForm:" + bsForm.strBuySell + " Acc:" + bsForm.strAcc + " Stockcode:" + bsForm.strStkCode + " Price:" + bsForm.strStkPrice + " Qty:" + bsForm.strStkQty + " Amt:" + bsForm.strStkTtlAmt, "mnuSellRightClick_Click_ok", false);

            }     

        }

        private void labelBidVol0_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickBuy(1);
            
        }

        private void labelBidCount0_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickBuy(1);

        }


        private void labelBidVol1_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickBuy(2);

        }

        
        private void labelBidVol2_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickBuy(3);


        }


        private void labelBidCount1_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickBuy(2);

        }



        private void labelBidCount2_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickBuy(3);

        }


        private void labelBidVol3_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickBuy(4);
        }

        private void labelBidCount3_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickBuy(4);
        }

        private void labelBidVol4_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickBuy(5);
        }

        private void labelBidCount4_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickBuy(5);
        }

        private void labelBidVol5_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickBuy(6);
        }

        private void labelBidCount5_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickBuy(6);
        }

        private void labelBidVol6_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickBuy(7);
        }

        private void labelBidCount6_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickBuy(7);
        }

        private void labelBidVol7_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickBuy(8);
        }

        private void labelBidCount7_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickBuy(8);
        }

        private void labelBidVol8_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickBuy(9);
        }

        private void labelBidCount8_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickBuy(9);
        }

        private void labelBidVol9_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickBuy(10);
        }

        private void labelBidCount9_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickBuy(10);
        }

        private void labelAskVol0_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickSell(1);

        }

        private void labelAskVol1_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickSell(2);

        }


        private void labelAskVol2_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickSell(3);

        }

        private void labelAskCount0_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickSell(1);

        }


        private void labelAskCount1_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickSell(2);
        }

        private void labelAskCount2_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickSell(3);
        }

        private void labelAskVol3_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickSell(4);
        }

        private void labelAskCount3_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickSell(4);
        }

        private void labelAskVol4_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickSell(5);
        }

        private void labelAskCount4_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickSell(5);
        }

        private void labelAskVol5_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickSell(6);
        }

        private void labelAskCount5_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickSell(6);
        }

        private void labelAskVol6_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickSell(7);
        }

        private void labelAskCount6_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickSell(7);
        }

        private void labelAskVol7_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickSell(8);
        }

        private void labelAskCount7_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickSell(8);
        }

        private void labelAskVol8_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickSell(9);
        }

        private void labelAskCount8_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickSell(9);
        }

        private void labelAskVol9_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickSell(10);
        }

        private void labelAskCount9_MouseDown(object sender, MouseEventArgs e)
        {
            ShowRightClickSell(10);
        }

        private void ShowRightClickBuy(int intSpreadCount)
        {
            if (CurrStock != null && CurrStock.BidVol != null && CurrStock.BidCount != null && buttonAccno.Text.Trim() != "" && checkBoxSHG.Checked == false && checkBoxSZE.Checked == false)
            {
                SpreadTableSet.SpreadTable spreadTable = TradeDB.SpreadTables[CurrStock.SpreadTableCode];

                decimal decPrice = 0;
                int resultIndex;

                if (intSpreadCount == 1)
                    decPrice = CurrStock.Bid > 0 ? CurrStock.Bid : 0;
                else if (intSpreadCount > 1)
                    decPrice = spreadTable.GetSpread(CurrStock.Bid, -(intSpreadCount - 1), out resultIndex);

                int SellQty = 0;
                if (CurrStock.BidVol[0] > 0 && decPrice > 0)
                {
                    if (intSpreadCount == 1)
                        SellQty = CheckOrderQty((int)(CurrStock.BidVol[0]), CurrStock.LotSize, 'S', decPrice, labelBuyLimit.Text);
                    else if (intSpreadCount == 2)
                        SellQty = CheckOrderQty((int)(CurrStock.BidVol[0] + CurrStock.BidVol[1]), CurrStock.LotSize, 'S', decPrice, labelBuyLimit.Text);
                    else if (intSpreadCount == 3)
                        SellQty = CheckOrderQty((int)(CurrStock.BidVol[0] + CurrStock.BidVol[1] + CurrStock.BidVol[2]), CurrStock.LotSize, 'S', decPrice, labelBuyLimit.Text);
                    else if (intSpreadCount == 4)
                        SellQty = CheckOrderQty((int)(CurrStock.BidVol[0] + CurrStock.BidVol[1] + CurrStock.BidVol[2] + CurrStock.BidVol[3]), CurrStock.LotSize, 'S', decPrice, labelBuyLimit.Text);
                    else if (intSpreadCount == 5)
                        SellQty = CheckOrderQty((int)(CurrStock.BidVol[0] + CurrStock.BidVol[1] + CurrStock.BidVol[2] + CurrStock.BidVol[3] + CurrStock.BidVol[4]), CurrStock.LotSize, 'S', decPrice, labelBuyLimit.Text);
                    else if (intSpreadCount == 6)
                        SellQty = CheckOrderQty((int)(CurrStock.BidVol[0] + CurrStock.BidVol[1] + CurrStock.BidVol[2] + CurrStock.BidVol[3] + CurrStock.BidVol[4] + CurrStock.BidVol[5]), CurrStock.LotSize, 'S', decPrice, labelBuyLimit.Text);
                    else if (intSpreadCount == 7)
                        SellQty = CheckOrderQty((int)(CurrStock.BidVol[0] + CurrStock.BidVol[1] + CurrStock.BidVol[2] + CurrStock.BidVol[3] + CurrStock.BidVol[4] + CurrStock.BidVol[5] + CurrStock.BidVol[6]), CurrStock.LotSize, 'S', decPrice, labelBuyLimit.Text);
                    else if (intSpreadCount == 8)
                        SellQty = CheckOrderQty((int)(CurrStock.BidVol[0] + CurrStock.BidVol[1] + CurrStock.BidVol[2] + CurrStock.BidVol[3] + CurrStock.BidVol[4] + CurrStock.BidVol[5] + CurrStock.BidVol[6] + CurrStock.BidVol[7]), CurrStock.LotSize, 'S', decPrice, labelBuyLimit.Text);
                    else if (intSpreadCount == 9)
                        SellQty = CheckOrderQty((int)(CurrStock.BidVol[0] + CurrStock.BidVol[1] + CurrStock.BidVol[2] + CurrStock.BidVol[3] + CurrStock.BidVol[4] + CurrStock.BidVol[5] + CurrStock.BidVol[6] + CurrStock.BidVol[7] + CurrStock.BidVol[8]), CurrStock.LotSize, 'S', decPrice, labelBuyLimit.Text);
                    else if (intSpreadCount == 10)
                        SellQty = CheckOrderQty((int)(CurrStock.BidVol[0] + CurrStock.BidVol[1] + CurrStock.BidVol[2] + CurrStock.BidVol[3] + CurrStock.BidVol[4] + CurrStock.BidVol[5] + CurrStock.BidVol[6] + CurrStock.BidVol[7] + CurrStock.BidVol[8] + CurrStock.BidVol[9]), CurrStock.LotSize, 'S', decPrice, labelBuyLimit.Text);

                }

                if (SellQty > 0)
                {
                    mnuBuyRightClick.Text = GetResxString("Sell2") + "$" + Stock.GetDeltaDecPlaceValue(decPrice, 1) + GetResxString("Sell3") + SellQty.ToString("#,###,##0") + " = [$" + (decPrice * SellQty).ToString("#,###,##0") + "]";
                    OneClickOrder.StockCode = labelStockCode.Text.Trim();
                    OneClickOrder.Price = decPrice;
                    OneClickOrder.Quantity = SellQty;
                }
                else
                { mnuBuyRightClick.Text = GetResxString("Nostockonhand"); ResetOneClickOrder(); }
            }
            else
            {
                if (buttonAccno.Text.Trim() == "")
                    mnuBuyRightClick.Text = GetResxString("Invalidaccountnumber");
                else if (CurrStock == null)
                    mnuBuyRightClick.Text = GetResxString("Invalidstockcode");
                else if (checkBoxSHG.Checked == true)
                    mnuBuyRightClick.Text = GetResxString("SupportHKStockOnly");
                else if (checkBoxSZE.Checked == true)
                    mnuBuyRightClick.Text = GetResxString("SupportHKStockOnly");
                else
                    mnuBuyRightClick.Text = "---";
                ResetOneClickOrder();
            }
        }


        private void ShowRightClickSell(int intSpreadCount)
        {
            if (CurrStock != null && CurrStock.AskVol != null && CurrStock.AskCount != null && buttonAccno.Text.Trim() != "" && checkBoxSHG.Checked == false && checkBoxSZE.Checked == false)
            {
                SpreadTableSet.SpreadTable spreadTable = TradeDB.SpreadTables[CurrStock.SpreadTableCode];

                decimal decPrice = 0;
                int resultIndex;

                if (intSpreadCount == 1)
                    decPrice = CurrStock.Ask > 0 ? CurrStock.Ask : 0;
                else if (intSpreadCount > 1)
                    decPrice = spreadTable.GetSpread(CurrStock.Ask, intSpreadCount - 1, out resultIndex);

                int BuyQty = 0;
                if (CurrStock.AskVol[0] > 0 && decPrice > 0)
                {
                    if (intSpreadCount == 1)
                        BuyQty = CheckOrderQty((int)CurrStock.AskVol[0], CurrStock.LotSize, 'B', decPrice, labelBuyLimit.Text);
                    else if (intSpreadCount == 2)
                        BuyQty = CheckOrderQty((int)(CurrStock.AskVol[0] + CurrStock.AskVol[1]), CurrStock.LotSize, 'B', decPrice, labelBuyLimit.Text);
                    else if (intSpreadCount == 3)
                        BuyQty = CheckOrderQty((int)(CurrStock.AskVol[0] + CurrStock.AskVol[1] + CurrStock.AskVol[2]), CurrStock.LotSize, 'B', decPrice, labelBuyLimit.Text);
                    else if (intSpreadCount == 4)
                        BuyQty = CheckOrderQty((int)(CurrStock.AskVol[0] + CurrStock.AskVol[1] + CurrStock.AskVol[2] + CurrStock.AskVol[3]), CurrStock.LotSize, 'B', decPrice, labelBuyLimit.Text);
                    else if (intSpreadCount == 5)
                        BuyQty = CheckOrderQty((int)(CurrStock.AskVol[0] + CurrStock.AskVol[1] + CurrStock.AskVol[2] + CurrStock.AskVol[3] + CurrStock.AskVol[4]), CurrStock.LotSize, 'B', decPrice, labelBuyLimit.Text);
                    else if (intSpreadCount == 6)
                        BuyQty = CheckOrderQty((int)(CurrStock.AskVol[0] + CurrStock.AskVol[1] + CurrStock.AskVol[2] + CurrStock.AskVol[3] + CurrStock.AskVol[4] + CurrStock.AskVol[5]), CurrStock.LotSize, 'B', decPrice, labelBuyLimit.Text);
                    else if (intSpreadCount == 7)
                        BuyQty = CheckOrderQty((int)(CurrStock.AskVol[0] + CurrStock.AskVol[1] + CurrStock.AskVol[2] + CurrStock.AskVol[3] + CurrStock.AskVol[4] + CurrStock.AskVol[5] + CurrStock.AskVol[6]), CurrStock.LotSize, 'B', decPrice, labelBuyLimit.Text);
                    else if (intSpreadCount == 8)
                        BuyQty = CheckOrderQty((int)(CurrStock.AskVol[0] + CurrStock.AskVol[1] + CurrStock.AskVol[2] + CurrStock.AskVol[3] + CurrStock.AskVol[4] + CurrStock.AskVol[5] + CurrStock.AskVol[6] + CurrStock.AskVol[7]), CurrStock.LotSize, 'B', decPrice, labelBuyLimit.Text);
                    else if (intSpreadCount == 9)
                        BuyQty = CheckOrderQty((int)(CurrStock.AskVol[0] + CurrStock.AskVol[1] + CurrStock.AskVol[2] + CurrStock.AskVol[3] + CurrStock.AskVol[4] + CurrStock.AskVol[5] + CurrStock.AskVol[6] + CurrStock.AskVol[7] + CurrStock.AskVol[8]), CurrStock.LotSize, 'B', decPrice, labelBuyLimit.Text);
                    else if (intSpreadCount == 10)
                        BuyQty = CheckOrderQty((int)(CurrStock.AskVol[0] + CurrStock.AskVol[1] + CurrStock.AskVol[2] + CurrStock.AskVol[3] + CurrStock.AskVol[4] + CurrStock.AskVol[5] + CurrStock.AskVol[6] + CurrStock.AskVol[7] + CurrStock.AskVol[8] + CurrStock.AskVol[9]), CurrStock.LotSize, 'B', decPrice, labelBuyLimit.Text);

                }

                if (BuyQty > 0)
                {
                    mnuSellRightClick.Text = GetResxString("Buy2") + "$" + Stock.GetDeltaDecPlaceValue(decPrice, 1) + GetResxString("Buy") + BuyQty.ToString("#,###,##0") + " = [$" + (decPrice * BuyQty).ToString("#,###,##0") + "]";
                    OneClickOrder.StockCode = labelStockCode.Text.Trim();
                    OneClickOrder.Price = decPrice;
                    OneClickOrder.Quantity = BuyQty;
                }
                else
                { mnuSellRightClick.Text = GetResxString("Insufficientbuyingpower"); ResetOneClickOrder(); }

            }
            else
            {
                if (buttonAccno.Text.Trim() == "")
                    mnuSellRightClick.Text = GetResxString("Invalidaccountnumber");
                else if (CurrStock == null)
                    mnuSellRightClick.Text = GetResxString("Invalidstockcode");
                else if (checkBoxSHG.Checked == true)
                    mnuSellRightClick.Text = GetResxString("SupportHKStockOnly");
                else if (checkBoxSZE.Checked == true)
                    mnuSellRightClick.Text = GetResxString("SupportHKStockOnly");
                else
                    mnuSellRightClick.Text = "---";
                ResetOneClickOrder();
            }
        }



        private void ResetOneClickOrder()
        {
            OneClickOrder.Price = 0;
            OneClickOrder.Quantity = 0;
        }

        private void textBoxAccno_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBoxAccno_DoubleClick(object sender, EventArgs e)
        {

        }

        private void textBoxAccno_Click(object sender, EventArgs e)
        {

        }

        private int CheckOrderQty(int intOrderQty, int intLotsize, char chrBuySell, decimal decPrice, string strBuylimit)  // return correct qty
        {
            decimal decComm = 0.01M;

            if (intLotsize <= 0) return -1;
            if (intOrderQty <= 0) return -1;            
            if (chrBuySell == 'B' && decPrice <= 0) return -1;
            if (checkBoxSHG.Checked == true) return -1;
            if (checkBoxSZE.Checked == true) return -1;

            decimal decBuylimit = 0;
            int intAdjustQty = 0;

            if (chrBuySell == 'B')
            {
                //////decimal.TryParse(strBuylimit, out decBuylimit);

                decBuylimit = CheckMaxBuyPower(strBuylimit, 1);

                if (decBuylimit <= 0) return -1;

                decBuylimit = decBuylimit * (1 - decComm);
                if (decPrice * intOrderQty < decBuylimit)
                    intAdjustQty = intOrderQty;
                else
                {
                    decimal MaxBuyQty = Math.Truncate(decBuylimit / (intLotsize * decPrice)) * intLotsize;

                    MaxBuyQty = MaxBuyQty >= 0 ? MaxBuyQty : 0;

                    intAdjustQty = (int)MaxBuyQty;
                }
            }
            else if (chrBuySell == 'S')
            {
                intAdjustQty = intOrderQty;
                int intMaxSellQty = SellMax();
                if (intMaxSellQty <= 0) { return -1; }

                if (checkBoxSellAll.Checked == true)
                {
                    intAdjustQty = intMaxSellQty;
                }
                else
                {
                    if (intAdjustQty > intMaxSellQty)
                    { intAdjustQty = intMaxSellQty; }
                }
            }
            
            if (intAdjustQty <= intLotsize * 3000)
                return intAdjustQty;
            else
                return intLotsize * 3000;
            
        }

        public void SetAccountno(string strAccountno)
        {
            ListenedAccount = strAccountno;
        }

        private void buttonSearchOrder_Click(object sender, EventArgs e)
        {

        }

        private void textBoxAccno_TextChanged_1(object sender, EventArgs e)
        {

        }


        private decimal CheckMaxBuyPower(string strBuylimit, int intType) // intType 1-Get Min(buylimit, buypower), 2-Getbuypower
        {
            decimal decBuylimit = 0;
            decimal.TryParse(strBuylimit, out decBuylimit);
            decimal BuyPower = 0; //CurrAccount.T2DayBal + CurrAccount.FundHold + CurrAccount.Interest;

            if (checkBoxSHG.Checked == true) return -1;
            if (checkBoxSZE.Checked == true) return -1;
            if (CurrAccount == null) return -1;

            if (CurrAccount.CreditClass != null && CurrAccount.CreditClass.Trim() != "")
                BuyPower = CurrAccount.Balances["***"].AvailableCredit;
            else if (CurrAccount.Balances.ContainsKey("***"))
            {
                if (CurrAccount.AccountType == 'C' || CurrAccount.AccountType == 'T')
                {
                    BuyPower = CurrAccount.Balances["***"].T2DayBal + CurrAccount.Balances["***"].FundHold + CurrAccount.Balances["***"].Interest;
                    BuyPower = Math.Max(0, BuyPower + CurrAccount.Balances["***"].AcceptableMarketValue * CurrAccount.Balances["***"].CreditIndex * 0.01M);
                }
                else if (CurrAccount.AccountType == 'M')
                {
                    BuyPower = CurrAccount.Balances["***"].T2DayBal + CurrAccount.Balances["***"].FundHold + CurrAccount.Balances["***"].Interest;
                    BuyPower = Math.Max(0, BuyPower + Math.Min(CurrAccount.Balances["***"].CreditLimit, CurrAccount.Balances["***"].AcceptableMarketValue));
                }
            }
            if (intType == 2) return BuyPower;

            if (decBuylimit > BuyPower)            
                return BuyPower;
            else
                return decBuylimit;
            
        }


        private int SellMax()
        {
            if (checkBoxSHG.Checked == true) return -1;
            if (checkBoxSZE.Checked == true) return -1;
            if (CurrAccount == null) return -1;
            if (CurrStock == null || CurrStock.LotSize <= 0) return -1;

            decimal QtyRemains = 0;
            decimal MaxSellQty = 0;
            int stkCode = 0, acStkCode;
            int.TryParse(labelStockCode.Text, out stkCode);

            if (stkCode == 0) return -1;

            foreach (KeyValuePair<string, AccountStock> kvp in CurrAccount.Stocks)
            {
                if (kvp.Value != null && int.TryParse(kvp.Value.Code, out acStkCode))
                {
                    if (stkCode == acStkCode)
                    {
                        if (FormExType == ExchangeTypeEnum.HKG)
                            QtyRemains = kvp.Value.QtyOnHand + kvp.Value.QtyInTransit;
                        else if (FormExType == ExchangeTypeEnum.SHG || FormExType == ExchangeTypeEnum.SZE)
                            QtyRemains = kvp.Value.QtyOnHand + kvp.Value.QtyInTransitSold;
                        break;
                    }
                }
            }

            if (QtyRemains >= 0)
            {
                if (FormExType == ExchangeTypeEnum.HKG)
                    MaxSellQty = Math.Truncate(((decimal)QtyRemains / CurrStock.LotSize)) * CurrStock.LotSize;
                else if (FormExType == ExchangeTypeEnum.SHG || FormExType == ExchangeTypeEnum.SZE)
                    MaxSellQty = QtyRemains;
            }

            return (int)MaxSellQty;
            //////textBoxStockQty.Text = String.Format("{0:N0}", MaxSellQty);
            //////calAmount();
        }

        private void PrepareOrder(char BuySell)
        {
            //////return;

            Order order1 = new Order();
            order1.AccountNo = ListenedAccount.ToUpper().Trim(); // CurrAccount.AccountNo.ToUpper().Trim();
            order1.StockCode = OneClickOrder.StockCode.Trim();
            order1.Side = BuySell;
            //////decimal.TryParse(textBoxStockPrice.Text, out order1.Price);
            order1.Price = OneClickOrder.Price;
            //////int.TryParse(this.textBoxStockQty.Text.Replace(",", ""), out order1.Quantity);
            order1.Quantity = OneClickOrder.Quantity;
            //////if (checkBoxAuction.Checked == true)
            //////order1.OrderType = 'A';
            //////else if (checkBoxSpecialLimit.Checked == true)
            if (checkBoxSpecialLimit.Checked == true)
            {
                order1.OrderType = 'S';  //order1.OrderType = 'X';
            }
            else
            {
                order1.OrderType = 'X';
            }

            if (FormExType == ExchangeTypeEnum.SHG || FormExType == ExchangeTypeEnum.SZE) { order1.OrderType = 'L'; }

            order1.Filled = 0;
            //////bool shortonhand = (checkBoxStockOnHand.Checked) ? true : false;
            bool shortonhand = false;
            //////bool allornothing = (checkBoxAllOrNothing.Checked) ? true : false;
            bool allornothing = false;
            if (checkBoxAllOrNothing.Checked == true)
            {
                allornothing = true;
            }
            
            string Message;
            string actRef;
            bool reply = TradeDB.OrderPlace(order1.AccountNo, order1.Side, order1.StockCode, order1.Price, order1.Quantity, order1.OrderType, allornothing, shortonhand, 0, FormExType, out Message, out actRef);
            if (reply == false)
            {
                ShowMessageBox(GetResxString("PlaceOrderError"), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                //(GetFormByFormType(typeof(MainForm))[0] as MainForm).RequestToLogout();
            }
        }


        private void labelBuyLimit_DoubleClick(object sender, EventArgs e)
        {
            checkBoxBuyAllCredit.Checked = false;
            textBoxBuyLimit.Visible = true;
            textBoxBuyLimit.Text = "";
            labelBuyLimit.Text = "";
            labelBuyLimit.Visible = false;            
            textBoxBuyLimit.Focus();            
        }

        private void textBoxBuyLimit_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                long lngBuylimit = 0;

                long.TryParse(textBoxBuyLimit.Text, out lngBuylimit);
                labelBuyLimit.Text = lngBuylimit.ToString("#,###,##0");

                AppendLog("1", "StockQuoteForm:" + " textBoxBuyLimit:" + textBoxBuyLimit.Text + " labelBuyLimit:" + labelBuyLimit.Text, "textBoxBuyLimit_KeyPress", false);

                textBoxBuyLimit.Text = "";
                labelBuyLimit.Visible = true;
                textBoxBuyLimit.Visible = false;
            }
        }

        private void buttonAccno_Click(object sender, EventArgs e)
        {
            List<BaseForm> OrderBookFormList = BaseForm.GetFormByFormType(typeof(OrderBookForm));

            if (OrderBookFormList != null && OrderBookFormList.Count > 0)
                ((OrderBookForm)OrderBookFormList[0]).QuoteFormShowSearch(textBoxCode.Text, buttonAccno.Text);
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


        private void checkBoxBuyAllCredit_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxBuyAllCredit.Checked == true)
            {
                SetAllin();
            }
            else
            {
                SetNonAllin();
            }

        }

        private void SetAllin()
        {
            checkBoxBuyAllCredit.BackColor = Color.FromArgb(255, 192, 192);

            decimal decBuylimit = CheckMaxBuyPower("0", 2);
            if (decBuylimit <= 0) decBuylimit = 0;
            labelBuyLimit.Text = decBuylimit.ToString("#,###,##0");

            AppendLog("2", "StockQuoteForm:" + " decBuylimit:" + decBuylimit + " labelBuyLimit:" + labelBuyLimit.Text, "SetAllin", false);

            textBoxBuyLimit.Text = "";
            labelBuyLimit.Visible = true;
            textBoxBuyLimit.Visible = false;
        }

        private void SetNonAllin()
        {
            if (checkBoxSHG.Checked)
                checkBoxBuyAllCredit.BackColor = Color.FromArgb(170, 255, 170);
            else if (checkBoxSZE.Checked)
                checkBoxBuyAllCredit.BackColor = Color.FromArgb(218, 174, 232);
            else
                checkBoxBuyAllCredit.BackColor = Color.FromArgb(255, 255, 205);

            AppendLog("3", "StockQuoteForm:" + " decBuylimit:0" + " labelBuyLimit:" + labelBuyLimit.Text, "SetNonAllin", false);

            labelBuyLimit.Text = "0";
            textBoxBuyLimit.Text = "";
            labelBuyLimit.Visible = true;
            textBoxBuyLimit.Visible = false;
        }

        private void AutoBuy(Stock stock1)
        {
            if (checkBoxAutoBuy.Checked == false) return;

            decimal decAutoBuyPrice = 0;
            decimal.TryParse(textBoxAutoBuy.Text, out decAutoBuyPrice);

            if (decAutoBuyPrice > 0 && decAutoBuyPrice == stock1.Ask)
            {
                MessageBox.Show(stock1.Ask + " Price match");

                AppendLog("4", "StockQuoteForm:" + " decAutoBuyPrice:" + decAutoBuyPrice.ToString() + " Ask:" + stock1.Ask.ToString(), "AutoBuy", false);
            }
        }

        private void checkBoxAutoBuy_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxAutoBuy.Checked == true)
            {
                checkBoxAutoBuy.BackColor = Color.FromArgb(255, 192, 192);
            }
            else
            {
                checkBoxAutoBuy.BackColor = Color.FromArgb(255, 255, 205);
            }
        }

        private void timerUpdateStatusLabel_Tick(object sender, EventArgs e)
        {
            ShowTradeStatus(CurrStock);
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
