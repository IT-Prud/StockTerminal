using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using TradeDB;
using System.Globalization;
using WeifenLuo.WinFormsUI.Docking;
using System.Net;

namespace StockTerminal.Forms
{
    public partial class QuoteFormFullScreen : StockTerminal.Forms.BaseForm
    {
        Dictionary<int, EP> EPDict = new Dictionary<int, EP>(9000); // Exchange participant
        private bool enterKeyPressed = false;
        private Stock CurrStock = null;
        private ExchangeTypeEnum pListenedExType = ExchangeTypeEnum.HKG;
        private string pListenedStockCode = null;
        public string ListenedStockCode
        {
            get
            {
                return pListenedStockCode;
            }

            set
            {
                string code = value != null ? value.Trim() : "";
                //if (code != (pListenedStockCode != null ? pListenedStockCode : ""))
                {
                    ClearForm();

                    if (code.Length > 0)
                    {
                        //pListenedExType = ExchangeTypeEnum.HKG;
                        //if (checkBoxASHR.Checked)
                        //    pListenedExType = ExchangeTypeEnum.SHG;
                        //else if (checkBoxSZE.Checked)
                        //    pListenedExType = ExchangeTypeEnum.SZE;
                        //else if (checkBoxPMHKG.Checked)
                        //    pListenedExType = ExchangeTypeEnum.PMHKG;
                        pListenedStockCode = int.Parse(code).ToString();

                        ListenStock(new List<string> { Stock.GetSignature(pListenedExType, pListenedStockCode) });

                        /*
                        // for testing
                        switch (pTestState)
                        {
                            case 0:
                                GetStock(new List<string> { Stock.GetSignature(pListenedExType, "5") });
                                pTestState++;
                                break;

                            case 1:
                                pTestState++;
                                ListenStock(new List<string> { Stock.GetSignature(pListenedExType, "5") }, true);
                                break;

                            case 2:
                                pTestState++;
                                GetStock(new List<string> { Stock.GetSignature(pListenedExType, "5") });
                                break;
                        }
                         */
                    }
                    else
                    {
                        if (pListenedStockCode != null && pListenedStockCode.Length > 0 && pListenedExType != ExchangeTypeEnum.Unassigned)
                        {
                            UnListenStock(new List<string> { Stock.GetSignature(pListenedExType, pListenedStockCode) });
                            CurrStock = null;
                        }

                        pListenedStockCode = "";
                    }
                    textBoxCode.Text = pListenedStockCode;
                }
            }
        }

        public QuoteFormFullScreen(CultureInfo Culture, string PersistString)
            : base(Culture, PersistString)
        {
            InitializeComponent();
            Utils.Utils.EnableDoubleBuffered(labelAskQ);
            Utils.Utils.EnableDoubleBuffered(labelBidQ);
            GetBrokerName();
            ListenIndex(new List<string> { "HSI", "CEI" }, true); //, "ASHRQUOTA"

            //this.dockPanelMain = dockPanelMain;
            this.Enter += new System.EventHandler(this.SQForm_Enter);
            ListenFormListChange();
        }

        protected override void OnCultureChange(CultureInfo Culture)
        { }

        protected override void OnIndex(Index TheIndex)
        {
            if (TheIndex != null)
            {
                switch (TheIndex.Code)
                {
                    case "HSI":
                        labelValueIndexHSI.Text = TheIndex.Last.ToString("#,##0.00");
                        labelValueIndexHSIChange.Text = TheIndex.Change.ToString("+#,##0.00;-#,##0.00;0.00");
                        labelValueIndexHSIChangePercent.Text = "("+ TheIndex.ChangePC.ToString("+#,##0.00;-#,##0.00;0.00") + "%)";
                        if (TheIndex.Change > 0)
                        {
                            labelValueIndexHSI.ForeColor = Color.Red;
                            labelValueIndexHSI.Text = char.ConvertFromUtf32(0x2191) + " " + labelValueIndexHSI.Text;
                        }
                        else if (TheIndex.Change < 0)
                        {
                            labelValueIndexHSI.ForeColor = Color.LimeGreen;
                            labelValueIndexHSI.Text = char.ConvertFromUtf32(0x2193) + " " + labelValueIndexHSI.Text;
                        }
                        else
                        {
                            labelValueIndexHSI.ForeColor = Color.White;
                        }
                        break;
                    case "CEI":
                        labelValueIndexCEI.Text = TheIndex.Last.ToString("#,##0.00");
                        labelValueIndexCEIChange.Text = TheIndex.Change.ToString("+#,##0.00;-#,##0.00;0.00");
                        labelValueIndexCEIChangePercent.Text = "(" + TheIndex.ChangePC.ToString("+#,##0.00;-#,##0.00;0.00") + "%)";
                        if (TheIndex.Change >= 0)
                        {
                            labelValueIndexCEI.ForeColor = Color.Red;
                            labelValueIndexCEI.Text = char.ConvertFromUtf32(0x2191) + " " + labelValueIndexCEI.Text;
                        }
                        else if (TheIndex.Change < 0)
                        {
                            labelValueIndexCEI.ForeColor = Color.LimeGreen;
                            labelValueIndexCEI.Text = char.ConvertFromUtf32(0x2193) + " " + labelValueIndexCEI.Text;
                        }
                        else
                        {
                            labelValueIndexCEI.ForeColor = Color.White;
                        }
                        break;
                }
            }
        }

