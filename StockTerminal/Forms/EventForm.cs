using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Media;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using StockTerminal.Utils;
using TradeDB;
using Utils;
using System.Drawing.Printing;
using System.Diagnostics;

namespace StockTerminal.Forms
{
    public partial class EventForm : StockTerminal.Forms.BaseForm
    {
        private const int MaxEventShowCount = 1000;

        private delegate void AddEventDelegate(SystemEvent e);

        private OrderBookForm.SearchChangedDelegate pSearchChangedDelegate = null;
        private Dictionary<BaseForm, OrderBookForm.SearchOption> pSearchOptionDict = new Dictionary<BaseForm, OrderBookForm.SearchOption>(10);
        private Dictionary<string, byte> AlertDictAECode = null;

        private bool FirstSound = false;
        private long TimeLastSound = 0;

        private long TimeLastEvent = long.MaxValue;
        private bool GridSorted = false;

        private readonly System.Media.SoundPlayer SndPlayer = new SoundPlayer();

        public delegate void EventDoubleClickedDelegate(SystemEvent TheSystemEvent);
        public EventDoubleClickedDelegate EventDoubleClicked = null;

        private Dictionary<SystemEvent.SystemEventSoundType, MemoryStream> SoundStreamDict = new Dictionary<SystemEvent.SystemEventSoundType, MemoryStream>(10);

        //PrintDocument pdDocumentDeal = null;
        private Dictionary<string, Order> OrdersPrinted = new Dictionary<string, Order>();
        List<Order> ordersToPrint = null;
        List<Order> ordersToPrintCopy = null;
        private object dealPrintMutex = new object();
        private object printDealProcessMutex = new object();

        public EventForm() : this(null,null)
        {
        }

        public EventForm(CultureInfo Culture, string PersistString) : base(Culture, PersistString)
        {
            InitializeComponent();
            Utils.Utils.EnableDoubleBuffered(dataGridViewEvent);

            InitSoundStreamDict();

            pSearchChangedDelegate = new OrderBookForm.SearchChangedDelegate(OnOrderBookSearchOptionChanged);

            ListenSystemEvent();
            ListenFormListChange();

            {
                byte[] pDest = new byte[204800]; 

/*              LogFolderFetcher logFF = new LogFolderFetcher(@"C:\Users\Percy\Documents\Projects\StockTerminal\StockTerminal\bin\Debug\Log\__UserAction\", "20090415", ".log");
                List<TradeDB.Net.TradeMessage> msgList = logFF.GetNext();

                TradeDB.Net.TradeMessage msg = msgList[0];
                int fileNameLength = (msg.MessageBody[0] << 8 | msg.MessageBody[1]);
                byte[] SubPathBytes = new byte[fileNameLength];
                Buffer.BlockCopy(msg.MessageBody, 2, SubPathBytes, 0, fileNameLength);
                string fileName = Encoding.UTF8.GetString(SubPathBytes);

                MemoryStream DestStream = new MemoryStream();
                ComponentAce.Compression.Libs.zlib.ZOutputStream zStream = new ComponentAce.Compression.Libs.zlib.ZOutputStream(DestStream);

                zStream.Write(msg.MessageBody, fileNameLength + 2, msg.MessageBody.Length - fileNameLength - 2);
                zStream.Flush();
                zStream.Close();

                long lSize = DestStream.Position;
                if (lSize > 204800) lSize = 204800;

                byte[] pTemp = DestStream.ToArray();
                Buffer.BlockCopy(pTemp, 0, pDest, 0, (int)lSize);

                DestStream.Close();
                DestStream.Dispose();
*/
            }
        }

        private void EventForm_Resize(object sender, EventArgs e)
        {
            RelocatePanelChkBox();
        }

        private void EventForm_Shown(object sender, EventArgs e)
        {
            if (TradeDB != null && TradeDB.UserType == UserTypeEnum.Quote)
                this.CloseButton = true;
            else
                this.CloseButton = false;

            SetGridHeader();
            SetCheckBox();
            ListenOrderStatus(new List<string> { "" });
        }

