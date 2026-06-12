using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using TradeDB;
using System.Globalization;

namespace StockTerminal.Web
{

    /// <summary>
    /// Listen to orders changes and write to SQL for web (& mobile) service to read.
    /// </summary>
    public partial class WebDBForm : StockTerminal.Forms.BaseForm
    {
        private WebSQLManager webSQLManager = new WebSQLManager();
        private DataEventHandler DataEventProcessor = null;
        private bool IsFormClosing = false;

        public WebDBForm(CultureInfo Culture, string PersistString)
            : base(Culture, PersistString)
        {
            InitializeComponent();
            webSQLManager.DataEvent += DataEventInvoker;
            webSQLManager.TradeDB = TradeDB;
            DataEventProcessor += DataEventProcess;
        }

        protected override void OnOrderStatus(Order TheOrder)
        {
            /*
            List<string> OrderListXML = null;
            List<string> DealListXML = null;
            List<string> FirstActionRefList = null;

            GetXMLList(Orders, out OrderListXML, out DealListXML, out FirstActionRefList);
            webSQLManager.Write(OrderListXML, DealListXML, FirstActionRefList);
             */
        }

        private void GetXMLList(List<Order> Orders, out List<string> OrderListXML, out List<string> DealListXML, out List<string> FirstActionRefList)
        {
            OrderListXML = null;
            DealListXML = null;
            FirstActionRefList = null;

            if (Orders == null || Orders.Count <= 0) 
                return;

            StringBuilder sbOrder = new StringBuilder(8000);
            StringBuilder sbDeal = new StringBuilder(8000);
            int OrderCount = 0;
            int DealCount = 0;
            OrderListXML = new List<string>(20);
            DealListXML = new List<string>(20);
            FirstActionRefList = new List<string>(20);

            sbOrder.Append("<ROOT>");
            sbDeal.Append("<ROOT>");

            foreach (Order ord in Orders)
            {
                if (ord.FirstActionRef.Trim() != "")
                    FirstActionRefList.Add(ord.FirstActionRef);

                foreach (KeyValuePair<int, Deal> kvp in ord.Deals)
                {
                    Deal clonedDeal = (Deal)kvp.Value.Clone();
                    sbDeal.Append("<Deal RN=\"" + clonedDeal.DealNo + "\"");
                    sbDeal.Append(" O=\"" + clonedDeal.OrderNo + "\"");
                    sbDeal.Append(" SC=\"" + ord.StockCode + "\"");
                    sbDeal.Append(" BS=\"" + ord.Side + "\"");
                    sbDeal.Append(" NM=\"" + clonedDeal.Price + "\"");
                    sbDeal.Append(" Q=\"" + clonedDeal.Quantity + "\"");
                    sbDeal.Append(" TDT=\"" + clonedDeal.Time.ToString("yyyy/MM/dd HH:mm:ss") + "\"");
                    sbDeal.Append(" SBN=\"" + clonedDeal.BrokerId + "\"");
                    sbDeal.Append(" BBN=\"0\"");
                    sbDeal.Append(" TD=\"" + clonedDeal.Time.ToString("yyyy/MM/dd") + "\"");
                    sbDeal.Append(" />");
                    DealCount++;

                    if (DealCount >= 40)
                    {
                        sbDeal.Append("<DealCount C=\"" + DealCount.ToString() + "\" /></ROOT>");
                        DealListXML.Add(sbDeal.ToString());
                        sbDeal = new StringBuilder(8000);
                        sbDeal.Append("<ROOT>");
                        DealCount = 0;
                    }
                }

                sbOrder.Append("<Order SC=\"" + ord.StockCode + "\"");
                sbOrder.Append(" O=\"" + ord.OrderNo + "\"");
                sbOrder.Append(" AC=\"" + ord.AccountNo + "\"");
                sbOrder.Append(" AE=\"" + ord.AECode + "\"");
                sbOrder.Append(" S=\"" + ord.Side + "\"");
                sbOrder.Append(" NM=\"" + ord.Price + "\"");
                sbOrder.Append(" Q=\"" + ord.Quantity + "\"");
                sbOrder.Append(" OT=\"" + ord.OrderType + "\"");
                sbOrder.Append(" AP=\"" + ord.AvgPrice + "\"");
                sbOrder.Append(" FQ=\"" + ord.Filled + "\"");
                sbOrder.Append(" ST=\"" + ord.Status + "\"");
                sbOrder.Append(" SM=\"" + ord.StatusMessage + "\"");
                sbOrder.Append(" OGM=\"" + ord.OGMessage + "\"");
                sbOrder.Append(" LA=\"" + ord.LastAction + "\"");
                sbOrder.Append(" RP=\"" + ord.Replied + "\"");
                sbOrder.Append(" MM=\"" + ord.Memo + "\"");
                sbOrder.Append(" />");

                OrderCount++;

                if (OrderCount >= 40)
                {
                    sbOrder.Append("<OrderCount C=\"" + OrderCount.ToString() + "\" /></ROOT>");
                    OrderListXML.Add(sbOrder.ToString());
                    sbOrder = new StringBuilder(8000);
                    sbOrder.Append("<ROOT>");
                    OrderCount = 0;
                }
            }

            if (OrderCount > 0)
            {
                sbOrder.Append("<OrderCount C=\"" + OrderCount.ToString() + "\" /></ROOT>");
                OrderListXML.Add(sbOrder.ToString());
                sbOrder = new StringBuilder(8000);
                sbOrder.Append("<ROOT>");
            }

            if (DealCount > 0)
            {
                sbDeal.Append("<DealCount C=\"" + DealCount.ToString() + "\" /></ROOT>");
                DealListXML.Add(sbDeal.ToString());
                sbDeal = new StringBuilder(8000);
                sbDeal.Append("<ROOT>");
            }

            return;
        }

