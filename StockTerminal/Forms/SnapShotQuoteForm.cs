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
    public partial class SnapShotQuoteForm : StockTerminal.Forms.BaseForm
    {
        //public SnapShotQuoteForm()
        //{
        //    InitializeComponent();
        //}

        public SnapShotQuoteForm(CultureInfo Culture, string PersistString)
            : base(Culture, PersistString)
        {
            InitializeComponent();
        }

        private void SnapShotQuoteForm_Shown(object sender, EventArgs e)
        {
            Load_HTML();
        }

        private void Load_HTML()
        {
            Uri url = null;
            string AAStockQuoteWebURL = "https://www.pru.hk";

            if (SettingsForms["AAStockQuoteWebURL"] != null && SettingsForms["AAStockQuoteWebURL"].Trim() != "")
                AAStockQuoteWebURL = SettingsForms["AAStockQuoteWebURL"];

            try
            {
                string destWeb = AAStockQuoteWebURL + "/ETrade/Secured/Session/Login.asp" + TradeDB.UserId + "&Password=" + TradeDB.Password + "&Redirect=Quote";
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

            webBrowserQuote.Url = url;
        }

        protected override void OnCultureChange(CultureInfo Culture)
        {
            Load_HTML();
        }

        private void SnapShotQuoteForm_DockStateChanged(object sender, EventArgs e)
        {
            //if (webBrowserQuote.Url != null && webBrowserQuote.Url.ToString().IndexOf("Quote") < 0)
            //    Load_HTML();
        }

        private void SnapShotQuoteForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            List<BaseForm> mainFormList = BaseForm.GetFormByFormType(typeof(MainForm));
            if (mainFormList != null && mainFormList.Count > 0 && mainFormList[0] != null)
            {
                MainForm mainForm = (MainForm)mainFormList[0];
                mainForm.TopMost = true;
                mainForm.TopMost = false;
            }
        }

    }
}
