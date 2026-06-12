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
    public partial class AccountSettingsForm : BaseForm
    {
        private bool formShown = false;
        private string settings = "";
        public string Settings
        {
            get
            {
                return settings;
            }
            set
            {
                settings = value;
                LocalFormSettings["Settings"] = value;
                Load_HTML();
            }
        }

        public AccountSettingsForm(CultureInfo Culture, string PersistString)
            : base(Culture, PersistString)
        {
            InitializeComponent();
            Settings = LocalFormSettings["Settings"];
        }

        //public AccountSettingsForm()
        //{
        //    InitializeComponent();
        //}

        private void AccountSettingsForm_Shown(object sender, EventArgs e)
        {
            OnResize();
            formShown = true;
            Load_HTML();
        }

        private void AccountSettingsForm_Resize(object sender, EventArgs e)
        {
            OnResize();
        }

        protected override void OnCultureChange(CultureInfo Culture)
        {
            Load_HTML();
        }

        private void Load_HTML()
        {
            if (!formShown || Settings == null || Settings.Trim() == "")
                return;

            string AccSettingsWebFormURL = "https://www.pru.hk";

            if (SettingsForms["AccSettingsWebURL"] != null && SettingsForms["AccSettingsWebURL"].Trim() != "")
                AccSettingsWebFormURL = SettingsForms["AccSettingsWebURL"];
            Uri url = null;

            try
            {
                string destWeb = AccSettingsWebFormURL + "/ETrade/Secured/Session/Login.asp?Action=Login&Redirect=" +
                    Settings + "&UserID=" + TradeDB.UserId + "&Password=" + TradeDB.Password;
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
                url = new Uri(destWeb);
            }
            catch { return; }

            webBrowser1.Url = url;
        }

        private void OnResize()
        {
            int v = this.Size.Width - buttonClose.Width - 15;
            if (v < 0) v = 0;
            buttonClose.Left = v;
        }

        private void AccountSettingsForm_FormClosed(object sender, FormClosedEventArgs e)
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
    }
}
