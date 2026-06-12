using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using WeifenLuo.WinFormsUI.Docking;
using TradeDB;
using Processors;
using Utils;
using Logger;
using StockTerminal.Utils;

namespace StockTerminal.Forms
{
    public partial class BaseForm : DockContent, IMessageFilter
    {
        protected enum ApplicationState { Run, Logout, Exit };
        protected static ApplicationState AppState = ApplicationState.Logout;

        private static BaseFormRegistry FormRegistry = new BaseFormRegistry();
        private static BaseForm MainBaseForm = null;

        protected static bool LayoutLocked = true;
        protected bool LayoutLockable = false; // true;
        
        private bool Loaded = false;

        private ResourceManager ResManUI;
        private ResourceManager ResManGeneralUI;

        public readonly string InstanceId;
        private string pDocumentName = null;
        private string pDocumentNameEn = null;

        public string DocumentName
        {
            get { return pDocumentName; }
        }

        private string pDeploymentGroupName = null;
        protected string DeploymentGroupName
        {
            get {
                if (pDeploymentGroupName != null)
                    return pDeploymentGroupName;
                else
                {
                    string deploymentGroupName = "";
                    if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
                    {
                        string S = "" + System.Deployment.Application.ApplicationDeployment.CurrentDeployment.UpdateLocation;
                        if (S != null && S.Trim().Length > 0)
                        {
                            if (S.IndexOf("pru.hk") >= 0 || S.IndexOf("pru.com.hk") >= 0)
                            {
                                deploymentGroupName = "Internet";
                            }
                            else
                            {
                                string srchString = "Publications";
                                int startIndex = S.IndexOf(srchString);
                                if (startIndex >= 0)
                                {
                                    startIndex += srchString.Length + 1;
                                    int endIndex = S.IndexOf("/", startIndex);
                                    if (endIndex >= 0)
                                    {
                                        deploymentGroupName = S.Substring(startIndex, endIndex - startIndex);
                                    }
                                }
                            }
                        }
                    }
                    pDeploymentGroupName = deploymentGroupName;
                    return deploymentGroupName;
                }
            }
        }

        private Settings pLocalFormSettings = new Settings();

        protected Settings LocalFormSettings
        {
            get { return pLocalFormSettings; }
        }

        protected static readonly Settings SettingsTradeDB = new Settings("LogMessage328", "LoginHost01", "LoginPort01", "LoginHost02", "LoginPort02", "LoginHost03", "LoginPort03", "LoginHost04", "LoginPort04", "LoginHost05", "LoginPort05", "StockUdpIP", "StockUdpPort", "StockUdpEnable", "ByPassCertValidation", "AllowOldSecurityProtocol", "Language");
        protected static readonly Settings SettingsForms = new Settings("MaxCountStockQuoteForm", "ETradeWebURL", "AAStockQuoteWebURL", "StockQuoteType", "AutoLogoutWarnTime", "AutoLogoutAfterWarnTime", "ShowCancelAlertChkBox", "ShowRejectAlertChkBox", "AccSettingsWebURL", "ShowPrintDealChkBox", "QuoteAAStockLoginID", "QuoteAAStockLoginPassword", "AllowOrderChangeAccount", "ShowDailyDetailRpt", "ShowOddLotOrderBook");
        protected static readonly Settings SettingsUserPreference = new Settings("ConfirmBeforeOrder", "ConfirmBeforeQuickAmend", "UncheckCancelAlert", "UncheckRejectAlert");
        protected static readonly Settings SettingsWeb = new Settings("DBHost", "DBName", "DBUserID", "DBPassword");
        
        public const string INIFile = @".\StockTerminal.ini";

        private delegate void CultureChangeDelegate(CultureInfo Culture);
        private CultureChangeDelegate pCultureChangeDelegate, pCultureChangeDelegateInvoke;
        private CultureInfo pCulture = new CultureInfo("en-US");

        public delegate void FormListChangeDelegate();
        private static object FormListChangeDelegateMutex = new object();
        private static Dictionary<BaseForm, FormListChangeDelegate> FormListChangeDelegateDict = new Dictionary<BaseForm, FormListChangeDelegate>(10);

        private static object SystemEventDelegateMutex = new object();
        private static Dictionary<BaseForm, SystemEventDelegate> SystemEventDelegateDict = new Dictionary<BaseForm, SystemEventDelegate>(10);

        private static object ConnectionStatusDelegateMutex = new object();
        private static Dictionary<BaseForm, StateChangedDelegate> ConnectionStatusDelegateDict = new Dictionary<BaseForm, StateChangedDelegate>(10);

        private static object QuoteConnectionStatusDelegateMutex = new object();
        private static Dictionary<BaseForm, StateChangedDelegate> QuoteConnectionStatusDelegateDict = new Dictionary<BaseForm, StateChangedDelegate>(10);

        private static DateTime AccountListLastRequestTime = DateTime.MinValue;
        private static object AccountListDelegateMutex = new object();
        private static Dictionary<BaseForm, AccountListDelegate> AccountListDelegateDict = new Dictionary<BaseForm, AccountListDelegate>(10);

        private static object AEDelegateMutex = new object();
        private static Dictionary<BaseForm, AEDelegate> AEDelegateDict = new Dictionary<BaseForm, AEDelegate>(10);

        //private static object OrderStatusDelegateMutex = new object();
        //private static Dictionary<BaseForm, OrderStatusDelegate> OrderStatusDelegateDict = new Dictionary<BaseForm,OrderStatusDelegate>(10);
        //private static SubscriptionManager<string, SettingKeyValue> CentralUserSettingSubscription = new SubscriptionManager<string, SettingKeyValue>("CentralUserSetting");
        //private static SubscriptionManager<string, Order> OrderSubscription = new SubscriptionManager<string, Order>("Order");
        //private static SubscriptionManager<string, OddLotOrder> OddLotSubscription = new SubscriptionManager<string, OddLotOrder>("OddLotOrder");
        //private static SubscriptionManager<string, Account> AccountSubscription = new SubscriptionManager<string, Account>("Account");
        //private static SubscriptionManager<string, Stock> StockSubscription = new SubscriptionManager<string, Stock>("Stock");
        //private static SubscriptionManager<string, Index> IndexSubscription = new SubscriptionManager<string, Index>("Index");
        //private static SubscriptionManager<string, TransactionCharge> TransactionChargeSubscription = new SubscriptionManager<string, TransactionCharge>("TransactionCharge");
        
        /*
        protected static bool StopExtraSubscriptionStock
        {
            get { return StockSubscription.StopExtraPublication; }
            set { StockSubscription.StopExtraPublication = value; }
        }
         */

        private static readonly DataProcessor<SettingKeyValue> DPCentralUserSetting = new DataProcessor<SettingKeyValue>();
        private static readonly DataProcessor<Order> DPOrder = new DataProcessor<Order>();
        private static readonly DataProcessor<Account> DPAccount = new DataProcessor<Account>();
        private static readonly DataProcessor<Stock> DPStock = new DataProcessor<Stock>();
        private static readonly DataProcessor<Index> DPIndex = new DataProcessor<Index>();
        private static readonly DataProcessor<MarketTurnover> DPMarketTurnover = new DataProcessor<MarketTurnover>();
        private static readonly DataProcessor<TransactionCharge> DPTxnCharge = new DataProcessor<TransactionCharge>();
        private static readonly DataProcessor<OddLotOrder> DPOddLotOrder = new DataProcessor<OddLotOrder>();

        private static FormListChangeDelegate formListChangeDelegateStatic;
        private static SystemEventDelegate systemEventDelegateStatic;
        private static StateChangedDelegate connectionStatusDelegateStatic;
        private static StateChangedDelegate quoteConnectionStatusDelegateStatic;
        //private static CentralUserSettingDelegate centralUserSettingDelegateStatic;
        //private static OrderStatusDelegate orderStatusDelegateStatic;
        //private static OddLotOrderDelegate oddLotOrderDelegateStatic;
        private static AccountListDelegate accountListDelegateStatic;
        //private static AccountDelegate accountDelegateStatic;
        private static AEDelegate aeDelegateStatic;
        //private static StockDelegate stockDelegateStatic;
        //private static IndexDelegate indexDelegateStatic;
        //private static TransactionChargeDelegate transactionChargeDelegateStatic;

        private FormListChangeDelegate formListChangeDelegate;
        private SystemEventDelegate systemEventDelegateInvoke, systemEventDelegate;
        private StateChangedDelegate connectionStatusDelegateInvoke, connectionStatusDelegate;
        private StateChangedDelegate quoteConnectionStatusDelegateInvoke, quoteConnectionStatusDelegate;
        //private SubscriptionManager<string, SettingKeyValue>.DispatchDelegate centralUserSettingDelegateInvoke, centralUserSettingDelegate;
        private DataProcessor<SettingKeyValue>.DispatchDelegate centralUserSettingDelegateInvoke, centralUserSettingDelegate;
        //private SubscriptionManager<string, Order>.DispatchDelegate orderStatusDelegateInvoke, orderStatusDelegate;
        private DataProcessor<Order>.DispatchDelegate orderStatusDelegateInvoke, orderStatusDelegate;
        //private SubscriptionManager<string, OddLotOrder>.DispatchDelegate oddLotOrderDelegateInvoke, oddLotOrderDelegate;
        private DataProcessor<OddLotOrder>.DispatchDelegate oddLotOrderStatusDelegateInvoke, oddLotOrderStatusDelegate;
        private AccountListDelegate accountListDelegateInvoke, accountListDelegate;
        private AEDelegate aeDelegateInvoke, aeDelegate;
        //private SubscriptionManager<string, Account>.DispatchDelegate accountDelegateInvoke, accountDelegate;
        private DataProcessor<Account>.DispatchDelegate accountDelegateInvoke, accountDelegate;
        //private SubscriptionManager<string, Stock>.DispatchDelegate stockDelegateInvoke, stockDelegate;
        private DataProcessor<Stock>.DispatchDelegate stockDelegateInvoke, stockDelegate;
        //private SubscriptionManager<string, Index>.DispatchDelegate indexDelegateInvoke, indexDelegate;
        private DataProcessor<Index>.DispatchDelegate indexDelegateInvoke, indexDelegate;
        private DataProcessor<MarketTurnover>.DispatchDelegate marketTurnoverDelegateInvoke, marketTurnoverDelegate;
        //private SubscriptionManager<string, TransactionCharge>.DispatchDelegate transactionChargeDelegateInvoke, transactionChargeDelegate;
        private DataProcessor<TransactionCharge>.DispatchDelegate transactionChargeDelegateInvoke, transactionChargeDelegate;

        EventHandler checkedChangedHandler;
        ItemCheckEventHandler itemCheckHandler;
        MouseEventHandler mouseClickHandler;
        PreviewKeyDownEventHandler buttonPreviewKeyDownHandler;
        EventHandler selectedIndexChangedHandler;
        EventHandler textChangedHandler;
        EventHandler numericUpDownHandler;
        ScrollEventHandler scrollBarScrollHandler;
        DataGridViewCellMouseEventHandler dataGridViewCellMouseEventHandler;

        protected static readonly string LogFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\" + Application.ProductName + @"\Log\";
        public bool LogAction = true;
        private Log actionLog = null;
        private Log currentLog = null;
        private Dictionary<string, Log> logDict = new Dictionary<string, Log>(10);

        // for hot key handling
        private static bool VKCtrl = false;
        private static bool VKShift = false;
        private static bool VKAlt = false;
//        private static bool VKContext = false;   // not implemented because Keys enumeration doesn't support Context + Key combination

//        protected delegate void HotKeyDelegate(object sender, KeyEventArgs e);
        public event KeyEventHandler HotKeyUp;

        private static DateTime TimeLastUserAction = DateTime.Now;
        private static object TimeLastUserActionMutex = new object();

        private List<MessageBoxForm> MsgBoxes = new List<MessageBoxForm>(5);
        private object MsgBoxesMutex = new object();

        private static ITradeDB pTradeDB = null;

        public bool IsProgramClosing = false;

        private static bool pIsSoundOn = true;
        protected bool IsSoundOn
        {
            get { return pIsSoundOn; }
        }

        protected static bool AutoFillAccNo = true;
        protected static bool KeepAccNoAfterPlaceOrder = true;
        protected static bool booVisibleSpecialType = false;

        static BaseForm()
        {
            //CentralUserSettingSubscription.SubscriptionSnapShotDelegate = new SubscriptionManager<string, SettingKeyValue>.SubscriptionSyncDelegate(SubscriptionCentralUserSettingGet);
            //CentralUserSettingSubscription.SubscriptionStreamDelegate = new SubscriptionManager<string, SettingKeyValue>.SubscriptionSyncDelegate(SubscriptionCentralUserSettingRequest);
            //CentralUserSettingSubscription.SubscriptionStopDelegate = new SubscriptionManager<string, SettingKeyValue>.SubscriptionSyncDelegate(SubscriptionCentralUserSettingUnRequest);
            //OrderSubscription.SubscriptionSnapShotDelegate = new SubscriptionManager<string, Order>.SubscriptionSyncDelegate(SubscriptionOrderGet);
            //OrderSubscription.SubscriptionStreamDelegate = new SubscriptionManager<string, Order>.SubscriptionSyncDelegate(SubscriptionOrderRequest);
            //OrderSubscription.SubscriptionStopDelegate = new SubscriptionManager<string, Order>.SubscriptionSyncDelegate(SubscriptionOrderUnRequest);
            //OddLotSubscription.SubscriptionStreamDelegate = new SubscriptionManager<string, OddLotOrder>.SubscriptionSyncDelegate(SubscriptionOddLotOrderRequest);
            //OddLotSubscription.SubscriptionStopDelegate = new SubscriptionManager<string, OddLotOrder>.SubscriptionSyncDelegate(SubscriptionOddLotOrderUnRequest);
            //AccountSubscription.SubscriptionSnapShotDelegate = new SubscriptionManager<string, Account>.SubscriptionSyncDelegate(SubscriptionAccountGet);
            //AccountSubscription.SubscriptionStreamDelegate = new SubscriptionManager<string, Account>.SubscriptionSyncDelegate(SubscriptionAccountRequest);
            //AccountSubscription.SubscriptionStopDelegate = new SubscriptionManager<string, Account>.SubscriptionSyncDelegate(SubscriptionAccountUnRequest);
            //StockSubscription.SubscriptionSnapShotDelegate = new SubscriptionManager<string, Stock>.SubscriptionSyncDelegate(SubscriptionStockGet);
            //StockSubscription.SubscriptionStreamDelegate = new SubscriptionManager<string, Stock>.SubscriptionSyncDelegate(SubscriptionStockRequest);
            //StockSubscription.SubscriptionStopDelegate = new SubscriptionManager<string, Stock>.SubscriptionSyncDelegate(SubscriptionStockUnRequest);
            //IndexSubscription.SubscriptionSnapShotDelegate = new SubscriptionManager<string, Index>.SubscriptionSyncDelegate(SubscriptionIndexGet);
            //IndexSubscription.SubscriptionStreamDelegate = new SubscriptionManager<string, Index>.SubscriptionSyncDelegate(SubscriptionIndexRequest);
            //IndexSubscription.SubscriptionStopDelegate = new SubscriptionManager<string, Index>.SubscriptionSyncDelegate(SubscriptionIndexUnRequest);
            //TransactionChargeSubscription.SubscriptionSnapShotDelegate = new SubscriptionManager<string, TransactionCharge>.SubscriptionSyncDelegate(SubscriptionTransactionChargeGet);
            //TransactionChargeSubscription.SubscriptionStreamDelegate = new SubscriptionManager<string, TransactionCharge>.SubscriptionSyncDelegate(SubscriptionTransactionChargeRequest);
            //TransactionChargeSubscription.SubscriptionStopDelegate = new SubscriptionManager<string, TransactionCharge>.SubscriptionSyncDelegate(SubscriptionTransactionChargeUnRequest);

            formListChangeDelegateStatic = new FormListChangeDelegate(OnFormListChangeStatic);
            systemEventDelegateStatic = new SystemEventDelegate(OnSystemEventStatic);
            connectionStatusDelegateStatic = new StateChangedDelegate(OnConnectionStatusStatic);
            quoteConnectionStatusDelegateStatic = new StateChangedDelegate(OnQuoteConnectionStatusStatic);
            //centralUserSettingDelegateStatic = new CentralUserSettingDelegate(OnCentralUserSettingStatic);
            //orderStatusDelegateStatic = new OrderStatusDelegate(OnOrderStatusStatic);
            //oddLotOrderDelegateStatic = new OddLotOrderDelegate(OnOddLotOrderStatic);
            accountListDelegateStatic = new AccountListDelegate(OnAccountListStatic);
            //accountDelegateStatic = new AccountDelegate(OnAccountStatic);
            aeDelegateStatic = new AEDelegate(OnAEStatic);
            //stockDelegateStatic = new StockDelegate(OnStockStatic);
            //indexDelegateStatic = new IndexDelegate(OnIndexStatic);
            //transactionChargeDelegateStatic = new TransactionChargeDelegate(OnTransactionChargeStatic);

            FormRegistry.FormListChange = formListChangeDelegateStatic;
        }

        public BaseForm() : this(null, null)
        {
        }

