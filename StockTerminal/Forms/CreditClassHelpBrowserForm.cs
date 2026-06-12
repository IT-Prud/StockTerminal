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
    public partial class CreditClassHelpBrowserForm : StockTerminal.Forms.BaseForm
    {
        public string host = SettingsTradeDB["LoginHost01"];
        public string port = SettingsTradeDB["LoginPort01"];

        //public CreditClassHelpBrowserForm()
        //{
        //    InitializeComponent();
        //}

        public CreditClassHelpBrowserForm(CultureInfo Culture, string PersistString)
            : base(Culture, PersistString)
        {
            InitializeComponent();
        }

        private void CreditClassHelpBrowserForm_Shown(object sender, EventArgs e)
        {
            Load_HTML();
            this.Text = GetResxString("DocumentName");
        }

        protected override void OnCultureChange(CultureInfo Culture)
        {
            Load_HTML();
        }

        private void Load_HTML()
        {
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
            Uri url = null;

            try
            {
                url = new Uri(destWeb);//GetResxString("URL"));
            }
            catch { return; }

            if (webBrowserMain != null && !webBrowserMain.IsDisposed)
                webBrowserMain.Url = url;
        }

        private void CreditClassHelpBrowserForm_FormClosed(object sender, FormClosedEventArgs e)
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
