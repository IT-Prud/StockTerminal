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
    public partial class TotalAmtLimitForm : BaseForm
    {
        string ttlAmt, currencySign;
        int limit;

        public TotalAmtLimitForm()
        {
            InitializeComponent();
        }

        public TotalAmtLimitForm(CultureInfo Culture, string PersistString, string ttlAmt, int limit, string currencySign)
            : base(Culture, PersistString)
        {
            InitializeComponent();
            this.ttlAmt = ttlAmt;
            this.currencySign = currencySign;
            this.limit = limit;
        }

        private void TotalAmtLimitForm_Shown(object sender, EventArgs e)
        {
            if (currencySign == "CHINEXT")
            {
                label1.Visible = false;
                label2.Visible = true;
                label3.Visible = true;
                label4.Visible = false;
                label5.Visible = true;
                label2.Text = GetResxString("ConfirmMsg4");
                label3.Text = GetResxString("ConfirmMsg5");
                label5.Text = GetResxString("ConfirmMsg6");
            }
            else
            {
                label1.Visible = true;
                label2.Visible = true;
                label3.Visible = true;
                label4.Visible = true;
                label5.Visible = false;
                label1.Text = GetResxString("ConfirmMsg1") + currencySign + String.Format("{0:N0}", ttlAmt);
                label4.Text = GetResxString("ConfirmMsg2") + currencySign + String.Format("{0:N0}", limit) + GetResxString("ConfirmMsg3");
                label2.Text = GetResxString("ConfirmMsg4");
                label3.Text = GetResxString("ConfirmMsg5");
                label5.Text = GetResxString("ConfirmMsg6");
            }
        }

        private void TotalAmtLimitForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            List<BaseForm> mainFormList = BaseForm.GetFormByFormType(typeof(MainForm));
            if (mainFormList != null && mainFormList.Count > 0 && mainFormList[0] != null)
            {
                MainForm mainForm = (MainForm)mainFormList[0];
                //mainForm.TopMost = true;
                mainForm.TopMost = false;
            }
        }

        private void myTextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Numbers only
            if (e.KeyChar == (char)Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                List<BaseForm> mainFormList = BaseForm.GetFormByFormType(typeof(MainForm));
                if (mainFormList != null && mainFormList.Count > 0 && mainFormList[0] != null)
                {
                    MainForm mainForm = (MainForm)mainFormList[0];
                    mainForm.TopMost = true;
                    //mainForm.TopMost = false;
                }
                //this.Close();
                this.Dispose();
            }
            else if (e.KeyChar == (char)Keys.Enter)
            {
                if (myTextBox1.Text == "258")
                {
                    this.DialogResult = DialogResult.OK;
                    List<BaseForm> mainFormList = BaseForm.GetFormByFormType(typeof(MainForm));
                    if (mainFormList != null && mainFormList.Count > 0 && mainFormList[0] != null)
                    {
                        MainForm mainForm = (MainForm)mainFormList[0];
                        mainForm.TopMost = true;
                        //mainForm.TopMost = false;
                    }
                    //this.Close();
                    this.Dispose();
                }
                else
                {
                    myTextBox1.SelectAll();
                }
            }
            else if ((e.KeyChar < (char)Keys.D0 || e.KeyChar > (char)Keys.D9) && //&& (e.KeyChar < (char)Keys.NumPad0 || e.KeyChar > (char)Keys.NumPad9)
                e.KeyChar != (char)Keys.Back && e.KeyChar != (char)Keys.Delete)
                e.Handled = true;
        }

    }
}