        public BaseForm(CultureInfo Culture, string PersistString)
        {
            if (MainBaseForm == null) MainBaseForm = this;  // the first BaseForm is treated as the main form.

            InitializeComponent();

            DPCentralUserSetting.Name = "DataProcessorCentralUserSetting";
            DPOrder.Name = "DataProcessorOrder";
            DPAccount.Name = "DataProcessorAccount";
            DPStock.Name = "DataProcessorStock";
            DPIndex.Name = "DataProcessorIndex";
            DPTxnCharge.Name = "DataProcessorTxnCharge";
            DPOddLotOrder.Name = "DataProcessorOddLotOrder";

            pCultureChangeDelegateInvoke = new CultureChangeDelegate(OnCultureChangeInvoke);
            pCultureChangeDelegate = new CultureChangeDelegate(OnCultureChange);

            formListChangeDelegate = new FormListChangeDelegate(OnFormListChange);

            systemEventDelegate = new SystemEventDelegate(OnSystemEvent);
            systemEventDelegateInvoke = new SystemEventDelegate(OnSystemEventInvoke);

            connectionStatusDelegate = new StateChangedDelegate(OnConnectionStatus);
            connectionStatusDelegateInvoke = new StateChangedDelegate(OnConnectionStatusInvoke);

            quoteConnectionStatusDelegate = new StateChangedDelegate(OnQuoteConnectionStatus);
            quoteConnectionStatusDelegateInvoke = new StateChangedDelegate(OnQuoteConnectionStatusInvoke);

            //centralUserSettingDelegate = new SubscriptionManager<string, SettingKeyValue>.DispatchDelegate(OnCentralUserSetting);
            //centralUserSettingDelegateInvoke = new SubscriptionManager<string, SettingKeyValue>.DispatchDelegate(OnCentralUserSettingInvoke);
            centralUserSettingDelegateInvoke = new DataProcessor<SettingKeyValue>.DispatchDelegate(OnCentralUserSettingInvoke);
            centralUserSettingDelegate = new DataProcessor<SettingKeyValue>.DispatchDelegate(OnCentralUserSetting);
            
            //orderStatusDelegateInvoke = new SubscriptionManager<string, Order>.DispatchDelegate(OnOrderStatusInvoke);
            //orderStatusDelegate = new SubscriptionManager<string, Order>.DispatchDelegate(OnOrderStatus);
            orderStatusDelegateInvoke = new DataProcessor<Order>.DispatchDelegate(OnOrderStatusInvoke);
            orderStatusDelegate = new DataProcessor<Order>.DispatchDelegate(OnOrderStatus);

            //oddLotOrderDelegateInvoke = new SubscriptionManager<string, OddLotOrder>.DispatchDelegate(OnOddLotOrderInvoke);
            //oddLotOrderDelegate = new SubscriptionManager<string, OddLotOrder>.DispatchDelegate(OnOddLotOrder);
            oddLotOrderStatusDelegateInvoke = new DataProcessor<OddLotOrder>.DispatchDelegate(OnOddLotOrderStatusInvoke);
            oddLotOrderStatusDelegate = new DataProcessor<OddLotOrder>.DispatchDelegate(OnOddLotOrderStatus);

            accountListDelegateInvoke = new AccountListDelegate(OnAccountListInvoke);
            accountListDelegate = new AccountListDelegate(OnAccountList);

            //accountDelegateInvoke = new SubscriptionManager<string, Account>.DispatchDelegate(OnAccountInvoke);
            //accountDelegate = new SubscriptionManager<string, Account>.DispatchDelegate(OnAccount);
            accountDelegateInvoke = new DataProcessor<Account>.DispatchDelegate(OnAccountInvoke);
            accountDelegate = new DataProcessor<Account>.DispatchDelegate(OnAccount);

            aeDelegateInvoke = new AEDelegate(OnAEInvoke);
            aeDelegate = new AEDelegate(OnAE);

            //stockDelegateInvoke = new SubscriptionManager<string, Stock>.DispatchDelegate(OnStockInvoke);
            //stockDelegate = new SubscriptionManager<string, Stock>.DispatchDelegate(OnStock);
            stockDelegateInvoke = new DataProcessor<Stock>.DispatchDelegate(OnStockInvoke);
            stockDelegate = new DataProcessor<Stock>.DispatchDelegate(OnStock);

            //indexDelegateInvoke = new SubscriptionManager<string, Index>.DispatchDelegate(OnIndexInvoke);
            //indexDelegate = new SubscriptionManager<string, Index>.DispatchDelegate(OnIndex);
            indexDelegateInvoke = new DataProcessor<Index>.DispatchDelegate(OnIndexInvoke);
            indexDelegate = new DataProcessor<Index>.DispatchDelegate(OnIndex);

            marketTurnoverDelegateInvoke = new DataProcessor<MarketTurnover>.DispatchDelegate(OnMarketTurnoverInvoke);
            marketTurnoverDelegate = new DataProcessor<MarketTurnover>.DispatchDelegate(OnMarketTurnover);

            //transactionChargeDelegateInvoke = new SubscriptionManager<string, TransactionCharge>.DispatchDelegate(OnTransactionChargeInvoke);
            //transactionChargeDelegate = new SubscriptionManager<string, TransactionCharge>.DispatchDelegate(OnTransactionCharge);
            transactionChargeDelegateInvoke = new DataProcessor<TransactionCharge>.DispatchDelegate(OnTransactionChargeInvoke);
            transactionChargeDelegate = new DataProcessor<TransactionCharge>.DispatchDelegate(OnTransactionCharge);

            checkedChangedHandler = new EventHandler(OnCheckedChanged);
            itemCheckHandler = new ItemCheckEventHandler(OnItemCheck);
            mouseClickHandler = new MouseEventHandler(OnMouseClick);
            buttonPreviewKeyDownHandler = new PreviewKeyDownEventHandler(OnButtonPreviewKeyDown);
            selectedIndexChangedHandler = new EventHandler(OnSelectedIndexChanged);
            textChangedHandler = new EventHandler(OnTextChanged);
            numericUpDownHandler = new EventHandler(OnNumericUpDownClick);
            scrollBarScrollHandler = new ScrollEventHandler(OnScrollBarScroll);
            dataGridViewCellMouseEventHandler = new DataGridViewCellMouseEventHandler(OnDataGridViewCellMouseClick);

            ResManUI = new ResourceManager(this.GetType().ToString() + "_", Assembly.GetExecutingAssembly());
            ResManGeneralUI = new ResourceManager("StockTerminal.Resources.GeneralUI", Assembly.GetExecutingAssembly());

            SetPersistString(PersistString);

            InstanceId = FormRegistry.RegisterInstance(this, LocalFormSettings["InstanceId"]);
            LocalFormSettings["InstanceId"] = InstanceId;

            pCulture = Culture; // internally set and let Culture be applied in the _Loaded event
            //this.Culture = Culture;

            SetDocumentName(null, null);
            // text not changed does not imply FolderPath not changed too. FolderPath can be null
            PrepareActionLog();
        }

        [System.ComponentModel.Browsable(false)]
        public CultureInfo Culture
        {
            get
            {
                if (pCulture == null) return null;
                return CultureInfo.GetCultureInfo(pCulture.ToString());
            }

            set
            {
                if (value == null) { value = new CultureInfo("en-US"); }
                if (pCulture == null || pCulture.Equals(value) == false)
                {
                    SetCulture(value);
                    SaveCultureSetting(value);
                    if (Loaded && pCultureChangeDelegateInvoke != null) this.Invoke(pCultureChangeDelegateInvoke, value);
                }
            }
        }

        public ITradeDB TradeDB
        {
            get
            {
                return pTradeDB;
            }

            set
            {
                DetachListenTradeDB();
                CloseLog();
                pTradeDB = value;
                PrepareActionLog();
                AttachListenTradeDB();
                if (pTradeDB != null) pTradeDB.Culture = pCulture;
            }
        }

        protected FormWindowState AppWindowState
        {
            get
            {
                List<BaseForm> formList = GetFormByFormType(typeof(MainForm));

                if (formList != null && formList.Count > 0 && formList[0] != null)
                {
                    return formList[0].WindowState;
                }
                return FormWindowState.Normal;
            }

            set
            {
                List<BaseForm> formList = GetFormByFormType(typeof(MainForm));

                if (formList != null && formList.Count > 0 && formList[0] != null)
                {
                    try
                    {
                        formList[0].WindowState = value;
                    }
                    catch { }
                }
            }
        }

        #region "Form settings persistance"

        /// <summary>
        /// Return the persistence string of settings for Local Form Settings. Handled by the Docking objects.
        /// </summary>
        /// <returns>The persistence string of settings for Local Form Settings.</returns>
        protected override string GetPersistString()
        {
            LocalFormSettings["InstanceId"] = InstanceId;
            return GetType().ToString() + "|" + LocalFormSettings.PersistString;
        }

        /// <summary>
        /// Fill up Local Form Settings from the persistence string.
        /// </summary>
        /// <param name="PersistString">The persistence string used to fill up the Local Form Settings.</param>
        public void SetPersistString(string PersistString)
        {
            LocalFormSettings.PersistString = PersistString;
        }

        #endregion

        #region "Instance Management"

        public static int GetInstanceCount(Type FormType)
        {
            return FormRegistry.GetInstanceCount(FormType);
        }

        public static List<BaseForm> GetFormByFormType(Type FormType)
        {
            return FormRegistry.GetByFormType(FormType);
        }

        public static BaseForm GetFormByInstanceId(string InstanceId)
        {
            return FormRegistry.GetByInstanceId(InstanceId);
        }

        #endregion

        #region "Culture handling"

        private void SetCulture(CultureInfo Culture)
        {
            ComponentResourceManager ResMan = new ComponentResourceManager(this.GetType());
            SuspendLayout();
            ApplyResources(this, ResMan, Culture);   // recursively apply resources to every UI elements

            pCulture = Culture;
            if (pTradeDB != null) pTradeDB.Culture = Culture;

            SetDocumentName(null, null, true);

            if (this == MainBaseForm)
            {
                formListChangeDelegateStatic.Invoke();
            }

            ResumeLayout();
        }

        private void SaveCultureSetting(CultureInfo ci)
        {
            if (ci != null)
            {
                switch (ci.Name)
                {
                    case "zh-CHT": SettingsTradeDB["Language"] = "TC"; break;
                    case "zh-CHS": SettingsTradeDB["Language"] = "SC"; break;
                    default: SettingsTradeDB["Language"] = "EN"; break;
                }

                SettingsTradeDB.SaveToFile(INIFile, "TradeDB", "Language");
            }
        }

        protected void SetDocumentName(string DocumentName, string DocumentNameEn)
        {
            SetDocumentName(DocumentName, DocumentNameEn, false);
        }

        protected void SetDocumentName(string DocumentName, string DocumentNameEn, bool SuppressFormListChangeEvent)
        {
            string docName = DocumentName != null ? DocumentName.Trim() : GetResxString("DocumentName");
            string docNameEn = DocumentNameEn != null ? DocumentNameEn.Trim() : GetResxString("DocumentName", CultureInfo.GetCultureInfo("en-US"));
            string newDocumentName;
            string newDocumentNameEn;
            
            if (docName == null || docName.Length <= 0) docName = GetDefaultDocName();
            if (docNameEn == null || docNameEn.Length <= 0) docNameEn = GetDefaultDocName();

            if (FormRegistry.RegisterDocumentName(this, docName, docNameEn, out newDocumentName, out newDocumentNameEn))
            {
                pDocumentName = newDocumentName;
                pDocumentNameEn = newDocumentNameEn;

                if (!SuppressFormListChangeEvent) formListChangeDelegateStatic.Invoke();
            }

            if (this.Text != pDocumentName) this.Text = pDocumentName;
            if (this.TabText != pDocumentName) this.TabText = pDocumentName;
        }

        protected string GetResxString(string Key)
        {
            try
            {
                return ResManUI.GetString(Key, pCulture);
            }
            catch (Exception e)
            {
                Debug.Print(DateTime.Now.ToString() + ": " + this.GetType().ToString() + ": " + Utils.Utils.GetFunctionName() + ": " + e.Message);
            }

            return null;
        }

        protected string GetResxString(string Key, CultureInfo Ci)
        {
            try
            {
                return ResManUI.GetString(Key, Ci);
            }
            catch (Exception e)
            {
                Debug.Print(DateTime.Now.ToString() + ": " + this.GetType().ToString() + ": " + Utils.Utils.GetFunctionName() + ": " + e.Message);
            }

            return null;
        }

        protected object GetResxObject(string Key)
        {
            try
            {
                return ResManUI.GetObject(Key, pCulture);
            }
            catch (Exception e)
            {
                Debug.Print(DateTime.Now.ToString() + ": " + this.GetType().ToString() + ": " + Utils.Utils.GetFunctionName() + ": " + e.Message);
            }

            return null;
        }

        protected UnmanagedMemoryStream GetResxStream(string Key)
        {
            try
            {
                return ResManUI.GetStream(Key);
            }
            catch (Exception e)
            {
                Debug.Print(DateTime.Now.ToString() + ": " + this.GetType().ToString() + ": " + Utils.Utils.GetFunctionName() + ": " + e.Message);
            }

            return null;
        }

        protected string GetGeneralResxString(string Key)
        {
            try
            {
                return ResManGeneralUI.GetString(Key, pCulture);
            }
            catch (Exception e)
            {
                Debug.Print(DateTime.Now.ToString() + ": " + this.GetType().ToString() + ": " + Utils.Utils.GetFunctionName() + ": " + e.Message);
            }

            return null;
        }

        protected string GetGeneralResxString(string Key, CultureInfo Ci)
        {
            try
            {
                return ResManGeneralUI.GetString(Key, Ci);
            }
            catch (Exception e)
            {
                Debug.Print(DateTime.Now.ToString() + ": " + this.GetType().ToString() + ": " + Utils.Utils.GetFunctionName() + ": " + e.Message);
            }

            return null;
        }

        protected string GetBestCurrencySign(string Currency)
        {
            if (Currency == "CNY")
                return "￥";
            else
                return "$";
        }

        private string GetDefaultDocName()
        {
            string[] typeParts = this.GetType().ToString().Split('.');
            string docName = typeParts[typeParts.Length - 1];
            for (int i = 0, n = docName.Length - 1; i < n; i++)
            {
                char c = docName[i];
                if (c >= 97 && c <= 122)
                {
                    c = docName[i + 1];
                    if (c >= 65 && c <= 90)
                    {
                        docName = docName.Insert(i + 1, " ");
                        i += 3;
                        n++;
                    }
                }
            }
            return docName;
        }

        private void ApplyResources(object Element, ComponentResourceManager ResMan, CultureInfo Culture)
        {
            if (Element == null) return;

            switch (Element.GetType().ToString())
            {
                case "System.Windows.Forms.ComboBox":
                    ApplyResourcesComboBox((ComboBox)Element, ResMan, Culture);
                    return;

                case "System.Windows.Forms.ListBox":
                    ApplyResourcesListBox((ListBox)Element, ResMan, Culture);
                    return;

                case "System.Windows.Forms.CheckedListBox":
                    ApplyResourcesCheckedListBox((CheckedListBox)Element, ResMan, Culture);
                    return;

                case "System.Windows.Forms.TabPage":
                    ApplyResourcesObject(Element, ((System.Windows.Forms.TabPage)Element).Name, ResMan, Culture);
                    foreach (System.Windows.Forms.Control e in ((System.Windows.Forms.TabPage)Element).Controls) ApplyResources(e, ResMan, Culture);
                    return;

                case "System.Windows.Forms.TabControl":
                    ApplyResourcesObject(Element, ((System.Windows.Forms.TabControl)Element).Name, ResMan, Culture);
                    foreach (System.Windows.Forms.Control e in ((System.Windows.Forms.TabControl)Element).Controls) ApplyResources(e, ResMan, Culture);
                    foreach (System.Windows.Forms.TabPage e in ((System.Windows.Forms.TabControl)Element).TabPages) ApplyResources(e, ResMan, Culture);
                    return;

                case "WeifenLuo.WinFormsUI.Docking.DockPane":
                    ApplyResourcesObject(Element, ((WeifenLuo.WinFormsUI.Docking.DockPane)Element).Name, ResMan, Culture);
                    foreach (WeifenLuo.WinFormsUI.Docking.DockContent e in ((WeifenLuo.WinFormsUI.Docking.DockPane)Element).Contents) ApplyResources(e, ResMan, Culture);
                    foreach (System.Windows.Forms.Control e in ((WeifenLuo.WinFormsUI.Docking.DockPane)Element).Controls) ApplyResources(e, ResMan, Culture);
                    return;

                case "WeifenLuo.WinFormsUI.Docking.DockPanel+AutoHideWindowControl":
                    ApplyResourcesObject(Element, ((WeifenLuo.WinFormsUI.Docking.DockPanel.AutoHideWindowControl)Element).Name, ResMan, Culture);
                    foreach (WeifenLuo.WinFormsUI.Docking.DockPane e in ((WeifenLuo.WinFormsUI.Docking.DockPanel.AutoHideWindowControl)Element).DockPanel.Panes) ApplyResources(e, ResMan, Culture);
                    foreach (System.Windows.Forms.Control e in ((WeifenLuo.WinFormsUI.Docking.DockPanel.AutoHideWindowControl)Element).DockPanel.Controls) ApplyResources(e, ResMan, Culture);
                    foreach (System.Windows.Forms.Control e in ((WeifenLuo.WinFormsUI.Docking.DockPanel.AutoHideWindowControl)Element).Controls) ApplyResources(e, ResMan, Culture);
                    return;

                case "System.Windows.Forms.ToolStripButton":
                    ApplyResourcesObject(Element, ((System.Windows.Forms.ToolStripButton)Element).Name, ResMan, Culture);
                    return;

                case "System.Windows.Forms.ToolStripItem":
                    ApplyResourcesObject(Element, ((System.Windows.Forms.ToolStripItem)Element).Name, ResMan, Culture);
                    return;

                case "System.Windows.Forms.ToolStrip":
                    ApplyResourcesObject(Element, ((System.Windows.Forms.ToolStrip)Element).Name, ResMan, Culture);
                    foreach (System.Windows.Forms.ToolStripItem e in ((System.Windows.Forms.ToolStrip)Element).Items) ApplyResources(e, ResMan, Culture);
                    return;

                case "System.Windows.Forms.ToolStripMenuItem":
                    ApplyResourcesObject(Element, ((System.Windows.Forms.ToolStripMenuItem)Element).Name, ResMan, Culture);
                    foreach (System.Windows.Forms.ToolStripItem e in ((System.Windows.Forms.ToolStripMenuItem)Element).DropDownItems) ApplyResources(e, ResMan, Culture);
                    return;

                case "System.Windows.Forms.MenuStrip":
                    ApplyResourcesObject(Element, ((System.Windows.Forms.MenuStrip)Element).Name, ResMan, Culture);
                    foreach (System.Windows.Forms.ToolStripItem e in ((System.Windows.Forms.MenuStrip)Element).Items) ApplyResources(e, ResMan, Culture);
                    return;

                case "System.Windows.Forms.Form":
                    ApplyResourcesObject(Element, ((System.Windows.Forms.Form)Element).Name, ResMan, Culture);
                    foreach (System.Windows.Forms.Control e in ((System.Windows.Forms.Form)Element).Controls) ApplyResources(e, ResMan, Culture);
                    return;

                case "System.Windows.Forms.DataGridView":
                    //MessageBox.Show("System.Windows.Forms.DataGridView");
                    ApplyResourcesObject(Element, ((System.Windows.Forms.DataGridView)Element).Name, ResMan, Culture);
                    foreach (System.Windows.Forms.Control e in ((System.Windows.Forms.DataGridView)Element).Controls)
                    {
                        //MessageBox.Show("Type: " + ((System.Windows.Forms.DataGridView)Element).Controls.ToString());
                        ApplyResources(e, ResMan, Culture);
                    }
                    //foreach (System.Windows.Forms.DataGridViewColumn e in ((System.Windows.Forms.DataGridView)Element).Columns) ApplyResources(e, ResMan, Culture);
                    return;
                //default:
                //    MessageBox.Show(Element.GetType().ToString(), "BaseForm -> ApplyResources()");
                //    break;
            }
            if (Element is BaseForm && Element.Equals(this) == false)   // Element.Equals(this) ==> dead loop recursion
            {
                ((BaseForm)Element).Culture = Culture;
            }
            else if (Element is System.Windows.Forms.Control)
            {
                foreach (System.Windows.Forms.Control e in ((System.Windows.Forms.Control)Element).Controls)
                {
                    ApplyResources(e, ResMan, Culture);
                }
                ApplyResourcesObject(Element, ((System.Windows.Forms.Control)Element).Name, ResMan, Culture);
            }
        }

