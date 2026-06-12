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
    public partial class ModifyOrder_ConfirmForm : BaseForm
    {
        public string FormName = "";
        public bool IsBuyOrder;
        public string strOrderNo, strExchangeCode, strStockCode, strStkPrice, strStkQty, AccountNo = "";
        public bool IsAmendOrder = true;     // Cancel order if set to false
        public bool RequireOTP = false;
        public string OTP = "";

        public ModifyOrder_ConfirmForm()
            : this(null, null)
        {
            //InitializeComponent();
        }

        public ModifyOrder_ConfirmForm(CultureInfo Culture, string PersistString) : base(Culture, PersistString)
        {
            InitializeComponent();
        }

        private void ModifyOrder_ConfirmForm_Shown(object sender, EventArgs e)
        {
            //this.Height = 329;
            this.ControlBox = false;
            if (IsBuyOrder)
                panel1.BackColor = Color.SkyBlue;
            else
                panel1.BackColor = Color.LightPink;
            labelOrderNo.Text = strOrderNo;
            labelStockCode.Text = strStockCode;
            labelExchangeCode.Text = strExchangeCode;
            labelStkPrice.Text = strStkPrice;
            labelStkQty.Text = strStkQty;
            labelAccountNo.Text = AccountNo;
            if (IsAmendOrder)
                labelHeader.Text = GetResxString("AmendOrderHeading");
            else
                labelHeader.Text = GetResxString("CancelOrderHeading");
            AppendLog(FormName, Environment.NewLine + " Yes/No button: " + button2.Text + " Order No.: " + labelOrderNo.Text + " Exchange: " + labelExchangeCode.Text + 
                " Stock Code: " + labelStockCode.Text + " Acc: " + labelAccountNo.Text + " Price: " + labelStkPrice.Text + 
                " Qty: " + labelStkQty.Text, (IsAmendOrder ? "Modify Order" : "Cancel Order"), false);
        }

        private void ModifyOrder_ConfirmForm_FormClosed(object sender, FormClosedEventArgs e)
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

        private void ModifyOrder_ConfirmForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                this.Close();
        }

        public void SetOTPVisible()
        {
            panelOTP.Visible = true;
        }

        private void ModifyOrder_ConfirmForm_Load(object sender, EventArgs e)
        {
            panelOTP.Visible = (RequireOTP) ? true : false;
            buttonOK.Enabled = (RequireOTP) ? false : true;
            if (RequireOTP)
                textBoxOTP.Focus();
            this.Size = new Size(this.Size.Width, buttonOK.Location.Y + buttonOK.Size.Height + 35);
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
            if (panelOTP.Visible)
                buttonOK.Enabled = (textBoxOTP.Text.Trim().Length > 0) ? true : false;
        }
    }
}
