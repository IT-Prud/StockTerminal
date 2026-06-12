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
    public partial class ETradeWebForm : BaseForm
    {
        private bool formShown = false;
        private string redirect = "";
        public string Redirect
        {
            get
            {
                return redirect;
            }
            set
            {
                redirect = value;
                LocalFormSettings["Redirect"] = value;
                Load_HTML();
            }
        }

        //public ETradeWebForm()
        //{
        //    InitializeComponent();
        //}

        public ETradeWebForm(CultureInfo Culture, string PersistString) : base(Culture, PersistString)
        {
            InitializeComponent();
            Redirect = LocalFormSettings["Redirect"];
        }

        private void ETradeWebForm_Shown(object sender, EventArgs e)
        {
            OnResize();

            formShown = true;
            //if (redirect == "InternetStockStatement")
            //    this.DocumentName = GetResxString("DocumentName_Statement");
            //else
            //    this.DocumentName = GetResxString("DocumentName_Settlement");
            Load_HTML();
        }

        private void Load_HTML()
        {
            if (!formShown || Redirect == null || Redirect.Trim() == "")
                return;

            string ETradeWebFormURL = "https://www.pru.hk";

            if (SettingsForms["ETradeWebURL"] != null && SettingsForms["ETradeWebURL"].Trim() != "")
                ETradeWebFormURL = SettingsForms["ETradeWebURL"];
            Uri url = null;

            try
            {
                string destWeb = ETradeWebFormURL + "/ETrade/Secured/Session/Login.asp?Action=Login&Redirect=" +
                    Redirect + "&UserID=" + TradeDB.UserId + "&Password=" + TradeDB.Password;
                if (this.Culture != null)
                {
                    switch (this.Culture.Name)
                    {
                        case "zh-CHS":
                            destWeb += "&LangID=TC";
                            break;

                        case "zh-CHT":
                            destWeb += "&LangID=TC";
                            break;

                        default:
                            destWeb += "&LangID=EN";
                            break;
                    }
                }
                url = new Uri(destWeb);//GetResxString("URL"));
            }
            catch { return; }

            webBrowser1.Url = url;
        }

        protected override void OnCultureChange(CultureInfo Culture)
        {
            Load_HTML();
        }

        private void ETradeWebForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            List<BaseForm> mainFormList = BaseForm.GetFormByFormType(typeof(MainForm));
            if (mainFormList != null && mainFormList.Count > 0 && mainFormList[0] != null)
            {
                MainForm mainForm = (MainForm)mainFormList[0];
                mainForm.TopMost = true;
                mainForm.TopMost = false;
            }
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            this.Close();
            this.Dispose();
        }

        private void ETradeWebForm_Resize(object sender, EventArgs e)
        {
            OnResize();
        }

        private void OnResize()
        {
            int v = this.Size.Width - buttonClose.Width - 15;
            if (v < 0) v = 0;
            buttonClose.Left = v;
        }
    }
}
