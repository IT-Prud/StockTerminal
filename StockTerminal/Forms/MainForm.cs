using System;
using System.Collections.Generic;
using System.Globalization;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using StockTerminal.Utils;
using TradeDB;
using Utils;
using System.Deployment.Application;
using StockTerminal.Web;

namespace StockTerminal.Forms
{
    public partial class MainForm : BaseForm
    {
        private int AutoLogoutWarnTime = 14400;            // 4 hours
        private int AutoLogoutAfterWarnTime = 14700;     // 4 hours & 5 minutes

        //private int childFormNumber = 0;

        private enum LayoutSourceType { Empty, Standard, AutoSave, Specific };

        private static readonly string LayoutFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\" + Application.ProductName + @"\Layout\";
        private static readonly string[] LayoutAutoSaveFilePath = new string[] { Path.Combine(LayoutFolder, "AutoSaveClient.xml"), Path.Combine(LayoutFolder, "AutoSaveAE.xml"), Path.Combine(LayoutFolder, "AutoSaveQuote.xml") };
        private Dictionary<LayoutSourceType, object> LayoutDict = new Dictionary<LayoutSourceType, object>(5);
        private bool LayoutLoaded = false;
        private DeserializeDockContent deserializeDockContent = null;

        private bool autoLogoutMessageBoxShown = false;

        private ReconnectForm reconnectForm = null;

        private ResourceManager ResManUILayout = null;

        private readonly long AAStockMask = 1;     // 0001
        private readonly long SnapShotMask = 2;   // 0010 
        private readonly long StreamingMask = 4;   // 0100
        private readonly long StockListMask = 8;   // 1000

        private readonly Dictionary<string, MarketTurnover> MarketTurnDict = new Dictionary<string, MarketTurnover>(10);
        private decimal LastMarketTurnover = 0;


        public MainForm() : this(null, null)
        {
        }

        public MainForm(CultureInfo Culture, string PersistString) : base(Culture, PersistString)
        {
            CheckCreateShortcut();
            LayoutLockable = false;

            LayoutDict[LayoutSourceType.Standard] = null;
            LayoutDict[LayoutSourceType.AutoSave] = null;
            LayoutDict[LayoutSourceType.Specific] = null;

            InitializeComponent();

            Type thisType = this.GetType();
            string targetResName = "Layouts.resources";
            Assembly thisAssembly = thisType.Assembly;
            foreach (string resName in thisAssembly.GetManifestResourceNames())
            {
                if (resName.EndsWith(targetResName))
                {
                    ResManUILayout = new ResourceManager(resName.Remove(resName.Length - 10), thisAssembly);
                    break;
                }
            }

            ListenConnectionStatus();
            ListenQuoteConnectionStatus();

            // remarked because to be read whenever login form is shown
            //SettingsTradeDB.ReadFromFile(INIFile, "TradeDB");
            //SettingsForms.ReadFromFile(INIFile, "Forms");

            deserializeDockContent = new DeserializeDockContent(GetContentFromPersistString);
        }

        private void CheckCreateShortcut()
        {
            try
            {
                if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
                {
                    ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;
                    if (ad.IsFirstRun)  //first time user has run the app since installation or update
                    {
                        Assembly code = Assembly.GetExecutingAssembly();
                        string description = string.Empty;
                        string product = string.Empty;
                        if (Attribute.IsDefined(code, typeof(AssemblyProductAttribute)))
                        {
                            AssemblyProductAttribute asProduct =
                                (AssemblyProductAttribute)Attribute.GetCustomAttribute(code,
                                typeof(AssemblyProductAttribute));
                            product = asProduct.Product;
                        }
                        if (Attribute.IsDefined(code, typeof(AssemblyDescriptionAttribute)))
                        {
                            AssemblyDescriptionAttribute asdescription =
                                (AssemblyDescriptionAttribute)Attribute.GetCustomAttribute(code,
                                typeof(AssemblyDescriptionAttribute));
                            description = asdescription.Description;
                        }
                        if (description != string.Empty)
                        {
                            string desktopPath = string.Empty;
                            desktopPath = string.Concat( Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "\\", product, ".appref-ms");
                            string shortcutName = string.Empty;
                            shortcutName = string.Concat( Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                                "\\", description, "\\", product, ".appref-ms");
                            System.IO.File.Copy(shortcutName, desktopPath, true);
                        }
                    }
                }
            }
            catch { }
        }

        /*
        private void ShowNewForm(object sender, EventArgs e)
        {
            Form childForm = new Form();
            childForm.MdiParent = this;
            childForm.Text = "Window " + childFormNumber++;
            childForm.Show();
        }

        private void OpenFile(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            openFileDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                string FileName = openFileDialog.FileName;
            }
        }
        */

        #region "Form Events"

        private void MainForm_Load(object sender, EventArgs e)
        {
            LockMenu(true);
            LockLayout(true);
            SetIndexesVisible(false);
            UpdateTitleText();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (AppState != ApplicationState.Exit && TradeDB != null)
            {
                e.Cancel = true;
                if (TradeDB.UserType == UserTypeEnum.Quote || 
                    e.CloseReason == CloseReason.WindowsShutDown || 
                    ShowMessageBox(GetResxString("MsgBoxConfirmExit"), this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                {
                    AppState = ApplicationState.Exit;
                    Logout();
                }
            }
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            WSound.Stop();
            SyncTradeSubscriptionStop();
            SyncQuoteSubscriptionStop();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            ReLocateLabelTradeStatus();
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            this.Location = new Point(0, 0);
            this.Width = SystemInformation.PrimaryMonitorMaximizedWindowSize.Width - 5;
            this.Height = SystemInformation.PrimaryMonitorMaximizedWindowSize.Height - 2;
            this.WindowState = FormWindowState.Maximized;

            SetMenu();
            ShowLoginForm(null);

            ReLocateLabelTradeStatus();
        }

        private void logoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ShowMessageBox(GetResxString("MsgBoxConfirmLogout"), this.Text, MessageBoxButtons.YesNo,
                MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                AppState = ApplicationState.Logout;
                Logout();
            }
        }
/*
        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            saveFileDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
            if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                string FileName = saveFileDialog.FileName;
            }
        }

        private void ExitToolsStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void CutToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void CopyToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void PasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }
*/
        private void ToolBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStrip.Visible = toolBarToolStripMenuItem.Checked;
        }