        private void ApplyResourcesObject(object Element, string ElementName, ComponentResourceManager ResMan, CultureInfo Culture)
        {
            try { ResMan.ApplyResources(Element, ElementName, Culture); }
            catch (Exception e)
            {
                Debug.Print(DateTime.Now.ToString() + ": " + this.GetType().ToString() + ": " + Utils.Utils.GetFunctionName() + ": " + e.Message);
            }
        }

        private void ApplyResourcesComboBox(ComboBox comboBox, ComponentResourceManager ResMan, CultureInfo Culture)
        {
            try
            {
                if (comboBox != null && comboBox.Items != null && comboBox.Items.Count > 0)
                {
                    int i, n = comboBox.Items.Count;
                    bool sorted = comboBox.Sorted;
                    string resName = comboBox.Name + ".Items";

                    int selectedIndex = comboBox.SelectedIndex;

                    string firstString = ResMan.GetString(resName, Culture);

                    if (firstString != null)
                    {
                        if (sorted) comboBox.Sorted = false;
                        comboBox.Items[0] = firstString;

                        for (i = 1; i < n; i++)
                        {
                            comboBox.Items[i] = ResMan.GetString(resName + i, Culture);
                        }
                        if (sorted)
                        {
                            comboBox.Sorted = true;
                        }
                        else
                        {
                            comboBox.SelectedIndex = selectedIndex;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Print(DateTime.Now.ToString() + ": " + this.GetType().ToString() + ": " + Utils.Utils.GetFunctionName() + ": " + e.Message);
            }
        }

        private void ApplyResourcesListBox(ListBox listBox, ComponentResourceManager ResMan, CultureInfo Culture)
        {
            try
            {
                if (listBox != null && listBox.Items != null && listBox.Items.Count > 0)
                {
                    int i, n = listBox.Items.Count;
                    bool sorted = listBox.Sorted;
                    string resName = listBox.Name + ".Items";
                    int[] selectedItems = null;

                    string firstString = ResMan.GetString(resName, Culture);

                    if (firstString != null)
                    {
                        if (sorted)
                        {
                            listBox.Sorted = false;
                        }
                        else
                        {
                            selectedItems = new int[listBox.SelectedIndices.Count];
                            listBox.SelectedIndices.CopyTo(selectedItems, 0);
                        }

                        listBox.Items[0] = firstString;

                        for (i = 1; i < n; i++)
                        {
                            listBox.Items[i] = ResMan.GetString(resName + i, Culture);
                        }

                        if (sorted)
                        {
                            listBox.Sorted = true;
                        }
                        else
                        {
                            for (i = 0; i < selectedItems.Length; i++)
                            {
                                listBox.SetSelected(selectedItems[i], true);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Print(DateTime.Now.ToString() + ": " + this.GetType().ToString() + ": " + Utils.Utils.GetFunctionName() + ": " + e.Message);
            }
        }

        private void ApplyResourcesCheckedListBox(CheckedListBox checkedListBox, ComponentResourceManager ResMan, CultureInfo Culture)
        {
            try
            {
                if (checkedListBox != null && checkedListBox.Items != null && checkedListBox.Items.Count > 0)
                {
                    int i, n = checkedListBox.Items.Count;
                    string resName = checkedListBox.Name + ".Items";
                    int[] selectedItems = null;

                    if (!checkedListBox.Sorted)
                    {
                        selectedItems = new int[checkedListBox.SelectedIndices.Count];
                        checkedListBox.SelectedIndices.CopyTo(selectedItems, 0);
                    }

                    checkedListBox.Items.Clear();
                    checkedListBox.Items.Add(ResMan.GetString(resName, Culture));

                    for (i = 1; i < n; i++)
                    {
                        checkedListBox.Items.Add(ResMan.GetString(resName + i, Culture));
                    }

                    if (!checkedListBox.Sorted)
                    {
                        for (i = 0; i < selectedItems.Length; i++)
                        {
                            checkedListBox.SetSelected(selectedItems[i], true);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Print(DateTime.Now.ToString() + ": " + this.GetType().ToString() + ": " + Utils.Utils.GetFunctionName() + ": " + e.Message);
            }
        }

        #endregion

        #region "Message Box"

        protected DialogResult ShowMessageBox(string text)
        {
            try
            {
                Graphics g = this.CreateGraphics();
                SizeF fontSize = g.MeasureString(text, this.Font);
                int formWidth = ((int)fontSize.Width) + 150;
                int formHeight = ((int)fontSize.Height) + 50;
                g.Dispose();

                if (formWidth < 300) { formWidth = 300; }
                else if (formWidth > 750) { formWidth = 750; }

                if (formHeight < 149) { formHeight = 149; }
                else if (formHeight > 550) { formHeight = 550; }

                MessageBoxForm msgBox = new MessageBoxForm(this.Culture, null);
                lock (MsgBoxesMutex)
                {
                    MsgBoxes.Add(msgBox);
                }
                msgBox.Width = formWidth;
                msgBox.StartPosition = FormStartPosition.CenterParent;
                //msgBox.Text = this.Text;  // currently no use because BaseForm will overwrite this
                msgBox.Message = text;
                msgBox.Buttons = MessageBoxButtons.OK;
                msgBox.DefaultButton = MessageBoxDefaultButton.Button1;
                msgBox.MessageIcon = MessageBoxIcon.None;
                if (AppWindowState == FormWindowState.Minimized) AppWindowState = FormWindowState.Normal;
                return msgBox.ShowDialog(this);
            }
            catch (Exception exp)
            {
                AppendLog("ShowMessageBox", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "MessageBox", false);
            }

            return DialogResult.Ignore;
        }

        protected DialogResult ShowMessageBox(string text, string caption, MessageBoxButtons buttons,
            MessageBoxIcon icon, MessageBoxDefaultButton defaultButton)
        {
            try
            {
                Graphics g = this.CreateGraphics();
                SizeF fontSize = g.MeasureString(text, this.Font);
                int formWidth = ((int)fontSize.Width) + 150;
                int formHeight = ((int)fontSize.Height) + 50;
                g.Dispose();

                if (formWidth < 300) { formWidth = 300; }
                else if (formWidth > 750) { formWidth = 750; }

                if (formHeight < 149) { formHeight = 149; }
                else if (formHeight > 550) { formHeight = 550; }

                MessageBoxForm msgBox = new MessageBoxForm(this.Culture, null);
                lock (MsgBoxesMutex)
                {
                    MsgBoxes.Add(msgBox);
                }
                msgBox.Width = formWidth;
                msgBox.Height = formHeight;
                msgBox.StartPosition = FormStartPosition.CenterParent;
                //msgBox.Text = caption;    // currently no use because BaseForm will overwrite this
                msgBox.Message = text;
                msgBox.Buttons = buttons;
                msgBox.DefaultButton = defaultButton;
                msgBox.MessageIcon = icon;
                if (AppWindowState == FormWindowState.Minimized) AppWindowState = FormWindowState.Normal;
                return msgBox.ShowDialog(this);
            }
            catch (Exception exp)
            {
                AppendLog("ShowMessageBox", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "MessageBox", false);
            }

            return DialogResult.Ignore;
        }

        protected void HideMessageBox()
        {
            MessageBoxForm[] msgBoxArr = null;
            lock (MsgBoxesMutex)
            {
                msgBoxArr = MsgBoxes.ToArray();
                MsgBoxes.Clear();
            }

            foreach (MessageBoxForm msgBox in msgBoxArr)
            {
                msgBox.Close();
                //msgBox.Dispose(); // calling dispose here might result in FormClosed event not raised.
            }
        }

        #endregion

        #region "Event and Log management"

        /*
        /// <summary>
        /// Append a system event into the event window.
        /// </summary>
        /// <param name="EventType">Type of the event.</param>
        /// <param name="Description">Description of the event.</param>
        protected void AppendEvent(string EventType, string Description)
        {
            systemEventDelegateStatic.Invoke(new List<SystemEvent> { new SystemEvent(EventType, Description) });
        }
         */

        private void PrepareActionLog()
        {
            if (actionLog != null) actionLog.Close();

            actionLog = Program.TheLogProcessor.Open(null, "", ".log");
            actionLog.FlushAfterAppend = true;

            actionLog.FolderPath = Path.Combine(
                Path.Combine(
                    (pTradeDB != null && pTradeDB.UserId != null && pTradeDB.UserId.Length > 0) ? Path.Combine(LogFolder, pTradeDB.UserId) : LogFolder,
                    @"__UserAction\"
                    ),
                pDocumentNameEn.Replace('/', '_').Replace('\\', '_')
                );

            //CentralUserSettingSubscription.LogBaseFolderPath = (pTradeDB != null && pTradeDB.UserId != null && pTradeDB.UserId.Length > 0) ? Path.Combine(LogFolder, pTradeDB.UserId) : LogFolder;
            //OrderSubscription.LogBaseFolderPath = (pTradeDB != null && pTradeDB.UserId != null && pTradeDB.UserId.Length > 0) ? Path.Combine(LogFolder, pTradeDB.UserId) : LogFolder;
            //OddLotSubscription.LogBaseFolderPath = (pTradeDB != null && pTradeDB.UserId != null && pTradeDB.UserId.Length > 0) ? Path.Combine(LogFolder, pTradeDB.UserId) : LogFolder;
            //AccountSubscription.LogBaseFolderPath = (pTradeDB != null && pTradeDB.UserId != null && pTradeDB.UserId.Length > 0) ? Path.Combine(LogFolder, pTradeDB.UserId) : LogFolder;
            //StockSubscription.LogBaseFolderPath = (pTradeDB != null && pTradeDB.UserId != null && pTradeDB.UserId.Length > 0) ? Path.Combine(LogFolder, pTradeDB.UserId) : LogFolder;
            //IndexSubscription.LogBaseFolderPath = (pTradeDB != null && pTradeDB.UserId != null && pTradeDB.UserId.Length > 0) ? Path.Combine(LogFolder, pTradeDB.UserId) : LogFolder;
            //TransactionChargeSubscription.LogBaseFolderPath = (pTradeDB != null && pTradeDB.UserId != null && pTradeDB.UserId.Length > 0) ? Path.Combine(LogFolder, pTradeDB.UserId) : LogFolder;
        }

        /// <summary>
        /// Append a message to the appropriate log file. 
        /// </summary>
        /// <param name="MessageType">A string to be put together with the message into the log file for remarking the message.</param>
        /// <param name="Message">The message to be added to the log file.</param>
        /// <param name="LogName">A folder named as the specified string will be created to store the log file.</param>
        /// <param name="ShowMsgBox">A message box showing the MessageType and Message if true.</param>
        /// <returns></returns>
        protected void AppendLog(string MessageType, string Message, string LogName, bool ShowMsgBox)
        {
            string logName = "";
            if (LogName != null) logName = LogName.Trim();
            if (currentLog == null || currentLog.Name != logName)
            {
                if (logDict.TryGetValue(logName, out currentLog))
                {
                    if (currentLog == null)
                    {
                        logDict.Remove(logName);
                    }
                }
                else
                {
                    currentLog = null;
                }
            }

            if (currentLog == null) // in case logDict with matching logName returning null can also be trapped here.
            {
                currentLog = Program.TheLogProcessor.Open(logName, 
                    Path.Combine(
                        Path.Combine(
                            (pTradeDB != null && pTradeDB.UserId != null && pTradeDB.UserId.Length > 0) ? Path.Combine(LogFolder, pTradeDB.UserId) : LogFolder, 
                            InstanceId.Replace('/', '_').Replace('\\', '_')
                            ), 
                        logName.Replace('/', '_').Replace('\\', '_')), "", ".log"
                        );
                currentLog.FlushAfterAppend = true;
                logDict.Add(logName, currentLog);
            }

            currentLog.Append((pTradeDB != null ? pTradeDB.UserId : "") + "," + MessageType, Message);

            if (ShowMsgBox) MessageBox.Show(Message, MessageType);
        }

        /// <summary>
        /// Close log for use for example just before when User Id is about to be changed
        /// </summary>
        private void CloseLog()
        {
            if (actionLog != null) actionLog.Close();

            if (logDict != null)
            {
                foreach (KeyValuePair<string, Log> kvp in logDict)
                {
                    if (kvp.Value != null) kvp.Value.Close();
                }
                logDict.Clear();
            }
        }

        private void PurgeLog()
        {
            if (actionLog != null) actionLog.Purge(30);

            if (logDict != null && logDict.Count > 0)
            {
                foreach (KeyValuePair<string, Log> kvp in logDict)
                {
                    if (kvp.Value != null) kvp.Value.Purge(30);
                }
            }
        }

        #endregion

        #region "Form Events"

        private void BaseForm_Deactivate(object sender, EventArgs e)
        {
            if (this.ParentForm == null)    // when the main form lost focus, reset all key modifiers because the WM_KEYUP and WM_SYSKEYUP events for the modifier keys lost
            {
                VKCtrl = false;
                VKShift = false;
                VKAlt = false;
//                VKContext = false;
            }
        }

        private void BaseForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (AppState == ApplicationState.Run && this.DockState != DockState.Unknown && this.DockState != DockState.Float && LayoutLocked && 
                !IsProgramClosing && e.CloseReason == CloseReason.UserClosing)
                e.Cancel = true;
            //if (AppState == ApplicationState.Run && LayoutLockable && !IsProgramClosing)
            //{
            //    switch (e.CloseReason)
            //    {
            //        case CloseReason.UserClosing:
            //            if (LayoutLocked) e.Cancel = true;
            //            break;
            //    }
            //}
        }

        private void BaseForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            UnListenFormListChange();
            UnListenSystemEvent();
            UnListenConnectionStatus();
            UnListenOrderStatus(null);
            UnListenOddLotOrder(null);
            UnListenAccountList();
            UnListenAccount(null);
            UnListenStock(null);
            UnListenIndex(null);

            actionLog.Dispose();
            Log log = null;
            foreach (KeyValuePair<string, Log> kvp in logDict)
            {
                log = (kvp.Value as Log);
                if (log != null) log.Dispose();
            }
            FormRegistry.UnRegisterInstance(this);
            PurgeLog();
        }

        private void BaseForm_Load(object sender, EventArgs e)
        {
            /* re apply the language settings in case the controls were not loaded and 
             * InitializeComponent is called again in the derived class when the Language property is set
             */
            ListenAllEvents(this);
            SetCulture(pCulture);
            Loaded = true;
        }

        protected void UpdateUserActionTime()
        {
            lock (TimeLastUserActionMutex)
            {
                TimeLastUserAction = DateTime.Now;
            }
        }

        protected int GetTimeSinceLastUserAction()
        {
            int secs = 0;

            lock (TimeLastUserActionMutex)
            {
                secs = (int)(DateTime.Now - TimeLastUserAction).TotalSeconds;
            }

            return secs;
        }

        private void OnCheckedChanged(object sender, EventArgs e)
        {
            UpdateUserActionTime();

            string senderType = sender.GetType().ToString();
            string messagePrefix = senderType + "," + ((Control)sender).Name + ",";
            string messageBody = "";

            switch (senderType)
            {
                case "System.Windows.Forms.RadioButton":
                    if ((sender as RadioButton).Checked == true)   // record checked item only, unchecked need not be recorded because at any time only one is checked in a radio button group.
                    {
                        messageBody = (sender as RadioButton).Text + " (checked)";
                        actionLog.Append((pTradeDB != null ? pTradeDB.UserId : "") + "," + "CheckedChange", messagePrefix + messageBody);
                    }
                    break;
            }
        }

        private void OnItemCheck(object sender, EventArgs e)
        {
            UpdateUserActionTime();

            string senderType = sender.GetType().ToString();
            string messagePrefix = senderType + "," + ((Control)sender).Name + ",";
            string messageBody = "";

            switch (senderType)
            {
                case "System.Windows.Forms.CheckedListBox":
                    messageBody = (sender as CheckedListBox).Text;
                    if ((sender as CheckedListBox).GetItemChecked((sender as CheckedListBox).SelectedIndex) == false)   // check state will be changed after this event finished
                    { messageBody += " (checked)"; }
                    else { messageBody += " (unchecked)"; }
                    break;

                default:
                    messageBody = (sender as Control).Text;
                    break;
            }

            actionLog.Append((pTradeDB != null ? pTradeDB.UserId : "") + "," + "ItemCheck", messagePrefix + messageBody);
        }

        private void OnButtonPreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            UpdateUserActionTime();

            string senderType = sender.GetType().ToString();
            string messagePrefix = senderType + "," + ((Control)sender).Name + "," + e.KeyCode + ",";
            string messageBody = "";

            switch (senderType)
            {
                case "System.Windows.Forms.Button":
                    if ((sender as Button).Parent != null && (sender as Button).Parent.Parent != null &&
                        (sender as Button).Parent.Parent.GetType().ToString() == "StockTerminal.Forms.MessageBoxForm")
                    {
                        messageBody = ((sender as Button).Parent.Parent as MessageBoxForm).Message + " - " + (sender as Button).Text;
                    }
                    else
                    {
                        messageBody = (sender as Button).Text;
                    }
                    break;

                case "System.Windows.Forms.CheckBox":
                    messageBody = (sender as CheckBox).Text;
                    if ((sender as CheckBox).Checked) { messageBody += " (checked)"; }
                    else { messageBody += " (unchecked)"; }
                    break;

                case "System.Windows.Forms.UpDownBase+UpDownButtons":
                    messageBody = (sender as Control).Parent.Controls[1].Text;
                    break;

                default:
                    messageBody = (sender as Control).Text;
                    break;
            }

            actionLog.Append((pTradeDB != null ? pTradeDB.UserId : "") + "," + "ButtonKeyDown", messagePrefix + messageBody);
        }

        private void OnMouseClick(object sender, MouseEventArgs e)
        {
            UpdateUserActionTime();

            string senderType = sender.GetType().ToString();
            string messagePrefix = senderType + "," + ((Control)sender).Name + "," + e.Button + ",";
            string messageBody = "";

            switch (senderType)
            {
                case "System.Windows.Forms.Button":
                    if ((sender as Button).Parent != null && (sender as Button).Parent.Parent != null &&
                        (sender as Button).Parent.Parent.GetType().ToString() == "StockTerminal.Forms.MessageBoxForm")
                    {
                        messageBody = ((sender as Button).Parent.Parent as MessageBoxForm).Message + " - " + (sender as Button).Text;
                    }
                    else
                    {
                        messageBody = (sender as Button).Text;
                    }
                    break;

                case "System.Windows.Forms.CheckBox":
                    messageBody = (sender as CheckBox).Text;
                    if ((sender as CheckBox).Checked) { messageBody += " (checked)"; }
                    else { messageBody += " (unchecked)"; }
                    break;

                case "System.Windows.Forms.UpDownBase+UpDownButtons":
                    messageBody = (sender as Control).Parent.Controls[1].Text;
                    break;

                default:
                    messageBody = (sender as Control).Text;
                    break;
            }

            actionLog.Append((pTradeDB != null ? pTradeDB.UserId : "") + "," + "MouseClick", messagePrefix + messageBody);
        }

        private void OnSelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateUserActionTime();

            string senderType = sender.GetType().ToString();
            string messagePrefix = senderType + "," + ((Control)sender).Name + ",";
            string messageBody = "";

            switch (senderType)
            {
                case "System.Windows.Forms.ComboBox":
                    messageBody = (sender as ComboBox).Text;
                    break;

                case "StockTerminal.Utils.AccountComboBox":
                    messageBody = (sender as AccountComboBox).Text;
                    break;

                default:
                    messageBody = (sender as Control).Text;
                    break;
            }

            actionLog.Append((pTradeDB != null ? pTradeDB.UserId : "") + "," + "MouseClick", messagePrefix + messageBody);
        }

        private void OnTextChanged(object sender, EventArgs e)
        {
            UpdateUserActionTime();

            string senderType = sender.GetType().ToString();
            string messagePrefix = senderType + "," + ((Control)sender).Name + ",";
            string messageBody = "";

            switch (senderType)
            {
                case "System.Windows.Forms.TextBox": // no log if it is for password
                    if ((sender as TextBox).PasswordChar == '\0' && (sender as TextBox).UseSystemPasswordChar != true)
                    {
                        messageBody = (sender as TextBox).Text;
                    }
                    break;

                case "StockTerminal.Utils.MyTextBox": // no log if it is for password
                    if ((sender as MyTextBox).PasswordChar == '\0' && (sender as MyTextBox).UseSystemPasswordChar != true)
                    {
                        messageBody = (sender as TextBox).Text;
                    }
                    break;

                default:
                    messageBody = (sender as Control).Text;
                    break;
            }

            actionLog.Append((pTradeDB != null ? pTradeDB.UserId : "") + "," + "TextChanged", messagePrefix + messageBody);
        }

        private void OnNumericUpDownClick(object sender, EventArgs e)
        {
            UpdateUserActionTime();

            string senderType = sender.GetType().ToString();
            string messagePrefix = senderType + "," + ((Control)sender).Name + ",";
            string messageBody = (sender as NumericUpDown).Value.ToString();

            actionLog.Append((pTradeDB != null ? pTradeDB.UserId : "") + "," + "TextChanged", messagePrefix + messageBody);
        }

        private void OnScrollBarScroll(object sender, ScrollEventArgs e)
        {
            UpdateUserActionTime();

            string senderType = sender.GetType().ToString();
            string messagePrefix = senderType + "," + ((Control)sender).Name + ",";
            string messageBody = "";
            string direction = "NoDirection";

            if (e.ScrollOrientation == ScrollOrientation.HorizontalScroll)
            {
                direction = e.NewValue < e.OldValue ? "Left" : "Right";
            }
            else if (e.ScrollOrientation == ScrollOrientation.VerticalScroll)
            {
                direction = e.NewValue < e.OldValue ? "Up" : "Down";
            }
            messageBody = direction + " " + e.OldValue.ToString() + "->" + e.NewValue.ToString();

            actionLog.Append((pTradeDB != null ? pTradeDB.UserId : "") + "," + "ScrollbarScroll", messagePrefix + messageBody);
        }

        private void OnDataGridViewCellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            UpdateUserActionTime();

            DataGridView d = (sender as DataGridView);
            DataGridViewCell cell = null;

            try
            {
                cell = d.Rows[e.RowIndex].Cells[e.ColumnIndex];
            }
            catch { cell = null; }

            string senderType = sender.GetType().ToString();
            string messagePrefix = senderType + "," + ((Control)sender).Name + ",";
            string messageBody = "Cell Row " + e.RowIndex.ToString() + " Col " + e.ColumnIndex.ToString();

            if (cell != null && cell.Value != null) messageBody += " Text " + cell.Value.ToString();

            actionLog.Append((pTradeDB != null ? pTradeDB.UserId : "") + "," + "DataGridViewCellClick", messagePrefix + messageBody);
        }

        private void ListenAllEvents(Control control)
        {
            foreach (Control c in control.Controls)
            {
                c.MouseClick += mouseClickHandler;  // for user action log and UpdateUserActionTime

                switch (c.GetType().ToString())
                {
                    case "System.Windows.Forms.Label":
                        break;

                    case "System.Windows.Forms.TextBox": // no log if it is for password
                        if ((c as TextBox).PasswordChar == '\0' && (c as TextBox).UseSystemPasswordChar == false)
                        {
                            (c as TextBox).TextChanged += textChangedHandler;
                        }
                        break;

                    case "System.Windows.Forms.RadioButton":
                        (c as RadioButton).CheckedChanged += checkedChangedHandler;
                        break;

                    case "System.Windows.Forms.Button":
                        (c as Button).PreviewKeyDown += buttonPreviewKeyDownHandler;
                        break;
//                        case "System.Windows.Forms.Button":
//                            c.MouseClick += mouseClickHandler;
//                            break;

                    case "System.Windows.Forms.ComboBox":
                        (c as ComboBox).SelectedIndexChanged += selectedIndexChangedHandler;
                        (c as ComboBox).TextChanged += selectedIndexChangedHandler;
                        break;

//                        case "System.Windows.Forms.CheckBox":
//                            c.MouseClick += mouseClickHandler;
//                            break;

                    case "StockTerminal.Utils.AccountComboBox":
                        (c as AccountComboBox).SelectedIndexChanged += selectedIndexChangedHandler;
                        (c as AccountComboBox).TextChanged += selectedIndexChangedHandler;
                        break;

                    case "System.Windows.Forms.CheckedListBox":
                        (c as CheckedListBox).ItemCheck += itemCheckHandler;
                        break;

//                        case "System.Windows.Forms.ListBox":
//                            c.MouseClick += mouseClickHandler;
//                            break;

                    case "System.Windows.Forms.NumericUpDown":
                        (c as NumericUpDown).Click += numericUpDownHandler;
                        break;

                    case "System.Windows.Forms.DataGridView":
                        (c as DataGridView).CellMouseClick += dataGridViewCellMouseEventHandler;
                        break;

                    case "StockTerminal.Utils.MyTextBox":
                        if ((c as TextBox).PasswordChar == '\0' && (c as TextBox).UseSystemPasswordChar == false)
                        {
                            (c as TextBox).TextChanged += textChangedHandler;
                        }
                        break;

                    case "System.Windows.Forms.HScrollBar":
                        (c as HScrollBar).Scroll += scrollBarScrollHandler;
                        break;

                    case "System.Windows.Forms.VScrollBar":
                        (c as VScrollBar).Scroll += scrollBarScrollHandler;
                        break;

                    default:
                        c.TextChanged += textChangedHandler;
                        if (c.Controls.Count > 0) ListenAllEvents(c);
                        break;
                }
            }
        }

        #endregion

        #region "Hot Key Management"

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == 0x104) // WM_SYSKEYDOWN
            {
                switch (m.WParam.ToInt32())
                {
                    case 16: VKShift = true; break;
                    case 17: VKCtrl = true; break;
                    case 18: VKAlt = true; break;
//                    case 93: VKContext = true; break;
                }
            }
            else if (m.Msg == 0x105)    // WM_SYSKEYUP
            {
                int KeyValue = m.WParam.ToInt32();

                switch (KeyValue)
                {
                    case 16: VKShift = false; break;
                    case 17: VKCtrl = false; break;
                    case 18: VKAlt = false; break;
//                    case 93: VKContext = false; break;
                    default:
                        DispatchHotKeyEvent(((Keys)KeyValue) | (VKCtrl ? Keys.Control : 0) | (VKShift ? Keys.Shift : 0) | (VKAlt ? Keys.Alt : 0));
//                      this.Text = m.WParam.ToInt32().ToString() + " " + (VKCtrl ? " Ctrl" : "") + (VKShift ? " Shift" : "") + (VKAlt ? " Alt" : "") + (VKContext ? " Context" : "");
                        break;
                }
            }
            else if (m.Msg == 0x100)    // WM_KEYDOWN
            {
                switch (m.WParam.ToInt32())
                {
                    case 16: VKShift = true; break;
                    case 17: VKCtrl = true; break;
                    case 18: VKAlt = true; break;
                    case (int)Keys.F1:      // F1
                    case (int)Keys.Divide:      // /
                        // pass the account to order ticket if stock quote form selected a order ticket
                        List<BaseForm> StockQuoteFormList = GetFormByFormType(typeof(StockQuoteForm));
                        if (StockQuoteFormList != null && StockQuoteFormList.Count > 0 && StockQuoteFormList[0] != null)
                            ((StockQuoteForm)StockQuoteFormList[0]).PassToOrderTicket(true);
                        else
                        {
                            List<BaseForm> QuoteBrowserFormList = GetFormByFormType(typeof(QuoteBrowserForm));
                            if (QuoteBrowserFormList != null && QuoteBrowserFormList.Count > 0 && QuoteBrowserFormList[0] != null)
                                ((QuoteBrowserForm)QuoteBrowserFormList[0]).passStkCodeToOrderTicket(true, "");
                        }
                        break;
//                    case 93: VKContext = true; break;
                }
            }
            else if (m.Msg == 0x101)    // WM_KEYUP
            {
                int KeyValue = m.WParam.ToInt32();

                switch (KeyValue)
                {
                    case 16: VKShift = false; break;
                    case 17: VKCtrl = false; break;
                    case 18: VKAlt = false; break;
//                    case 93: VKContext = false; break;
                    default:
                        DispatchHotKeyEvent(((Keys)KeyValue) | (VKCtrl ? Keys.Control : 0) | (VKShift ? Keys.Shift : 0) | (VKAlt ? Keys.Alt : 0));
//                      this.Text = m.WParam.ToInt32().ToString() + " " + (VKCtrl ? " Ctrl" : "") + (VKShift ? " Shift" : "") + (VKAlt ? " Alt" : "") + (VKContext ? " Context" : "");
                        break;
                }
            }
            return false;
        }

        private void DispatchHotKeyEvent(Keys key)
        {
            if (HotKeyUp != null)
            {
                actionLog.Append("HotKeyUp", key.ToString());
                HotKeyUp(this, new KeyEventArgs(key));
            }

            if (this.Parent == null)    // loop only for the top most form
            {
                foreach (Control ctl in Controls)
                {
                    if (ctl is WeifenLuo.WinFormsUI.Docking.DockPanel)
                    {
                        foreach (DockPane pane in (ctl as DockPanel).Panes)
                        {
                            foreach (DockContent content in pane.Contents)
                            {
                                if (content is StockTerminal.Forms.BaseForm)
                                {
                                    (content as BaseForm).DispatchHotKeyEvent(key);
                                }
                            }
                        }
                        break;
                    }
                }
            }
        }

        #endregion

        #region "Delegate Listen Management"

        protected void ListenFormListChange()
        {
            lock (FormListChangeDelegateMutex)
            {
                FormListChangeDelegateDict[this] = formListChangeDelegate;
            }
        }

        protected void UnListenFormListChange()
        {
            lock (FormListChangeDelegateMutex)
            {
                try
                {
                    FormListChangeDelegateDict.Remove(this);
                }
                catch (Exception e)
                {
                    Debug.Print(DateTime.Now.ToString() + ": " + this.GetType().ToString() + ": " + Utils.Utils.GetFunctionName() + ": " + e.Message);
                }
            }
        }

        protected void ListenSystemEvent()
        {
            lock (SystemEventDelegateMutex)
            {
                SystemEventDelegateDict[this] = systemEventDelegateInvoke;
            }
        }

        protected void UnListenSystemEvent()
        {
            lock (SystemEventDelegateMutex)
            {
                try
                {
                    SystemEventDelegateDict.Remove(this);
                }
                catch (Exception e)
                {
                    Debug.Print(DateTime.Now.ToString() + ": " + this.GetType().ToString() + ": " + Utils.Utils.GetFunctionName() + ": " + e.Message);
                }
            }
        }

        protected void ListenConnectionStatus()
        {
            lock (ConnectionStatusDelegateMutex)
            {
                ConnectionStatusDelegateDict[this] = connectionStatusDelegateInvoke;
            }
        }

        protected void UnListenConnectionStatus()
        {
            lock (ConnectionStatusDelegateMutex)
            {
                try
                {
                    ConnectionStatusDelegateDict.Remove(this);
                }
                catch (Exception e)
                {
                    Debug.Print(DateTime.Now.ToString() + ": " + this.GetType().ToString() + ": " + Utils.Utils.GetFunctionName() + ": " + e.Message);
                }
            }
        }

        protected void ListenQuoteConnectionStatus()
        {
            lock (QuoteConnectionStatusDelegateMutex)
            {
                QuoteConnectionStatusDelegateDict[this] = quoteConnectionStatusDelegateInvoke;
            }
        }

        protected void UnListenQuoteConnectionStatus()
        {
            lock (QuoteConnectionStatusDelegateMutex)
            {
                try
                {
                    QuoteConnectionStatusDelegateDict.Remove(this);
                }
                catch (Exception e)
                {
                    Debug.Print(DateTime.Now.ToString() + ": " + this.GetType().ToString() + ": " + Utils.Utils.GetFunctionName() + ": " + e.Message);
                }
            }
        }

        /// <summary>
        /// Get central user settings with keys in the KeyList.
        /// </summary>
        /// <param name="KeyList">List of account nos want to get.</param>
        protected void GetCentralUserSetting(string Key)
        {
            DPCentralUserSetting.Execute(DataProcessor<SettingKeyValue>.Operation.CodeEnum.Get, centralUserSettingDelegateInvoke, Key, null);

            /*
            List<string> existKeyList = null;
            List<string> nonExistKeyList = null;
            List<SettingKeyValue> existSettingList = null;

            if (pTradeDB != null) 
                existSettingList = pTradeDB.GetSettingByKey(KeyList, out nonExistKeyList, GetRefreshTypeEnum.NoRefresh);

            if (existSettingList != null && existSettingList.Count > 0)
            {
                int i, n = existSettingList.Count;
                existKeyList = new List<string>(n);
                for (i = 0; i < n; i++)
                {
                    existKeyList.Add(existSettingList[i].Key);
                }
                CentralUserSettingSubscription.Subscribe(centralUserSettingDelegateInvoke, existKeyList, SubscriptionManager<string, SettingKeyValue>.SubscriptionClassEnum.SnapShot, -1, true);
                CentralUserSettingSubscription.DispatchTo(centralUserSettingDelegateInvoke, existKeyList, existSettingList);
            }

            if (nonExistKeyList.Count > 0)
            {
                existKeyList = CentralUserSettingSubscription.Subscribe(centralUserSettingDelegateInvoke, nonExistKeyList, SubscriptionManager<string, SettingKeyValue>.SubscriptionClassEnum.SnapShot, 0, true); // use zero as SnapShotRefreshInterval because must guarantee the required setting to return at least once.
            }

            if (existKeyList != null && existKeyList.Count > 0)
            {
                if (pTradeDB != null)
                    existSettingList = pTradeDB.GetSettingByKey(KeyList, out nonExistKeyList, GetRefreshTypeEnum.NoRefresh);

                if (existSettingList != null && existSettingList.Count > 0)
                {
                    int i, n = existSettingList.Count;
                    existKeyList = new List<string>(n);
                    for (i = 0; i < n; i++)
                    {
                        existKeyList.Add(existSettingList[i].Key);
                    }
                    CentralUserSettingSubscription.DispatchTo(centralUserSettingDelegateInvoke, existKeyList, existSettingList);
                }
            }
             */
        }

        protected void SetCentralUserSetting(string Key, string Value)
        {
            if (pTradeDB != null)
            {
                pTradeDB.SetSetting(new SettingKeyValue(Key, Value));
            }
        }

        /// <summary>
        /// Listen to central user settings with keys in the KeyList and clear previous listened key(s).
        /// </summary>
        /// <param name="KeyList">List of keys want to listen.</param>
        /*
        protected void ListenCentralUserSetting(List<string> KeyList)
        {
            if (KeyList == null || KeyList.Count <= 0) return;

            List<string> SubscribedKeyList = CentralUserSettingSubscription.Subscribe(centralUserSettingDelegateInvoke, KeyList, SubscriptionManager<string, SettingKeyValue>.SubscriptionClassEnum.SnapShot, 0, false);

            if (SubscribedKeyList != null && SubscribedKeyList.Count > 0)
            {
                List<string> existKeyList = null;
                List<string> nonExistKeyList = null;
                List<SettingKeyValue> existKeyValueList = null;
         
                if (pTradeDB != null) pTradeDB.GetSettingByKey(SubscribedKeyList, out nonExistKeyList, false);

                if (existKeyValueList != null && existKeyValueList.Count > 0)
                {
                    int i, n = existKeyValueList.Count;
                    existKeyList = new List<string>(n);
                    for (i = 0; i < n; i++)
                    {
                        existKeyList.Add(existKeyValueList[i].Key);
                    }
                    CentralUserSettingSubscription.DispatchTo(centralUserSettingDelegateInvoke, existKeyList, existKeyValueList);
                }
            }
        }

        /// <summary>
        /// Stop listening to central user settings with keys in the KeyList.
        /// </summary>
        /// <param name="KeyList">List of keys want to stop listening to. If null then stop listening for all.</param>
        protected void UnListenCentralUserSetting(List<string> KeyList)
        {
            if (KeyList != null && KeyList.Count <= 0) return;

            CentralUserSettingSubscription.UnSubscribe(centralUserSettingDelegateInvoke, KeyList);
        }
         */

        /// <summary>
        /// Listen to orders changes for the orders with order nos in the OrderNoList and clear previous listened order(s).
        /// </summary>
        /// <param name="OrderNoList">List of order nos want to listen.</param>
        protected void ListenOrderStatus(List<string> OrderNoList)
        {
            ListenOrderStatus(OrderNoList, false);
        }

        /// <summary>
        /// Listen to orders changes for the orders with order nos in the OrderNoList.
        /// </summary>
        /// <param name="OrderNoList">List of order nos want to listen.</param>
        /// <param name="PreservePreviousListen">true: Keep previous listened order(s)</param>
        protected void ListenOrderStatus(List<string> OrderNoList, bool PreservePreviousListen)
        {
            DPOrder.Execute(DataProcessor<Order>.Operation.CodeEnum.Listen, orderStatusDelegateInvoke, "", null);

            /*
            if (OrderNoList == null || OrderNoList.Count <= 0) return;

            List<string> SubscribedOrderNoList = OrderNoList;   // because the server is sending all orders without request
            OrderSubscription.Subscribe(orderStatusDelegateInvoke, OrderNoList, SubscriptionManager<string, Order>.SubscriptionClassEnum.Stream, 0, PreservePreviousListen);
            List<int> intOrderNoList = null;
            int intOrderNo;
            bool DispatchAllOrder = false;

            if (SubscribedOrderNoList != null && SubscribedOrderNoList.Count > 0)
            {
                intOrderNoList = new List<int>(SubscribedOrderNoList.Count);
                foreach (string orderNo in SubscribedOrderNoList)
                {
                    if (int.TryParse(orderNo, out intOrderNo))
                    {
                        intOrderNoList.Add(intOrderNo);
                    }
                    else if (orderNo == "")
                    {
                        DispatchAllOrder = true;
                        break;
                    }
                }

                List<string> existOrderNoList = null;
                List<Order> existOrderList = null;

                if (DispatchAllOrder)
                {
                    if (pTradeDB != null) existOrderList = pTradeDB.GetOrder();
                }
                else if (intOrderNoList.Count > 0)
                {
                    if (pTradeDB != null) existOrderList = pTradeDB.GetOrderByOrderNo(intOrderNoList);
                }

                if (existOrderList != null && existOrderList.Count > 0)
                {
                    int i, n = existOrderList.Count;
                    existOrderNoList = new List<string>(n);
                    for (i = 0; i < n; i++)
                    {
                        existOrderNoList.Add(existOrderList[i].OrderNo.ToString());
                    }
                    OrderSubscription.DispatchTo(orderStatusDelegateInvoke, existOrderNoList, existOrderList);
                }
            }
             */
        }

        /// <summary>
        /// Stop listening to accounts changes for the accounts with account nos in the AccountNoList.
        /// </summary>
        /// <param name="AccountNoList">List of account codes want to stop listening to. If null then stop listening for all.</param>
        protected void UnListenOrderStatus(List<string> OrderNoList)
        {
            DPOrder.Execute(DataProcessor<Order>.Operation.CodeEnum.Unlisten, orderStatusDelegateInvoke, null, null);

            /*
            if (OrderNoList != null && OrderNoList.Count <= 0) return;

            OrderSubscription.UnSubscribe(orderStatusDelegateInvoke, OrderNoList);
             */
        }

        /// <summary>
        /// Listen to odd lot orders changes with StockCodeList.
        /// </summary>
        /// <param name="StockCodeList">List of stock code nos want to listen.</param>
        /// <param name="PreservePreviousListen">true: Keep previous listened order(s)</param>
        protected void ListenOddLotOrder(List<string> StockSignatureList, bool PreservePreviousListen)
        {
            if (StockSignatureList != null && StockSignatureList.Count > 0)
            {
                if (!PreservePreviousListen)
                    DPOddLotOrder.Execute(DataProcessor<OddLotOrder>.Operation.CodeEnum.Unlisten, oddLotOrderStatusDelegateInvoke, null, null);

                for (int i = 0; i < StockSignatureList.Count; i++)
                    DPOddLotOrder.Execute(DataProcessor<OddLotOrder>.Operation.CodeEnum.Listen, oddLotOrderStatusDelegateInvoke, StockSignatureList[i], null);
            }

            //if (StockCodeList == null || StockCodeList.Count <= 0) return;

            //List<string> SubscribedStockCodeList = StockCodeList;   // because the server is sending all orders without request
            //OddLotSubscription.Subscribe(oddLotOrderDelegateInvoke, StockCodeList, SubscriptionManager<string, OddLotOrder>.SubscriptionClassEnum.Stream, 0, PreservePreviousListen);
            //List<string> stockCodeSignatureList = new List<string>(StockCodeList);

            //if (SubscribedStockCodeList != null && SubscribedStockCodeList.Count > 0)
            //{
            //    List<OddLotOrder> existOddLotOrderList = null;

            //    //pTradeDB.RequestServerOddLotOder(stockCodeSignatureList);
            //    if (pTradeDB != null)
            //        existOddLotOrderList = pTradeDB.GetOddLotOrderByStockCode(stockCodeSignatureList);

            //    if (existOddLotOrderList != null && existOddLotOrderList.Count > 0)
            //    {
            //        int i, n = existOddLotOrderList.Count;
            //        List<string> existStockCodeList = new List<string>(n);
            //        for (i = 0; i < n; i++)
            //        {
            //            System.Diagnostics.Debug.Print("  BaseForm: Send existing Copy");
            //            existStockCodeList.Add(existOddLotOrderList[i].StockSignature.ToString());
            //            OddLotSubscription.DispatchTo(oddLotOrderDelegateInvoke, new List<string> { existStockCodeList[i] }, new List<OddLotOrder> { existOddLotOrderList[i] });
            //        }
            //    }
            //}
        }

        /// <summary>
        /// Stop listening to accounts changes for the accounts with account nos in the AccountNoList.
        /// </summary>
        /// <param name="AccountNoList">List of account codes want to stop listening to. If null then stop listening for all.</param>
        protected void UnListenOddLotOrder(List<string> StockSignatureList)
        {
            if (StockSignatureList != null && StockSignatureList.Count > 0)
            {
                for (int i = 0; i < StockSignatureList.Count; i++)
                    DPOddLotOrder.Execute(DataProcessor<OddLotOrder>.Operation.CodeEnum.Unlisten, oddLotOrderStatusDelegateInvoke, StockSignatureList[i], null);
            }
            else
                DPOddLotOrder.Execute(DataProcessor<OddLotOrder>.Operation.CodeEnum.Unlisten, oddLotOrderStatusDelegateInvoke, null, null);

            //if (StockCodeList != null && StockCodeList.Count <= 0) return;

            ////pTradeDB.UnRequestServerOddLotOder(StockCodeList);
            //OddLotSubscription.UnSubscribe(oddLotOrderDelegateInvoke, StockCodeList);
        }

        protected void ListenAccountList()
        {
            lock (AccountListDelegateMutex)
            {
                AccountListDelegateDict[this] = accountListDelegateInvoke;
            }

            if (pTradeDB == null) return;

            // if accounts exist then give the list to the listener, since those already exist won't be given to the listner via the delegate invoke
            List<string> accountList = pTradeDB.GetAccountList();

            if (accountList != null && accountList.Count > 0)
            {
                accountListDelegateInvoke.BeginInvoke(accountList, null, null);
            }
            else
            {
                DateTime TimeNow = DateTime.Now;

                if ((TimeNow - AccountListLastRequestTime).TotalSeconds >= 5)
                {
                    pTradeDB.RequestAccountList();
                    AccountListLastRequestTime = TimeNow;
                }
            }
        }

        protected void UnListenAccountList()
        {
            lock (AccountListDelegateMutex)
            {
                AccountListDelegateDict.Remove(this);
            }
        }

        /// <summary>
        /// Get accounts with account nos in the AccountNoList, no refresh if already exist in TradeDB.
        /// </summary>
        /// <param name="AccountNoList">List of account nos want to get.</param>
        protected void GetAccount(List<string> AccountNoList)
        {
            GetAccount(AccountNoList, -1, true);    // just get the account, should not alter existing subscriptions
        }

        /// <summary>
        /// Get accounts with account nos in the AccountNoList and clear previous snapshot refresh account(s).
        /// </summary>
        /// <param name="AccountNoList">List of account nos want to get. Clear all snapshot refresh if null.</param>
        /// <param name="SnapShotRefreshInterval">-1: No refresh if already exist, otherwise refresh once. 0: Force refresh once. >0: Force refresh every of such interval in seconds.</param>
        protected void GetAccount(List<string> AccountNoList, int SnapShotRefreshInterval)
        {
            GetAccount(AccountNoList, SnapShotRefreshInterval, false);
        }

        /// <summary>
        /// Get accounts with account nos in the AccountNoList.
        /// </summary>
        /// <param name="AccountNoList">List of account nos want to get .Clear all snapshot refresh if null.</param>
        /// <param name="SnapShotRefreshInterval">-1: No refresh if already exist, otherwise refresh once. 0: Force refresh once. >0: Force refresh every of such interval in seconds.</param>
        /// <param name="PreservePreviousListen">true: Keep previous snapshot refresh account(s)</param>
        protected void GetAccount(List<string> AccountNoList, int SnapShotRefreshInterval, bool PreservePreviousListen)
        {
            if (AccountNoList != null && AccountNoList.Count > 0)
            {
                for (int i = 0; i < AccountNoList.Count; i++)
                    DPAccount.Execute(DataProcessor<Account>.Operation.CodeEnum.Get, accountDelegateInvoke, AccountNoList[i], null);
            }
            else
                DPAccount.Execute(DataProcessor<Account>.Operation.CodeEnum.Unlisten, accountDelegateInvoke, null, null);

            /*
            List<string> existAccountNoList = null;
            List<string> nonExistAccountNoList = null;
            List<Account> existAccountList = null;

            if (AccountNoList == null || AccountNoList.Count <= 0)
            {
                AccountSubscription.UnSubscribe(accountDelegateInvoke, null);
            }
            if (SnapShotRefreshInterval < 0)
            {
                if (pTradeDB != null)
                    existAccountList = pTradeDB.GetAccountByAccountNo(AccountNoList, out nonExistAccountNoList, GetRefreshTypeEnum.NoRefresh);

                if (existAccountList != null && existAccountList.Count > 0)
                {
                    int i, n = existAccountList.Count;
                    existAccountNoList = new List<string>(n);
                    for (i = 0; i < n; i++)
                    {
                        existAccountNoList.Add(existAccountList[i].AccountNo);
                    }
                    AccountSubscription.Subscribe(accountDelegateInvoke, existAccountNoList, SubscriptionManager<string, Account>.SubscriptionClassEnum.SnapShot, -1, PreservePreviousListen);
                    AccountSubscription.DispatchTo(accountDelegateInvoke, existAccountNoList, existAccountList);
                }

                if (nonExistAccountNoList.Count > 0)
                {
                    existAccountNoList = AccountSubscription.Subscribe(accountDelegateInvoke, nonExistAccountNoList, SubscriptionManager<string, Account>.SubscriptionClassEnum.SnapShot, 0, PreservePreviousListen); // use zero as SnapShotRefreshInterval because must guarantee the required account to return at least once.
                }
            }
            else
            {
                existAccountNoList = AccountSubscription.Subscribe(accountDelegateInvoke, AccountNoList, SubscriptionManager<string, Account>.SubscriptionClassEnum.SnapShot, SnapShotRefreshInterval, PreservePreviousListen);
            }

            if (existAccountNoList != null && existAccountNoList.Count > 0)
            {
                if (pTradeDB != null)
                    existAccountList = pTradeDB.GetAccountByAccountNo(existAccountNoList, out nonExistAccountNoList, GetRefreshTypeEnum.NoRefresh);

                if (existAccountList != null && existAccountList.Count > 0)
                {
                    int i, n = existAccountList.Count;
                    existAccountNoList = new List<string>(n);
                    for (i = 0; i < n; i++)
                    {
                        existAccountNoList.Add(existAccountList[i].AccountNo);
                    }
                    AccountSubscription.DispatchTo(accountDelegateInvoke, existAccountNoList, existAccountList);
                }
            }
             */
        }

        /// <summary>
        /// Listen to accounts changes for the accounts with account nos in the AccountNoList and clear previous listened account(s).
        /// </summary>
        /// <param name="AccountNoList">List of account nos want to listen.</param>
        protected void ListenAccount(List<string> AccountNoList)
        {
            ListenAccount(AccountNoList, false);
        }

        /// <summary>
        /// Listen to accounts changes for the accounts with account nos in the AccountNoList.
        /// </summary>
        /// <param name="AccountNoList">List of account nos want to listen.</param>
        /// <param name="PreservePreviousListen">true: Keep previous listened account(s)</param>
        protected void ListenAccount(List<string> AccountNoList, bool PreservePreviousListen)
        {
            if (AccountNoList != null && AccountNoList.Count > 0)
            {
                if (!PreservePreviousListen)
                    DPAccount.Execute(DataProcessor<Account>.Operation.CodeEnum.Unlisten, accountDelegateInvoke, null, null);

                for (int i = 0; i < AccountNoList.Count; i++)
                    DPAccount.Execute(DataProcessor<Account>.Operation.CodeEnum.Listen, accountDelegateInvoke, AccountNoList[i], null);
            }

            /*
            if (AccountNoList == null || AccountNoList.Count <= 0) return;

            List<string> SubscribedAccountNoList = AccountSubscription.Subscribe(accountDelegateInvoke, AccountNoList, SubscriptionManager<string, Account>.SubscriptionClassEnum.Stream, 0, PreservePreviousListen);

            if (SubscribedAccountNoList != null && SubscribedAccountNoList.Count > 0)
            {
                List<string> existAccountNoList = null;
                List<string> nonExistAccountNoList = null;
                List<Account> existAccountList = null;
                
                if (pTradeDB != null)
                    existAccountList = pTradeDB.GetAccountByAccountNo(SubscribedAccountNoList, out nonExistAccountNoList, GetRefreshTypeEnum.NoRefresh);

                if (existAccountList != null && existAccountList.Count > 0)
                {
                    int i, n = existAccountList.Count;
                    existAccountNoList = new List<string>(n);
                    for (i = 0; i < n; i++)
                    {
                        existAccountNoList.Add(existAccountList[i].AccountNo);
                    }
                    AccountSubscription.DispatchTo(accountDelegateInvoke, existAccountNoList, existAccountList);
                }
            }
             */
        }

        /// <summary>
        /// Stop listening to accounts changes for the accounts with account nos in the AccountNoList.
        /// </summary>
        /// <param name="AccountNoList">List of account codes want to stop listening to. If null then stop listening for all.</param>
        protected void UnListenAccount(List<string> AccountNoList)
        {
            if (AccountNoList != null && AccountNoList.Count > 0)
            {
                for (int i = 0; i < AccountNoList.Count; i++)
                    DPAccount.Execute(DataProcessor<Account>.Operation.CodeEnum.Unlisten, accountDelegateInvoke, AccountNoList[i], null);
            }
            else
                DPAccount.Execute(DataProcessor<Account>.Operation.CodeEnum.Unlisten, accountDelegateInvoke, null, null);

            /*
            if (AccountNoList != null && AccountNoList.Count <= 0) return;

            AccountSubscription.UnSubscribe(accountDelegateInvoke, AccountNoList);
             */
        }

        protected List<AccountExecutive> GetAEByCode(string Code)
        {
            if (pTradeDB != null && Code != null)
            {
                return pTradeDB.GetAEByCode(Code);
            }
            return null;
        }

        protected void ListenAE()
        {
            lock (AEDelegateMutex)
            {
                AEDelegateDict[this] = aeDelegateInvoke;
            }

            if (pTradeDB == null) return;

            // if accounts exist then give the list to the listener, since those already exist won't be given to the listner via the delegate invoke
            List<AccountExecutive> aeList = pTradeDB.GetAEList();

            if (aeList != null && aeList.Count > 0)
            {
                aeDelegateInvoke.BeginInvoke(aeList, null, null);
            }
            else
            {
                DateTime TimeNow = DateTime.Now;

                if ((TimeNow - AccountListLastRequestTime).TotalSeconds >= 5)
                {
                    pTradeDB.RequestAccountList();
                    AccountListLastRequestTime = TimeNow;
                }
            }
        }

        protected void UnListenAE()
        {
            lock (AEDelegateMutex)
            {
                AEDelegateDict.Remove(this);
            }
        }

        protected Dictionary<string, Stock> GetStockLocal(ICollection<string> StockSignatures)
        {
            return TradeDB.GetStock(StockSignatures);
        }

        /// <summary>
        /// Get stocks with stock codes in the StockCodeList, no refresh if already exist in TradeDB and clear previous snapshot refresh account(s).
        /// </summary>
        /// <param name="StockSignatureList">List of stock signatures want to get. Clear all snapshot refresh if null.</param>
        protected void GetStock(List<string> StockSignatureList)
        {
            GetStock(StockSignatureList, -1, false);    // just get the stock, should not alter existing subscriptions
        }

        /// <summary>
        /// Get stocks with stock codes in the StockCodeList and clear previous snapshot refresh account(s).
        /// </summary>
        /// <param name="StockSignatureList">List of stock signatures want to get. Clear all snapshot refresh if null.</param>
        /// <param name="SnapShotRefreshInterval">-1: No refresh if already exist, otherwise refresh once. 0: Force refresh once. >0: Force refresh every of such interval in seconds.</param>
        protected void GetStock(List<string> StockSignatureList, int SnapShotRefreshInterval)
        {
            GetStock(StockSignatureList, SnapShotRefreshInterval, false);
        }

        /// <summary>
        /// Get stocks with stock codes in the StockCodeList.
        /// </summary>
        /// <param name="StockSignatureList">List of stock signatures want to get. Clear all snapshot refresh if null.</param>
        /// <param name="ExTypeList">List of exchange type refer to stock code list. Do nothing if not same size as stock code list. Clear all snapshot refresh if null.</param>
        /// <param name="SnapShotRefreshInterval">-1: No refresh if already exist, otherwise refresh once. 0: Force refresh once. >0: Force refresh every of such interval in seconds.</param>
        /// <param name="PreservePreviousListen">true: Keep previous snapshot refresh stock(s)</param>
        protected void GetStock(List<string> StockSignatureList, int SnapShotRefreshInterval, bool PreservePreviousListen)
        {
            if (StockSignatureList != null && StockSignatureList.Count > 0)
            {
                if (!PreservePreviousListen)
                    DPStock.Execute(DataProcessor<Stock>.Operation.CodeEnum.Unlisten, stockDelegateInvoke, null, null);

                for (int i = 0; i < StockSignatureList.Count; i++)
                    DPStock.Execute(DataProcessor<Stock>.Operation.CodeEnum.Get, stockDelegateInvoke, StockSignatureList[i], null);
            }
            else
                DPStock.Execute(DataProcessor<Stock>.Operation.CodeEnum.Unlisten, stockDelegateInvoke, null, null);

            /*
            List<string> existStockCodeList = null;
            List<string> nonExistStockCodeList = null;
            List<Stock> existStockList = null;

            if (StockSignatureList == null || StockSignatureList.Count <= 0)
            {
                StockSubscription.UnSubscribe(stockDelegateInvoke, null);
            }
            else
            {
                List<string> CombinedList = new List<string>(StockSignatureList);
                if (SnapShotRefreshInterval < 0)
                {
                    if (pTradeDB != null)
                        existStockList = pTradeDB.GetStock(CombinedList, out nonExistStockCodeList, GetRefreshTypeEnum.NoRefresh, GetStockInfoType.Basic, out existStockCodeList);
                        //existStockList = pTradeDB.GetStock(StockCodeList, out nonExistStockCodeList, GetRefreshTypeEnum.NoRefresh, GetStockInfoType.Basic);

                    if (existStockList != null && existStockList.Count > 0)
                    {
                        //int i, n = existStockList.Count;
                        //existStockCodeList = new List<string>(n);
                        //for (i = 0; i < n; i++)
                        //{
                        //    existStockCodeList.Add(existStockList[i].Code);
                        //}
                        StockSubscription.Subscribe(stockDelegateInvoke, existStockCodeList, SubscriptionManager<string, Stock>.SubscriptionClassEnum.SnapShot, -1, PreservePreviousListen);
                        StockSubscription.DispatchTo(stockDelegateInvoke, existStockCodeList, existStockList);
                    }

                    if (nonExistStockCodeList.Count > 0)
                    {
                        existStockCodeList = StockSubscription.Subscribe(stockDelegateInvoke, nonExistStockCodeList, SubscriptionManager<string, Stock>.SubscriptionClassEnum.SnapShot, 0, PreservePreviousListen);   // use zero as SnapShotRefreshInterval because must guarantee the required stock to return at least once.
                    }
                }
                else
                {
                    existStockCodeList = StockSubscription.Subscribe(stockDelegateInvoke, CombinedList, SubscriptionManager<string, Stock>.SubscriptionClassEnum.SnapShot, SnapShotRefreshInterval, PreservePreviousListen);
                    //existStockCodeList = StockSubscription.Subscribe(stockDelegateInvoke, StockCodeList, SubscriptionManager<string, Stock>.SubscriptionClassEnum.SnapShot, SnapShotRefreshInterval, PreservePreviousListen);
                }

                if (existStockCodeList != null && existStockCodeList.Count > 0)
                {
                    if (pTradeDB != null)
                        existStockList = pTradeDB.GetStock(existStockCodeList, out nonExistStockCodeList, GetRefreshTypeEnum.NoRefresh, GetStockInfoType.Basic, out existStockCodeList);

                    if (existStockList != null && existStockList.Count > 0)
                    {
                        //int i, n = existStockList.Count;
                        //existStockCodeList = new List<string>(n);
                        //for (i = 0; i < n; i++)
                        //{
                        //    existStockCodeList.Add(existStockList[i].Code);
                        //}
                        StockSubscription.DispatchTo(stockDelegateInvoke, existStockCodeList, existStockList);
                    }
                }
            }
             */
        }

        /// <summary>
        /// Listen to stocks changes for the stocks with stock codes in the StockCodeList and clear previous listened stock(s).
        /// </summary>
        /// <param name="StockSignatureList">List of stock signatures want to listen.</param>
        protected void ListenStock(List<string> StockSignatureList)
        {
            ListenStock(StockSignatureList, false);
        }

        /// <summary>
        /// Listen to stocks changes for the stocks with stock codes in the StockCodeList.
        /// </summary>
        /// <param name="StockSignatureList">List of stock signatures want to listen.</param>
        /// <param name="PreservePreviousListen">true: Keep previous listened stock(s)</param>
        protected void ListenStock(List<string> StockSignatureList, bool PreservePreviousListen)
        {
            if (StockSignatureList != null && StockSignatureList.Count > 0)
            {
                if (!PreservePreviousListen)
                    DPStock.Execute(DataProcessor<Stock>.Operation.CodeEnum.Unlisten, stockDelegateInvoke, null, null);

                for (int i = 0; i < StockSignatureList.Count; i++)
                    DPStock.Execute(DataProcessor<Stock>.Operation.CodeEnum.Listen, stockDelegateInvoke, StockSignatureList[i], null);
            }
            else
                DPStock.Execute(DataProcessor<Stock>.Operation.CodeEnum.Unlisten, stockDelegateInvoke, null, null);

            /*
            if (StockSignatureList == null || StockSignatureList.Count <= 0) 
                return;

            List<string> CombinedList = new List<string>(StockSignatureList);
            List<string> SubscribedStockCodeList = StockSubscription.Subscribe(stockDelegateInvoke, CombinedList, SubscriptionManager<string, Stock>.SubscriptionClassEnum.Stream, 0, PreservePreviousListen);
            //List<string> SubscribedStockCodeList = StockSubscription.Subscribe(stockDelegateInvoke, StockCodeList, SubscriptionManager<string, Stock>.SubscriptionClassEnum.Stream, 0, PreservePreviousListen);

            if (SubscribedStockCodeList != null && SubscribedStockCodeList.Count > 0)
            {
                List<string> existStockCodeList = null;
                List<string> nonExistStockCodeList = null;
                List<Stock> existStockList = null;
                
                if (pTradeDB != null)
                    existStockList = pTradeDB.GetStock(SubscribedStockCodeList, out nonExistStockCodeList, GetRefreshTypeEnum.NoRefresh, GetStockInfoType.BasicAndPriceDepth, out existStockCodeList);

                if (existStockList != null && existStockList.Count > 0)
                {
                    //int i, n = existStockList.Count;
                    //existStockCodeList = new List<string>(n);
                    //for (i = 0; i < n; i++)
                    //{
                    //    existStockCodeList.Add(existStockList[i].Code);
                    //}
                    StockSubscription.DispatchTo(stockDelegateInvoke, existStockCodeList, existStockList);
                }
            }
             */
        }

        /// <summary>
        /// Stop listening to stocks changes for the stocks with stock codes in the StockCodeList.
        /// </summary>
        /// <param name="StockSignatureList">List of stock signatures want to stop listening to. If null then stop listening for all.</param>
        protected void UnListenStock(List<string> StockSignatureList)
        {
            if (StockSignatureList != null && StockSignatureList.Count > 0)
            {
                for (int i = 0; i < StockSignatureList.Count; i++)
                    DPStock.Execute(DataProcessor<Stock>.Operation.CodeEnum.Unlisten, stockDelegateInvoke, StockSignatureList[i], null);
            }
            else
                DPStock.Execute(DataProcessor<Stock>.Operation.CodeEnum.Unlisten, stockDelegateInvoke, null, null);

            /*
            if (StockSignatureList != null && StockSignatureList.Count <= 0) return;

            StockSubscription.UnSubscribe(stockDelegateInvoke, StockSignatureList);
             */
        }

        /// <summary>
        /// Listen to indexes changes for the indexes with index codes in the IndexCodeList and clear previous listened index(s).
        /// </summary>
        /// <param name="IndexCodeList">List of index codes want to listen.</param>
        protected void ListenIndex(List<string> IndexCodeList)
        {
            ListenIndex(IndexCodeList, false);
        }

        /// <summary>
        /// Listen to indexes changes for the indexes with index codes in the IndexCodeList.
        /// </summary>
        /// <param name="IndexCodeList">List of index codes want to listen.</param>
        /// <param name="PreservePreviousListen">true: Keep previous listened index(s)</param>
        protected void ListenIndex(List<string> IndexCodeList, bool PreservePreviousListen)
        {
            if (IndexCodeList != null && IndexCodeList.Count > 0)
            {
                if (!PreservePreviousListen)
                    DPIndex.Execute(DataProcessor<Index>.Operation.CodeEnum.Unlisten, indexDelegateInvoke, null, null);

                for (int i = 0; i < IndexCodeList.Count; i++)
                    DPIndex.Execute(DataProcessor<Index>.Operation.CodeEnum.Listen, indexDelegateInvoke, IndexCodeList[i], null);
            }

            /*
            if (IndexCodeList == null || IndexCodeList.Count <= 0) return;

            List<string> SubscribedIndexCodeList = IndexSubscription.Subscribe(indexDelegateInvoke, IndexCodeList, SubscriptionManager<string, Index>.SubscriptionClassEnum.Stream, 0, PreservePreviousListen);

            if (SubscribedIndexCodeList != null && SubscribedIndexCodeList.Count > 0)
            {
                List<string> existIndexCodeList = null;
                List<string> nonExistIndexCodeList = null;
                List<Index> existIndexList = null;

                if (pTradeDB != null)
                    existIndexList = pTradeDB.GetIndex(SubscribedIndexCodeList, out nonExistIndexCodeList, GetRefreshTypeEnum.NoRefresh);

                if (existIndexList != null && existIndexList.Count > 0)
                {
                    int i, n = existIndexList.Count;
                    existIndexCodeList = new List<string>(n);
                    for (i = 0; i < n; i++)
                    {
                        existIndexCodeList.Add(existIndexList[i].Code);
                    }
                    IndexSubscription.DispatchTo(indexDelegateInvoke, existIndexCodeList, existIndexList);
                }
            }
             */
        }

        /// <summary>
        /// Stop listening to indexes changes for the indexes with index codes in the IndexCodeList.
        /// </summary>
        /// <param name="IndexCodeList">List of index codes want to stop listening to. If null then stop listening for all.</param>
        protected void UnListenIndex(List<string> IndexCodeList)
        {
            if (IndexCodeList != null && IndexCodeList.Count > 0)
            {
                for (int i = 0; i < IndexCodeList.Count; i++)
                    DPIndex.Execute(DataProcessor<Index>.Operation.CodeEnum.Unlisten, indexDelegateInvoke, IndexCodeList[i], null);
            }
            else
                DPIndex.Execute(DataProcessor<Index>.Operation.CodeEnum.Unlisten, indexDelegateInvoke, null, null);

            /*
            if (IndexCodeList != null && IndexCodeList.Count <= 0) return;

            IndexSubscription.UnSubscribe(indexDelegateInvoke, IndexCodeList);
             */
        }

        /// <summary>
        /// Listen to market turnover changes for the markets with market codes in the MarketCodeList and clear previous listened index(s).
        /// </summary>
        /// <param name="MarketCodeList">List of market codes want to listen.</param>
        protected void ListenMarketTurnover(List<string> MarketCodeList)
        {
            ListenMarketTurnover(MarketCodeList, false);
        }

        /// <summary>
        /// Listen to market turnover changes for the markets with market codes in the MarketCodeList.
        /// </summary>
        /// <param name="MarketCodeList">List of market codes want to listen.</param>
        /// <param name="PreservePreviousListen">true: Keep previous listened market turnover(s)</param>
        protected void ListenMarketTurnover(List<string> MarketCodeList, bool PreservePreviousListen)
        {
            if (MarketCodeList != null && MarketCodeList.Count > 0)
            {
                if (!PreservePreviousListen)
                    DPMarketTurnover.Execute(DataProcessor<MarketTurnover>.Operation.CodeEnum.Unlisten, marketTurnoverDelegateInvoke, null, null);

                for (int i = 0; i < MarketCodeList.Count; i++)
                    DPMarketTurnover.Execute(DataProcessor<MarketTurnover>.Operation.CodeEnum.Listen, marketTurnoverDelegateInvoke, MarketCodeList[i], null);
            }
        }

        /// <summary>
        /// Stop listening to market turnover changes for the markets with market codes in the MarketCodeList.
        /// </summary>
        /// <param name="MarketCodeList">List of market codes want to stop listening to. If null then stop listening for all.</param>
        protected void UnListenMarketTurnover(List<string> MarketCodeList)
        {
            if (MarketCodeList != null && MarketCodeList.Count > 0)
            {
                for (int i = 0; i < MarketCodeList.Count; i++)
                    DPMarketTurnover.Execute(DataProcessor<MarketTurnover>.Operation.CodeEnum.Unlisten, marketTurnoverDelegateInvoke, MarketCodeList[i], null);
            }
            else
                DPMarketTurnover.Execute(DataProcessor<MarketTurnover>.Operation.CodeEnum.Unlisten, marketTurnoverDelegateInvoke, null, null);
        }

        protected void GetTransactionCharge(TransactionCharge transChargeRequest)
        {
            DPTxnCharge.Execute(DataProcessor<TransactionCharge>.Operation.CodeEnum.Get, transactionChargeDelegateInvoke, transChargeRequest.Hash, null);

            /*
            List<string> existTransactionChargeCodeList = null;
            List<string> nonExistTransactionChargeCodeList = null;
            List<TransactionCharge> existTransactionChargeList = null;
            if (pTradeDB != null)
            {
                existTransactionChargeList = pTradeDB.GetTransactionCharge(
                    new List<string> { TransactionCharge.GetHash(transChargeRequest) },
                    out nonExistTransactionChargeCodeList, GetRefreshTypeEnum.NoRefresh);

                if (existTransactionChargeList.Count > 0)
                {
                    existTransactionChargeCodeList = new List<string>(existTransactionChargeList.Count);
                    foreach (TransactionCharge tc in existTransactionChargeList)
                    {
                        existTransactionChargeCodeList.Add(tc.Hash);
                    }
                    TransactionChargeSubscription.Subscribe(transactionChargeDelegateInvoke, existTransactionChargeCodeList, SubscriptionManager<string, TransactionCharge>.SubscriptionClassEnum.SnapShot, -1, true);
                    TransactionChargeSubscription.DispatchTo(transactionChargeDelegateInvoke, existTransactionChargeCodeList, existTransactionChargeList);
                }

                if (nonExistTransactionChargeCodeList.Count > 0)
                {
                    existTransactionChargeCodeList = TransactionChargeSubscription.Subscribe(transactionChargeDelegateInvoke, nonExistTransactionChargeCodeList, SubscriptionManager<string, TransactionCharge>.SubscriptionClassEnum.SnapShot, 0, true);
                }
            }

            if (existTransactionChargeCodeList != null && existTransactionChargeCodeList.Count > 0)
            {
                existTransactionChargeList = pTradeDB.GetTransactionCharge(
                    new List<string> { TransactionCharge.GetHash(transChargeRequest) },
                    out nonExistTransactionChargeCodeList, GetRefreshTypeEnum.NoRefresh);

                if (existTransactionChargeList.Count > 0)
                {
                    existTransactionChargeCodeList = new List<string>(existTransactionChargeList.Count);
                    foreach (TransactionCharge tc in existTransactionChargeList)
                    {
                        existTransactionChargeCodeList.Add(tc.Hash);
                    }
                    TransactionChargeSubscription.DispatchTo(transactionChargeDelegateInvoke, existTransactionChargeCodeList, existTransactionChargeList);
                }
            }
             */
        }

        private void AttachListenTradeDB()
        {
            if (pTradeDB != null)
            {
                pTradeDB.LogFolder = LogFolder;
                pTradeDB.ListenSystemEvent(systemEventDelegateStatic);
                pTradeDB.ListenConnectionStatus(connectionStatusDelegateStatic);
                pTradeDB.ListenQuoteConnectionStatus(quoteConnectionStatusDelegateStatic);
                //pTradeDB.ListenCentralUserSetting(centralUserSettingDelegateStatic);
                pTradeDB.DPCentralUserSetting = DPCentralUserSetting;
                //pTradeDB.ListenOrderStatus(orderStatusDelegateStatic);
                pTradeDB.DPOrder = DPOrder;
                pTradeDB.ListenAccountList(accountListDelegateStatic);
                //pTradeDB.ListenAccount(accountDelegateStatic);
                pTradeDB.DPAccount = DPAccount;
                pTradeDB.ListenAE(aeDelegateStatic);
                //pTradeDB.ListenStock(stockDelegateStatic);
                pTradeDB.DPStock = DPStock;
                //pTradeDB.ListenIndex(indexDelegateStatic);
                pTradeDB.DPIndex = DPIndex;
                pTradeDB.DPMarketTurnover = DPMarketTurnover;
                //pTradeDB.ListenTransactionCharge(transactionChargeDelegateStatic);
                pTradeDB.DPTxnCharge = DPTxnCharge;
                //pTradeDB.ListenOddLotOrder(oddLotOrderDelegateStatic);
                pTradeDB.DPOddLotOrder = DPOddLotOrder;

                DPCentralUserSetting.GetData = new DataProcessor<SettingKeyValue>.GetDataDelegate(pTradeDB.GetSetting);
                DPCentralUserSetting.RequestData = new DataProcessor<SettingKeyValue>.RequestDataDelegate(pTradeDB.RequestSetting);
                DPCentralUserSetting.UnrequestData = new DataProcessor<SettingKeyValue>.UnrequestDataDelegate(pTradeDB.UnrequestSetting);

                DPAccount.GetData = new DataProcessor<Account>.GetDataDelegate(pTradeDB.GetAccount);
                DPAccount.RequestData = new DataProcessor<Account>.RequestDataDelegate(pTradeDB.RequestAccount);
                DPAccount.UnrequestData = new DataProcessor<Account>.UnrequestDataDelegate(pTradeDB.UnrequestAccount);

                DPStock.GetData = new DataProcessor<Stock>.GetDataDelegate(pTradeDB.GetStock);
                DPStock.RequestData = new DataProcessor<Stock>.RequestDataDelegate(pTradeDB.RequestStock);
                DPStock.UnrequestData = new DataProcessor<Stock>.UnrequestDataDelegate(pTradeDB.UnrequestStock);

                DPIndex.GetData = new DataProcessor<Index>.GetDataDelegate(pTradeDB.GetIndex);
                DPIndex.RequestData = new DataProcessor<Index>.RequestDataDelegate(pTradeDB.RequestIndex);
                DPIndex.UnrequestData = new DataProcessor<Index>.UnrequestDataDelegate(pTradeDB.UnrequestIndex);

                DPTxnCharge.GetData = new DataProcessor<TransactionCharge>.GetDataDelegate(pTradeDB.GetTransactionCharge);
                DPTxnCharge.RequestData = new DataProcessor<TransactionCharge>.RequestDataDelegate(pTradeDB.RequestTransactionCharge);
                DPTxnCharge.UnrequestData = new DataProcessor<TransactionCharge>.UnrequestDataDelegate(pTradeDB.UnrequestTransactionCharge);

                //DPOddLotOrder.GetData = new DataProcessor<OddLotOrder>.GetDataDelegate(pTradeDB.GetOddLotOrder);
                DPOddLotOrder.RequestData = new DataProcessor<OddLotOrder>.RequestDataDelegate(pTradeDB.RequestServerOddLotOder);
                DPOddLotOrder.UnrequestData = new DataProcessor<OddLotOrder>.UnrequestDataDelegate(pTradeDB.UnRequestServerOddLotOder);
            }
        }

        private void DetachListenTradeDB()
        {
            if (pTradeDB != null)
            {
                pTradeDB.UnListenSystemEvent(systemEventDelegateStatic);
                pTradeDB.UnListenConnectionStatus(connectionStatusDelegateStatic);
                pTradeDB.UnListenQuoteConnectionStatus(quoteConnectionStatusDelegateStatic);
                //pTradeDB.UnListenCentralUserSetting(centralUserSettingDelegateStatic);
                pTradeDB.DPCentralUserSetting = null;
                //pTradeDB.UnListenOrderStatus(orderStatusDelegateStatic);
                pTradeDB.DPOrder = null;
                pTradeDB.UnListenAccountList(accountListDelegateStatic);
                //pTradeDB.UnListenAccount(accountDelegateStatic);
                pTradeDB.DPAccount = null;
                pTradeDB.UnListenAE(aeDelegateStatic);
                //pTradeDB.UnListenStock(stockDelegateStatic);
                pTradeDB.DPStock = null;
                //pTradeDB.UnListenIndex(indexDelegateStatic);
                pTradeDB.DPIndex = null;
                //pTradeDB.UnListenTransactionCharge(transactionChargeDelegateStatic);
                pTradeDB.DPTxnCharge = null;
                //pTradeDB.UnListenOddLotOrder(oddLotOrderDelegateStatic);
                pTradeDB.DPOddLotOrder = null;

                DPAccount.GetData = null;
                DPAccount.RequestData = null;
                DPAccount.UnrequestData = null;

                DPStock.GetData = null;
                DPStock.RequestData = null;
                DPStock.UnrequestData = null;

                DPIndex.GetData = null;
                DPIndex.RequestData = null;
                DPIndex.UnrequestData = null;

                DPTxnCharge.GetData = null;
                DPTxnCharge.RequestData = null;
                DPTxnCharge.UnrequestData = null;

                DPOddLotOrder.GetData = null;
                DPOddLotOrder.RequestData = null;
                DPOddLotOrder.UnrequestData = null;
            }
        }

        #endregion

        #region "Outbound Delegates"

        private void OnCultureChangeInvoke(CultureInfo Culture)
        {
            try
            {
                this.BeginInvoke(pCultureChangeDelegate, Culture);
            }
            catch (ObjectDisposedException exp)
            {
                AppendLog("OnCultureChangeInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
            }
            catch (InvalidOperationException exp)
            {
                AppendLog("OnCultureChangeInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
            }
            catch (Exception exp)
            {
                AppendLog("OnCultureChangeInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
                throw exp;
            }
        }

        protected virtual void OnCultureChange(CultureInfo Culture)
        {
        }

        protected static void OnFormListChangeStatic()
        {
            // copy delegates to the array before calling invoke to prevent deadlock
            FormListChangeDelegate[] arr = null;
            lock (FormListChangeDelegateMutex)
            {
                arr = new FormListChangeDelegate[FormListChangeDelegateDict.Count];
                FormListChangeDelegateDict.Values.CopyTo(arr, 0);
            }

            foreach (FormListChangeDelegate d in arr)
            {
                d.Invoke();
            }
        }

        protected virtual void OnFormListChange()
        {
        }

        private static void OnSystemEventStatic(SystemEvent TheSystemEvent)
        {
            // copy delegates to the array before calling invoke to prevent deadlock
            SystemEventDelegate[] arr = null;
            lock (SystemEventDelegateMutex)
            {
                arr = new SystemEventDelegate[SystemEventDelegateDict.Count];
                SystemEventDelegateDict.Values.CopyTo(arr, 0);
            }

            foreach (SystemEventDelegate d in arr)
            {
                d.Invoke(TheSystemEvent);
            }
        }

        private void OnSystemEventInvoke(SystemEvent TheSystemEvent)
        {
            try
            {
                this.BeginInvoke(systemEventDelegate, TheSystemEvent);
            }
            catch (ObjectDisposedException exp)
            {
                AppendLog("OnSystemEventInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
            }
            catch (InvalidOperationException exp)
            {
                AppendLog("OnSystemEventInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
            }
            catch (Exception exp)
            {
                AppendLog("OnSystemEventInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
                throw exp;
            }
        }

        protected virtual void OnSystemEvent(SystemEvent TheSystemEvent)
        {
        }

        private static void OnConnectionStatusStatic(int State)
        {
            // copy delegates to the array before calling invoke to prevent deadlock
            StateChangedDelegate[] arr = null;
            lock (ConnectionStatusDelegateMutex)
            {
                arr = new StateChangedDelegate[ConnectionStatusDelegateDict.Count];
                ConnectionStatusDelegateDict.Values.CopyTo(arr, 0);
            }

            foreach (StateChangedDelegate d in arr)
            {
                d.Invoke(State);
            }
        }

        private static void OnQuoteConnectionStatusStatic(int State)
        {
            // copy delegates to the array before calling invoke to prevent deadlock
            StateChangedDelegate[] arr = null;
            lock (QuoteConnectionStatusDelegateMutex)
            {
                arr = new StateChangedDelegate[QuoteConnectionStatusDelegateDict.Count];
                QuoteConnectionStatusDelegateDict.Values.CopyTo(arr, 0);
            }

            foreach (StateChangedDelegate d in arr)
            {
                d.Invoke(State);
            }
        }

        private void OnConnectionStatusInvoke(int State)
        {
            try
            {
                this.BeginInvoke(connectionStatusDelegate, State);
            }
            catch (ObjectDisposedException exp)
            {
                AppendLog("OnConnectionStatusInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
            }
            catch (InvalidOperationException exp)
            {
                AppendLog("OnConnectionStatusInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
            }
            catch (Exception exp)
            {
                AppendLog("OnConnectionStatusInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
                throw exp;
            }
        }

        protected virtual void OnConnectionStatus(int State)
        {
        }

        private void OnQuoteConnectionStatusInvoke(int State)
        {
            try
            {
                this.BeginInvoke(quoteConnectionStatusDelegate, State);
            }
            catch (ObjectDisposedException exp)
            {
                AppendLog("OnQuoteConnectionStatusInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
            }
            catch (InvalidOperationException exp)
            {
                AppendLog("OnQuoteConnectionStatusInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
            }
            catch (Exception exp)
            {
                AppendLog("OnQuoteConnectionStatusInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
                throw exp;
            }
        }

        protected virtual void OnQuoteConnectionStatus(int State)
        {
        }

        /*
        private static void OnCentralUserSettingStatic(List<SettingKeyValue> KeyValues)
        {
            if (KeyValues == null || KeyValues.Count <= 0) return;

            int i, n = KeyValues.Count;
            List<string> KeyList = new List<string>(n);

            for (i = 0; i < n; i++)
            {
                KeyList.Add(KeyValues[i].Key);
            }

            CentralUserSettingSubscription.Dispatch(KeyList, KeyValues);
        }
         */

        private void OnCentralUserSettingInvoke(SettingKeyValue KeyValue)
        {
            try
            {
                this.BeginInvoke(centralUserSettingDelegate, KeyValue);
            }
            catch (ObjectDisposedException exp)
            {
                AppendLog("OnCentralUserSettingInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
            }
            catch (InvalidOperationException exp)
            {
                AppendLog("OnCentralUserSettingInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
            }
            catch (Exception exp)
            {
                AppendLog("OnCentralUserSettingInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
                throw exp;
            }
        }

        protected virtual void OnCentralUserSetting(SettingKeyValue KeyValue)
        {
        }

        /*
        private static void OnOrderStatusStatic(List<Order> Orders)
        {
            if (Orders == null || Orders.Count <= 0) return;

            int i, n = Orders.Count;
            List<string> OrderNoList = new List<string>(n);

            for (i = 0; i < n; i++)
            {
                OrderNoList.Add(Orders[i].OrderNo.ToString());
            }

            OrderSubscription.Dispatch(OrderNoList, Orders);
        }
         */

        private void OnOrderStatusInvoke(Order TheOrder)
        {
            try
            {
                this.BeginInvoke(orderStatusDelegate, TheOrder);
            }
            catch (ObjectDisposedException exp)
            {
                AppendLog("OnOrderStatusInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
            }
            catch (InvalidOperationException exp)
            {
                AppendLog("OnOrderStatusInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
            }
            catch (Exception exp)
            {
                AppendLog("OnOrderStatusInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
                throw exp;
            }
        }

        protected virtual void OnOrderStatus(Order TheOrder)
        {
        }

        //private static void OnOddLotOrderStatic(List<OddLotOrder> Orders)
        //{
        //    if (Orders == null || Orders.Count <= 0) return;

        //    int i, n = Orders.Count;
        //    List<string> OrderNoList = new List<string>(n);

        //    for (i = 0; i < n; i++)
        //    {
        //        OrderNoList.Add(Orders[i].StockSignature.ToString());
        //    }

        //    OddLotSubscription.Dispatch(OrderNoList, Orders);
        //}

        private void OnOddLotOrderStatusInvoke(OddLotOrder Orders)
        {
            try
            {
                this.BeginInvoke(oddLotOrderStatusDelegate, Orders);
            }
            catch (ObjectDisposedException exp)
            {
                AppendLog("OnOddLotOrderStatusInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
            }
            catch (InvalidOperationException exp)
            {
                AppendLog("OnOddLotOrderStatusInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
            }
            catch (Exception exp)
            {
                AppendLog("OnOddLotOrderStatusInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
                throw exp;
            }
        }

        protected virtual void OnOddLotOrderStatus(OddLotOrder OddLotOrderList)
        {
        }

        private static void OnAccountListStatic(List<string> AccountList)
        {
            // copy delegates to the array before calling invoke to prevent deadlock
            AccountListDelegate[] arr = null;
            lock (AccountListDelegateMutex)
            {
                arr = new AccountListDelegate[AccountListDelegateDict.Count];
                AccountListDelegateDict.Values.CopyTo(arr, 0);
            }

            foreach (AccountListDelegate d in arr)
            {
                d.Invoke(AccountList);
            }
        }

        private void OnAccountListInvoke(List<string> AccountList)
        {
            try
            {
                this.BeginInvoke(accountListDelegate, AccountList);
            }
            catch (ObjectDisposedException exp)
            {
                AppendLog("OnAccountListInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
            }
            catch (InvalidOperationException exp)
            {
                AppendLog("OnAccountListInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
            }
            catch (Exception exp)
            {
                AppendLog("OnAccountListInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
                throw exp;
            }
        }

        protected virtual void OnAccountList(List<string> AccountList)
        {
        }

        /*
        private static void OnAccountStatic(List<Account> Accounts)
        {
            if (Accounts == null || Accounts.Count <= 0) return;

            int i, n = Accounts.Count;
            List<string> AccountNoList = new List<string>(n);

            for (i = 0; i < n; i++)
            {
                AccountNoList.Add(Accounts[i].AccountNo);
            }

            AccountSubscription.Dispatch(AccountNoList, Accounts);
        }
         */

        private void OnAccountInvoke(Account TheAccount)
        {
            try
            {
                this.BeginInvoke(accountDelegate, TheAccount);
            }
            catch (ObjectDisposedException exp)
            {
                AppendLog("OnAccountInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
            }
            catch (InvalidOperationException exp)
            {
                AppendLog("OnAccountInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
            }
            catch (Exception exp)
            {
                AppendLog("OnAccountInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
                throw exp;
            }
        }

        protected virtual void OnAccount(Account TheAccount)
        {
        }

        private static void OnAEStatic(List<AccountExecutive> AEList)
        {
            // copy delegates to the array before calling invoke to prevent deadlock
            AEDelegate[] arr = null;
            lock (AEDelegateMutex)
            {
                arr = new AEDelegate[AEDelegateDict.Count];
                AEDelegateDict.Values.CopyTo(arr, 0);
            }

            foreach (AEDelegate d in arr)
            {
                d.Invoke(AEList);
            }
        }

        private void OnAEInvoke(List<AccountExecutive> AEList)
        {
            try
            {
                this.BeginInvoke(aeDelegate, AEList);
            }
            catch (ObjectDisposedException exp)
            {
                AppendLog("OnAEInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
            }
            catch (InvalidOperationException exp)
            {
                AppendLog("OnAEInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
            }
            catch (Exception exp)
            {
                AppendLog("OnAEInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
                throw exp;
            }
        }

        protected virtual void OnAE(List<AccountExecutive> AE)
        {
        }

        /*
        private static void OnStockStatic(Stock TheStock)
        {
            DPStock.Execute(DataProcessor<Stock>.Operation.CodeEnum.DataArrival, null, Stocks[i].StockSignature, Stocks[i]);
        }
         */

        private void OnStockInvoke(Stock TheStock)
        {
            for (int i = 0; i < 5; i++) // sometimes the form has not been initialized enough, wait and try a few more times.
            {
                try
                {
                    this.BeginInvoke(stockDelegate, TheStock);
                    return;
                }
                catch (ObjectDisposedException exp)
                {
                    AppendLog("OnStockInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
                }
                catch (InvalidOperationException exp)
                {
                    AppendLog("OnStockInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
                }
                catch (Exception exp)
                {
                    AppendLog("OnStockInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
                    throw exp;
                }

                Thread.Sleep(100);
            }
        }

        protected virtual void OnStock(Stock TheStock)
        {
        }

        /*
        private static void OnIndexStatic(List<Index> Indexes)
        {
            if (Indexes == null || Indexes.Count <= 0) return;

            int i, n = Indexes.Count;
            List<string> IndexCodeList = new List<string>(n);

            for (i = 0; i < n; i++)
            {
                IndexCodeList.Add(Indexes[i].Code);
            }

            IndexSubscription.Dispatch(IndexCodeList, Indexes);
        }
         */

        private void OnIndexInvoke(Index TheIndex)
        {
            try
            {
                this.BeginInvoke(indexDelegate, TheIndex);
            }
            catch (ObjectDisposedException exp)
            {
                AppendLog("OnIndexInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
            }
            catch (InvalidOperationException exp)
            {
                AppendLog("OnIndexInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
            }
            catch (Exception exp)
            {
                AppendLog("OnIndexInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
                throw exp;
            }
        }

        protected virtual void OnIndex(Index TheIndex)
        {
        }

        private void OnMarketTurnoverInvoke(MarketTurnover TheMarketTurnover)
        {
            try
            {
                this.BeginInvoke(marketTurnoverDelegate, TheMarketTurnover);
            }
            catch (ObjectDisposedException exp)
            {
                AppendLog("OnMarketTurnoverInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
            }
            catch (InvalidOperationException exp)
            {
                AppendLog("OnMarketTurnoverInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
            }
            catch (Exception exp)
            {
                AppendLog("OnMarketTurnoverInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
                throw exp;
            }
        }

        protected virtual void OnMarketTurnover(MarketTurnover TheMarketTurnover)
        {
        }

        /*
        private static void OnTransactionChargeStatic(List<TransactionCharge> TransactionCharges)
        {
            if (TransactionCharges == null || TransactionCharges.Count <= 0) return;

            int i, n = TransactionCharges.Count;
            List<string> TransactionChargeCodeList = new List<string>(n);

            for (i = 0; i < n; i++)
            {
                TransactionChargeCodeList.Add(TransactionCharges[i].Hash);
            }

            TransactionChargeSubscription.Dispatch(TransactionChargeCodeList, TransactionCharges);
        }
         */

        private void OnTransactionChargeInvoke(TransactionCharge TxnCharge)
        {
            try
            {
                this.BeginInvoke(transactionChargeDelegate, TxnCharge);
            }
            catch (ObjectDisposedException exp)
            {
                AppendLog("OnTransactionChargeInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
            }
            catch (InvalidOperationException exp)
            {
                AppendLog("OnTransactionChargeInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
            }
            catch (Exception exp)
            {
                AppendLog("OnTransactionChargeInvoke", exp.GetType().FullName + Environment.NewLine + exp.Message + Environment.NewLine + exp.StackTrace.ToString(), "Invoke", false);
                throw exp;
            }
        }

        protected virtual void OnTransactionCharge(TransactionCharge TxnCharge)
        {
        }

        protected static void SyncTradeSubscriptionStart()
        {
            //CentralUserSettingSubscription.Start();
            DPCentralUserSetting.Start();
            //OrderSubscription.Start();
            DPOrder.Start();
            //OddLotSubscription.Start();
            DPOddLotOrder.Start();
            //AccountSubscription.Start();
            DPAccount.Start();
            //IndexSubscription.Start();
            DPIndex.Start();
            DPMarketTurnover.Start();
            //TransactionChargeSubscription.Start();
            DPTxnCharge.Start();
        }

        protected static void SyncTradeSubscriptionStop()
        {
            //CentralUserSettingSubscription.Stop();
            DPCentralUserSetting.Stop();
            //OrderSubscription.Stop();
            DPOrder.Stop();
            //OddLotSubscription.Stop();
            DPOddLotOrder.Stop();
            //AccountSubscription.Stop();
            DPAccount.Stop();
            //IndexSubscription.Stop();
            DPIndex.Stop();
            DPMarketTurnover.Stop();
            //TransactionChargeSubscription.Stop();
            DPTxnCharge.Stop();
        }

        protected static void SyncQuoteSubscriptionStart()
        {
            DPStock.Start();
            DPIndex.Start();
            DPMarketTurnover.Start();
            //StockSubscription.Start();
        }

        protected static void SyncQuoteSubscriptionStop()
        {
            DPStock.Stop();
            DPIndex.Stop();
            DPMarketTurnover.Stop();
            //StockSubscription.Stop();
        }

        protected static void SyncTradeSubscriptionSetAllOutstand()
        {
            //CentralUserSettingSubscription.SetAllPublicationOutstanding();
            DPCentralUserSetting.Execute(DataProcessor<SettingKeyValue>.Operation.CodeEnum.Listen, null, null, null);
            //OrderSubscription.SetAllPublicationOutstanding();
            DPOrder.Execute(DataProcessor<Order>.Operation.CodeEnum.Listen, null, null, null);
            //OddLotSubscription.SetAllPublicationOutstanding();
            DPOddLotOrder.Execute(DataProcessor<OddLotOrder>.Operation.CodeEnum.Listen, null, null, null);
            //AccountSubscription.SetAllPublicationOutstanding();
            DPAccount.Execute(DataProcessor<Account>.Operation.CodeEnum.Listen, null, null, null);  // Relisten
            //IndexSubscription.SetAllPublicationOutstanding();
            DPIndex.Execute(DataProcessor<Index>.Operation.CodeEnum.Listen, null, null, null);
            //TransactionChargeSubscription.SetAllPublicationOutstanding();
            DPTxnCharge.Execute(DataProcessor<TransactionCharge>.Operation.CodeEnum.Listen, null, null, null);
            //CentralUserSettingSubscription.Synchronize();
            //OrderSubscription.Synchronize();
            //OddLotSubscription.Synchronize();
            //AccountSubscription.Synchronize();
            //IndexSubscription.Synchronize();
            //TransactionChargeSubscription.Synchronize();
        }

        protected static void SyncQuoteSubscriptionSetAllOutstand()
        {
            DPStock.Execute(DataProcessor<Stock>.Operation.CodeEnum.Listen, null, null, null);  // Relisten
            //StockSubscription.SetAllPublicationOutstanding();
            //StockSubscription.Synchronize();
        }

        /*
        private static List<SettingKeyValue> SubscriptionCentralUserSettingGet(List<string> KeyList, out List<string> SubscribedKeyList, bool Refresh)
        {
            SubscribedKeyList = null;
            List<SettingKeyValue> SettingList = null;
            List<string> nonExistKeyList = null;

            if (pTradeDB != null && pTradeDB.TradeState == (int)ProcessState.Ready &&
                KeyList != null && KeyList.Count > 0)
            {
                SettingList = pTradeDB.GetSettingByKey(KeyList, out nonExistKeyList, Refresh ? GetRefreshTypeEnum.ForceRefresh : GetRefreshTypeEnum.NoRefresh);
                if (SettingList != null && SettingList.Count > 0)
                {
                    int i, n = SettingList.Count;
                    SubscribedKeyList = new List<string>(n);
                    for (i = 0; i < n; i++)
                    {
                        SubscribedKeyList.Add(SettingList[i].Key);
                    }
                }
            }

            return SettingList;
        }

        private static List<SettingKeyValue> SubscriptionCentralUserSettingRequest(List<string> KeyList, out List<string> SubscribedKeyList, bool Refresh)
        {
            return SubscriptionCentralUserSettingGet(KeyList, out SubscribedKeyList, Refresh);
        }

        private static List<SettingKeyValue> SubscriptionCentralUserSettingUnRequest(List<string> KeyList, out List<string> SubscribedKeyList, bool Refresh)
        {
            SubscribedKeyList = null;
            return null;
        }

        private static List<Order> SubscriptionOrderGet(List<string> OrderNoList, out List<string> SubscribedOrderNoList, bool Refresh)
        {
            SubscribedOrderNoList = null;
            List<int> intOrderNoList = null;
            int intOrderNo;
            List<Order> OrderList = null;

            if (pTradeDB != null && pTradeDB.TradeState == (int)ProcessState.Ready &&
                OrderNoList != null && OrderNoList.Count > 0)
            {
                intOrderNoList = new List<int>(OrderNoList.Count);
                foreach (string orderNo in OrderNoList)
                {
                    if (int.TryParse(orderNo, out intOrderNo)) intOrderNoList.Add(intOrderNo);
                }

                if (intOrderNoList.Count > 0)
                {
                    OrderList = pTradeDB.GetOrderByOrderNo(intOrderNoList);
                    if (OrderList != null && OrderList.Count > 0)
                    {
                        int i, n = OrderList.Count;
                        SubscribedOrderNoList = new List<string>(n);
                        for (i = 0; i < n; i++)
                        {
                            SubscribedOrderNoList.Add(OrderList[i].OrderNo.ToString());
                        }
                    }
                }
            }
            return OrderList;
        }

        private static List<Order> SubscriptionOrderRequest(List<string> OrderNoList, out List<string> SubscribedOrderNoList, bool Refresh)
        {
            SubscribedOrderNoList = null;

            if (pTradeDB != null && pTradeDB.TradeState == (int)ProcessState.Ready &&
                OrderNoList != null && OrderNoList.Count > 0)
            {
                pTradeDB.RequestOrder(OrderNoList);
            }
            return null;
        }
         */

        //private static List<Order> SubscriptionOrderUnRequest(List<string> OrderNoList, out List<string> SubscribedOrderNoList, bool Refresh)
        //{
        //    SubscribedOrderNoList = null;

        //    if (pTradeDB != null && pTradeDB.TradeState == (int)ProcessState.Ready &&
        //        OrderNoList != null && OrderNoList.Count > 0)
        //    {
        //        // implementation not needed at the moment
        //        // pTradeDB.UnRequestOrder(OrderNoList);
        //    }
        //    return null;
        //}

        //private static List<OddLotOrder> SubscriptionOddLotOrderRequest(List<string> OrderNoList, out List<string> SubscribedOrderNoList, bool Refresh)
        //{
        //    SubscribedOrderNoList = null;

        //    if (pTradeDB != null && pTradeDB.TradeState == (int)ProcessState.Ready &&
        //        OrderNoList != null && OrderNoList.Count > 0)
        //    {
        //        pTradeDB.RequestServerOddLotOder(OrderNoList);
        //    }
        //    return null;
        //}

        //private static List<OddLotOrder> SubscriptionOddLotOrderUnRequest(List<string> OrderNoList, out List<string> SubscribedOrderNoList, bool Refresh)
        //{
        //    SubscribedOrderNoList = null;

        //    if (pTradeDB != null && pTradeDB.TradeState == (int)ProcessState.Ready &&
        //        OrderNoList != null && OrderNoList.Count > 0)
        //    {
        //        // implementation not needed at the moment
        //        pTradeDB.UnRequestServerOddLotOder(OrderNoList);
        //    }
        //    return null;
        //}

        /*
        private static List<Account> SubscriptionAccountGet(List<string> AccountNoList, out List<string> SubscribedAccountNoList, bool Refresh)
        {
            SubscribedAccountNoList = null;
            List<Account> AccountList = null;
            List<string> nonExistAccountNoList = null;

            if (pTradeDB != null && pTradeDB.TradeState == (int)ProcessState.Ready &&
                AccountNoList != null && AccountNoList.Count > 0)
            {
                AccountList = pTradeDB.GetAccountByAccountNo(AccountNoList, out nonExistAccountNoList, Refresh ? GetRefreshTypeEnum.ForceRefresh : GetRefreshTypeEnum.NoRefresh);
                if (AccountList != null && AccountList.Count > 0)
                {
                    int i, n = AccountList.Count;
                    SubscribedAccountNoList = new List<string>(n);
                    for (i = 0; i < n; i++)
                    {
                        SubscribedAccountNoList.Add(AccountList[i].AccountNo);
                    }
                }
            }
            return AccountList;
        }

        private static List<Account> SubscriptionAccountRequest(List<string> AccountNoList, out List<string> SubscribedAccountNoList, bool Refresh)
        {
            SubscribedAccountNoList = null;

            if (pTradeDB != null && pTradeDB.TradeState == (int)ProcessState.Ready && 
                AccountNoList != null && AccountNoList.Count > 0)
            {
                pTradeDB.RequestAccount(AccountNoList);
            }
            return null;
        }

        private static List<Account> SubscriptionAccountUnRequest(List<string> AccountNoList, out List<string> SubscribedAccountNoList, bool Refresh)
        {
            SubscribedAccountNoList = null;

            if (pTradeDB != null && pTradeDB.TradeState == (int)ProcessState.Ready &&
                AccountNoList != null && AccountNoList.Count > 0)
            {
                pTradeDB.UnRequestAccount(AccountNoList);
            }
            return null;
        }
         */

        /*
        // Subscription class SubscriptionSnapShotDelegate.Invoke call this function
        private static List<Stock> SubscriptionStockGet(List<string> StockCodeList, out List<string> SubscribedStockCodeList, bool Refresh)
        {
            SubscribedStockCodeList = null;
            List<Stock> StockList = null;
            List<string> nonExistStockCodeList = null;

            if (pTradeDB != null && pTradeDB.QuoteState == (int)global::TradeDB.Net.QuoteConnector.ProcessState.Ready &&
                StockCodeList != null && StockCodeList.Count > 0)
            {
                StockList = pTradeDB.GetStock(StockCodeList, out nonExistStockCodeList, Refresh ? GetRefreshTypeEnum.ForceRefresh : GetRefreshTypeEnum.NoRefresh, GetStockInfoType.Basic, out SubscribedStockCodeList);
                if (StockList != null && StockList.Count > 0)
                {
                    SubscribedStockCodeList = new List<string>(StockList.Count);
                    //int i, n = StockList.Count;
                    //SubscribedStockCodeList = new List<string>(n);
                    //for (i = 0; i < n; i++)
                    //{
                    //    SubscribedStockCodeList.Add(StockList[i].Code);
                    //}
                }
            }
            return StockList;
        }

        // Subscription class SubscriptionStreamDelegate.Invoke call this function
        private static List<Stock> SubscriptionStockRequest(List<string> StockCodeList, out List<string> SubscribedStockCodeList, bool Refresh)
        {
            SubscribedStockCodeList = null;

            if (pTradeDB != null && pTradeDB.QuoteState == (int)global::TradeDB.Net.QuoteConnector.ProcessState.Ready &&
                StockCodeList != null && StockCodeList.Count > 0)
            {
                pTradeDB.RequestStock(StockCodeList);
            }
            return null;
        }

        private static List<Stock> SubscriptionStockUnRequest(List<string> StockCodeList, out List<string> SubscribedStockCodeList, bool Refresh)
        {
            SubscribedStockCodeList = null;

            if (pTradeDB != null && pTradeDB.QuoteState == (int)global::TradeDB.Net.QuoteConnector.ProcessState.Ready &&
                StockCodeList != null && StockCodeList.Count > 0)
            {
                pTradeDB.UnRequestStock(StockCodeList);
            }
            return null;
        }

        private static List<Index> SubscriptionIndexGet(List<string> IndexCodeList, out List<string> SubscribedIndexCodeList, bool Refresh)
        {
            SubscribedIndexCodeList = null;
            List<Index> IndexList = null;
            List<string> nonExistIndexCodeList = null;

            if (pTradeDB != null && pTradeDB.TradeState == (int)ProcessState.Ready &&
                IndexCodeList != null && IndexCodeList.Count > 0)
            {
                IndexList = pTradeDB.GetIndex(IndexCodeList, out nonExistIndexCodeList, Refresh ? GetRefreshTypeEnum.ForceRefresh : GetRefreshTypeEnum.NoRefresh);
                if (IndexList != null && IndexList.Count > 0)
                {
                    int i, n = IndexList.Count;
                    SubscribedIndexCodeList = new List<string>(n);
                    for (i = 0; i < n; i++)
                    {
                        SubscribedIndexCodeList.Add(IndexList[i].Code);
                    }
                }
            }
            return IndexList;
        }

        private static List<Index> SubscriptionIndexRequest(List<string> IndexCodeList, out List<string> SubscribedIndexCodeList, bool Refresh)
        {
            SubscribedIndexCodeList = null;

            if (pTradeDB != null && pTradeDB.TradeState == (int)ProcessState.Ready &&
                IndexCodeList != null && IndexCodeList.Count > 0)
            {
                pTradeDB.RequestIndex(IndexCodeList);
            }
            return null;
        }

        private static List<Index> SubscriptionIndexUnRequest(List<string> IndexCodeList, out List<string> SubscribedIndexCodeList, bool Refresh)
        {
            SubscribedIndexCodeList = null;

            if (pTradeDB != null && pTradeDB.TradeState == (int)ProcessState.Ready &&
                IndexCodeList != null && IndexCodeList.Count > 0)
            {
                pTradeDB.UnRequestIndex(IndexCodeList);
            }
            return null;
        }

        private static List<TransactionCharge> SubscriptionTransactionChargeGet(List<string> TransactionChargeCodeList, out List<string> SubscribedTransactionChargeCodeList, bool Refresh)
        {
            SubscribedTransactionChargeCodeList = null;
            List<TransactionCharge> TransactionChargeList = null;
            List<string> nonExistTransactionChargeCodeList = null;

            if (pTradeDB != null && pTradeDB.TradeState == (int)ProcessState.Ready &&
                TransactionChargeCodeList != null && TransactionChargeCodeList.Count > 0)
            {
                TransactionChargeList = pTradeDB.GetTransactionCharge(TransactionChargeCodeList, out nonExistTransactionChargeCodeList, Refresh ? GetRefreshTypeEnum.ForceRefresh : GetRefreshTypeEnum.NoRefresh);
                if (TransactionChargeList != null && TransactionChargeList.Count > 0)
                {
                    int i, n = TransactionChargeList.Count;
                    SubscribedTransactionChargeCodeList = new List<string>(n);
                    for (i = 0; i < n; i++)
                    {
                        SubscribedTransactionChargeCodeList.Add(TransactionChargeList[i].Hash);
                    }
                }
            }
            return TransactionChargeList;
        }

        private static List<TransactionCharge> SubscriptionTransactionChargeRequest(List<string> TransactionChargeCodeList, out List<string> SubscribedTransactionChargeCodeList, bool Refresh)
        {
            return SubscriptionTransactionChargeGet(TransactionChargeCodeList, out SubscribedTransactionChargeCodeList, Refresh);
        }

        private static List<TransactionCharge> SubscriptionTransactionChargeUnRequest(List<string> TransactionChargeCodeList, out List<string> SubscribedTransactionChargeCodeList, bool Refresh)
        {
            SubscribedTransactionChargeCodeList = null;
            return null;
        }
         */

        #endregion

        public void TurnSoundOn(bool onOff)
        {
            pIsSoundOn = onOff;
        }
    }
}