        private void DataEventProcess(object source, DataEvent e)
        {
            //log.Append(e.Type, e.Message);
            listBoxEvent.Items.Insert(0, e.ToString());

            listBoxEvent.SuspendLayout();
            while (listBoxEvent.Items.Count > 500)
            {
                listBoxEvent.Items.RemoveAt(500);
            }
            listBoxEvent.ResumeLayout();
        }

        private void DataEventInvoker(object source, DataEvent e)
        {
            if (!IsFormClosing)
            {
                this.Invoke(DataEventProcessor, source, e);
                //if (e.Type.ToUpper() == "ERROR")
                //    pHeart.Beat("24", e.Message);
                //else if (pHeart.AutoBeat)
                //    pHeart.AutoBeatMessage = e.Message;
                //else
                //    pHeart.Beat("20", e.Message);
            }
        }

        private void Start()
        {
            buttonStartStop.BackColor = Color.FromKnownColor(KnownColor.Yellow);
            buttonStartStop.Text = "Stop";

            //if (!ApplySettings())
            //{
            //    MessageBox.Show("Invalid settings.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            //}
            if (!webSQLManager.Start())
            {
                MessageBox.Show("Please wait for process to stop before restarting.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else
            {
                DataEventInvoker(this, new DataEvent("Operation", "Started."));
                //pHeart.AutoBeat = true;
                ListenOrderStatus(new List<string> { "" });
                //OnOrderStatus(TradeDB.GetOrder());
                webSQLManager.SQLHost = SettingsWeb["DBHost"];
                webSQLManager.SQLName = SettingsWeb["DBName"];
                webSQLManager.SQLUserID = SettingsWeb["DBUserID"];
                webSQLManager.SQLPassword = SettingsWeb["DBPassword"];
            }
        }

        private void Stop()
        {
            //pHeart.AutoBeat = false;
            UnListenOrderStatus(new List<string> { "" });
            webSQLManager.Stop();
            buttonStartStop.BackColor = Color.FromKnownColor(KnownColor.Control);
            buttonStartStop.Text = "Start";
            DataEventInvoker(this, new DataEvent("Operation", "Stopped."));
        }

        #region Form Event

        private void WebDBForm_Shown(object sender, EventArgs e)
        {
            SettingsWeb.ReadFromFile(INIFile, "Web");
        }

        private void WebDBForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Stop();
            IsFormClosing = true;
        }

        private void buttonStartStop_Click(object sender, EventArgs e)
        {
            if (!webSQLManager.Started)
                Start();
            else
                Stop();
        }

        #endregion

    }
}
