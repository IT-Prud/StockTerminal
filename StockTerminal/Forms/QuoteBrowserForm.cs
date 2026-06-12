using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using TradeDB;

namespace StockTerminal.Forms
{
    public partial class QuoteBrowserForm : StockTerminal.Forms.BaseForm
    {
        HtmlDocument domdoc = null;

        //public QuoteBrowserForm() : this(null, null)
        //{
        //}

        public QuoteBrowserForm(CultureInfo Culture, string PersistString) : base(Culture, PersistString)
        {
            InitializeComponent();
        }

        private void OnPassStkCodeClicked(object sender, EventArgs e)
        {
            if (domdoc == null)
                return;

            passStkCodeToOrderTicket(false, "");
        }

        public void passStkCodeToOrderTicket(bool SetFocus, string stkPrice)
        {
            if (domdoc == null)
                return;

            object tempObj = domdoc.InvokeScript("GetEnablePassCodeChecked");
            string outHtml_PassStkChecked = domdoc.GetElementById("PassStkChecked").OuterHtml.ToString();
            if (outHtml_PassStkChecked != null && outHtml_PassStkChecked.IndexOf("false") >= 0)
                return;
            string stkCode;//, stkPrice = "";
            string outHtml = domdoc.GetElementById("currentStkCode").OuterHtml.ToString();
            int startIndex = outHtml.IndexOf("value=");
            if (startIndex < 0)
                return;
            startIndex += 6;
            int endIndex = outHtml.IndexOf(" ", startIndex);
            stkCode = outHtml.Substring(startIndex, endIndex - startIndex);
            LocalFormSettings["StockCode"] = stkCode;

            List<BaseForm> BaseFormList = BaseForm.GetFormByFormType(typeof(OrderTicketForm_simple));
            if (BaseFormList != null && BaseFormList.Count > 0 && BaseFormList[0] != null)
            {
                OrderTicketForm_simple OTFrm = (OrderTicketForm_simple)BaseFormList[0];
                OTFrm.setStk_Price_Code(stkPrice, stkCode, true, true, SetFocus, ExchangeTypeEnum.HKG);
            }
            else
            {
                BaseFormList = BaseForm.GetFormByFormType(typeof(OrderTicketForm));
                if (BaseFormList != null && BaseFormList.Count > 0 && BaseFormList[0] != null)
                {
                    OrderTicketForm OTFrm = (OrderTicketForm)BaseFormList[0];
                    OTFrm.setStk_Price_Code(stkPrice, stkCode, true, true, SetFocus, ExchangeTypeEnum.HKG);
                }
                else
                {
                    BaseFormList = BaseForm.GetFormByFormType(typeof(FastOrderTicketForm));
                    if (BaseFormList != null && BaseFormList.Count > 0 && BaseFormList[0] != null)
                    {
                        FastOrderTicketForm Fast_OT_Form = (FastOrderTicketForm)BaseFormList[0];
                        Fast_OT_Form.setStk_Price_Code(stkPrice, stkCode, true, true, SetFocus);
                    }
                }
            }
        }

        private void OnPassBuySellClicked(object sender, EventArgs e)
        {
            if (domdoc == null)
                return;

            string outHtml = domdoc.GetElementById("currentStkPrice").OuterHtml.ToString();
            int startIndex = outHtml.IndexOf("value=");
            if (startIndex < 0)
                return;
            startIndex += 6;
            int endIndex = outHtml.IndexOf(" ", startIndex);
            string stkPrice = outHtml.Substring(startIndex, endIndex - startIndex);
            passStkCodeToOrderTicket(false, stkPrice);
        }

        public void ChangeStockCode(string stkCode)
        {
            if (domdoc != null && stkCode != null && stkCode != "")
            {
                object tempObj = domdoc.InvokeScript("GetEnablePassCodeChecked");
                string outHtml_PassStkChecked = domdoc.GetElementById("PassStkChecked").OuterHtml.ToString();
                if (outHtml_PassStkChecked != null && outHtml_PassStkChecked.IndexOf("false") >= 0)
                    return;
                domdoc.InvokeScript("ChangeAppletStock", new object[] { stkCode });
            }
        }

        private void webBrowserMain_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            try
            {
                //if (e.Url.OriginalString != destWeb)
                if (e.Url.OriginalString.IndexOf("QuoteAAStocks.asp") < 0) 
                    return;

                //HtmlElement htme = webBrowserMain.Document.Body;   //if the browser has anything loaded this will get you the body element, you'll have to experiment with other elements
                domdoc = webBrowserMain.Document as HtmlDocument;
                ChangeStockCode(LocalFormSettings["StockCode"]);

                HtmlElement htmlElementBtn = domdoc.GetElementById("passStk");
                if (htmlElementBtn != null)
                    htmlElementBtn.AttachEventHandler("onclick", OnPassStkCodeClicked);

                HtmlElement htmlElementBtn2 = domdoc.GetElementById("passBuySellPrice");
                if (htmlElementBtn2 != null)
                    htmlElementBtn2.AttachEventHandler("onclick", OnPassBuySellClicked);
                timerAttachEvent.Enabled = true;
            }
            catch (NullReferenceException nre)
            {
                string temp = nre.Message;
                webBrowserMain.Navigate("About:Blank");  //The webbrowser never navigated anywhere so load up a blank document to get addressability, you can put any url here.
                while (webBrowserMain.Document.Body == null)
                {
                    Application.DoEvents(); // stay in the loop until it gets time to fully load
                }
            }   
        }