        private void CascadeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.Cascade);
        }

        private void TileVerticalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.TileVertical);
        }

        private void TileHorizontalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.TileHorizontal);
        }

        private void ArrangeIconsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.ArrangeIcons);
        }

        private void CloseAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form childForm in MdiChildren)
            {
                childForm.Close();
            }
        }

        private void quoteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (GetInstanceCount(typeof(QuoteBrowserForm)) < 1)
            {
                QuoteBrowserForm form = new QuoteBrowserForm(this.Culture, null);
                form.Show(dockPanelMain, DockState.Float, new Rectangle(200, 200, form.Width, form.Height));
            }
        }

        private void orderTicketSimpleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (GetInstanceCount(typeof(OrderTicketForm_simple)) < 10)
            {
                OrderTicketForm form = new OrderTicketForm(this.Culture, null, this.dockPanelMain, ExchangeTypeEnum.HKG, null);
                form.Show(dockPanelMain, DockState.Float, new Rectangle(200, 200, form.Width, form.Height));
            }
        }

        private void orderTicketAdvanceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (GetInstanceCount(typeof(OrderTicketForm)) < 10)
            {
                OrderTicketForm form = new OrderTicketForm(this.Culture, null, this.dockPanelMain, ExchangeTypeEnum.HKG, null);
                form.Show(dockPanelMain, DockState.Float, new Rectangle(200, 200, form.Width, form.Height));
            }
        }

        private void orderTicketFastToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (GetInstanceCount(typeof(FastOrderTicketForm)) < 10)
            {
                FastOrderTicketForm form = new FastOrderTicketForm(this.Culture, null, this.dockPanelMain);
                form.Show(dockPanelMain, DockState.Float, new Rectangle(200, 200, form.Width, form.Height));
            }
        }

        private void orderBookToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (GetInstanceCount(typeof(OrderBookForm)) < 5)
            {
                OrderBookForm form = new OrderBookForm(this.Culture, null, this.dockPanelMain);
                form.Show(dockPanelMain, DockState.Float, new Rectangle(200, 200, form.Width, form.Height));
            }
        }

        private void balanceStockToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (GetInstanceCount(typeof(AccountForm)) < 5)
            {
                AccountForm form = new AccountForm(this.Culture, null, this.dockPanelMain);
                form.Show(dockPanelMain, DockState.Float, new Rectangle(200, 200, form.Width, form.Height));
            }
        }

        private void OrderListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (GetInstanceCount(typeof(OrderListForm)) < 5)
            {
                OrderListForm form = new OrderListForm(this.Culture, null);
                form.Show(dockPanelMain, DockState.Float, new Rectangle(200, 200, form.Width, form.Height));
            }
        }

        private void stockQuoteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string MaxCountStockQuoteForm = SettingsForms["MaxCountStockQuoteForm"];
            int maxCount = 0;

            if (TradeDB != null && TradeDB.UserType == UserTypeEnum.Quote)
            {
                maxCount = 18;
            }
            else
            {
                if (!int.TryParse(MaxCountStockQuoteForm, out maxCount))
                    maxCount = 1;
                else if (maxCount < 0)
                    maxCount = 0;
                else if (maxCount > 10)
                    maxCount = 10;
            }

            if (GetInstanceCount(typeof(StockQuoteForm)) < maxCount)
            {
                StockQuoteForm form = new StockQuoteForm(this.Culture, null, ExchangeTypeEnum.Unassigned);
                form.Show(dockPanelMain, DockState.Float, new Rectangle(50, 80, form.Width, form.Height));
            }
        }

        //private void EventLogtoolStripMenuItem_Click(object sender, EventArgs e)
        //{
        //    if (GetInstanceCount(typeof(EventForm)) < 1)
        //    {
        //        EventForm form = new EventForm(this.Culture, null);
        //        form.Show(dockPanelMain, DockState.Float, new Rectangle(200, 200, form.Width, form.Height));
        //    }
        //}

        private void PrintReportToolStripButton_Click(object sender, EventArgs e)
        {
            //Report.DailyReportForm form = new StockTerminal.Report.DailyReportForm(this.Culture, null, 10);
            //form.Show(dockPanelMain, DockState.Float, new Rectangle(200, 200, form.Width, form.Height));
            Report.SelectAEListForm form = new StockTerminal.Report.SelectAEListForm(this.Culture, null, dockPanelMain);
            form.Show(dockPanelMain, DockState.Float, new Rectangle(200, 200, form.Width, form.Height));
        }

        private void presetAccountToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (GetInstanceCount(typeof(PresetAcc)) < 2)
            {
                PresetAcc form = new PresetAcc(this.Culture, null);
                form.Show(dockPanelMain, DockState.Float, new Rectangle(200, 200, form.Width, form.Height));
            }
        }

        private void SettingsLangENToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Culture = null;
        }

        private void SettingsLangTCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Culture = new CultureInfo("zh-CHT");
        }

        private void SettingsLangSCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Culture = new CultureInfo("zh-CHS");
        }

        private void LoadLayoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = LayoutFolder;
            openFileDialog.Filter = "Layout Files (*.xml)|*.xml|All Files (*.*)|*.*";
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                LoadLayout(openFileDialog.FileName);
            }
        }

        private void SaveLayoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = LayoutFolder;
            saveFileDialog.Filter = "Layout Files (*.xml)|*.xml|All Files (*.*)|*.*";
            if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                SaveLayout(saveFileDialog.FileName);
            }
        }

        /// <summary>
        /// Lock Layout item of Settings menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lockLayoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LockLayout(!(sender as ToolStripMenuItem).Checked);
        }

        private void standardLayoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string layoutXML = null;
                if (TradeDB.UserType == UserTypeEnum.AE)
                    layoutXML = (string)ResManUILayout.GetObject("Standard");
                else if (TradeDB.UserType == UserTypeEnum.Client)
                {
                    long stockTypeValue;
                    long.TryParse(SettingsForms["StockQuoteType"], out stockTypeValue);
                    if ((stockTypeValue & AAStockMask) > 0)
                        layoutXML = (string)ResManUILayout.GetObject("AAStockStandardLayout");
                    else
                        layoutXML = (string)ResManUILayout.GetObject("Standard_Client");
                }
                if (layoutXML != null)
                {
                    Stream layoutStream = new MemoryStream(Encoding.Unicode.GetBytes(layoutXML));
                    if (layoutStream != null) LoadLayout(layoutStream);
                }
            }
            catch { }
        }

        private void advancedLayoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string layoutXML = null;
                if (TradeDB.UserType == UserTypeEnum.AE)
                    layoutXML = (string)ResManUILayout.GetObject("Advanced");
                else if (TradeDB.UserType == UserTypeEnum.Client)
                {
                    long stockTypeValue;
                    long.TryParse(SettingsForms["StockQuoteType"], out stockTypeValue);
                    if ((stockTypeValue & AAStockMask) > 0)
                        layoutXML = (string)ResManUILayout.GetObject("AAStockAdvancedLayout");
                    else
                        layoutXML = (string)ResManUILayout.GetObject("Advanced_Client");
                }

                if (layoutXML != null)
                {
                    Stream layoutStream = new MemoryStream(Encoding.Unicode.GetBytes(layoutXML));
                    if (layoutStream != null) LoadLayout(layoutStream);
                }
            }
            catch { }
        }

        private void compactLayoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                this.WindowState = FormWindowState.Normal;
                string layoutXML = (string)ResManUILayout.GetObject("Compact");
                Stream layoutStream = new MemoryStream(Encoding.Unicode.GetBytes(layoutXML));
                if (layoutStream != null) 
                    LoadLayout(layoutStream);
            }
            catch { }
        }

        private void quoteLayoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                this.WindowState = FormWindowState.Normal;
                string layoutXML = (string)ResManUILayout.GetObject("QuoteOnly");
                Stream layoutStream = new MemoryStream(Encoding.Unicode.GetBytes(layoutXML));
                if (layoutStream != null)
                    LoadLayout(layoutStream);
            }
            catch { }
        }

        private void dailyReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Report.SelectAEListForm form = new StockTerminal.Report.SelectAEListForm(this.Culture, null, dockPanelMain);
            form.Show(dockPanelMain, DockState.Float, new Rectangle(200, 200, form.Width, form.Height));
        }

        private void timerLoadLayout_Tick(object sender, EventArgs e)
        {
            timerLoadLayout.Enabled = false;

            bool LoadStop = false;
            string XMLLayoutFilePath = null;
            object LayoutObject = null;
            LayoutSourceType LayoutType;
            Stream LayoutStream = null;

            LayoutType = (timerLoadLayout.Tag != null && timerLoadLayout.Tag is LayoutSourceType) ?
                (LayoutSourceType)timerLoadLayout.Tag : LayoutSourceType.AutoSave;

            try
            {
                LayoutObject = LayoutDict[LayoutType];
                if (LayoutObject != null)
                {
                    if (LayoutObject is string)
                    {
                        XMLLayoutFilePath = (string)LayoutObject;
                        if (!File.Exists(XMLLayoutFilePath) && LayoutType == LayoutSourceType.AutoSave)
                        {
                            switch (TradeDB.UserType)   // try load from old stock terminal version's auto save path
                            {
                                case UserTypeEnum.AE: XMLLayoutFilePath = XMLLayoutFilePath.Replace("\\AutoSaveAE.xml", "\\AutoSave.xml"); break;
                                case UserTypeEnum.Client: XMLLayoutFilePath = XMLLayoutFilePath.Replace("\\AutoSaveClient.xml", "\\AutoSave.xml"); break;
                                case UserTypeEnum.Quote: XMLLayoutFilePath = XMLLayoutFilePath.Replace("\\AutoSaveQuote.xml", "\\AutoSave.xml"); break;
                            }
                        }
                        LayoutStream = new FileStream(XMLLayoutFilePath, FileMode.Open, FileAccess.Read);
                    }
                    else if (LayoutObject is Stream)
                    {
                        LayoutStream = (Stream)LayoutObject;
                    }
                }
                else
                {
                    switch (TradeDB.UserType)
                    {
                        case UserTypeEnum.AE:
                            LayoutStream = new MemoryStream(Encoding.Unicode.GetBytes((string)ResManUILayout.GetObject("Standard")));
                            break;

                        case UserTypeEnum.Client:
                            long stockTypeValue;
                            long.TryParse(SettingsForms["StockQuoteType"], out stockTypeValue);
                            if ((stockTypeValue & AAStockMask) > 0)
                                LayoutStream = new MemoryStream(Encoding.Unicode.GetBytes((string)ResManUILayout.GetObject("AAStockStandardLayout")));
                            else
                                LayoutStream = new MemoryStream(Encoding.Unicode.GetBytes((string)ResManUILayout.GetObject("Standard_Client")));
                            break;

                        case UserTypeEnum.Quote:
                            LayoutStream = new MemoryStream(Encoding.Unicode.GetBytes((string)ResManUILayout.GetObject("QuoteOnly")));
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog("Layout", "Failed to load layout \"" + (XMLLayoutFilePath != null ? XMLLayoutFilePath : "") + 
                    "\" - " + ex.Message, "Error", LayoutType == LayoutSourceType.Specific);

                if (LayoutType == LayoutSourceType.Specific)
                {
                    LayoutType = LayoutSourceType.AutoSave;
                }
                else if (LayoutType == LayoutSourceType.AutoSave)
                {
                    LayoutType = LayoutSourceType.Standard;
                }
                else
                {
                    LayoutType = LayoutSourceType.Empty;
                    LoadStop = true;
                }
            }

            if (LayoutStream != null)
            {
                try
                {
                    dockPanelMain.LoadFromXml(LayoutStream, deserializeDockContent);
                    LoadStop = true;
                }
                catch (System.InvalidOperationException)
                {
                    bool pLayoutLocked = LayoutLocked;
                    if (LayoutLocked) LockLayout(false);
                    CloseAllWindows(dockPanelMain);  // must close all DockContent or the docking UI won't load layout
                    if (pLayoutLocked) LockLayout(true);
                }
                catch (Exception ex)
                {
                    AppendLog("Layout", "Failed to load layout \"" + (XMLLayoutFilePath != null ? XMLLayoutFilePath : "") +
                        "\" - " + ex.Message, "Error", LayoutType == LayoutSourceType.Specific);

                    if (LayoutType == LayoutSourceType.Specific)
                    {
                        LayoutType = LayoutSourceType.AutoSave;
                    }
                    else if (LayoutType == LayoutSourceType.AutoSave)
                    {
                        LayoutType = LayoutSourceType.Standard;
                    }
                    else
                    {
                        LayoutType = LayoutSourceType.Empty;
                        LoadStop = true;
                    }
                }
            }

            if (!LoadStop)
            {
                timerLoadLayout.Tag = LayoutType;
                timerLoadLayout.Interval = 200;
                timerLoadLayout.Enabled = true;
            }
            else
            {
                LayoutLoaded = true;
                LockMenu(false);
                if (TradeDB.UserType == UserTypeEnum.AE || TradeDB.UserType == UserTypeEnum.Quote)
                {
                    ListenIndex(new List<string> { "HSI", "CEI" }, true); //, "ASHRQUOTA"
                    ListenMarketTurnover(new List<string> { "MAIN", "GEM", "ETS", "NASD" }, true);
                    SetIndexesVisible(true);
                    KeepAccNoAfterPlaceOrderToolStripMenuItem.Visible = true;
                    AutoFillAccNoToolStripMenuItem.Visible = true;
                    SetAutoFillAccNo(true);
                    SetKeepAccNoAfterPlaceOrder(false);
                    SetSpecialType(true);
                }
                else
                {
                    KeepAccNoAfterPlaceOrderToolStripMenuItem.Visible = false;
                    AutoFillAccNoToolStripMenuItem.Visible = false;
                    SetAutoFillAccNo(false);
                    SetKeepAccNoAfterPlaceOrder(true);
                    SetIndexesVisible(false);
                    SetSpecialType(true);
                }

                List<BaseForm> eventFormList = GetFormByFormType(typeof(EventForm));
                if (eventFormList == null)
                {
                    if (TradeDB == null && (TradeDB.UserType == UserTypeEnum.AE || TradeDB.UserType == UserTypeEnum.Client))
                    {
                        EventForm form = new EventForm(this.Culture, null);
                        form.Show(dockPanelMain, DockState.Float, new Rectangle(200, 200, form.Width, form.Height));
                    }
                }
                else
                {
                    // DockPanel is not call GetPersistentString () in backForm, thus (use ugly way), borrow eventForm save and load settings
                    int x = 0, y = 0, width = 0, height = 0;
                    bool IsSoundChecked;

                    if (eventFormList != null && eventFormList.Count > 0 && eventFormList[0] != null)
                    {
                        int.TryParse(((EventForm)eventFormList[0]).LoadMainFormSetting("X"), out x);
                        int.TryParse(((EventForm)eventFormList[0]).LoadMainFormSetting("Y"), out y);
                        int.TryParse(((EventForm)eventFormList[0]).LoadMainFormSetting("Width"), out width);
                        int.TryParse(((EventForm)eventFormList[0]).LoadMainFormSetting("Height"), out height);
                        if (bool.TryParse(((EventForm)eventFormList[0]).LoadMainFormSetting("IsSoundOn"), out IsSoundChecked))
                            checkBoxSound.Checked = IsSoundChecked;
                        if (TradeDB.UserType == UserTypeEnum.AE)
                        {
                            bool result = false;
                            if (bool.TryParse(((EventForm)eventFormList[0]).LoadMainFormSetting("AutoFillAccount"), out result))
                            {
                                AutoFillAccNoToolStripMenuItem.Checked = result;
                                AutoFillAccNo = result;
                            }
                            if (bool.TryParse(((EventForm)eventFormList[0]).LoadMainFormSetting("clrAccNoAfterPlaceOrder"), out result))
                            {
                                KeepAccNoAfterPlaceOrderToolStripMenuItem.Checked = result;
                                KeepAccNoAfterPlaceOrder = result;
                            }
                        }
                        base.TurnSoundOn(checkBoxSound.Checked);
                    }
                    if (width > 0 && height > 0)
                    {
                        switch (((EventForm)eventFormList[0]).LoadMainFormSetting("MainFormWindowState"))
                        {
                            case "Maximized":
                                this.WindowState = FormWindowState.Maximized;
                                break;
                            case "Minimized":
                                this.WindowState = FormWindowState.Minimized;
                                break;
                            case "Normal":
                                this.WindowState = FormWindowState.Normal;
                                break;
                            default:
                                this.WindowState = FormWindowState.Maximized;
                                break;
                        }
                        this.Location = new Point(x, y);
                        this.Width = width;
                        this.Height = height;
                    }
                }
            }
        }

        private void timerLogout_Tick(object sender, EventArgs e)
        {
            timerLogout.Enabled = false;
            if (IsReadyForLogout())
            {
                if (AppState == ApplicationState.Exit)
                {
                    Application.Exit();
                }
                else
                {
                    CloseAllWindows(dockPanelMain);
                    SetMenu();
                    ShowLoginForm(
                        (TradeDB.SessionLastTradeError == global::TradeDB.LastErrorEnum.ForceLogout) ?
                        "Login_Message_ForceLogout" :
                        null);
                    this.Enabled = true;

                    TradeDB = null;
                    UpdateTitleText();
                    UpdateStatusLabels();

                    Instance.UnregisterInstance();

                    MarketTurnDict.Clear();
                    LastMarketTurnover = 0;
                }
            }
            else
            {
                timerLogout.Interval = 500;
                timerLogout.Enabled = true;
            }
        }

        private void timerLastUserAction_Tick(object sender, EventArgs e)
        {
            if (TradeDB.ConnectionType == ConnectionTypeEnum.Local)
                return;

            int tempAutoLogoutWarmTime = -1, tempAutoLogoutAfterWarnTime = -1;
            if (int.TryParse(SettingsForms["AutoLogoutWarnTime"], out tempAutoLogoutWarmTime) && tempAutoLogoutWarmTime >= 0)
                AutoLogoutWarnTime = tempAutoLogoutWarmTime;
            if (int.TryParse(SettingsForms["AutoLogoutAfterWarnTime"], out tempAutoLogoutAfterWarnTime) && tempAutoLogoutAfterWarnTime >= 0)
                AutoLogoutAfterWarnTime = tempAutoLogoutAfterWarnTime;
            int secs = GetTimeSinceLastUserAction();
            if (secs >= AutoLogoutAfterWarnTime && AutoLogoutAfterWarnTime > 0) // seconds
            {
                autoLogoutMessageBoxShown = false;
                HideMessageBox();
                AppState = ApplicationState.Logout;
                Logout();
                return;
            }
            else if (secs >= AutoLogoutWarnTime && AutoLogoutWarnTime > 0 && autoLogoutMessageBoxShown == false) // seconds
            {
                autoLogoutMessageBoxShown = true;
                if (ShowMessageBox(String.Format(GetResxString("MsgBoxConfirmAutoLogout"), AutoLogoutWarnTime/60, (AutoLogoutAfterWarnTime - AutoLogoutWarnTime)/60), this.Text, 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                {
                    autoLogoutMessageBoxShown = false;
                    AppState = ApplicationState.Logout;
                    Logout();
                    return;
                }
                autoLogoutMessageBoxShown = false;
            }
     
        }

        #endregion

        private void LoginForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            timerLastUserAction.Enabled = true;
            if (AppState == ApplicationState.Logout)
            {
                AppState = ApplicationState.Run;

                SetMenu();
                LoadLayout(null as string);

                if (TradeDB != null)
                {
                    if (TradeDB.UserType == UserTypeEnum.AE || TradeDB.UserType == UserTypeEnum.Client)
                    {
                        SyncTradeSubscriptionStart();
                        SyncQuoteSubscriptionStart();
                        SyncTradeSubscriptionSetAllOutstand();
                        HideReconnectForm();

                        GetCentralUserSetting("FormCentralSettings");
                        GetCentralUserSetting("UserPreference");
                    }
                    else if (TradeDB.UserType == UserTypeEnum.Quote)
                    {
                        SyncQuoteSubscriptionStart();
                    }

                    if (TradeDB.UserType == UserTypeEnum.Client || TradeDB.UserType == UserTypeEnum.Quote)
                        TradeDB.RequireOTP = false;
                    else
                    {
                        if (TradeDB.IPType.ToUpper().Trim() == "LOCAL" || TradeDB.IPType.ToUpper().Trim() == "BRANCH")
                            TradeDB.RequireOTP = false;
                        else
                            TradeDB.RequireOTP = (SettingsForms["SkipOrderOTP"] == "1") ? false : true;
                    }
                    orderTicketFastToolStripMenuItem.Visible = (TradeDB.RequireOTP) ? false : true;
                }
            }
        }

        protected override void OnConnectionStatus(int State)
        {
            UpdateTitleText();
            UpdateStatusLabels();
            if (AppState == ApplicationState.Run)
            {
                switch (State)
                {
                    case (int)global::TradeDB.ProcessState.Ready:
                        if (TradeDB.UserType == UserTypeEnum.AE || TradeDB.UserType == UserTypeEnum.Client)
                        {
                            SyncTradeSubscriptionSetAllOutstand();
                        }
                        /*
                        else
                        {
                            SetStockQuoteMenu();
                            LoadLayout(null as string);
                        }
                         */
                        HideReconnectForm();
                        break;

                    case (int)global::TradeDB.ProcessState.Stopped:
                        if (TradeDB.SessionLastTradeError == LastErrorEnum.ForceLogout)
                        {
                            AppState = ApplicationState.Logout;
                            Logout();
                        }
                        else
                        {
                            ShowReconnectForm();
                        }
                        //ShowMessageBox(GetResxString(""), GetResxString(""), MessageBoxButtons.RetryCancel, MessageBoxDefaultButton.Button2);
                        break;

                    default:
                        if (TradeDB.SessionLastTradeError == LastErrorEnum.ForceLogout)
                        {
                            AppState = ApplicationState.Logout;
                            Logout();
                        }
                        else
                        {
                            ShowReconnectForm();
                        }
                        break;
                }
            }
        }

        protected override void OnQuoteConnectionStatus(int State)
        {
            if (AppState == ApplicationState.Run)
            {
                switch (State)
                {
                    case (int)global::TradeDB.Net.QuoteConnector.ProcessState.Ready:
                        SyncQuoteSubscriptionSetAllOutstand();
                        break;

                    case (int)global::TradeDB.Net.QuoteConnector.ProcessState.Stopped:
                        if (TradeDB.SessionLastQuoteError == LastErrorEnum.ForceLogout)
                        {
                            SyncQuoteSubscriptionStop();
                        }
                        break;

                    default:
                        if (TradeDB.SessionLastQuoteError == LastErrorEnum.ForceLogout)
                        {
                            SyncQuoteSubscriptionStop();
                        }
                        break;
                }
            }
        }

        protected override void OnIndex(Index TheIndex)
        {
            if (TheIndex != null)
            {
                switch (TheIndex.Code)
                {
                    case "HSI":
                        labelValueIndexHSI.Text = TheIndex.Last.ToString("#,##0.00");
                        labelValueIndexHSIChange.Text = TheIndex.Change.ToString("+#,##0.00;-#,##0.00;0.00");
                        if (TheIndex.Change >= 0)
                        {
                            labelValueIndexHSI.ForeColor = Color.FromArgb(0xc0, 0x00, 0x00);
                            labelValueIndexHSI.BackColor = Color.FromArgb(0xe0, 0xc8, 0xc8);
                            labelValueIndexHSIChange.ForeColor = Color.FromArgb(0xc0, 0x00, 0x00);
                            labelValueIndexHSIChange.BackColor = Color.FromArgb(0xe0, 0xc8, 0xc8);
                        }
                        else
                        {
                            labelValueIndexHSI.ForeColor = Color.FromArgb(0x00, 0xb0, 0x00);
                            labelValueIndexHSI.BackColor = Color.FromArgb(0xc8, 0xe0, 0xc8);
                            labelValueIndexHSIChange.ForeColor = Color.FromArgb(0x00, 0xb0, 0x00);
                            labelValueIndexHSIChange.BackColor = Color.FromArgb(0xc8, 0xe0, 0xc8);
                        }
                        break;

                    case "CEI":
                        labelValueIndexCEI.Text = TheIndex.Last.ToString("#,##0.00");
                        labelValueIndexCEIChange.Text = TheIndex.Change.ToString("+#,##0.00;-#,##0.00;0.00");
                        if (TheIndex.Change >= 0)
                        {
                            labelValueIndexCEI.ForeColor = Color.FromArgb(0xc0, 0x00, 0x00);
                            labelValueIndexCEI.BackColor = Color.FromArgb(0xe0, 0xc8, 0xc8);
                            labelValueIndexCEIChange.ForeColor = Color.FromArgb(0xc0, 0x00, 0x00);
                            labelValueIndexCEIChange.BackColor = Color.FromArgb(0xe0, 0xc8, 0xc8);
                        }
                        else
                        {
                            labelValueIndexCEI.ForeColor = Color.FromArgb(0x00, 0xb0, 0x00);
                            labelValueIndexCEI.BackColor = Color.FromArgb(0xc8, 0xe0, 0xc8);
                            labelValueIndexCEIChange.ForeColor = Color.FromArgb(0x00, 0xb0, 0x00);
                            labelValueIndexCEIChange.BackColor = Color.FromArgb(0xc8, 0xe0, 0xc8);
                        }
                        break;
                        /*
                    case "ASHRQUOTA":
                        if (TheIndex.Last <= 0)
                            labelValueASHRQuota.Text = "0";
                        else if (TheIndex.Last <= 1000)
                            labelValueASHRQuota.Text = TheIndex.Last.ToString("#,##");
                        else if (TheIndex.Last <= 1000000)
                            labelValueASHRQuota.Text = (TheIndex.Last * 0.001M).ToString("#,##") + "k";
                        else
                            labelValueASHRQuota.Text = (TheIndex.Last * 0.000001M).ToString("#,##") + "M";
                        labelValueASHRQuota.BackColor = Color.Lime;
                        break;
                         */
                }

                ReLocateLabelTradeStatus();
            }
        }

        protected override void OnMarketTurnover(MarketTurnover TheMarketTurnover)
        {
            MarketTurnover oldMarketTurnover = null;
            decimal oldTurn = 0;

            if (TheMarketTurnover != null)
            {
                if (MarketTurnDict.TryGetValue(TheMarketTurnover.MarketCode, out oldMarketTurnover) && oldMarketTurnover != null)
                    oldTurn = oldMarketTurnover.Turnover;

                MarketTurnDict[TheMarketTurnover.MarketCode] = TheMarketTurnover;

                LastMarketTurnover += TheMarketTurnover.Turnover - oldTurn;
            }

            labelValueMarketTurnover.Text = LastMarketTurnover > 0 ? Stock.GetShortValue(LastMarketTurnover, 6, 0) : "";
        }

        protected override void OnCultureChange(CultureInfo ci)
        {
            SetStockQuoteMenu();
            UpdateTitleText();
            UpdateStatusLabels();
            ReLocateLabelTradeStatus();
        }

        public void UpdateTitleText()
        {
            this.Text = GetTitleText();
        }

        public string GetTitleText()
        {
            string titleText = null;

            if (Culture != null)
            {
                if (TradeDB == null)
                {
                    titleText = GetResxString("DocumentName") + " " + Application.ProductVersion;
                }
                else if (TradeDB.UserType == UserTypeEnum.Quote)
                {
                    titleText = GetResxString("DocumentNameQuote") + " " + Application.ProductVersion;
                }
                else
                {
                    titleText = GetResxString("DocumentName") + " " + Application.ProductVersion;

                    if (TradeDB.UserType == UserTypeEnum.Client)
                        titleText += " - " + GetResxString("UserTypeClient");
                    else if (TradeDB.UserType == UserTypeEnum.AE)
                        titleText += " - " + GetResxString("UserTypeAE");
                }

                string GroupName = base.DeploymentGroupName.Trim();

                if (GroupName != null && GroupName.Length > 0)
                    titleText += " - " + GroupName;
            }

            return titleText;
        }

        public void UpdateStatusLabels()
        {
            // public new enum ProcessState { Stopped, Connecting, Ready, Disconnecting, MaxState };

            int State = (TradeDB != null) ? TradeDB.TradeState : -1;
            string loginHostName = (TradeDB != null && TradeDB.CurrentLoginHostTrade != null) ? TradeDB.CurrentLoginHostTrade.Name : "";

            switch (State)
            {
                case 0:
                    labelTradeDBStatus.Text = GetResxString("ToolStripStatusTradeDB_Stopped") + " " + loginHostName;
                    labelTradeDBStatus.BackColor = Color.FromArgb(0xff, 0x80, 0x80);
                    break;

                case 1:
                    labelTradeDBStatus.Text = GetResxString("ToolStripStatusTradeDB_Login") + " " + loginHostName;
                    labelTradeDBStatus.BackColor = Color.FromArgb(0xff, 0xff, 0x80);
                    break;

                case 2:
                    labelTradeDBStatus.Text = GetResxString("ToolStripStatusTradeDB_Connecting") + " " + loginHostName;
                    labelTradeDBStatus.BackColor = Color.FromArgb(0xff, 0xff, 0x80);
                    break;

                case 3:
                    //labelTradeDBStatus.Text = GetResxString("ToolStripStatusTradeDB_Ready") + " " + loginHostName;
                    labelTradeDBStatus.Text = loginHostName;
                    labelTradeDBStatus.BackColor = Color.FromArgb(0x80, 0xff, 0x80);
                    break;

                case 4:
                    labelTradeDBStatus.Text = GetResxString("ToolStripStatusTradeDB_Disconnecting");
                    labelTradeDBStatus.BackColor = Color.FromArgb(0xff, 0xff, 0x80);
                    break;

                default:
					labelTradeDBStatus.Text = 
                        (SettingsTradeDB["LoginHost01"] ?? "") + " " + 
                        (SettingsTradeDB["LoginHost02"] ?? "") + " " + 
                        (SettingsTradeDB["LoginHost03"] ?? "") + " " + 
                        (SettingsTradeDB["LoginHost04"] ?? "") + " " + 
                        (SettingsTradeDB["LoginHost05"] ?? "");
                    labelTradeDBStatus.BackColor = Color.FromKnownColor(KnownColor.Control);
                    break;
            }

            ReLocateLabelTradeStatus();
        }

        protected override void OnCentralUserSetting(SettingKeyValue KeyValue)
        {
            if (KeyValue != null)
            {
                bool formCentralSettingsFound = false;

                ConfirmBeforeOrderToolStripMenuItem.Checked = true;
                ConfirmBeforeOrderToolStripMenuItem.Enabled = false;
                SettingsUserPreference["ConfirmBeforeOrder"] = "1";
                ConfirmQuickAmendToolStripMenuItem.Checked = true;
                SettingsUserPreference["ConfirmBeforeQuickAmend"] = "0";
                SettingsUserPreference["UncheckCancelAlert"] = "0";
                SettingsUserPreference["UncheckRejectAlert"] = "0";

                if (KeyValue != null)
                {
                    if (KeyValue.Key == "FormCentralSettings")
                    {
                        Settings setting = new Settings();
                        setting.PersistString = KeyValue.Value;
                        if (setting["AAStockQuoteWebURL"] != null && setting["AAStockQuoteWebURL"] != "")
                            SettingsForms["AAStockQuoteWebURL"] = setting["AAStockQuoteWebURL"];
                        if (setting["ETradeWebURL"] != null && setting["ETradeWebURL"] != "")
                            SettingsForms["ETradeWebURL"] = setting["ETradeWebURL"];
                        if (setting["MaxCountStockQuoteForm"] != null && setting["MaxCountStockQuoteForm"] != "")
                            SettingsForms["MaxCountStockQuoteForm"] = setting["MaxCountStockQuoteForm"];
                        if (setting["StockQuoteType"] != null && setting["StockQuoteType"] != "")
                            SettingsForms["StockQuoteType"] = setting["StockQuoteType"];
                        if (setting["AutoLogoutWarnTime"] != null && setting["AutoLogoutWarnTime"] != "")
                            SettingsForms["AutoLogoutWarnTime"] = setting["AutoLogoutWarnTime"];
                        if (setting["AutoLogoutAfterWarnTime"] != null && setting["AutoLogoutAfterWarnTime"] != "")
                            SettingsForms["AutoLogoutAfterWarnTime"] = setting["AutoLogoutAfterWarnTime"];
                        if (setting["AccSettingsWebURL"] != null && setting["AccSettingsWebURL"] != "")
                            SettingsForms["AccSettingsWebURL"] = setting["AccSettingsWebURL"];
                        //if (setting["ShowEventFormChkBox"] != null && setting["ShowEventFormChkBox"] != "")
                        //{
                        //    SettingsForms["ShowEventFormChkBox"] = setting["ShowEventFormChkBox"];
                        //    //ShowSoundChkBox();
                        //}
                        if (setting["ShowCancelAlertChkBox"] != null && setting["ShowCancelAlertChkBox"] != "")
                            SettingsForms["ShowCancelAlertChkBox"] = setting["ShowCancelAlertChkBox"];
                        if (setting["ShowRejectAlertChkBox"] != null && setting["ShowRejectAlertChkBox"] != "")
                            SettingsForms["ShowRejectAlertChkBox"] = setting["ShowRejectAlertChkBox"];
                        if (setting["ShowPrintDealChkBox"] != null && setting["ShowPrintDealChkBox"] != "")
                            SettingsForms["ShowPrintDealChkBox"] = setting["ShowPrintDealChkBox"];
                        if (setting["QuoteAAStockLoginID"] != null && setting["QuoteAAStockLoginID"] != "")
                            SettingsForms["QuoteAAStockLoginID"] = setting["QuoteAAStockLoginID"];
                        if (setting["QuoteAAStockLoginPassword"] != null && setting["QuoteAAStockLoginPassword"] != "")
                            SettingsForms["QuoteAAStockLoginPassword"] = setting["QuoteAAStockLoginPassword"];
                        if (setting["AllowOrderChangeAccount"] != null && setting["AllowOrderChangeAccount"] != "")
                            SettingsForms["AllowOrderChangeAccount"] = setting["AllowOrderChangeAccount"];
                        if (setting["DisableHKG"] != null && setting["DisableHKG"] != "")
                        {
                            SettingsForms["DisableHKG"] = setting["DisableHKG"];
                            if (SettingsForms["DisableHKG"] != null && SettingsForms["DisableHKG"] == "1")
                                orderTicketFastToolStripMenuItem.Visible = false;
                        }
                        if (setting["DisableSHG"] != null && setting["DisableSHG"] != "")
                            SettingsForms["DisableSHG"] = setting["DisableSHG"];
                        if (setting["DisableSZE"] != null && setting["DisableSZE"] != "")
                            SettingsForms["DisableSZE"] = setting["DisableSZE"];
                        if (setting["DisableGray"] != null && setting["DisableGray"] != "")
                            SettingsForms["DisableGray"] = setting["DisableGray"];
                        if (setting["AllowSZEChiNext"] != null && setting["AllowSZEChiNext"] != "")
                            SettingsForms["AllowSZEChiNext"] = setting["AllowSZEChiNext"];
                        if (setting["ShowBalanceDetails"] != null && setting["ShowBalanceDetails"] != "")
                            SettingsForms["ShowBalanceDetails"] = setting["ShowBalanceDetails"];
                        if (setting["SHGStockonhand"] != null && setting["SHGStockonhand"] != "")
                            SettingsForms["SHGStockonhand"] = setting["SHGStockonhand"];
                        if (setting["SZEStockonhand"] != null && setting["SZEStockonhand"] != "")
                            SettingsForms["SZEStockonhand"] = setting["SZEStockonhand"];
                        if (setting["CreditPassCancel"] != null && setting["CreditPassCancel"] != "")
                            SettingsForms["CreditPassCancel"] = setting["CreditPassCancel"];
                        if (setting["ShowDailyDetailRpt"] != null && setting["ShowDailyDetailRpt"] != "")
                            SettingsForms["ShowDailyDetailRpt"] = setting["ShowDailyDetailRpt"];
                        if (setting["ShowOddLotOrderBook"] != null && setting["ShowOddLotOrderBook"] != "")
                            SettingsForms["ShowOddLotOrderBook"] = setting["ShowOddLotOrderBook"];
                        if (setting["MaxGrayStockQuoteForm"] != null && setting["MaxGrayStockQuoteForm"] != "")
                            SettingsForms["MaxGrayStockQuoteForm"] = setting["MaxGrayStockQuoteForm"];
                        if (setting["DailyStatusReport"] != null && setting["DailyStatusReport"] != "")
                        {
                            SettingsForms["DailyStatusReport"] = setting["DailyStatusReport"];
                            if (SettingsForms["DailyStatusReport"] == null || SettingsForms["DailyStatusReport"].Trim() != "1")
                                statusReportToolStripMenuItem.Visible = false;
                        }
                        if (setting["AggOrderBtn"] != null && setting["AggOrderBtn"] != "")
                            SettingsForms["AggOrderBtn"] = setting["AggOrderBtn"];
                        if (setting["SkipOrderOTP"] != null && setting["SkipOrderOTP"] != "")
                            SettingsForms["SkipOrderOTP"] = setting["SkipOrderOTP"];

                        formCentralSettingsFound = true;
                    }
                    else if (KeyValue.Key == "UserPreference")
                    {
                        Settings setting = new Settings();
                        setting.PersistString = KeyValue.Value;
                        if (setting["UncheckCancelAlert"] != null && setting["UncheckCancelAlert"] != "")
                            SettingsUserPreference["UncheckCancelAlert"] = setting["UncheckCancelAlert"];
                        if (setting["UncheckRejectAlert"] != null && setting["UncheckRejectAlert"] != "")
                            SettingsUserPreference["UncheckRejectAlert"] = setting["UncheckRejectAlert"];
                        if (setting["ConfirmBeforeOrder"] != null && setting["ConfirmBeforeOrder"] != "")
                        {
                            SettingsUserPreference["ConfirmBeforeOrder"] = setting["ConfirmBeforeOrder"];
                            if (SettingsUserPreference["ConfirmBeforeOrder"] != null && SettingsUserPreference["ConfirmBeforeOrder"] == "0")
                                ConfirmBeforeOrderToolStripMenuItem.Checked = false;
                            ConfirmBeforeOrderToolStripMenuItem.Enabled = true;
                        }
                        if (setting["ConfirmBeforeQuickAmend"] != null && setting["ConfirmBeforeQuickAmend"] != "")
                        {
                            SettingsUserPreference["ConfirmBeforeQuickAmend"] = setting["ConfirmBeforeQuickAmend"];
                            if (SettingsUserPreference["ConfirmBeforeQuickAmend"] != null && SettingsUserPreference["ConfirmBeforeQuickAmend"] == "0")
                                ConfirmQuickAmendToolStripMenuItem.Checked = false;
                        }
                    }
                }

                if (SettingsForms["DisableHKG"] == "1" && SettingsForms["DisableSHG"] == "1" && SettingsForms["DisableSZE"] == "1" && SettingsForms["DisableGray"] == "1")
                {
                    orderTicketSimpleToolStripMenuItem.Visible = false;
                    orderTicketAdvanceToolStripMenuItem.Visible = false;
                }
                else
                {
                    orderTicketSimpleToolStripMenuItem.Visible = true;
                    orderTicketAdvanceToolStripMenuItem.Visible = true;
                }

                if (formCentralSettingsFound)
                {
                    SetStockQuoteMenu();
                    LoadLayout(null as string);
                }
            }
        }

        //private void ShowSoundChkBox()
        //{
        //    if (SettingsForms["ShowEventFormChkBox"] != null && SettingsForms["ShowEventFormChkBox"] == "true")
        //    {
        //        checkBoxSound.Visible = true;
        //        ReLocateLabelTradeStatus();
        //    }
        //    else
        //        checkBoxSound.Visible = false;
        //}

        private void SetStockQuoteMenu()
        {
            long stockTypeValue;
            long.TryParse(SettingsForms["StockQuoteType"], out stockTypeValue);
            AAStockQuoteToolStripMenuItem.Visible = (stockTypeValue & AAStockMask) > 0 ? true : false;
            stockQuoteToolStripMenuItem.Visible = TradeDB != null && TradeDB.UserType == UserTypeEnum.Quote || ((stockTypeValue & StreamingMask) > 0) || (stockTypeValue == 0 && TradeDB != null &&
                TradeDB.TradeState == (int)global::TradeDB.ProcessState.Ready && TradeDB.UserType == UserTypeEnum.AE) ? true : false;
            stockQuoteFullScreenToolStripMenuItem.Visible = TradeDB != null && TradeDB.UserType == UserTypeEnum.Quote || ((stockTypeValue & StreamingMask) > 0) || (stockTypeValue == 0 && TradeDB != null &&
                TradeDB.TradeState == (int)global::TradeDB.ProcessState.Ready && TradeDB.UserType == UserTypeEnum.AE) ? true : false;
            StockListStripMenuItem.Visible = ((stockTypeValue & StockListMask) > 0) || (stockTypeValue == 0 && TradeDB != null &&
                TradeDB.TradeState == (int)global::TradeDB.ProcessState.Ready && TradeDB.UserType == UserTypeEnum.AE) ? true : false;
            DailyDetailReportToolStripMenuItem.Visible = (SettingsForms["ShowDailyDetailRpt"] != null && SettingsForms["ShowDailyDetailRpt"] == "1");
            ConfirmBeforeOrderToolStripMenuItem.Checked = true;
            if (SettingsUserPreference["ConfirmBeforeOrder"] != null && SettingsUserPreference["ConfirmBeforeOrder"] == "0")
                ConfirmBeforeOrderToolStripMenuItem.Checked = false;
            ConfirmQuickAmendToolStripMenuItem.Checked = true;
            if (SettingsUserPreference["ConfirmBeforeQuickAmend"] != null && SettingsUserPreference["ConfirmBeforeQuickAmend"] == "0")
                ConfirmQuickAmendToolStripMenuItem.Checked = false;
            if (SettingsForms["ShowOddLotOrderBook"] != null && SettingsForms["ShowOddLotOrderBook"] == "1")
                toolStripMenuItemOddLotOrderBook.Visible = true;
            else
                toolStripMenuItemOddLotOrderBook.Visible = false;
        }

        private void ReLocateLabelTradeStatus()
        {
            int minLeft = 400;

            int v = this.Width - labelTradeDBStatus.Width - 18;
            if (v < minLeft) v = minLeft;
            labelTradeDBStatus.Left = v;

            //checkBoxSound.Width = 63;
            v -= checkBoxSound.Width - 10;
            if (v < minLeft) v = minLeft;
            checkBoxSound.Left = v;

            int w = 470;
            v -= w - 10;
            if (v < minLeft)
            {
                w = Math.Max(0, w - (minLeft - v));
                v = minLeft;
            }
            panelTopIndexes.Width = w;
            panelTopIndexes.Left = v;

            /*
            if (labelValueIndexCEIChange.Visible)
            {
                v = v - labelValueIndexCEIChange.Width - 20;
                if (v < 0) v = 0;
                labelValueIndexCEIChange.Left = v;
            }
            if (labelValueIndexCEI.Visible)
            {
                v = v - labelValueIndexCEI.Width - 5;
                if (v < 0) v = 0;
                labelValueIndexCEI.Left = v;
            }
            if (labelIndexCEI.Visible)
            {
                v = v - labelIndexCEI.Width - 5;
                if (v < 0) v = 0;
                labelIndexCEI.Left = v;
            }
            if (labelValueIndexHSIChange.Visible)
            {
                v = v - labelValueIndexHSIChange.Width - 20;
                if (v < 0) v = 0;
                labelValueIndexHSIChange.Left = v;
            }
            if (labelValueIndexHSI.Visible)
            {
                v = v - labelValueIndexHSI.Width - 5;
                if (v < 0) v = 0;
                labelValueIndexHSI.Left = v;
            }
            if (labelIndexHSI.Visible)
            {
                v = v - labelIndexHSI.Width - 5;
                if (v < 0) v = 0;
                labelIndexHSI.Left = v;
            }
            if (labelValueMarketTurnover.Visible)
            {
                v = v - labelValueMarketTurnover.Width - 15;
                if (v < 0) v = 0;
                labelValueMarketTurnover.Left = v;
            }
            if (labelMarketTurnover.Visible)
            {
                v = v - labelMarketTurnover.Width - 5;
                if (v < 0) v = 0;
                labelMarketTurnover.Left = v;
            }*/
            /*
            if (labelValueASHRQuota.Visible)
            {
                v = v - labelValueASHRQuota.Width - 15;
                if (v < 0) v = 0;
                labelValueASHRQuota.Left = v;
            }
            if (labelASHRQuota.Visible)
            {
                v = v - labelASHRQuota.Width - 5;
                if (v < 0) v = 0;
                labelASHRQuota.Left = v;
            }*/
        }

        private IDockContent GetContentFromPersistString(string PersistString)
        {
            IDockContent dockContent = null;

            string[] pStrParts = PersistString.Split('|');
            string settingString = pStrParts.Length >= 2 ? pStrParts[1] : null;
            long stockTypeValue = 0;

            switch (pStrParts[0])
            {
                case "StockTerminal.Forms.FastOrderTicketForm":
                    if (TradeDB.UserType == UserTypeEnum.AE || TradeDB.UserType == UserTypeEnum.Client)
                        dockContent = new FastOrderTicketForm(this.Culture, settingString, this.dockPanelMain);
                    break;
                case "StockTerminal.Forms.OrderBookForm":
                    if (TradeDB.UserType == UserTypeEnum.AE || TradeDB.UserType == UserTypeEnum.Client)
                        dockContent = new OrderBookForm(this.Culture, settingString, this.dockPanelMain);
                    break;
                case "StockTerminal.Forms.OddLotOrderBookForm":
                    if (TradeDB.UserType == UserTypeEnum.AE || TradeDB.UserType == UserTypeEnum.Client)
                    {
                        if (SettingsForms["ShowOddLotOrderBook"] != null && SettingsForms["ShowOddLotOrderBook"] == "1")
                            dockContent = new OddLotOrderBookForm(this.Culture, settingString, this.dockPanelMain);
                    }
                    break;
                case "StockTerminal.Forms.GrayStockQuoteForm":
                    //if (SettingsForms["ShowOddLotOrderBook"] != null && SettingsForms["ShowOddLotOrderBook"] == "1")
                        dockContent = new GrayStockQuoteForm(this.Culture, settingString, this.dockPanelMain);
                    break;
                case "StockTerminal.Forms.AccountForm":
                    if (TradeDB.UserType == UserTypeEnum.AE || TradeDB.UserType == UserTypeEnum.Client)
                        dockContent = new AccountForm(this.Culture, settingString, this.dockPanelMain);
                    break;
                case "StockTerminal.Forms.QuoteBrowserForm":
                    long.TryParse(SettingsForms["StockQuoteType"], out stockTypeValue);
                    bool IsAAStockFormType = (stockTypeValue & AAStockMask) > 0 ? true : false;
                    if (IsAAStockFormType)
                        dockContent = new QuoteBrowserForm(this.Culture, settingString);
                    break;
                case "StockTerminal.Forms.SnapShotQuoteForm":
                    long.TryParse(SettingsForms["StockQuoteType"], out stockTypeValue);
                    bool IsSnapShotFormType = (stockTypeValue & SnapShotMask) > 0 ? true : false;
                    if (IsSnapShotFormType || (stockTypeValue == 0 && TradeDB.UserType == UserTypeEnum.Client))
                    {
                        if (GetInstanceCount(typeof(SnapShotQuoteForm)) < 1)
                            dockContent = new SnapShotQuoteForm(this.Culture, settingString);
                    }
                    break;
                case "StockTerminal.Forms.StockQuoteForm":
                    long.TryParse(SettingsForms["StockQuoteType"], out stockTypeValue);
                    bool IsStreamingFormType = (stockTypeValue & StreamingMask) > 0 ? true : false;
                    if (IsStreamingFormType || (stockTypeValue == 0 && (TradeDB.UserType == UserTypeEnum.AE || TradeDB.UserType == UserTypeEnum.Quote)))
                        dockContent = new StockQuoteForm(this.Culture, settingString, ExchangeTypeEnum.Unassigned);
                    break;
                case "StockTerminal.Forms.OrderTicketForm":
                    if (TradeDB.UserType == UserTypeEnum.AE || TradeDB.UserType == UserTypeEnum.Client)
                        dockContent = new OrderTicketForm(this.Culture, settingString, this.dockPanelMain, ExchangeTypeEnum.Unassigned, null);
                    break;
                case "StockTerminal.Forms.OrderTicketForm_simple":
                    if (TradeDB.UserType == UserTypeEnum.AE || TradeDB.UserType == UserTypeEnum.Client)
                        dockContent = new OrderTicketForm(this.Culture, settingString, this.dockPanelMain, ExchangeTypeEnum.Unassigned, null);
                    break;
                case "StockTerminal.Forms.EventForm":
                    if (TradeDB.UserType == UserTypeEnum.AE || TradeDB.UserType == UserTypeEnum.Client)
                        dockContent = new EventForm(this.Culture, settingString);
                    break;
                case "StockTerminal.Forms.ETradeWebForm":
                    if (TradeDB.UserType == UserTypeEnum.Client)
                        dockContent = new ETradeWebForm(this.Culture, settingString);
                    break;
                case "StockTerminal.Forms.AccountSettingsForm":
                    if (TradeDB.UserType == UserTypeEnum.Client)
                        dockContent = new AccountSettingsForm(this.Culture, settingString);
                    break;
                case "StockTerminal.Web.WriteDBForm":
                    //dockContent = new WebDBForm(this.Culture, settingString);
                    break;
                case "StockTerminal.Forms.StockListBrowserForm":
                    long.TryParse(SettingsForms["StockQuoteType"], out stockTypeValue);
                    if ((stockTypeValue & StockListMask) > 0 || (stockTypeValue == 0 && TradeDB.UserType == UserTypeEnum.AE))
                        dockContent = new StockListBrowserForm(this.Culture, settingString);
                    break;
                case "StockTerminal.Forms.QuoteFormFullScreen":
                    long.TryParse(SettingsForms["StockQuoteType"], out stockTypeValue);
                    IsStreamingFormType = (stockTypeValue & StreamingMask) > 0 ? true : false;
                    if (IsStreamingFormType || (stockTypeValue == 0 && (TradeDB.UserType == UserTypeEnum.AE || TradeDB.UserType == UserTypeEnum.Quote)))
                        dockContent = new QuoteFormFullScreen(this.Culture, settingString);
                    break;
            }
            return dockContent;
        }

        private void ShowLoginForm(string Message)
        {
            LoginForm loginForm = new LoginForm(this.Culture, null);
            loginForm.DockHandler.CloseButton = false;
            loginForm.FormClosed += new FormClosedEventHandler(LoginForm_FormClosed);
            loginForm.Show(dockPanelMain, DockState.Document);
            loginForm.ShowMessage(Message);
        }

        private void ShowReconnectForm()
        {
            if (reconnectForm == null)
            {
                reconnectForm = new ReconnectForm(this.Culture, null);
                reconnectForm.StartPosition = FormStartPosition.CenterParent;
                if (AppWindowState == FormWindowState.Minimized) AppWindowState = FormWindowState.Normal;
                reconnectForm.Show(this);
                reconnectForm.Location = new Point(
                    this.Location.X + ((this.Width - reconnectForm.Width) >> 1),
                    this.Location.Y + ((this.Height - reconnectForm.Height) >> 1));
            }
        }

        private void HideReconnectForm()
        {
            if (reconnectForm != null)
            {
                reconnectForm.Close();
                reconnectForm = null;
            }
        }

        public void RequestToLogout()
        {
            AppState = ApplicationState.Logout;
            Logout();
        }

        private void Logout()
        {
            timerLastUserAction.Enabled = false;

            SyncTradeSubscriptionStop();
            SyncQuoteSubscriptionStop();

            this.Enabled = false;
            LockMenu(true);
            SetIndexesVisible(false);

            HideReconnectForm();

            if (LayoutLoaded)  // make sure the layout is loaded
            {
                SaveLayout(null);
                LayoutLoaded = false;
            }
            if (TradeDB != null) TradeDB.Disconnect();

            timerLogout.Interval = 500;
            timerLogout.Enabled = true;
        }

        private void CloseAllWindows(DockPanel dockPanel)
        {
            dockPanel.SuspendLayout(true);

            if (dockPanel.DocumentStyle == DocumentStyle.SystemMdi)
            {
                foreach (Form form in MdiChildren)
                    form.Close();
            }
            else
            {
                for (int index = dockPanel.Contents.Count - 1; index >= 0; index--)
                {
                    if (dockPanel.Contents[index] is IDockContent)
                    {
                        IDockContent content = (IDockContent)dockPanel.Contents[index];
                        ((BaseForm)content.DockHandler.Form).IsProgramClosing = true;
                        content.DockHandler.Close();
                    }
                }
            }

            dockPanel.ResumeLayout();
        }

        private void SetIndexesVisible(bool Visible)
        {
            labelIndexHSI.Visible = Visible;
            labelValueIndexHSI.Visible = Visible;
            labelValueIndexHSIChange.Visible = Visible;

            labelIndexCEI.Visible = Visible;
            labelValueIndexCEI.Visible = Visible;
            labelValueIndexCEIChange.Visible = Visible;

            labelValueMarketTurnover.Visible = Visible;
            labelMarketTurnover.Visible = Visible;

            //labelValueASHRQuota.Visible = Visible;
            //labelASHRQuota.Visible = Visible;

            ReLocateLabelTradeStatus();
        }

        /// <summary>
        /// Set the menu items properly for different user type
        /// </summary>
        private void SetMenu()
        {
            SetStockQuoteMenu();
            logoutToolStripMenuItem.Visible = (TradeDB != null &&
                (TradeDB.UserType == UserTypeEnum.AE || TradeDB.UserType == UserTypeEnum.Client) &&
                Program.QuoteOnly == "" || Program.QuoteOnly == "2");
            tradeToolStripMenuItem.Visible = (TradeDB != null &&
                (TradeDB.UserType == UserTypeEnum.AE || TradeDB.UserType == UserTypeEnum.Client) &&
                TradeDB.TradeState == (int)global::TradeDB.ProcessState.Ready);
            reportToolStripMenuItem.Visible = (TradeDB != null &&
                TradeDB.TradeState == (int)global::TradeDB.ProcessState.Ready && 
                TradeDB.UserType == UserTypeEnum.AE ? true : false);
            StatementToolStripMenuItem2.Visible = (TradeDB != null &&
                TradeDB.TradeState == (int)global::TradeDB.ProcessState.Ready &&
                TradeDB.UserType == UserTypeEnum.Client ? true : false);
            SettlementToolStripMenuItem2.Visible = (TradeDB != null &&
                TradeDB.TradeState == (int)global::TradeDB.ProcessState.Ready &&
                TradeDB.UserType == UserTypeEnum.Client ? true : false);
            toolStripMenuItemAccSettings.Visible = (TradeDB != null &&
                TradeDB.TradeState == (int)global::TradeDB.ProcessState.Ready &&
                TradeDB.UserType == UserTypeEnum.Client ? true : false);
            toolStripSeparator9.Visible = (TradeDB != null &&
                TradeDB.TradeState == (int)global::TradeDB.ProcessState.Ready &&
                TradeDB.UserType == UserTypeEnum.Client ? true : false);
            webFormToolStripMenuItem.Visible = false;
            //webFormToolStripMenuItem.Visible = (TradeDB != null &&
            //    TradeDB.TradeState == (int)global::TradeDB.ProcessState.Ready &&
            //    SettingsWeb["DBHost"] != null && SettingsWeb["DBHost"] != "");
            //checkBoxSound.Visible = false;
            //checkBoxSound.Visible = (TradeDB != null &&
            //    TradeDB.State == (int)global::TradeDB.ProcessState.Ready &&
            //    TradeDB.UserType == UserTypeEnum.AE ? true : false);

            if (TradeDB != null)
            {
                if ((TradeDB.UserType == UserTypeEnum.AE || TradeDB.UserType == UserTypeEnum.Client))
                {
                    quoteLayoutToolStripMenuItem.Visible = false;
                }
                else if (TradeDB.UserType == UserTypeEnum.Quote)
                {
                    AutoFillAccNoToolStripMenuItem.Visible = false;
                    KeepAccNoAfterPlaceOrderToolStripMenuItem.Visible = false;
                    SpecialTypeToolStripMenuItem.Visible = false;
                    ConfirmBeforeOrderToolStripMenuItem.Visible = false;
                    ConfirmQuickAmendToolStripMenuItem.Visible = false;
                    standardLayoutToolStripMenuItem.Visible = false;
                    advancedLayoutToolStripMenuItem.Visible = false;
                    compactLayoutToolStripMenuItem.Visible = false;
                    quoteLayoutToolStripMenuItem.Visible = true;
                }
            }
        }

        private void LockMenu(bool DoLock)
        {
            if (DoLock)
            {
                for (int i = 0; i < menuStrip.Items.Count; i++)
                {
                    menuStrip.Items[i].Enabled = false;
                }
                //foreach (ToolStripMenuItem m in menuStrip.Items)
                //{
                //    m.Enabled = false;
                //}
                //toolStrip1.Visible = false;
            }
            else
            {
                for (int i = 0; i < menuStrip.Items.Count; i++)
                {
                    menuStrip.Items[i].Enabled = true;
                }
                //foreach (ToolStripMenuItem m in menuStrip.Items)
                //{
                //    m.Enabled = true;
                //}
                if (TradeDB.UserType == UserTypeEnum.AE)
                {
                    PrintReportToolStripButton.Visible = true;
                    //toolStrip1.Visible = true;
                    //foreach (ToolStripItem m in toolStrip1.Items)
                    //{
                    //    m.Enabled = true;
                    //}
                }
                else
                    PrintReportToolStripButton.Visible = false;
            }
        }

        private void LockLayout(bool DoLock)
        {
            if (DoLock)
            {
                lockLayoutToolStripMenuItem.Checked = true;
                LayoutLocked = true;
                dockPanelMain.AllowEndUserDocking = false;
            }
            else
            {
                lockLayoutToolStripMenuItem.Checked = false;
                LayoutLocked = false;
                dockPanelMain.AllowEndUserDocking = true;
            }
        }

        /// <summary>
        /// Load layout from specified layout file.
        /// </summary>
        /// <param name="XMLLayoutFilePath">File path of the layout file. Assume the auto save file if null.</param>
        private void LoadLayout(object LayoutObject)
        {
            LockMenu(true); // prevent user opening new dock window
            SetIndexesVisible(false);

            if (LayoutObject != null)
            {
                timerLoadLayout.Tag = LayoutSourceType.Specific;
                LayoutDict[LayoutSourceType.Specific] = LayoutObject;
            }
            else
            {
                timerLoadLayout.Tag = LayoutSourceType.AutoSave;
                if (TradeDB != null)
                    LayoutDict[LayoutSourceType.AutoSave] = LayoutAutoSaveFilePath[(int)TradeDB.UserType];
                else
                    LayoutDict[LayoutSourceType.AutoSave] = null;
            }

            timerLoadLayout.Interval = 200;
            timerLoadLayout.Enabled = true;
        }

        /// <summary>
        /// Save layout to specified layout file.
        /// </summary>
        /// <param name="XMLLayoutFilePath">File path of the layout file. Assume the auto save file if null.</param>
        private void SaveLayout(string XMLLayoutFilePath)
        {
            string filePath = XMLLayoutFilePath != null ? XMLLayoutFilePath : LayoutAutoSaveFilePath[(int)TradeDB.UserType];
            string folderPath = null;

            try
            {
                // DockPanel is not call GetPersistentString () in backForm, thus (use ugly way), borrow eventForm save and load settings
                List<BaseForm> eventFormList = GetFormByFormType(typeof(EventForm));
                if (eventFormList != null && eventFormList.Count > 0 && eventFormList[0] != null)
                    ((EventForm)eventFormList[0]).SaveMainFormSetting(this.Location.X, this.Location.Y, this.Width, this.Height, this.WindowState, checkBoxSound.Checked,
                        AutoFillAccNoToolStripMenuItem.Checked, KeepAccNoAfterPlaceOrderToolStripMenuItem.Checked);

                folderPath = Path.GetDirectoryName(filePath);

                if (Directory.Exists(folderPath) == false)
                {
                    Directory.CreateDirectory(folderPath);
                }

                dockPanelMain.SaveAsXml(filePath);
            }
            catch (Exception e)
            {
                AppendLog("Layout", "Failed to save layout \"" + XMLLayoutFilePath + "\" - " + e.Message, "Error", XMLLayoutFilePath != null);
            }
        }

        /// <summary>
        /// Check to see if the application is ready for close. It checks for the followings: 
        ///     - TradeDB
        /// </summary>
        /// <returns></returns>
        private bool IsReadyForLogout()
        {
            if (TradeDB == null || TradeDB.TradeState == (int)ProcessState.Stopped)
            {
                return true;
            }
            return false;
        }

        private void StatementToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadETradeWebPage("InternetStockStatement");
        }

        private void SettlementToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadETradeWebPage("InternetStockSettlement");
        }

        private void LoadETradeWebPage(string redirect)
        {
            string ETradeWebFormURL = "https://www.pru.hk";

            if (SettingsForms["ETradeWebURL"] != null && SettingsForms["ETradeWebURL"].Trim() != "")
                ETradeWebFormURL = SettingsForms["ETradeWebURL"];

            switch (redirect)
            {
                case "InternetStockStatement":
                    System.Diagnostics.Process.Start(ETradeWebFormURL + "/ETrade/Secured/Session/Login.asp?Redirect=InternetStockStatement");
                    break;

                case "InternetStockSettlement":
                    System.Diagnostics.Process.Start(ETradeWebFormURL + "/ETrade/Secured/Session/Login.asp?Redirect=InternetStockSettlement");
                    break;
            }
        }

        private void HSIImpacttoolStripMenuItem_Click(object sender, EventArgs e)
        {
            string destWeb = "https://www.pru.hk/Indexes/Default.asp?IndexCode=HSI&Referer=PruWebHome&Type=2";
            System.Diagnostics.Process.Start(@destWeb);
        }

        private void CEItoolStripMenuItem_Click(object sender, EventArgs e)
        {
            string destWeb = "https://www.pru.hk/Indexes/Default.asp?IndexCode=CEI&Referer=PruWebHome&Type=2";
            System.Diagnostics.Process.Start(@destWeb);
        }

        private void checkBoxSound_CheckedChanged(object sender, EventArgs e)
        {
            base.TurnSoundOn(checkBoxSound.Checked);
        }

        private void timerServerTime_Tick(object sender, EventArgs e)
        {
            timerServerTime.Enabled = false;

            DateTime nowTime;

            if (TradeDB != null && TradeDB.ServerTime.CompareTo(DateTime.MinValue) > 0)
            {
                nowTime = TradeDB.ServerTime;

                string time = null;
                time = nowTime.ToString("hh:mm:ss");
                if (TradeDB.ServerTime.Hour > 11)
                    time = time + " " + GetResxString("PM");
                else
                    time = time + " " + GetResxString("AM");
                this.Text = GetTitleText() + "  -  " + time;
            }
            else
                nowTime = DateTime.Now;

            timerServerTime.Interval = 1000 - nowTime.Millisecond;
            timerServerTime.Enabled = true;
        }

        private void toolStripMenuItemAccSettings_Click(object sender, EventArgs e)
        {
            if (GetInstanceCount(typeof(AccountSettingsForm)) < 1)
            {
                AccountSettingsForm form = new AccountSettingsForm(this.Culture, null);
                form.Settings = "SETTINGS";
                form.Show(dockPanelMain, DockState.Float, new Rectangle(50, 80, form.Width, form.Height));
            }
            else
            {
                List<BaseForm> ETWebFormList = GetFormByFormType(typeof(AccountSettingsForm));
                if (ETWebFormList != null && ETWebFormList.Count > 0 && ETWebFormList[0] != null)
                    ((AccountSettingsForm)ETWebFormList[0]).Settings = "SETTINGS";
            }
        }

        private void webFormToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (SettingsWeb["DBHost"] != null && SettingsWeb["DBHost"] != "" && GetInstanceCount(typeof(WebDBForm)) < 1)
            {
                WebDBForm form = new WebDBForm(this.Culture, null);
                form.Show(dockPanelMain, DockState.Float, new Rectangle(200, 200, form.Width, form.Height));
            }
        }

        private void SetAutoFillAccNo(bool enable)
        {
            AutoFillAccNoToolStripMenuItem.Checked = enable;
            AutoFillAccNo = enable;
        }

        private void SetKeepAccNoAfterPlaceOrder(bool enable)
        {
            KeepAccNoAfterPlaceOrderToolStripMenuItem.Checked = enable;
            KeepAccNoAfterPlaceOrder = enable;
        }

        private void SetConfirmBeforeOrder(bool itemChecked)
        {
            ConfirmBeforeOrderToolStripMenuItem.Checked = itemChecked;
            SettingsUserPreference["ConfirmBeforeOrder"] = itemChecked ? "1" : "0";
            SetCentralUserSetting("UserPreference", SettingsUserPreference.PersistString);
        }

        private void SetConfirmBeforeQuickAmend(bool itemChecked)
        {
            ConfirmQuickAmendToolStripMenuItem.Checked = itemChecked;
            SettingsUserPreference["ConfirmBeforeQuickAmend"] = itemChecked ? "1" : "0";
            SetCentralUserSetting("UserPreference", SettingsUserPreference.PersistString);
        }

        private void AutoFillAccNoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetAutoFillAccNo(!(sender as ToolStripMenuItem).Checked);
        }

        private void clearAccNoAfterPlaceOrderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetKeepAccNoAfterPlaceOrder(!(sender as ToolStripMenuItem).Checked);
        }

        private void SpecialTypeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetSpecialType(!(sender as ToolStripMenuItem).Checked);
        }

        private void SetSpecialType(bool enable)
        {
            SpecialTypeToolStripMenuItem.Checked = enable;
            booVisibleSpecialType = enable;

            //List<BaseForm> OTS_FormList = BaseForm.GetFormByFormType(typeof(OrderTicketForm_simple));
            List<BaseForm> OT_FormList = BaseForm.GetFormByFormType(typeof(OrderTicketForm));
            List<BaseForm> FastOT_FormList = BaseForm.GetFormByFormType(typeof(FastOrderTicketForm));
            List<BaseForm> StockQuote_FormList = BaseForm.GetFormByFormType(typeof(StockQuoteForm));

            //if (OTS_FormList != null)
            //{
            //    foreach (BaseForm baseForm in OTS_FormList)
            //    {
            //        if (Utils.Utils.IsTargetForm("OrderTicketForm_simple", baseForm.InstanceId))
            //        {
            //            OrderTicketForm_simple OTFrm = BaseForm.GetFormByInstanceId(baseForm.InstanceId) as OrderTicketForm_simple;
            //            if (OTFrm != null) OTFrm.SetPassword(checkBoxTradePwd.Checked);
            //        }
            //    }
            //}
            if (OT_FormList != null)
            {
                foreach (BaseForm baseForm in OT_FormList)
                {
                    if (Utils.Utils.IsTargetForm("OrderTicketForm", baseForm.InstanceId))
                    {
                        OrderTicketForm OTFrm = BaseForm.GetFormByInstanceId(baseForm.InstanceId) as OrderTicketForm;
                        if (OTFrm != null) OTFrm.SetVisibleSpecialType(enable);
                    }
                }
            }
            if (FastOT_FormList != null)
            {
                foreach (BaseForm baseForm in FastOT_FormList)
                {
                    if (Utils.Utils.IsTargetForm("FastOrderTicketForm", baseForm.InstanceId))
                    {
                        FastOrderTicketForm OTFrm = BaseForm.GetFormByInstanceId(baseForm.InstanceId) as FastOrderTicketForm;
                        if (OTFrm != null) OTFrm.SetVisibleSpecialType(enable);
                    }
                }
            }
            if (StockQuote_FormList != null)
            {
                foreach (BaseForm baseForm in StockQuote_FormList)
                {
                    if (Utils.Utils.IsTargetForm("StockQuoteForm", baseForm.InstanceId))
                    {
                        StockQuoteForm OTFrm = BaseForm.GetFormByInstanceId(baseForm.InstanceId) as StockQuoteForm;
                        if (OTFrm != null) OTFrm.SetVisibleSpecialType(enable);
                    }
                }
            }

        }

        private void StockListStripMenuItem_Click(object sender, EventArgs e)
        {
            if (GetInstanceCount(typeof(StockListBrowserForm)) < 1)
            {
                StockListBrowserForm form = new StockListBrowserForm(this.Culture, null);
                form.Show(dockPanelMain, DockState.Float, new Rectangle(200, 200, form.Width, form.Height));
            }
        }

        private void DailyDetailReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Report.SelectAEListForm form = new StockTerminal.Report.SelectAEListForm(this.Culture, null, dockPanelMain);
            form.IsDetailReport = true;
            form.Show(dockPanelMain, DockState.Float, new Rectangle(200, 200, form.Width, form.Height));
        }

        private void disableConfirmMessageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetConfirmBeforeOrder(!(sender as ToolStripMenuItem).Checked);
        }

        private void disableConfirmQuickAmendToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetConfirmBeforeQuickAmend(!(sender as ToolStripMenuItem).Checked);
        }

        private void toolStripMenuItemOddLotOrderBook_Click(object sender, EventArgs e)
        {
            OddLotOrderBookForm form = new OddLotOrderBookForm(this.Culture, null, this.dockPanelMain);
            form.Show(dockPanelMain, DockState.Float, new Rectangle(400, 600, form.Width, form.Height));
        }

        private void GrayStockQuoteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string MaxGrayStockQuoteForm = SettingsForms["MaxGrayStockQuoteForm"];
            int maxCount = 0;

            if (!int.TryParse(MaxGrayStockQuoteForm, out maxCount))
                maxCount = 2;
            else if (maxCount < 0)
                maxCount = 0;
            else if (maxCount > 10)
                maxCount = 10;

            if (GetInstanceCount(typeof(GrayStockQuoteForm)) < maxCount)
            {
                GrayStockQuoteForm form = new GrayStockQuoteForm(this.Culture, null, this.dockPanelMain);
                form.Show(dockPanelMain, DockState.Float, new Rectangle(333, 293, form.Width, form.Height));
            }
        }

        private void stockQuoteFullScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int maxCount = 1;

            if (GetInstanceCount(typeof(QuoteFormFullScreen)) < maxCount)
            {
                QuoteFormFullScreen form = new QuoteFormFullScreen(this.Culture, null);
                form.WindowState = FormWindowState.Maximized;
                form.Show(dockPanelMain, DockState.Float, new Rectangle(1000, 750, form.Width, form.Height));
            }
        }

        private void statusReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Report.SelectOrderStatusForm form = new StockTerminal.Report.SelectOrderStatusForm(this.Culture, null, dockPanelMain);
            form.IsDetailReport = true;
            form.Show(dockPanelMain, DockState.Float, new Rectangle(200, 200, form.Width, form.Height));
        }

        private void GrayStockQuoteFastToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string MaxGrayStockQuoteForm = SettingsForms["MaxGrayStockQuoteForm"];
            int maxCount = 0;

            if (!int.TryParse(MaxGrayStockQuoteForm, out maxCount))
                maxCount = 2;
            else if (maxCount < 0)
                maxCount = 0;
            else if (maxCount > 10)
                maxCount = 10;

            if (GetInstanceCount(typeof(GrayStockQuoteForm)) < maxCount)
            {
                StockQuoteForm form = new StockQuoteForm(this.Culture, null, ExchangeTypeEnum.Gray);
                form.Show(dockPanelMain, DockState.Float, new Rectangle(333, 293, form.Width, form.Height));
            }
        }
    }
}