        private void GetBrokerName()
        {
            WebClient webClient = null;
            try
            {
                //ServicePointManager.Expect100Continue = true;
                //ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                webClient = new WebClient();
                using (WebClient client = new WebClient())
                {
                    client.DownloadFileCompleted += new AsyncCompletedEventHandler(client_DownloadFileCompleted);
                    client.DownloadFileAsync(new Uri("http://192.1.2.13/EP.txt"), @".\EP.txt");
                    client.Dispose();
                }
            }
            catch (Exception exp)
            {
                MessageBox.Show("Failed to download Broker name file.\n " + exp.ToString());
            }
            finally
            {
                if (webClient != null)
                    webClient.Dispose();
            }
        }

        class EP
        {
            public string EnName = null;
            public string ChtName = null;
        }

        private void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (!System.IO.File.Exists(@".\EP.txt"))
                return;

            string[] lines = null;
            try { lines = System.IO.File.ReadAllLines(@".\EP.txt"); }
            catch (Exception ) { return; }
            if (lines == null || lines.Length <= 0)
                return;

            try
            {
                foreach (string line in lines)
                {
                    string[] valueArray = line.Split('	');
                    EP ep = new EP();
                    if (valueArray.Length >= 3)
                    {
                        ep.ChtName = valueArray[1].Trim();
                        ep.EnName = valueArray[2].Trim();
                        string[] brokerNumArray = valueArray[0].Split(',');
                        int[] iBrokerNumArray = new int[brokerNumArray.Length];
                        for (int i = 0; i < brokerNumArray.Length; i++)
                        {
                            int iB;
                            if (int.TryParse (brokerNumArray[i].Trim(), out iB))
                                iBrokerNumArray[i] = iB;
                        }
                        foreach (int brokerNum in iBrokerNumArray)
                        {
                            if (!EPDict.ContainsKey(brokerNum))
                                EPDict[brokerNum] = ep;
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        protected override void OnStock(Stock TheStock)
        {
            if (TheStock != null)
            {
                string code = ListenedStockCode != null ? ListenedStockCode : "";
                ExchangeTypeEnum exType = pListenedExType;

                if (TheStock.Code == code && TheStock.ExchangeType == exType)
                {
                    FillForm(TheStock);

                    if (enterKeyPressed)
                    {
                        enterKeyPressed = false;
                        bool CanBuy = false, CanSell = false;
                        decimal stkPrice = Utils.OrderTypeSpread.GetSpreadNoStkPrice('B', 0, CurrStock, TradeDB, out CanBuy, out CanSell);
                        //passInfoToStockQuoteForm(CurrStock.Nominal.ToString(), CanBuy, CanSell, false);
                    }
                }
            }
        }

        private void FillForm(Stock TheStock)
        {
            if (TheStock == null)
            {
                if (CurrStock != null)
                {
                    ClearForm();
                    CurrStock = null;
                }
            }
            else
            {
                bool fillStatics, fillDynamics;
                SpreadTableSet.SpreadTable spreadTable = TradeDB.SpreadTables[TheStock.SpreadTableCode];

                if (CurrStock == null || CurrStock.StockSignature != TheStock.StockSignature)
                    fillStatics = fillDynamics = true;
                else
                {
                    fillStatics = CurrStock.StaticVersion != TheStock.StaticVersion ? true : false;
                    fillDynamics = CurrStock.DynamicVersion != TheStock.DynamicVersion ? true : false;
                }

                if (fillStatics)
                {
                    int stockCodeInt;

                    SetDocumentName(TheStock.GetBestName(this.Culture), TheStock.GetBestName(CultureInfo.GetCultureInfo("en-US")));

                    labelStockCode.Text = (int.TryParse(TheStock.Code, out stockCodeInt) ? stockCodeInt.ToString() : TheStock.Code) + "  " + TheStock.NameCHT + "  " + TheStock.NameENShort.Trim();
                    spreadTable.Format(TheStock.PrevClose);

                    labelValuePrevious.Text = TheStock.PrevClose > 0 ? spreadTable.Format(TheStock.PrevClose) : "";


                    if (TheStock.Change > 0)
                        labelValueNominal.ForeColor = Color.FromArgb(0xff0000);
                    else if (TheStock.Change < 0)
                        labelValueNominal.ForeColor = Color.FromArgb(0x40a040);
                    else
                        labelValueNominal.ForeColor = Color.FromArgb(0x000000);

                    labelValueChange.Text = Stock.GetDispChange(TheStock.Change);
                    labelValueChangePC.Text = Stock.GetDispChangePC(TheStock.ChangePC) + "%";

                    labelValueLotSize.Text = TheStock.LotSize.ToString();
                }

                if (fillDynamics)
                {
                    StringBuilder sb = new StringBuilder(200);

                    labelValueHigh.Text = TheStock.High > 0 ? spreadTable.Format(TheStock.High) : "";
                    labelValueLow.Text = TheStock.Low > 0 ? spreadTable.Format(TheStock.Low) : "";
                    labelValueNominal.Text = TheStock.Nominal > 0 ? spreadTable.Format(TheStock.Nominal) : "";

                    if (TheStock.Change > 0)
                    {
                        labelValueNominal.Text = char.ConvertFromUtf32(0x2191) + "  " + labelValueNominal.Text;
                        labelValueNominal.ForeColor = Color.FromArgb(0xff0000);
                    }
                    else if (TheStock.Change < 0)
                    {
                        labelValueNominal.Text = char.ConvertFromUtf32(0x2193) + "  " + labelValueNominal.Text;
                        labelValueNominal.ForeColor = Color.FromArgb(0x40a040);
                    }
                    else
                        labelValueNominal.ForeColor = Color.FromArgb(0x000000);

                    labelValueChange.Text = Stock.GetDispChange(TheStock.Change);
                    labelValueChangePC.Text = Stock.GetDispChangePC(TheStock.ChangePC) + "%";
                    labelValueVolume.Text = TheStock.Volume >= 0 ? Stock.GetShortValue(TheStock.Volume, 6, 3) : "";
                    labelValueTurnover.Text = TheStock.Turnover >= 0 ? Stock.GetShortValue(TheStock.Turnover, 6, 3) : "";

                    ShowTradeStatus(TheStock);

                    #region Fill Bid

                    labelValueBid.Text = labelValueBid2.Text = TheStock.Bid > 0 ? spreadTable.Format(TheStock.Bid) : "";

                    labelBidVol0.Text = TheStock.BidVol[0] > 0 ? Stock.FormatQtyValue(TheStock.BidVol[0]) : "";
                    if (TheStock.BidCount[0] > 0) { labelBidCount0.Text = Stock.FormatQtyValue(TheStock.BidCount[0]); }
                    else if (TheStock.BidCount[0] < 0) { labelBidCount0.Text = "***"; }
                    else { labelBidCount0.Text = ""; }

                    labelBidVol1.Text = TheStock.BidVol[1] > 0 ? Stock.FormatQtyValue(TheStock.BidVol[1]) : "";
                    if (TheStock.BidCount[1] > 0) { labelBidCount1.Text = Stock.FormatQtyValue(TheStock.BidCount[1]); }
                    else if (TheStock.BidCount[1] < 0) { labelBidCount1.Text = "***"; }
                    else { labelBidCount1.Text = ""; }

                    labelBidVol2.Text = TheStock.BidVol[2] > 0 ? Stock.FormatQtyValue(TheStock.BidVol[2]) : "";
                    if (TheStock.BidCount[2] > 0) { labelBidCount2.Text = Stock.FormatQtyValue(TheStock.BidCount[2]); }
                    else if (TheStock.BidCount[2] < 0) { labelBidCount2.Text = "***"; }
                    else { labelBidCount2.Text = ""; }

                    labelBidVol3.Text = TheStock.BidVol[3] > 0 ? Stock.FormatQtyValue(TheStock.BidVol[3]) : "";
                    if (TheStock.BidCount[3] > 0) { labelBidCount3.Text = Stock.FormatQtyValue(TheStock.BidCount[3]); }
                    else if (TheStock.BidCount[3] < 0) { labelBidCount3.Text = "***"; }
                    else { labelBidCount3.Text = ""; }

                    labelBidVol4.Text = TheStock.BidVol[4] > 0 ? Stock.FormatQtyValue(TheStock.BidVol[4]) : "";
                    if (TheStock.BidCount[4] > 0) { labelBidCount4.Text = Stock.FormatQtyValue(TheStock.BidCount[4]); }
                    else if (TheStock.BidCount[4] < 0) { labelBidCount4.Text = "***"; }
                    else { labelBidCount4.Text = ""; }

                    labelBidVol5.Text = TheStock.BidVol[5] > 0 ? Stock.FormatQtyValue(TheStock.BidVol[5]) : "";
                    if (TheStock.BidCount[5] > 0) { labelBidCount5.Text = Stock.FormatQtyValue(TheStock.BidCount[5]); }
                    else if (TheStock.BidCount[5] < 0) { labelBidCount5.Text = "***"; }
                    else { labelBidCount5.Text = ""; }

                    labelBidVol6.Text = TheStock.BidVol[6] > 0 ? Stock.FormatQtyValue(TheStock.BidVol[6]) : "";
                    if (TheStock.BidCount[6] > 0) { labelBidCount6.Text = Stock.FormatQtyValue(TheStock.BidCount[6]); }
                    else if (TheStock.BidCount[6] < 0) { labelBidCount6.Text = "***"; }
                    else { labelBidCount6.Text = ""; }

                    labelBidVol7.Text = TheStock.BidVol[7] > 0 ? Stock.FormatQtyValue(TheStock.BidVol[7]) : "";
                    if (TheStock.BidCount[7] > 0) { labelBidCount7.Text = Stock.FormatQtyValue(TheStock.BidCount[7]); }
                    else if (TheStock.BidCount[7] < 0) { labelBidCount7.Text = "***"; }
                    else { labelBidCount7.Text = ""; }

                    labelBidVol8.Text = TheStock.BidVol[8] > 0 ? Stock.FormatQtyValue(TheStock.BidVol[8]) : "";
                    if (TheStock.BidCount[8] > 0) { labelBidCount8.Text = Stock.FormatQtyValue(TheStock.BidCount[8]); }
                    else if (TheStock.BidCount[8] < 0) { labelBidCount8.Text = "***"; }
                    else { labelBidCount8.Text = ""; }

                    labelBidVol9.Text = TheStock.BidVol[9] > 0 ? Stock.FormatQtyValue(TheStock.BidVol[9]) : "";
                    if (TheStock.BidCount[9] > 0) { labelBidCount9.Text = Stock.FormatQtyValue(TheStock.BidCount[9]); }
                    else if (TheStock.BidCount[9] < 0) { labelBidCount9.Text = "***"; }
                    else { labelBidCount9.Text = ""; }

                    #endregion

                    #region Fill Ask

                    labelValueAsk.Text = labelValueAsk2.Text = TheStock.Ask > 0 ? spreadTable.Format(TheStock.Ask) : "";

                    labelAskVol0.Text = TheStock.AskVol[0] > 0 ? Stock.FormatQtyValue(TheStock.AskVol[0]) : "";
                    if (TheStock.AskCount[0] > 0) { labelAskCount0.Text = Stock.FormatQtyValue(TheStock.AskCount[0]); }
                    else if (TheStock.AskCount[0] < 0) { labelAskCount0.Text = "***"; }
                    else { labelAskCount0.Text = ""; }

                    labelAskVol1.Text = TheStock.AskVol[1] > 0 ? Stock.FormatQtyValue(TheStock.AskVol[1]) : "";
                    if (TheStock.AskCount[1] > 0) { labelAskCount1.Text = Stock.FormatQtyValue(TheStock.AskCount[1]); }
                    else if (TheStock.AskCount[1] < 0) { labelAskCount1.Text = "***"; }
                    else { labelAskCount1.Text = ""; }

                    labelAskVol2.Text = TheStock.AskVol[2] > 0 ? Stock.FormatQtyValue(TheStock.AskVol[2]) : "";
                    if (TheStock.AskCount[2] > 0) { labelAskCount2.Text = Stock.FormatQtyValue(TheStock.AskCount[2]); }
                    else if (TheStock.AskCount[2] < 0) { labelAskCount2.Text = "***"; }
                    else { labelAskCount2.Text = ""; }

                    labelAskVol3.Text = TheStock.AskVol[3] > 0 ? Stock.FormatQtyValue(TheStock.AskVol[3]) : "";
                    if (TheStock.AskCount[3] > 0) { labelAskCount3.Text = Stock.FormatQtyValue(TheStock.AskCount[3]); }
                    else if (TheStock.AskCount[3] < 0) { labelAskCount3.Text = "***"; }
                    else { labelAskCount3.Text = ""; }

                    labelAskVol4.Text = TheStock.AskVol[4] > 0 ? Stock.FormatQtyValue(TheStock.AskVol[4]) : "";
                    if (TheStock.AskCount[4] > 0) { labelAskCount4.Text = Stock.FormatQtyValue(TheStock.AskCount[4]); }
                    else if (TheStock.AskCount[4] < 0) { labelAskCount4.Text = "***"; }
                    else { labelAskCount4.Text = ""; }

                    labelAskVol5.Text = TheStock.AskVol[5] > 0 ? Stock.FormatQtyValue(TheStock.AskVol[5]) : "";
                    if (TheStock.AskCount[5] > 0) { labelAskCount5.Text = Stock.FormatQtyValue(TheStock.AskCount[5]); }
                    else if (TheStock.AskCount[5] < 0) { labelAskCount5.Text = "***"; }
                    else { labelAskCount5.Text = ""; }

                    labelAskVol6.Text = TheStock.AskVol[6] > 0 ? Stock.FormatQtyValue(TheStock.AskVol[6]) : "";
                    if (TheStock.AskCount[6] > 0) { labelAskCount6.Text = Stock.FormatQtyValue(TheStock.AskCount[6]); }
                    else if (TheStock.AskCount[6] < 0) { labelAskCount6.Text = "***"; }
                    else { labelAskCount6.Text = ""; }

                    labelAskVol7.Text = TheStock.AskVol[7] > 0 ? Stock.FormatQtyValue(TheStock.AskVol[7]) : "";
                    if (TheStock.AskCount[7] > 0) { labelAskCount7.Text = Stock.FormatQtyValue(TheStock.AskCount[7]); }
                    else if (TheStock.AskCount[7] < 0) { labelAskCount7.Text = "***"; }
                    else { labelAskCount7.Text = ""; }

                    labelAskVol8.Text = TheStock.AskVol[8] > 0 ? Stock.FormatQtyValue(TheStock.AskVol[8]) : "";
                    if (TheStock.AskCount[8] > 0) { labelAskCount8.Text = Stock.FormatQtyValue(TheStock.AskCount[8]); }
                    else if (TheStock.AskCount[8] < 0) { labelAskCount8.Text = "***"; }
                    else { labelAskCount8.Text = ""; }

                    labelAskVol9.Text = TheStock.AskVol[9] > 0 ? Stock.FormatQtyValue(TheStock.AskVol[9]) : "";
                    if (TheStock.AskCount[9] > 0) { labelAskCount9.Text = Stock.FormatQtyValue(TheStock.AskCount[9]); }
                    else if (TheStock.AskCount[9] < 0) { labelAskCount9.Text = "***"; }
                    else { labelAskCount9.Text = ""; }

                    #endregion

                    #region Fill Broker Bid

                    //listBoxBidQ.BeginUpdate();
                    //listBoxBidQ.Items.Clear();
                    //string[] strBrokerBid = new string[TheStock.BrokerBid.Length];
                    //for (int i = 0; i < 40; i++)
                    //{
                    //    strBrokerBid[i] = GetBrokerName(TheStock.BrokerBid[i]);
                    //}
                    //listBoxBidQ.Items.AddRange(strBrokerBid);
                    //listBoxBidQ.EndUpdate();

                    int displayedItems = 40;
                    sb = new StringBuilder(displayedItems);
                    int spread = -1;
                    for (int i = 0; i < displayedItems; i++)
                    {
                        bool temp = true;
                        if (TheStock.BrokerBid[i] == null || TheStock.BrokerBid[i].Length == 0)
                        {
                            sb.Append("\n");
                            continue;
                        }
                        string minusSign = TheStock.BrokerBid[i].Substring(0, 1);
                        string brokerName = GetBrokerName(TheStock.BrokerBid[i]);
                        if (brokerName.Length > 15)
                            brokerName = brokerName.Substring(0, 15);
                        if (!minusSign.Equals("-"))
                            sb.Append(TheStock.BrokerBid[i] + " " + brokerName);
                        else
                        {
                            decimal stkPrice = Utils.OrderTypeSpread.GetSpreadNoStkPrice('B', spread, TheStock, TradeDB, out temp, out temp);
                            if (stkPrice >= 0)
                                sb.Append(TheStock.BrokerBid[i] + "    " + stkPrice);
                            else
                                sb.Append(TheStock.BrokerBid[i]);
                            spread -= 1;
                        }
                        //if (i != displayedItems - 1)
                            sb.Append("\n");
                    }
                    labelBidQ.Text = sb.ToString();

                    #endregion

                    #region Fill Broker Ask

                    //listBoxAskQ.BeginUpdate();
                    //listBoxAskQ.Items.Clear();
                    //string[] strBrokerAsk = new string[TheStock.BrokerAsk.Length];
                    //for (int i = 0; i < displayedItems; i++)
                    //{
                    //    strBrokerAsk[i] = GetBrokerName(TheStock.BrokerAsk[i]);
                    //}
                    //listBoxAskQ.Items.AddRange(strBrokerAsk);
                    //listBoxAskQ.EndUpdate();

                    sb = new StringBuilder(displayedItems);
                    spread = 1;
                    for (int i = 0; i < displayedItems; i++)
                    {
                        //System.Diagnostics.Debug.Print("Ask: " + i);
                        bool temp = true;
                        if (TheStock.BrokerAsk[i] == null || TheStock.BrokerAsk[i].Length == 0)
                        {
                            sb.Append("\n");
                            continue;
                        }
                        string plusSign = TheStock.BrokerAsk[i].Substring(0, 1);
                        string brokerName = GetBrokerName(TheStock.BrokerAsk[i]);
                        if (brokerName.Length > 15)
                            brokerName = brokerName.Substring(0, 15);
                        if (!plusSign.Equals("+"))
                            sb.Append(TheStock.BrokerAsk[i] + " " + brokerName);
                        else
                        {
                            decimal stkPrice = Utils.OrderTypeSpread.GetSpreadNoStkPrice('S', spread, TheStock, TradeDB, out temp, out temp);
                            if (stkPrice >= 0)
                                sb.Append(TheStock.BrokerAsk[i] + "   " + stkPrice);
                            else
                                sb.Append(TheStock.BrokerAsk[i]);
                            spread += 1;
                        }
                        //if (i != displayedItems - 1)
                            sb.Append("\n");
                    }
                    labelAskQ.Text = sb.ToString();

                    #endregion

                    #region Fill Ticker

                    int TickIdx = 14;
                    for (; TickIdx > 3; TickIdx--)
                    {
                        if (TheStock.Ticker[TickIdx] != null && TheStock.Ticker[TickIdx].Time != null && TheStock.Ticker[TickIdx].Time.Length > 0 && TheStock.Ticker[TickIdx].Time != "00:00")
                            break;
                    }

                    if (TheStock.Ticker[TickIdx] != null && TheStock.Ticker[TickIdx].Time != "00:00")
                    {
                        labelValueTickerTime3.Text = TheStock.Ticker[TickIdx].Time == null ? "" : (TheStock.Ticker[TickIdx].Time.Length > 5 ? TheStock.Ticker[TickIdx].Time.Substring(0, 5) : TheStock.Ticker[TickIdx].Time);
                        labelValueTickerRemark3.Text = TheStock.Ticker[TickIdx].Remarks ?? "";
                        labelValueTickerQty3.Text = TheStock.Ticker[TickIdx].Quantity > 0 ? Stock.FormatQtyValue(TheStock.Ticker[TickIdx].Quantity) : "";
                        labelValueTickerPrice3.Text = TheStock.Ticker[TickIdx].Price > 0 ? spreadTable.Format(TheStock.Ticker[TickIdx].Price) : "";
                    }
                    else
                    {
                        labelValueTickerTime3.Text = "";
                        labelValueTickerRemark3.Text = "";
                        labelValueTickerQty3.Text = "";
                        labelValueTickerPrice3.Text = "";
                    }

                    TickIdx--;
                    if (TheStock.Ticker[TickIdx] != null && TheStock.Ticker[TickIdx].Time != "00:00")
                    {
                        labelValueTickerTime2.Text = TheStock.Ticker[TickIdx].Time == null ? "" : (TheStock.Ticker[TickIdx].Time.Length > 5 ? TheStock.Ticker[TickIdx].Time.Substring(0, 5) : TheStock.Ticker[TickIdx].Time);
                        labelValueTickerRemark2.Text = TheStock.Ticker[TickIdx].Remarks ?? "";
                        labelValueTickerQty2.Text = TheStock.Ticker[TickIdx].Quantity > 0 ? Stock.FormatQtyValue(TheStock.Ticker[TickIdx].Quantity) : "";
                        labelValueTickerPrice2.Text = TheStock.Ticker[TickIdx].Price > 0 ? spreadTable.Format(TheStock.Ticker[TickIdx].Price) : "";
                    }
                    else
                    {
                        labelValueTickerTime2.Text = "";
                        labelValueTickerRemark2.Text = "";
                        labelValueTickerQty2.Text = "";
                        labelValueTickerPrice2.Text = "";
                    }

                    TickIdx--;
                    if (TheStock.Ticker[TickIdx] != null && TheStock.Ticker[TickIdx].Time != "00:00")
                    {
                        labelValueTickerTime1.Text = TheStock.Ticker[TickIdx].Time == null ? "" : (TheStock.Ticker[TickIdx].Time.Length > 5 ? TheStock.Ticker[TickIdx].Time.Substring(0, 5) : TheStock.Ticker[TickIdx].Time);
                        labelValueTickerRemark1.Text = TheStock.Ticker[TickIdx].Remarks ?? "";
                        labelValueTickerQty1.Text = TheStock.Ticker[TickIdx].Quantity > 0 ? Stock.FormatQtyValue(TheStock.Ticker[TickIdx].Quantity) : "";
                        labelValueTickerPrice1.Text = TheStock.Ticker[TickIdx].Price > 0 ? spreadTable.Format(TheStock.Ticker[TickIdx].Price) : "";
                    }
                    else
                    {
                        labelValueTickerTime1.Text = "";
                        labelValueTickerRemark1.Text = "";
                        labelValueTickerQty1.Text = "";
                        labelValueTickerPrice1.Text = "";
                    }

                    TickIdx--;
                    if (TheStock.Ticker[TickIdx] != null && TheStock.Ticker[TickIdx].Time != "00:00")
                    {
                        labelValueTickerTime0.Text = TheStock.Ticker[TickIdx].Time == null ? "" : (TheStock.Ticker[TickIdx].Time.Length > 5 ? TheStock.Ticker[TickIdx].Time.Substring(0, 5) : TheStock.Ticker[TickIdx].Time);
                        labelValueTickerRemark0.Text = TheStock.Ticker[TickIdx].Remarks ?? "";
                        labelValueTickerQty0.Text = TheStock.Ticker[TickIdx].Quantity > 0 ? Stock.FormatQtyValue(TheStock.Ticker[TickIdx].Quantity) : "";
                        labelValueTickerPrice0.Text = TheStock.Ticker[TickIdx].Price > 0 ? spreadTable.Format(TheStock.Ticker[TickIdx].Price) : "";
                    }
                    else
                    {
                        labelValueTickerTime0.Text = "";
                        labelValueTickerRemark0.Text = "";
                        labelValueTickerQty0.Text = "";
                        labelValueTickerPrice0.Text = "";
                    }

                    #endregion
                }

                CurrStock = TheStock;
            }
        }

        private string GetBrokerName(string brokerNum)
        {
            int iBrokerNum = 0;
            int.TryParse(brokerNum, out iBrokerNum);
            if (EPDict.ContainsKey(iBrokerNum))
            {
                if (EPDict[iBrokerNum].ChtName != null && EPDict[iBrokerNum].ChtName.Trim().Length > 0)
                    return EPDict[iBrokerNum].ChtName;
                else if (EPDict[iBrokerNum].EnName != null && EPDict[iBrokerNum].EnName.Trim().Length > 0)
                    return EPDict[iBrokerNum].EnName;
            }
            return brokerNum;
        }

        private void ClearForm()
        {
            SetDocumentName("", "");

            labelValueIndexHSI.Text = "";
            labelValueIndexHSIChange.Text = "";
            labelValueIndexHSIChangePercent.Text = "";
            labelValueIndexCEI.Text = "";
            labelValueIndexCEIChange.Text = "";
            labelValueIndexCEIChangePercent.Text = "";

            labelStockCode.Text = "";
            //labelExchange.Text = "";
            //labelValueStockName.Text = "";
            //labelValueOpen.Text = "";
            labelValueHigh.Text = "";
            labelValueLow.Text = "";
            labelValuePrevious.Text = "";
            labelValueNominal.Text = "";
            labelValueLotSize.Text = "";
            labelValueChange.Text = "";
            labelValueChangePC.Text = "";
            labelValueVolume.Text = "";
            labelValueTurnover.Text = "";
            ShowTradeStatus(null);

            labelAskQ.Text = "";
            labelValueBid.Text = labelValueBid2.Text = "";
            labelBidVol0.Text = "";
            labelBidVol1.Text = "";
            labelBidVol2.Text = "";
            labelBidVol3.Text = "";
            labelBidVol4.Text = "";
            labelBidVol5.Text = "";
            labelBidVol6.Text = "";
            labelBidVol7.Text = "";
            labelBidVol8.Text = "";
            labelBidVol9.Text = "";
            //labelValueBidVol.Text = "";
            labelBidCount0.Text = "";
            labelBidCount1.Text = "";
            labelBidCount2.Text = "";
            labelBidCount3.Text = "";
            labelBidCount4.Text = "";
            labelBidCount5.Text = "";
            labelBidCount6.Text = "";
            labelBidCount7.Text = "";
            labelBidCount8.Text = "";
            labelBidCount9.Text = "";
            //labelValueBidCount.Text = "";

            labelBidQ.Text = "";
            labelValueAsk.Text = labelValueAsk2.Text = "";
            labelAskVol0.Text = "";
            labelAskVol1.Text = "";
            labelAskVol2.Text = "";
            labelAskVol3.Text = "";
            labelAskVol4.Text = "";
            labelAskVol5.Text = "";
            labelAskVol6.Text = "";
            labelAskVol7.Text = "";
            labelAskVol8.Text = "";
            labelAskVol9.Text = "";
            //labelValueAskVol.Text = "";
            labelAskCount0.Text = "";
            labelAskCount1.Text = "";
            labelAskCount2.Text = "";
            labelAskCount3.Text = "";
            labelAskCount4.Text = "";
            labelAskCount5.Text = "";
            labelAskCount6.Text = "";
            labelAskCount7.Text = "";
            labelAskCount8.Text = "";
            labelAskCount9.Text = "";
            //labelValueAskCount.Text = "";

            ////labelValueBidBQ.Text = "";
            //labelValueBidBQ0.Text = "";
            //labelValueBidBQ1.Text = "";
            //labelValueBidBQ2.Text = "";
            //labelValueBidBQ3.Text = "";

            ////labelValueAskBQ.Text = "";
            //labelValueAskBQ0.Text = "";
            //labelValueAskBQ1.Text = "";
            //labelValueAskBQ2.Text = "";
            //labelValueAskBQ3.Text = "";

            //labelValueTickerTime.Text = "";
            //labelValueTickerRemark.Text = "";
            //labelValueTickerQty.Text = "";
            //labelValueTickerPrice.Text = "";

            labelValueTickerTime0.Text = "";
            labelValueTickerRemark0.Text = "";
            labelValueTickerQty0.Text = "";
            labelValueTickerPrice0.Text = "";
            labelValueTickerTime1.Text = "";
            labelValueTickerRemark1.Text = "";
            labelValueTickerQty1.Text = "";
            labelValueTickerPrice1.Text = "";
            labelValueTickerTime2.Text = "";
            labelValueTickerRemark2.Text = "";
            labelValueTickerQty2.Text = "";
            labelValueTickerPrice2.Text = "";
            labelValueTickerTime3.Text = "";
            labelValueTickerRemark3.Text = "";
            labelValueTickerQty3.Text = "";
            labelValueTickerPrice3.Text = "";
        }

        private void ShowTradeStatus(Stock theStock)
        {
            if (theStock != null)
            {
                SpreadTableSet.SpreadTable spreadTable = TradeDB.SpreadTables[theStock.SpreadTableCode];

                bool isToday = (theStock.dtVCMCoolOffEndTime.Year == TradeDB.ServerTime.Year && theStock.dtVCMCoolOffEndTime.Month == TradeDB.ServerTime.Month && theStock.dtVCMCoolOffEndTime.Day == TradeDB.ServerTime.Day);
                bool isCAS = ((theStock.CASLowerPrice > 0m || theStock.CASUpperPrice > 0m) && theStock.MarketBelong != null && theStock.MarketBelong.Status >= 10 && theStock.MarketBelong.Status <= 14);
                bool isVCM = ((theStock.VCMReferencePrice > 0m || theStock.VCMLowerPrice > 0m || theStock.VCMUpperPrice > 0m) && (theStock.dtVCMCoolOffEndTime.CompareTo(TradeDB.ServerTime) > 0));
                if (isToday && (isCAS || isVCM))
                {
                    if (isCAS)
                    {
                        labelValueTradeStatus.Text = "收市競價";// GetResxString("CAS");
                        //if (theStock.OrderImbalanceDirection != null && theStock.OrderImbalanceDirection.Trerim().Length > 0)
                        //    labelValueTradeStatus.Text += theStock.OrderImbalanceDirection + " " + theStock.OrderImbalanceQuantity; 
                        labelValueTradeStatus.ForeColor = Color.Red;
                        if (theStock.CASLowerPrice > 0m)
                        {
                            labelBid.Text = spreadTable.Format(theStock.CASLowerPrice);
                            labelBid.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold);
                            labelBid.BackColor = Color.SkyBlue;
                        }
                        if (theStock.CASUpperPrice > 0m)
                        {
                            labelAsk.Text = spreadTable.Format(theStock.CASUpperPrice);
                            labelAsk.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold);
                            labelAsk.BackColor = Color.LightPink;
                        }
                    }
                    else if (isVCM)
                    {
                        System.Diagnostics.Debug.Print("VCMCoolOffEndTime: " + theStock.dtVCMCoolOffEndTime.ToString("yyyy/MM/dd HH:mm:ss") +
                            " ServerTime: " + TradeDB.ServerTime.ToString("yyyy/MM/dd HH:mm:ss") + " Compare result: " + theStock.dtVCMCoolOffEndTime.CompareTo(TradeDB.ServerTime));
                        //labelValueTradeStatus.Text = GetResxString("VCMCooling") + (Math.Abs(theStock.dtVCMCoolOffEndTime.Subtract(TradeDB.ServerTime).Minutes) + "").PadLeft(2, '0') + ":" +
                        //     (Math.Abs(theStock.dtVCMCoolOffEndTime.Subtract(TradeDB.ServerTime).Seconds) + "").PadLeft(2, '0');
                        labelValueTradeStatus.Text = "冷靜期" + (Math.Abs(theStock.dtVCMCoolOffEndTime.Subtract(TradeDB.ServerTime).Minutes) + "").PadLeft(2, '0') + ":" +
                             (Math.Abs(theStock.dtVCMCoolOffEndTime.Subtract(TradeDB.ServerTime).Seconds) + "").PadLeft(2, '0');
                        labelValueTradeStatus.ForeColor = Color.Red;
                        if (theStock.VCMLowerPrice > 0)
                        {
                            labelBid.Text = spreadTable.Format(theStock.VCMLowerPrice);
                            labelBid.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold);
                            labelBid.BackColor = Color.SkyBlue;
                        }
                        if (theStock.VCMUpperPrice > 0)
                        {
                            labelAsk.Text = spreadTable.Format(theStock.VCMUpperPrice);
                            labelAsk.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold);
                            labelAsk.BackColor = Color.LightPink;
                        }
                    }
                }
                else // not VCM, not CAS
                {
                    if (theStock.SuspensionFlag == "Y")
                    {
                        //labelValueTradeStatus.Text = GetResxString("TradeStatusSuspended");
                        labelValueTradeStatus.Text = "交易暫停";
                        labelValueTradeStatus.ForeColor = Color.Red;
                    }
                    else if (theStock.FusingFlag == "Y")
                    {
                        //labelValueTradeStatus.Text = GetResxString("TradeStatusFusing");
                        labelValueTradeStatus.Text = "熔断";
                        labelValueTradeStatus.ForeColor = Color.Red;
                    }
                    else
                    {
                        labelCurrency.Text = GetGeneralResxString("Currency_" + theStock.Currency);
                        labelValueTradeStatus.Text = "";
                        labelCurrency.ForeColor = labelValueTradeStatus.ForeColor = (theStock.Currency == null || theStock.Currency == "" || theStock.Currency == "HKD") ? Color.FromKnownColor(KnownColor.ControlText) : Color.Red;
                    }
                    labelBid.Text = "買入價";// GetResxString("Bid");
                    labelAsk.Text = "賣出價";//GetResxString("Ask");
                    labelBid.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular);
                    labelAsk.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular);
                    if (checkBoxASHR.Checked == true)
                        labelAsk.BackColor = labelBid.BackColor = Color.FromArgb(170, 255, 170);
                    else if (checkBoxSZE.Checked == true)
                        labelAsk.BackColor = labelBid.BackColor = Color.FromArgb(218, 174, 232);
                    else if (checkBoxPMHKG.Checked == true)
                        labelAsk.BackColor = labelBid.BackColor = Color.FromArgb(255, 214, 124);
                    else
                        labelBid.BackColor = labelBid.BackColor = Color.FromArgb(255, 255, 205);
                    //if (theStock != null)
                    //{
                    //    labelValueTradeStatus.Text = GetResxString("VCMCooling") + (Math.Abs(theStock.dtVCMCoolOffEndTime.Subtract(TradeDB.ServerTime).Minutes) + "").PadLeft(2, '0') + ":" + 
                    //     (Math.Abs(theStock.dtVCMCoolOffEndTime.Subtract(TradeDB.ServerTime).Seconds) + "").PadLeft(2, '0');
                    //}
                }
            }
            else // no or invalid stock, clear form
            {
                labelBid.Text = "買入價";// GetResxString("Bid");
                labelAsk.Text = "賣出價";//GetResxString("Ask");
                labelBid.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular);
                labelAsk.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular);
                if (checkBoxASHR.Checked == true)
                    labelAsk.BackColor = labelBid.BackColor = Color.FromArgb(170, 255, 170);
                else if (checkBoxSZE.Checked == true)
                    labelAsk.BackColor = labelBid.BackColor = Color.FromArgb(218, 174, 232);
                else if (checkBoxPMHKG.Checked == true)
                    labelAsk.BackColor = labelBid.BackColor = Color.FromArgb(255, 214, 124);
                else
                    labelAsk.BackColor = labelBid.BackColor = Color.FromArgb(255, 255, 205);
                labelValueTradeStatus.Text = "";
                labelValueTradeStatus.ForeColor = Color.FromKnownColor(KnownColor.ControlText);
            }
        }

        protected void SQForm_Enter(object sender, EventArgs e)
        {
            textBoxCode.Focus();
        }

        private void textBoxCode_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                ListenedStockCode = textBoxCode.Text.Trim();
                LocalFormSettings["StockCode"] = ListenedStockCode;
                textBoxCode.Clear();
                enterKeyPressed = true;
                e.Handled = true;
            }
            else if ((e.KeyChar < 48 || e.KeyChar > 57) && e.KeyChar != 8)
            {
                e.Handled = true;
                enterKeyPressed = false;
            }
            else
            {
                enterKeyPressed = false;
            }
        }

        private void QuoteFormFullScreen_Shown(object sender, EventArgs e)
        {
            ClearForm();
            ListenedStockCode = LocalFormSettings["StockCode"];
        }
    }
}