        private void clearAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataGridViewEvent.Rows.Clear();
        }

        private void dataGridViewEvent_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (EventDoubleClicked != null && e.RowIndex >= 0 && dataGridViewEvent.Rows[e.RowIndex].Tag != null)
            {
                EventDoubleClicked.Invoke(dataGridViewEvent.Rows[e.RowIndex].Tag as SystemEvent);
            }
        }

        private void timerSortGrid_Tick(object sender, EventArgs e)
        {
            if (!GridSorted)
            {
                long nowTime = DateTime.Now.Ticks;

                if ((nowTime - TimeLastEvent) > 25000000L)
                {
                    dataGridViewEvent.SuspendLayout();
                    dataGridViewEvent.Sort(dataGridViewEvent.Columns[0], ListSortDirection.Descending);
                    dataGridViewEvent.ResumeLayout();

                    GridSorted = true;
                    timerSortGrid.Enabled = false;
                }
            }
        }

        protected override void OnCultureChange(CultureInfo ci)
        {
            SetGridHeader();
            SetCheckBox();
        }

        protected override void OnFormListChange()
        {
            List<BaseForm> OrderBookFormList = BaseForm.GetFormByFormType(typeof(OrderBookForm));

            if (OrderBookFormList == null || OrderBookFormList.Count <= 0) return;

            Dictionary<BaseForm, OrderBookForm.SearchOption> dict = new Dictionary<BaseForm, OrderBookForm.SearchOption>(OrderBookFormList.Count);

            OrderBookForm.SearchOption so = null;

            foreach (BaseForm FormObject in OrderBookFormList)
            {
                if (pSearchOptionDict.TryGetValue(FormObject, out so))
                {
                    dict[FormObject] = so;
                }
                else
                {
                    (FormObject as OrderBookForm).SearchOptionChanged += pSearchChangedDelegate;
                    dict[FormObject] = null;
                }
            }
            pSearchOptionDict = dict;

            PrepareAlertList();
        }

        private void OnOrderBookSearchOptionChanged(BaseForm SourceForm, OrderBookForm.SearchOption Option)
        {
            if (pSearchOptionDict.ContainsKey(SourceForm))
            {
                pSearchOptionDict[SourceForm] = Option;
            }
            else
            {
                pSearchOptionDict.Add(SourceForm, Option);
                (SourceForm as OrderBookForm).SearchOptionChanged += pSearchChangedDelegate;
            }

            PrepareAlertList();
        }

        protected override void OnSystemEvent(SystemEvent TheSystemEvent)
        {
            if (TheSystemEvent == null)
                return;

            TimeLastEvent = DateTime.Now.Ticks;

            dataGridViewEvent.SuspendLayout();

            // Remove excessive records in the grid
            if (dataGridViewEvent.Rows.Count >= MaxEventShowCount)
                dataGridViewEvent.Rows.RemoveAt(dataGridViewEvent.Rows.Count - 1);

            dataGridViewEvent.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;    // performance reason
            dataGridViewEvent.Rows.Insert(0, TheSystemEvent.EventTime.ToString("HH:mm:ss"), TheSystemEvent.EventType, TheSystemEvent.Description);   // assuming no row exists before any of the insertion. Not true if grid is editable. There is an empty editable row by default.
            dataGridViewEvent.Rows[0].DefaultCellStyle.BackColor = TheSystemEvent.BackColor;
            dataGridViewEvent.Rows[0].Height = 19;
            dataGridViewEvent.Rows[0].Tag = TheSystemEvent;
            AppendLog(TheSystemEvent.EventType, TheSystemEvent.Alert + "," + TheSystemEvent.OrderNo + "," + TheSystemEvent.OrderNoSort + "," + (TheSystemEvent.AECode != null ? TheSystemEvent.AECode : "") + "," + TheSystemEvent.Description, "Event", false);

            dataGridViewEvent.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dataGridViewEvent.ResumeLayout();

            if (base.IsSoundOn)
            {
                if (!FirstSound)
                {
                    PlaySound(SystemEvent.SystemEventSoundType.Filled);
                    FirstSound = true;
                }

                if ((TheSystemEvent.Alert & SystemEvent.SystemEventAlertType.Sound) == SystemEvent.SystemEventAlertType.Sound &&
                    (AlertDictAECode == null || AlertDictAECode.ContainsKey(TheSystemEvent.AECode)))
                {
                    PlaySound(TheSystemEvent.Sound);
                }
            }

            if ((TheSystemEvent.Alert & SystemEvent.SystemEventAlertType.MessageBox) == SystemEvent.SystemEventAlertType.MessageBox &&
                (AlertDictAECode == null || AlertDictAECode.ContainsKey(TheSystemEvent.AECode)))
            {
                if ((TheSystemEvent.EventType == "Cancelled" && checkBoxCancelAlert.Checked) || (TheSystemEvent.EventType == "Rejected" && checkBoxRejectAlert.Checked))
                    ShowMessageBox(TheSystemEvent.EventTime.ToString("MM/dd HH:mm:ss") + "\n\n" + TheSystemEvent.Description, TheSystemEvent.EventType,
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
            }
        }

        private void SetGridHeader()
        {
            dataGridViewEvent.Columns[0].HeaderText = GetResxString("EventGridHeaderTime");
            dataGridViewEvent.Columns[1].HeaderText = GetResxString("EventGridHeaderType");
            dataGridViewEvent.Columns[2].HeaderText = GetResxString("EventGridHeaderEvent");
            dataGridViewEvent.Columns[0].DefaultCellStyle.Font = new System.Drawing.Font("Arial", 9F, FontStyle.Bold);
            dataGridViewEvent.Columns[1].DefaultCellStyle.Font = new System.Drawing.Font("Arial", 9F, FontStyle.Bold);
            if (Culture.Name == "en-US")
                dataGridViewEvent.Columns[2].DefaultCellStyle.Font = new System.Drawing.Font("Arial", 9F, FontStyle.Regular);
            else
                dataGridViewEvent.Columns[2].DefaultCellStyle.Font = new System.Drawing.Font("MingLiU", 9F, FontStyle.Regular);
            RelocatePanelChkBox();
        }

        private void SetCheckBox()
        {
            //if (SettingsForms["ShowEventFormChkBox"] != null && SettingsForms["ShowEventFormChkBox"] == "true")
            if ((SettingsForms["ShowCancelAlertChkBox"] == null || SettingsForms["ShowCancelAlertChkBox"] != "0") ||
                (SettingsForms["ShowRejectAlertChkBox"] != null && SettingsForms["ShowRejectAlertChkBox"] == "1") || TradeDB.UserType == UserTypeEnum.AE)
            {
                panelCheckBox.Visible = true;
                dataGridViewEvent.SendToBack();
                panelCheckBox.BringToFront();
            }
            if (SettingsForms["ShowCancelAlertChkBox"] != null && SettingsForms["ShowCancelAlertChkBox"] == "0")
            {
                checkBoxCancelAlert.Visible = false;
                panelCheckBox.Width -= checkBoxCancelAlert.Width;
            }
            else
                checkBoxCancelAlert.Visible = true;
            if (SettingsForms["ShowRejectAlertChkBox"] != null && SettingsForms["ShowRejectAlertChkBox"] == "1")
                checkBoxRejectAlert.Visible = true;
            else
            {
                checkBoxRejectAlert.Visible = false;
                panelCheckBox.Width -= checkBoxRejectAlert.Width;
            }

            if (TradeDB.UserType == UserTypeEnum.AE && SettingsForms["ShowPrintDealChkBox"] != null && SettingsForms["ShowPrintDealChkBox"] == "1")
                checkBoxPrintDeal.Visible = true;
            else
            {
                checkBoxPrintDeal.Visible = false;
                panelCheckBox.Width -= checkBoxPrintDeal.Width;
            }

            if (SettingsUserPreference["UncheckCancelAlert"] != null && SettingsUserPreference["UncheckCancelAlert"] == "1")
                checkBoxCancelAlert.Checked = false;
            else
                checkBoxCancelAlert.Checked = true;

            if (SettingsUserPreference["UncheckRejectAlert"] != null && SettingsUserPreference["UncheckRejectAlert"] == "1")
                checkBoxRejectAlert.Checked = false;
            else
                checkBoxRejectAlert.Checked = true;
        }

        private void RelocatePanelChkBox()
        {
            panelCheckBox.Left = dataGridViewEvent.Columns[0].Width + dataGridViewEvent.Columns[1].Width + 60;
        }

        private void PrepareAlertList()
        {
            AlertDictAECode = new Dictionary<string, byte>(pSearchOptionDict.Count);
            string AECode = null;

            foreach (KeyValuePair<BaseForm, OrderBookForm.SearchOption> kvp in pSearchOptionDict)
            {
                if (kvp.Value == null || kvp.Value.AECode == null)
                {
                    AlertDictAECode = null;
                    return;
                }

                AECode = kvp.Value.AECode.Trim();
                if (AECode == "")   // == "" means now watching everything
                {
                    AlertDictAECode = null;
                    return;
                }
                AlertDictAECode[AECode] = 1;
            }
        }

        private void InitSoundStreamDict()
        {
            MemoryStream stream;
            byte[] buffer;

            try
            {
                stream = (MemoryStream)GetResxObject("EventSoundFilled");
                buffer = new byte[stream.Length];
                if (stream.Read(buffer, 0, buffer.Length) > 0)
                    SoundStreamDict.Add(SystemEvent.SystemEventSoundType.Filled, stream);

                stream = (MemoryStream)GetResxObject("EventSoundRejected");
                buffer = new byte[stream.Length];
                if (stream.Read(buffer, 0, buffer.Length) > 0)
                    SoundStreamDict.Add(SystemEvent.SystemEventSoundType.Rejected, stream);

                stream = (MemoryStream)GetResxObject("EventSoundNetwork");
                buffer = new byte[stream.Length];
                if (stream.Read(buffer, 0, buffer.Length) > 0)
                    SoundStreamDict.Add(SystemEvent.SystemEventSoundType.NetworkFailure, stream);
            }
            catch { }
        }

        private void PlaySound(SystemEvent.SystemEventSoundType SoundType)
        {
            MemoryStream stream;

            long nowTime = DateTime.Now.Ticks;

            if ((nowTime - TimeLastSound) >= 35000000L)   // 3.5 seconds
            {
                if (SoundStreamDict.TryGetValue(SoundType, out stream))
                {
                    if (stream != null)
                    {
                        stream.Position = 0;
                        SndPlayer.Stream = stream;
                        try
                        {
                            SndPlayer.Play();
                            TimeLastSound = nowTime;
                        }
                        catch
                        {
                            TimeLastSound = nowTime - 20000000L;
                        }
                    }
                }
            }
        }

        public void SaveMainFormSetting(int x, int y, int width, int height, FormWindowState windowState, bool IsSoundOn, bool AutoFillAccount, bool clrAccNoAfterPlaceOrder)
        {
            LocalFormSettings["X"] = x.ToString();
            LocalFormSettings["Y"] = y.ToString();
            LocalFormSettings["Width"] = width.ToString();
            LocalFormSettings["Height"] = height.ToString();
            LocalFormSettings["IsSoundOn"] = IsSoundOn.ToString();
            LocalFormSettings["AutoFillAccount"] = AutoFillAccount.ToString();
            LocalFormSettings["clrAccNoAfterPlaceOrder"] = clrAccNoAfterPlaceOrder.ToString();
            switch (windowState)
            {
                case FormWindowState.Maximized:
                    LocalFormSettings["MainFormWindowState"] = "Maximized";
                    break;
                case FormWindowState.Minimized:
                    LocalFormSettings["MainFormWindowState"] = "Minimized";
                    break;
                case FormWindowState.Normal:
                    LocalFormSettings["MainFormWindowState"] = "Normal";
                    break;
                default:
                    LocalFormSettings["MainFormWindowState"] = "Maximized";
                    break;
            }
        }

        public string LoadMainFormSetting(string paramName)
        {
            return LocalFormSettings[paramName];
        }

        protected override void OnOrderStatus(Order TheOrder)
        {
            if (TheOrder == null || TradeDB.UserType != UserTypeEnum.AE || checkBoxPrintDeal.Checked != true)
                return;

            if (TheOrder.Deals != null && TheOrder.Deals.Count > 0 && // (TheOrder.LastAction == null || TheOrder.LastAction.Trim() == "") &&
                ((TheOrder.Filled > 0 && TheOrder.BeforeChange == null) || (TheOrder.BeforeChange != null && (TheOrder.Filled - TheOrder.BeforeChange.Filled) > 0)))
            //((TheOrder.MostUpdateFilled > 0 && TheOrder.BeforeChange == null) || (TheOrder.BeforeChange != null && (TheOrder.MostUpdateFilled - TheOrder.BeforeChange.MostUpdateFilled) > 0)))
            {
                List<Deal> deals = GetDealsNotInBeforeChangeOrder(TheOrder);
                if (deals != null && deals.Count > 0)
                {
                    bool IsDealWithin2Mins = false;
                    foreach (KeyValuePair<int, Deal> kvp in TheOrder.Deals)
                    {
                        if (DateTime.Compare(TradeDB.ServerTime, kvp.Value.Time.AddMinutes(2.0)) < 0)
                        {
                            IsDealWithin2Mins = true;
                            break;
                        }
                    }
                    if (IsDealWithin2Mins) // Don't print deal that's over 2 minutes
                    {
                        lock (dealPrintMutex)
                        {
                            if (ordersToPrint == null)
                                ordersToPrint = new List<Order>();
                            Order tempOrder = null;
                            if (!OrdersPrinted.TryGetValue(TheOrder.OrderNo + " " + TheOrder.Filled, out tempOrder) || tempOrder == null)
                            {
                                ordersToPrint.Add(TheOrder);
                                OrdersPrinted.Add(TheOrder.OrderNo + " " + TheOrder.Filled, TheOrder);
                            }
                        }
                    }
                }
            }

            if (ordersToPrint != null && ordersToPrint.Count > 0)
                PrintDeal();
        }

        private void PrintDeal()
        {
            try
            {
                lock (dealPrintMutex)
                {
                    if (ordersToPrintCopy == null || ordersToPrintCopy.Count == 0)
                        ordersToPrintCopy = new List<Order>(ordersToPrint);
                    else
                        ordersToPrintCopy.AddRange(ordersToPrint);
                    ordersToPrint.Clear();
                }

                if (ordersToPrintCopy != null && ordersToPrintCopy.Count > 0)
                {
                    string printDealFolder;
                    string batFilePath;
                    string dealFilePath;

                    lock (printDealProcessMutex)
                    {
                        printDealFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "PrintDeal");
                        Directory.CreateDirectory(printDealFolder);
                        batFilePath = Path.Combine(printDealFolder, "printDeal.bat");
                        if (!File.Exists(batFilePath))
                        {
                            using (StreamWriter writer = new StreamWriter(batFilePath))
                            {
                                writer.Write("type %1 > lpt1");
                                writer.Close();
                            }
                        }
                        dealFilePath = Path.Combine(printDealFolder, ordersToPrintCopy[0].OrderNo.ToString() + ".txt");
                        using (StreamWriter writer = new StreamWriter(dealFilePath))
                        {
                            int lineCount = 0;
                            String line = null;
                            for (lineCount = 0; lineCount < ordersToPrintCopy.Count; lineCount++)
                            {
                                int iStkCode;
                                int.TryParse(ordersToPrintCopy[lineCount].StockCode, out iStkCode);
                                //String filled = String.Format("{0:N0}", ((ordersToPrintCopy[lineCount].BeforeChange != null) ? ordersToPrintCopy[lineCount].DealQtySum - ordersToPrintCopy[lineCount].BeforeChange.DealQtySum : ordersToPrintCopy[lineCount].DealQtySum));
                                line += ordersToPrintCopy[lineCount].OrderNo + " " + ((ordersToPrintCopy[lineCount].Side == 'B') ? "B" : "S") + " Stk:" + iStkCode +
                                    " Price:" + ordersToPrintCopy[lineCount].AvgPrice.ToString("G0") +
                                    " Total:" + String.Format("{0:N0}", ordersToPrintCopy[lineCount].Quantity) +
                                    " Fill:" + String.Format("{0:N0}", ordersToPrintCopy[lineCount].Filled) +
                                    //" Price:" + ordersToPrintCopy[lineCount].Price + " Total:" + String.Format("{0:N0}", ordersToPrintCopy[lineCount].Quantity) + " Fill:" + filled +
                                    //" Time:" + String.Format("{0:HH:mm:ss}", ordersToPrintCopy[lineCount].LastDealTime) + "\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n";
                                    " Time:" + String.Format("{0:HH:mm:ss}", ordersToPrintCopy[lineCount].LastDealTime) + "\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n";
                            }
                            writer.Write(line);
                            writer.Close();
                            ordersToPrintCopy.RemoveRange(0, lineCount);
                            dealFilePath = dealFilePath.Replace("C:\\", "C:\\\"") + "\"";
                            ProcessStartInfo startInfo = new ProcessStartInfo();
                            startInfo.FileName = batFilePath;
                            startInfo.Arguments = dealFilePath;
                            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                            Process.Start(startInfo);
                            //Process.Start(batFilePath, temp);
                            //System.Diagnostics.Process.Start("cmd", "/c start type C:\\PrintDeal\\32476.txt > LPT1");
                        }
                    }
                }
            }
            catch (InvalidPrinterException ex)
            {
                AppendLog("OnSystemEventInvoke", ex.Message + Environment.NewLine + ex.StackTrace.ToString(), "Invoke", false);
                ShowMessageBox(DateTime.Now.ToString("MM/dd HH:mm:ss") + "\n\n" + "Fail to print deal", "Print Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
            }
        }

        private List<Deal> GetDealsNotInBeforeChangeOrder(Order order)
        {
            List<Deal> deals = null;
            foreach (KeyValuePair<int, Deal> kvp in order.Deals)
            {
                Deal tempDeal = null;
                if (order.BeforeChange == null || order.BeforeChange.Deals == null || order.BeforeChange.Deals.Count == 0 || order.BeforeChange.Deals.TryGetValue(kvp.Key, out tempDeal) == false)
                {
                    if (deals == null)
                        deals = new List<Deal>();
                    deals.Add(kvp.Value);
                }
            }
            return deals;
        }

        private void EventForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if ((TradeDB == null || TradeDB.UserType == UserTypeEnum.AE || TradeDB.UserType == UserTypeEnum.Client) &&
                !IsProgramClosing && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
            }
        }

        private void SaveCentralUserSetting(bool itemChecked, string userPrefName)
        {
            SettingsUserPreference[userPrefName] = itemChecked ? "1" : "0";
            SetCentralUserSetting("UserPreference", SettingsUserPreference.PersistString);
        }

        private void checkBoxRejectAlert_CheckedChanged(object sender, EventArgs e)
        {
            SaveCentralUserSetting(!(sender as CheckBox).Checked, "UncheckRejectAlert");
        }

        private void checkBoxCancelAlert_CheckedChanged(object sender, EventArgs e)
        {
            SaveCentralUserSetting(!(sender as CheckBox).Checked, "UncheckCancelAlert");
        }

    }
}
