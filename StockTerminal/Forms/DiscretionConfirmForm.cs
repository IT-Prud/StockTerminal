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
    public partial class DiscretionConfirmForm : BaseForm
    {
        public DiscretionConfirmForm()
        {
            InitializeComponent();
        }

        public DiscretionConfirmForm(CultureInfo Culture, string PersistString)
            : base(Culture, PersistString)
        {
            InitializeComponent();
        }

        private void DiscretionConfirmForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            List<BaseForm> mainFormList = BaseForm.GetFormByFormType(typeof(MainForm));
            if (mainFormList != null && mainFormList.Count > 0 && mainFormList[0] != null)
            {
                MainForm mainForm = (MainForm)mainFormList[0];
                mainForm.TopMost = true;
                mainForm.TopMost = false;
            }
        }

        private void DiscretionConfirmForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                this.Close();
        }

        private void DiscretionConfirmForm_Load(object sender, EventArgs e)
        {
            this.AcceptButton = buttonCancel;
            this.ActiveControl = (Control)buttonCancel;
        }
    }
}