        private void Load_HTML()
        {
            /*
            string javaVersion = GetJavaVersionInformation();
            if (javaVersion == null || javaVersion.Trim() == "")
            {
                string JavaWeb = "https://www.pru.hk/InternetStock/Quote/JavaUpdate.asp?lang=";
                if (Culture != null)
                {
                    switch (Culture.Name)
                    {
                        case "zh-CHS":
                            JavaWeb += "chn";
                            break;

                        case "zh-CHT":
                            JavaWeb += "chi";
                            break;

                        default:
                            JavaWeb += "eng";
                            break;
                    }
                }
                System.Diagnostics.Process.Start(@JavaWeb);
                //ShowMessageBox(GetResxString("InsallJavaWarning"), "", MessageBoxButtons.OK, 
                //    MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                this.Close();
                this.Dispose();
                return;
            }
             */
            //destWeb = "https://secure.aastocks.com/pkages/broker/login_broker/auto_all.asp?uname=&password=&broker=&lang=";
            //destWeb = "http://uat2.aastocks.com/pkages/broker/login_broker/auto_all.asp";
            string destWeb = "https://www.pru.hk/InternetStock/Quote/AutoLogon.asp?UserId=" + SettingsForms["QuoteAAStockLoginID"] + "&Password=" + SettingsForms["QuoteAAStockLoginPassword"];
            if (Culture != null)
            {
                switch (Culture.Name)
                {
                    case "zh-CHS":
                        destWeb += "&lang=chn";
                        break;

                    case "zh-CHT":
                        destWeb += "&lang=chi";
                        break;

                    default:
                        destWeb += "&lang=eng";
                        break;
                }
            }

            System.Diagnostics.Process.Start(destWeb);
            this.Close();
            this.Dispose();

            /*
            Uri url = null;

            try
            {
                url = new Uri(destWeb);//GetResxString("URL"));
            }
            catch { return; }

            if (webBrowserMain != null && !webBrowserMain.IsDisposed)
                webBrowserMain.Url = url;
             */
        }

        protected override void OnCultureChange(CultureInfo Culture)
        {
            Load_HTML();
        }

        private void QuoteBrowserForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            List<BaseForm> mainFormList = BaseForm.GetFormByFormType(typeof(MainForm));
            if (mainFormList != null && mainFormList.Count > 0 && mainFormList[0] != null)
            {
                MainForm mainForm = (MainForm)mainFormList[0];
                mainForm.TopMost = true;
                mainForm.TopMost = false;
            }
        }

        private void QuoteBrowserForm_Shown(object sender, EventArgs e)
        {
            Load_HTML();
        }

        public string GetJavaVersionInformation()
        {
            try
            {
                System.Diagnostics.ProcessStartInfo procStartInfo = new System.Diagnostics.ProcessStartInfo("java", "-version ");
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.RedirectStandardError = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;
                System.Diagnostics.Process proc = new Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
                return proc.StandardError.ReadToEnd();
            }
            catch (Exception objException)
            {
                AppendLog("Java", "Java not installed: " + objException.ToString(), "Java", false);
                return null;
            }
        }
        private void timerAttachEvent_Tick(object sender, EventArgs e)
        {
            try
            {
                domdoc = webBrowserMain.Document as HtmlDocument;
                //ChangeStockCode(LocalFormSettings["StockCode"]);

                HtmlElement htmlElementBtn = domdoc.GetElementById("passStk");
                if (htmlElementBtn != null)
                {
                    htmlElementBtn.DetachEventHandler("onclick", OnPassStkCodeClicked);
                    htmlElementBtn.AttachEventHandler("onclick", OnPassStkCodeClicked);
                }

                HtmlElement htmlElementBtn2 = domdoc.GetElementById("passBuySellPrice");
                if (htmlElementBtn2 != null)
                {
                    htmlElementBtn2.DetachEventHandler("onclick", OnPassBuySellClicked);
                    htmlElementBtn2.AttachEventHandler("onclick", OnPassBuySellClicked);
                }
            }
            catch (NullReferenceException nre)
            {
                string temp = nre.Message;
                webBrowserMain.Navigate("About:Blank");  //The webbrowser never navigated anywhere so load up a blank document to get addressability, you can put any url here.
                while (webBrowserMain.Document.Body == null)
                {
                    Application.DoEvents(); // stay in the loop until it gets time to fully load
                }
            }
        }
    }
}
