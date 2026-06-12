using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Globalization;

namespace StockTerminal.Forms
{
    public partial class BuySellConfirmationForm : BaseForm
    {
        public bool IsBuyOrder;
        public string strAcc;
        public string strBuySell;
        public string strExchangeCode;
        public string strStkCode;
        public string strStkPrice;
        public string strStkQty;
        public string strStkTtlAmt;
        public bool IsAuctionType;
        public bool RequireOTP = false;
        public string OTP;
        public string Remarks;
        public bool ShowInlineWithInvestPref;

        public BuySellConfirmationForm()
        {
            InitializeComponent();
        }

        public BuySellConfirmationForm(CultureInfo Culture, string PersistString) : base(Culture, PersistString)
        {
            InitializeComponent();
        }

        private void BuySellConfirmationForm_Load(object sender, EventArgs e)
        {
            this.ControlBox = false;
            this.Size = new Size(this.Size.Width, buttonOK.Location.Y + buttonOK.Size.Height + 35);
            panelOTP.Visible = (SettingsForms["SkipOrderOTP"] != "1" && RequireOTP) ? true : false;
            buttonOK.Enabled = (SettingsForms["SkipOrderOTP"] != "1" && RequireOTP) ? false : true;
            if (RequireOTP)
                textBoxOTP.Focus();
            panel1.BackColor = (IsBuyOrder) ? Color.SkyBlue : Color.LightPink;
            labelAcc.Text = strAcc;
            labelBuySell.Text = strBuySell;
            labelStkCode.Text = strStkCode;
            labelExchangeCode.Text = strExchangeCode;
            labelStkPrice.Text = strStkPrice;
            labelStkQty.Text = strStkQty;
            labelTtlAmt.Text = strStkTtlAmt;
            labelOrderType.Text = (IsAuctionType) ? GetResxString("OrderType_Auction") : GetResxString("OrderType_Limit");

            labelRemarks.Text = Remarks;
            labelRemarks.Visible = (Remarks != null && Remarks.Trim().Length > 0) ? true : false;
            labelReminder.Visible = ShowInlineWithInvestPref;

            AppendLog("BuySellConfirmationForm", 
                "BuySell: " + labelBuySell.Text + (IsAuctionType ? " Auction " : " Limit ") + 
                " Acc: " + labelAcc.Text + 
                " Exchange: " + labelExchangeCode.Text +  
                " StkCode: " + labelStkCode.Text + 
                " StkPrice: " + labelStkPrice.Text + 
                " Qty: " + labelStkQty.Text + 
                " Amt: " + labelTtlAmt.Text + 
                " Type: " + labelOrderType.Text + 
                " Remarks: " + labelRemarks.Text, 
                "BuySell Order", false);
        }

        private void BuySellConfirmationForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            OTP = textBoxOTP.Text;
            List<BaseForm> mainFormList = BaseForm.GetFormByFormType(typeof(MainForm));
            if (mainFormList != null && mainFormList.Count > 0 && mainFormList[0] != null)
            {
                MainForm mainForm = (MainForm)mainFormList[0];
                mainForm.TopMost = true;
                mainForm.TopMost = false;
            }
        }

        private void BuySellConfirmationForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                this.Close();
        }

        private void textBoxOTP_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                if (textBoxOTP.Text.Trim().Length > 0)
                    buttonOK.Focus();
                return;
            }
        }

        private void textBoxOTP_KeyUp(object sender, KeyEventArgs e)
        {
            buttonOK.Enabled = (textBoxOTP.Text.Trim().Length > 0) ? true : false;
        }
    }
}
