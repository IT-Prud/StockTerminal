using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Threading;
using System.Web;
using Processors;
using Utils;
using TradeDB.TradeDBNet;
using StockTerminal;
using Logger;

namespace TradeDB.Net
{
    public partial class TradeDBNet : Processor, ITradeDB
    {
        #region "Define delegates for new ProcessState"

        public new enum ProcessState { Stopped, Login, Connecting, Ready, Disconnecting, MaxState };

        private string pLogFolder = null;
        private static readonly string pDeviceTokenFilePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\" + System.Windows.Forms.Application.ProductName + @"\DT\DT.dat";

        protected override void InitializeProcessDelegates()
        {
            ProcessDelegates = new ProcessDelegate[(int)ProcessState.MaxState];
            ProcessDelegates[(int)ProcessState.Stopped] = new ProcessDelegate(ProcessStopped);
            ProcessDelegates[(int)ProcessState.Login] = new ProcessDelegate(ProcessLogin);
            //ProcessDelegates[(int)ProcessState.Connecting] = new ProcessDelegate(ProcessConnecting);
            ProcessDelegates[(int)ProcessState.Ready] = new ProcessDelegate(ProcessReady);
            ProcessDelegates[(int)ProcessState.Disconnecting] = new ProcessDelegate(ProcessDisconnecting);
        }

        #endregion

        private readonly object SystemEventMutex = new object();
        private SystemEventDelegate pSystemEventDelegate;

        private readonly object pCentralUserSettingsMutex = new object();
        private readonly Settings pCentralUserSettings = new Settings();

        private readonly object pSettingFormCentralMutex = new object();
        private readonly Settings pSettingFormCentral = new Settings();

        private readonly OrderBook pOrderBook = new OrderBook();
        private bool pOrderBookSynced = false, pRequireOTP = false;
        private DateTime orderBookSyncTime = DateTime.MinValue;
        private int MaxOrderNo = 0;

        private readonly OddLotOrderBook pOddLotOrderBook = new OddLotOrderBook();

        private readonly object AccountListMutex = new object();
        private AccountListDelegate pAccountListDelegate;
        private readonly List<AccountAE> AccountListUpdateList = new List<AccountAE>(30000);
        private readonly List<string> AccountNoListUpdateList = new List<string>(30000);

        private readonly object accountMutex = new object();
        private readonly AccountBook pAccountBook = new AccountBook();
        private Account accountLastUpdate = null;

        private readonly AEBook pAEBook = new AEBook();
        private readonly object AEMutex = new object();
        private AEDelegate pAEDelegate;
        private readonly List<AccountExecutive> AEBookUpdateList = new List<AccountExecutive>(100);

        private readonly Dictionary<string, Market> MarketDict = new Dictionary<string, Market>(10);

        private readonly StockBook pStockBook = new StockBook();
        private readonly StockRelationBook pStockRelationBook = new StockRelationBook();

        private readonly IndexBook pIndexBook = new IndexBook();

        private readonly TransactionChargeBook pTransactionChargeBook = new TransactionChargeBook();

        private readonly SessionManager SessionMgr = new SessionManager();
        
        private delegate void MsgInDelegate(TradeMessage Msg);
        private readonly Dictionary<string, MsgInDelegate> MsgInDelegates = new Dictionary<string, MsgInDelegate>(10);
        private readonly Dictionary<string, MsgInDelegate> MsgInOthersDelegates = new Dictionary<string, MsgInDelegate>(10);

        private readonly QueueLockFree<TradeMessage> MsgInOthers = new QueueLockFree<TradeMessage>();
        
        private CultureInfo pCulture = new CultureInfo("en-US");
        private ResourceManager ResManUI;

        private DataProcessor<SettingKeyValue> pDPCentralUserSetting = null;
        private DataProcessor<Order> pDPOrder = null;
        private DataProcessor<OddLotOrder> pDPOddLotOrder = null;
        private DataProcessor<Account> pDPAccount = null;
        private DataProcessor<Stock> pDPStock = null;
        private DataProcessor<Index> pDPIndex = null;
        private DataProcessor<MarketTurnover> pDPMarketTurnover = null;
        private DataProcessor<TransactionCharge> pDPTxnCharge = null;

        private readonly SpreadTableSet pSpreadTables = new SpreadTableSet();

        //private readonly Log DynamicInLog = Program.TheLogProcessor.Open(null, "InDyn", ".log");

        #region "Interface implementation"

        public TradeDBNet()
        {
            MsgInDelegates.Add("ACT", ProcessMessageActOrdDea);
            MsgInDelegates.Add("ORD", ProcessMessageActOrdDea);
            MsgInDelegates.Add("DEA", ProcessMessageActOrdDea);
            MsgInDelegates.Add("CCK", ProcessMessageCCK);
            MsgInDelegates.Add("SRE", ProcessMessageSreMks);
            MsgInDelegates.Add("MKS", ProcessMessageSreMks);
            MsgInDelegates.Add("MKT", ProcessMessageMkt);
            MsgInDelegates.Add("IND", ProcessMessageIND);
            MsgInDelegates.Add("SET", ProcessMessageSET);

            MsgInOthersDelegates.Add("CCK", ProcessMessageCCKOthers);

            SessionMgr.SpreadTables = pSpreadTables;

            SessionMgr.StateChanged += new StateChangedDelegate(OnSessionManagerStateChange);
            SessionMgr.ListenMessage(OnMsgArrived);

            Type thisType = this.GetType();
            string targetResName = thisType.Name + "_.resources";
            Assembly thisAssembly = thisType.Assembly;
            foreach (string resName in thisAssembly.GetManifestResourceNames())
            {
                if (resName.EndsWith(targetResName))
                {
                    ResManUI = new ResourceManager(resName.Remove(resName.Length - 10), thisAssembly);
                    break;
                }
            }

            //DynamicInLog.FlushAfterAppend = true;
        }

        public string UserId
        {
            get { return SessionMgr.UserId; }
            set {
                SessionMgr.UserId = value;
                //DynamicInLog.FolderPath = pLogFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\" + System.Windows.Forms.Application.ProductName + @"\Log\" + SessionMgr.UserId + @"\__SocketLog\Quote\";
            }
        }

        public string Password
        {
            get { return SessionMgr.Password; }
            set { SessionMgr.Password = value; }
        }

        public int PasswordForceChange
        {
            get { return SessionMgr.PasswordForceChange; }
        }

        public string VerificationCode
        {
            get { return SessionMgr.VerificationCode; }
            set { SessionMgr.VerificationCode = value; }
        }

        public string VerificationCodeSendMethod
        {
            get { return SessionMgr.VerificationCodeSendMethod; }
            set { SessionMgr.VerificationCodeSendMethod = value; }
        }

        public string UserSystemName
        {
            get { return SessionMgr.UserSystemName; }
            set { SessionMgr.UserSystemName = value; }
        }

        public string UserSystemVersion
        {
            get { return SessionMgr.UserSystemVersion; }
            set { SessionMgr.UserSystemVersion = value; }
        }

        public string IPType
        {
            get { return SessionMgr.IPType; }
        }

        public string DeploymentGroupName
        {
            get { return SessionMgr.DeploymentGroupName; }
            set { SessionMgr.DeploymentGroupName = value; }
        }

        public string Email
        {
            get { return SessionMgr.Email; }
        }

        public string SMSNo
        {
            get { return SessionMgr.SMSNo; }
        }

        public UserTypeEnum UserType
        {
            get { return SessionMgr.UserType; }
        }

        public HostEndPoint CurrentLoginHostTrade
        {
            get { return SessionMgr.CurrentLoginHostTrade; }
        }

        public HostEndPoint CurrentLoginHostQuote
        {
            get { return SessionMgr.CurrentLoginHostQuote; }
        }

        public IPEndPoint StockUdpDataEP
        {
            get { return SessionMgr.StockUdpEP; }
            set { SessionMgr.StockUdpEP = value; }
        }

        public bool StockUdpDataEnable
        {
            get { return SessionMgr.StockUdpDataEnable; }
            set { SessionMgr.StockUdpDataEnable = value; }
        }

        public bool MessageLogEnabled
        {
            get { return SessionMgr.MessageLogEnabled; }
            set { SessionMgr.MessageLogEnabled = value; }
        }

        public bool OrderBookSynced
        {
            get { return pOrderBookSynced; }
        }

        public bool RequireOTP
        {
            get { return pRequireOTP; }
            set { pRequireOTP = value; }
        }

        public string LogFolder
        {
            get { return pLogFolder; }
            set
            {
                pLogFolder = value;
                SessionMgr.LogFolder = pLogFolder;
            }
        }

        public CultureInfo Culture
        {
            get { return pCulture; }
            set { pCulture = value; }
        }

        public ConnectionTypeEnum ConnectionType
        {
            get
            {
                HostEndPoint HostEP = SessionMgr.CurrentLoginHostTrade;
                if (HostEP == null || HostEP.Name.Length <= 0)
                {
                    return ConnectionTypeEnum.Unknown;
                }
                else if (HostEP.Name.Substring(0, 4) == "192.")
                {
                    return ConnectionTypeEnum.Local;
                }
                return ConnectionTypeEnum.Internet;
            }
        }

        public int TradeState
        {
            get { return State; }
        }

        public int QuoteState
        {
            get { return SessionMgr.QuoteState; }
        }

        public LastErrorEnum SessionLastTradeError
        {
            get
            {
                if (SessionMgr != null)
                {
                    return SessionMgr.LastTradeError;
                }
                return LastErrorEnum.NoError;
            }
        }

        public LastErrorEnum SessionLastQuoteError
        {
            get
            {
                if (SessionMgr != null)
                {
                    return SessionMgr.LastQuoteError;
                }
                return LastErrorEnum.NoError;
            }
        }

        public DateTime ServerTime
        {
            get
            {
                if (SessionMgr != null)
                {
                    return SessionMgr.ServerTime;
                }
                return DateTime.MinValue;
            }
        }

        public DataProcessor<SettingKeyValue> DPCentralUserSetting
        {
            get { return pDPCentralUserSetting; }
            set
            {
                pDPCentralUserSetting = value;
                if (pDPCentralUserSetting != null)
                    pDPCentralUserSetting.ParseData = new DataProcessor<SettingKeyValue>.ParseDataDelegate(ParseMessageCentralUserSetting);
            }
        }

        public DataProcessor<Order> DPOrder
        {
            get { return pDPOrder; }
            set
            {
                pDPOrder = value;
                if (pDPOrder != null)
                    pDPOrder.ParseData = new DataProcessor<Order>.ParseDataDelegate(ParseMessageOrder);
            }
        }

        public DataProcessor<OddLotOrder> DPOddLotOrder
        {
            get { return pDPOddLotOrder; }
            set
            {
                pDPOddLotOrder = value;
                if (pDPOddLotOrder != null)
                    pDPOddLotOrder.ParseData = new DataProcessor<OddLotOrder>.ParseDataDelegate(ParseMessageOddLotOrder);
            }
        }

        public DataProcessor<Account> DPAccount
        {
            get { return pDPAccount; }
            set
            {
                pDPAccount = value;
                if (pDPAccount != null)
                    pDPAccount.ParseData = new DataProcessor<Account>.ParseDataDelegate(ParseMessageAccount);
            }
        }

        public DataProcessor<Stock> DPStock
        {
            get { return pDPStock; }
            set
            {
                pDPStock = value;
                if (pDPStock != null)
                    pDPStock.ParseData = new DataProcessor<Stock>.ParseDataDelegate(ParseMessageStock);
            }
        }

        public DataProcessor<Index> DPIndex
        {
            get { return pDPIndex; }
            set
            {
                pDPIndex = value;
                if (pDPIndex != null)
                    pDPIndex.ParseData = new DataProcessor<Index>.ParseDataDelegate(ParseMessageIndex);
            }
        }

        public DataProcessor<MarketTurnover> DPMarketTurnover
        {
            get { return pDPMarketTurnover; }
            set
            {
                pDPMarketTurnover = value;
                if (pDPMarketTurnover != null)
                    pDPMarketTurnover.ParseData = new DataProcessor<MarketTurnover>.ParseDataDelegate(ParseMessageMarketTurnover);
            }
        }

        public DataProcessor<TransactionCharge> DPTxnCharge
        {
            get { return pDPTxnCharge; }
            set
            {
                pDPTxnCharge = value;
                if (pDPTxnCharge != null)
                    pDPTxnCharge.ParseData = new DataProcessor<TransactionCharge>.ParseDataDelegate(ParseMessageTxnCharge);
            }
        }

        public SpreadTableSet SpreadTables
        {
            get { return pSpreadTables; }
        }

        public void ListenSystemEvent(SystemEventDelegate systemEventDelegate)
        {
            if (systemEventDelegate != null)
            {
                lock (SystemEventMutex)
                {
                    if (this.pSystemEventDelegate == null)
                    {
                        this.pSystemEventDelegate = systemEventDelegate;
                    }
                    else
                    {
                        this.pSystemEventDelegate = systemEventDelegate;
                    }
                }
            }
        }

        public void UnListenSystemEvent(SystemEventDelegate systemEventDelegate)
        {
            lock (SystemEventMutex)
            {
                this.pSystemEventDelegate = (SystemEventDelegate)Delegate.Remove(this.pSystemEventDelegate, systemEventDelegate);
            }
        }

        public void ListenConnectionStatus(StateChangedDelegate connectionStatusDelegate)
        {
            StateChanged += connectionStatusDelegate;
        }

        public void UnListenConnectionStatus(StateChangedDelegate connectionStatusDelegate)
        {
            StateChanged -= connectionStatusDelegate;
        }

        public void ListenQuoteConnectionStatus(StateChangedDelegate connectionStatusDelegate)
        {
            SessionMgr.StateChangedQuote += connectionStatusDelegate;
        }

        public void UnListenQuoteConnectionStatus(StateChangedDelegate connectionStatusDelegate)
        {
            SessionMgr.StateChangedQuote -= connectionStatusDelegate;
        }

        public void ListenAccountList(AccountListDelegate accountListDelegate)
        {
            if (accountListDelegate != null)
            {
                lock (AccountListMutex)
                {
                    if (this.pAccountListDelegate == null)
                    {
                        this.pAccountListDelegate = accountListDelegate;
                    }
                    else
                    {
                        this.pAccountListDelegate = accountListDelegate;
                    }
                }
            }
        }

        public void UnListenAccountList(AccountListDelegate accountListDelegate)
        {
            lock (AccountListMutex)
            {
                this.pAccountListDelegate = (AccountListDelegate)Delegate.Remove(this.pAccountListDelegate, accountListDelegate);
            }
        }

        public void ListenAE(AEDelegate aeDelegate)
        {
            if (aeDelegate != null)
            {
                lock (AEMutex)
                {
                    if (this.pAEDelegate == null)
                    {
                        this.pAEDelegate = aeDelegate;
                    }
                    else
                    {
                        this.pAEDelegate = aeDelegate;
                    }
                }
            }
        }

        public void UnListenAE(AEDelegate aeDelegate)
        {
            lock (AEMutex)
            {
                this.pAEDelegate = (AEDelegate)Delegate.Remove(this.pAEDelegate, aeDelegate);
            }
        }

        public void AddLoginHostTrade(HostEndPoint HostEP)
        {
            SessionMgr.AddLoginHostTrade(HostEP);
        }

        public void ClearLoginHostTrade()
        {
            SessionMgr.ClearLoginHostTrade();
        }

        public void AddLoginHostQuote(HostEndPoint HostEP)
        {
            SessionMgr.AddLoginHostQuote(HostEP);
        }

        public void ClearLoginHostQuote()
        {
            SessionMgr.ClearLoginHostQuote();
        }

        public void Connect()
        {
            //DynamicInLog.Purge(31);
            Start();
        }

        public void Disconnect()
        {
            Stop(IProcessStopStyle.NoWait);
        }

        public SettingKeyValue GetSetting(string Key)
        {
            SettingKeyValue setKeyValue = null;
            string value = null;

            if (Key != null)
            {
                lock (pCentralUserSettingsMutex)
                {
                    value = pCentralUserSettings[Key];
                }

                setKeyValue = new SettingKeyValue(Key, value);
            }

            return setKeyValue;
        }

        public void RequestSetting(List<string> SettingKeyList, bool Listen)
        {
            if (SettingKeyList != null && SettingKeyList.Count > 0)
            {
                StringBuilder sb = new StringBuilder(500);
                string sep = "";

                foreach (string key in SettingKeyList)
                {
                    sb.Append(sep + HttpUtility.UrlEncode(key));
                    sep = " ";
                }

                Dictionary<string, TradeMessageTag> tags = new Dictionary<string, TradeMessageTag>(1);

                tags.Add("KEY", new TradeMessageTag("KEY", sb.ToString()));

                TradeMessage msg = new TradeMessage();
                msg.MessageId = "SET";
                msg.MessageType = "01";
                msg.Tags = tags;

                SessionMgr.Send(msg);
            }
        }

        public void UnrequestSetting(List<string> SettingKeyList)
        {

        }

        public bool SetSetting(SettingKeyValue KeyValue)
        {
            if (KeyValue == null) return false;

            string value = null;
            bool changed = false;
            lock (pCentralUserSettingsMutex)
            {
                value = pCentralUserSettings[KeyValue.Key];
                if (value != KeyValue.Value)
                {
                    pCentralUserSettings[KeyValue.Key] = KeyValue.Value;
                    changed = true;
                }
            }

            if (changed)
            {
                if (State == (int)ProcessState.Ready)
                {
                    Dictionary<string, TradeMessageTag> tags = new Dictionary<string, TradeMessageTag>(1);

                    tags.Add("KEY", new TradeMessageTag("KEY", HttpUtility.UrlEncode(KeyValue.Key)));
                    tags.Add("VAL", new TradeMessageTag("VAL", HttpUtility.UrlEncode(KeyValue.Value != null ? KeyValue.Value : "")));

                    TradeMessage msg = null;

                    msg = new TradeMessage();
                    msg.MessageId = "SET";
                    msg.MessageType = "03";
                    msg.Tags = tags;
                    SessionMgr.Send(msg);
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        public List<Order> GetOrder()
        {
            return pOrderBook.GetAll();
        }

        public List<Order> GetOrderByOrderNo(List<int> OrderNoList)
        {
            return pOrderBook.GetByOrderNo(OrderNoList);
        }

        public List<Order> GetOrderByAECode(string AECode)
        {
            return pOrderBook.GetByAECode(AECode);
        }

        public List<Order> GetOrderByAccountNo(string AccountNo)
        {
            return pOrderBook.GetByAccountNo(AccountNo);
        }

        //public List<Order> GetOrderByStockCode(string StockCode)
        //{
        //    return pOrderBook.GetByStockCode(StockCode);
        //}

        public List<Order> GetOrderByStatus(int Status)
        {
            return pOrderBook.GetByStatus(Status);
        }

        public List<Order> GetOrderByReplied(char Replied)
        {
            return pOrderBook.GetByReplied(Replied);
        }

        public void RequestOrder(List<string> OrderNoList)
        {
            if (State == (int)ProcessState.Ready)
            {
                TradeMessage msg = null;

                msg = new TradeMessage();
                msg.MessageId = "ORD";
                msg.MessageType = "04"; // request all orders in stream
                SessionMgr.Send(msg);
            }
        }

        public void UnRequestOrder(List<string> OrderNoList)
        {
            // no need to do anything at the moment since server will send all orders anyway.
        }

        public List<OddLotOrder> GetOddLotOrder()
        {
            return pOddLotOrderBook.GetAll();
        }

        public List<OddLotOrder> GetOddLotOrderByStockCode(List<string> StockCodeList)
        {
            return pOddLotOrderBook.GetByStockCode(StockCodeList);
        }

        public List<string> GetAccountList()
        {
            return pAccountBook.GetList();
        }

        public string[] GetAccountList(string SearchText)
        {
            return pAccountBook.GetList(SearchText);
        }

        public List<string> GetAccountListByAECode(string AECode)
        {
            return pAccountBook.GetListByAECode(AECode);
        }

        public Account GetAccount(string AccountNo)
        {
            return pAccountBook.GetByAccountNo(AccountNo);
        }

        public List<Account> GetAccountByAECode(string AECode)
        {
            return pAccountBook.GetByAECode(AECode);
        }

        public void RequestAccountList()
        {
            if (State == (int)ProcessState.Ready)
            {
                Dictionary<string, TradeMessageTag> tags = new Dictionary<string, TradeMessageTag>(1);

                TradeMessage msg = new TradeMessage();
                msg.MessageId = "CCK";
                msg.MessageType = "23";

                SessionMgr.Send(msg);
            }
        }

        public void RequestAccount(List<string> AccountNoList, bool Listen)
        {
            if (AccountNoList != null && AccountNoList.Count > 0)
            {
                StringBuilder sb = new StringBuilder(AccountNoList.Count * 30);

                sb.Append(AccountNoList[0]);

                for (int i = 1; i < AccountNoList.Count; i++)
                {
                    sb.Append(" ");
                    sb.Append(AccountNoList[i]);
                }

                if (sb.Length > 0 && State == (int)ProcessState.Ready)
                {
                    Dictionary<string, TradeMessageTag> tags = new Dictionary<string, TradeMessageTag>(1);

                    tags.Add("ACNO", new TradeMessageTag("ACNO", sb.ToString()));

                    TradeMessage msg = new TradeMessage();
                    msg.MessageId = "CCK";
                    msg.MessageType = "21";
                    msg.Tags = tags;

                    SessionMgr.Send(msg);
                }
            }
        }

        public void UnrequestAccount(List<string> AccountNoList)
        {
            if (AccountNoList != null && AccountNoList.Count > 0)
            {
                StringBuilder sb = new StringBuilder(AccountNoList.Count * 30);

                sb.Append(AccountNoList[0]);

                for (int i = 1; i < AccountNoList.Count; i++)
                {
                    sb.Append(" ");
                    sb.Append(AccountNoList[i]);
                }

                if (sb.Length > 0 && State == (int)ProcessState.Ready)
                {
                    Dictionary<string, TradeMessageTag> tags = new Dictionary<string, TradeMessageTag>(1);

                    tags.Add("ACNO", new TradeMessageTag("ACNO", sb.ToString()));

                    TradeMessage msg = new TradeMessage();
                    msg.MessageId = "CCK";
                    msg.MessageType = "22";
                    msg.Tags = tags;

                    SessionMgr.Send(msg);
                }
            }
        }

        public List<AccountExecutive> GetAEByCode(string Code)
        {
            if (pAEBook != null && Code != null)
            {
                return pAEBook.GetByCode(Code);
            }
            return null;
        }

        /// <summary>
        /// Get the number of AE Codes
        /// </summary>
        /// <returns>The number of AE Codes</returns>
        public int GetAECount()
        {
            return pAEBook.Count;
        }

        /// <summary>
        /// Get the list of AE Codes
        /// </summary>
        /// <returns>List of AE Codes</returns>
        public List<AccountExecutive> GetAEList()
        {
            return pAEBook.GetAEList();
        }


        public void RequestMarket()
        {
            TradeMessage msg = new TradeMessage();

            msg.MessageId = "MKS";
            msg.MessageType = "02";

            if (State == (int)ProcessState.Ready)
            {
                SessionMgr.Send(msg);
            }
        }

        public Stock GetStock(string StockSignature)
        {
            return pStockBook.GetByStockSignature(StockSignature);
        }

        public Dictionary<string, Stock> GetStock(ICollection<string> StockSignatures)
        {
            return pStockBook.GetByStockSignature(StockSignatures);
        }

        public List<Stock> GetStockByMarketCode(string MarketCode, ExchangeTypeEnum exType)
        {
            if (pStockBook != null && MarketCode != null)
            {
                return pStockBook.GetByMarketCode(MarketCode, exType);
            }
            return null;
        }

        public StockRelationBook StockRelationBook
        {
            get { return pStockRelationBook; }
        }

        public void RequestStock(List<string> StockSignatureList, bool Listen)
        {
            if (StockSignatureList != null && StockSignatureList.Count > 0)
            {
                string exCode, stkCode;

                StringBuilder sb = new StringBuilder(StockSignatureList.Count * 50);

                if (StockSignatureList.Count == 1)
                {
                    Stock.GetExchangeCodeStockCode(StockSignatureList[0], out exCode, out stkCode);

                    if (exCode == null || exCode.Trim().Length <= 0)
                    {
                        Dictionary<string, TradeMessageTag> tags = new Dictionary<string, TradeMessageTag>(1);

                        tags.Add("CODE", new TradeMessageTag("CODE", stkCode));

                        TradeMessage msg = new TradeMessage();
                        msg.MessageId = "SRE";
                        msg.MessageType = "40"; // Request to search for stock
                        msg.Tags = tags;

                        SessionMgr.Send(msg);
                    }
                    else
                    {
                        Dictionary<string, TradeMessageTag> tags = new Dictionary<string, TradeMessageTag>(1);

                        tags.Add("CODE", new TradeMessageTag("CODE", exCode + "~" + stkCode));

                        TradeMessage msg = new TradeMessage();
                        msg.MessageId = "SRE";
                        msg.MessageType = Listen ? "31" : "30";
                        msg.Tags = tags;

                        SessionMgr.Send(msg);
                    }
                }
                else
                {
                    Stock.GetExchangeCodeStockCode(StockSignatureList[0], out exCode, out stkCode);
                    sb.Append(exCode + "~" + stkCode);

                    for (int i = 1; i < StockSignatureList.Count; i++)
                    {
                        sb.Append(" ");
                        Stock.GetExchangeCodeStockCode(StockSignatureList[i], out exCode, out stkCode);
                        sb.Append(exCode + "~" + stkCode);
                    }

                    if (sb.Length > 0 && State == (int)ProcessState.Ready)
                    {
                        Dictionary<string, TradeMessageTag> tags = new Dictionary<string, TradeMessageTag>(1);

                        tags.Add("CODE", new TradeMessageTag("CODE", sb.ToString()));

                        TradeMessage msg = new TradeMessage();
                        msg.MessageId = "SRE";
                        msg.MessageType = Listen ? "31" : "30";
                        msg.Tags = tags;

                        SessionMgr.Send(msg);
                    }
                }
            }
        }

        public void UnrequestStock(List<string> StockSignatureList)
        {
            if (StockSignatureList != null && StockSignatureList.Count > 0)
            {
                string exCode, stkCode;

                StringBuilder sb = new StringBuilder(StockSignatureList.Count * 50);

                Stock.GetExchangeCodeStockCode(StockSignatureList[0], out exCode, out stkCode);
                sb.Append(exCode + "~" + stkCode);

                for (int i = 1; i < StockSignatureList.Count; i++)
                {
                    sb.Append(" ");
                    Stock.GetExchangeCodeStockCode(StockSignatureList[i], out exCode, out stkCode);
                    sb.Append(exCode + "~" + stkCode);
                }

                if (sb.Length > 0 && State == (int)ProcessState.Ready)
                {
                    Dictionary<string, TradeMessageTag> tags = new Dictionary<string, TradeMessageTag>(1);

                    tags.Add("CODE", new TradeMessageTag("CODE", sb.ToString()));

                    TradeMessage msg = new TradeMessage();
                    msg.MessageId = "SRE";
                    msg.MessageType = "32";
                    msg.Tags = tags;

                    SessionMgr.Send(msg);
                }
            }
        }

        // Requestest Odd Lot Oder from stock quote server by sending SRE 33
        public void RequestServerOddLotOder(List<string> StockSignatureList, bool Listen)
        {
            if (StockSignatureList == null || StockSignatureList.Count <= 0) return;

            string sep = "";
            StringBuilder stockCodeSB = new StringBuilder(StockSignatureList.Count * 6);
            foreach (string signature in StockSignatureList)
            {
                if (signature != null)
                {
                    stockCodeSB.Append(sep + signature);
                    sep = " ";
                }
            }

            if (stockCodeSB.Length > 0 && QuoteState == (int)QuoteConnector.ProcessState.Ready)
            {
                Dictionary<string, TradeMessageTag> tags = new Dictionary<string, TradeMessageTag>(1);

                tags.Add("CODE", new TradeMessageTag("CODE", stockCodeSB.ToString()));

                TradeMessage msg = new TradeMessage();
                msg.MessageId = "SRE";
                msg.MessageType = "33";
                msg.Tags = tags;

                SessionMgr.Send(msg);
            }
        }

        public void UnRequestServerOddLotOder(List<string> StockSignatureList)
        {
            if (StockSignatureList == null || StockSignatureList.Count <= 0) return;

            string sep = "";
            StringBuilder stockCodeSB = new StringBuilder(StockSignatureList.Count * 6);

            foreach (string signature in StockSignatureList)
            {
                if (signature != null)
                {
                    stockCodeSB.Append(sep + signature);
                    sep = " ";
                }
            }

            if (stockCodeSB.Length > 0 && QuoteState == (int)QuoteConnector.ProcessState.Ready)
            {
                Dictionary<string, TradeMessageTag> tags = new Dictionary<string, TradeMessageTag>(1);

                tags.Add("CODE", new TradeMessageTag("CODE", stockCodeSB.ToString()));

                TradeMessage msg = new TradeMessage();
                msg.MessageId = "SRE";
                msg.MessageType = "34";
                msg.Tags = tags;

                SessionMgr.Send(msg);
            }
        }

        public Index GetIndex(string IndexCode)
        {
            return pIndexBook.GetByIndexCode(IndexCode);
        }

        public void RequestIndex(List<string> IndexCodeList, bool Listen)
        {
            if (IndexCodeList == null || IndexCodeList.Count <= 0) return;

            string codeFormatted = null;

            foreach (string code in IndexCodeList)
            {
                if (code != null)
                {
                    codeFormatted = code.Trim();
                    if (codeFormatted.Length > 0)
                    {
                        Dictionary<string, TradeMessageTag> tags = new Dictionary<string, TradeMessageTag>(1);
                        TradeMessage msg = new TradeMessage();
                        tags.Add("CODE", new TradeMessageTag("CODE", codeFormatted));

                        msg.MessageId = "IND";
                        msg.MessageType = "50";
                        msg.Tags = tags;

                        if (State == (int)ProcessState.Ready)
                        {
                            SessionMgr.Send(msg);
                        }
                    }
                }
            }
        }

        public void UnrequestIndex(List<string> IndexCodeList)
        {
            if (IndexCodeList == null || IndexCodeList.Count <= 0) return;

            string codeFormatted = null;

            foreach (string code in IndexCodeList)
            {
                if (code != null)
                {
                    codeFormatted = code.Trim();
                    if (codeFormatted.Length > 0)
                    {
                        Dictionary<string, TradeMessageTag> tags = new Dictionary<string, TradeMessageTag>(1);
                        TradeMessage msg = new TradeMessage();
                        tags.Add("CODE", new TradeMessageTag("CODE", codeFormatted));

                        msg.MessageId = "IND";
                        msg.MessageType = "51";
                        msg.Tags = tags;

                        if (State == (int)ProcessState.Ready)
                        {
                            SessionMgr.Send(msg);
                        }
                    }
                }
            }
        }

        public TransactionCharge GetTransactionCharge(string TxnChargeHash)
        {
            return pTransactionChargeBook.GetByHash(TxnChargeHash);
        }

        public void RequestTransactionCharge(List<string> TxnChargeHashList, bool Listen)
        {
            if (TxnChargeHashList != null)
            {
                string accountNo, exCode, stockCode;
                char side;
                List<decimal> priceList;
                List<decimal> qtyList;

                CultureInfo ci = new CultureInfo("en-us");

                foreach (string code in TxnChargeHashList)
                {
                    if (code != null)
                    {
                        Dictionary<string, TradeMessageTag> tags = new Dictionary<string, TradeMessageTag>(5);
                        TradeMessage msg = new TradeMessage();
                        msg.MessageId = "CCK";

                        TransactionCharge.ParseHash(code, out accountNo, out side, out exCode, out stockCode, out priceList, out qtyList);
/*
                        if (Stock.GetExchangeType(exCode) != ExchangeTypeEnum.HKG)
                        {
                            tags.Add("ACNO", new TradeMessageTag("ACNO", accountNo));
                            tags.Add("SIDE", new TradeMessageTag("SIDE", side.ToString()));
                            tags.Add("STKC", new TradeMessageTag("STKC", stockCode));
                            for (int i = 0; i < priceList.Count; i++)
                                tags.Add("PRC" + (i + 1), new TradeMessageTag("PRC" + (i + 1), priceList[i].ToString("G", ci)));
                            for (int i = 0; i < qtyList.Count; i++)
                                tags.Add("QTY" + (i + 1), new TradeMessageTag("QTY" + (i + 1), qtyList[i].ToString("G", ci)));

                            msg.MessageType = "31";
                        }
                        else
                        {*/
                            tags.Add("ACNO", new TradeMessageTag("ACNO", accountNo));
                            tags.Add("SIDE", new TradeMessageTag("SIDE", side.ToString()));
                            tags.Add("EXC", new TradeMessageTag("EXC", exCode));
                            tags.Add("STKC", new TradeMessageTag("STKC", stockCode));
                            for (int i = 0; i < priceList.Count; i++)
                                tags.Add("PRC" + (i + 1), new TradeMessageTag("PRC" + (i + 1), priceList[i].ToString("G", ci)));
                            for (int i = 0; i < qtyList.Count; i++)
                                tags.Add("QTY" + (i + 1), new TradeMessageTag("QTY" + (i + 1), qtyList[i].ToString("G", ci)));

                            msg.MessageType = "33";
//                        }

                        msg.Tags = tags;
                        SessionMgr.Send(msg);
                    }
                }
            }
        }

        public void UnrequestTransactionCharge(List<string> TxnChargeHashList)
        {

        }

        public bool OrderPlace(string AccountNo, char Side, string StockCode, decimal Price, int Quantity, char OrderType, bool AllOrNothing, bool ShortSell, int CreditCheckByPass, ExchangeTypeEnum ExType, out string ErrorMessage, out string ActionRefNo)
        {
            return OrderPlace(AccountNo, Side, StockCode, Price, Quantity, OrderType, AllOrNothing, ShortSell, CreditCheckByPass, ExType, '\0', false, null, out ErrorMessage, out ActionRefNo);
        }

        public bool OrderPlace(string AccountNo, char Side, string StockCode, decimal Price, int Quantity, char OrderType, bool AllOrNothing, bool ShortSell, int CreditCheckByPass, ExchangeTypeEnum ExType, char DPGW, bool AGGO, string OTP, out string ErrorMessage, out string ActionRefNo)
        {
            CreditCheckByPass = DetermineCreditCheckByPass(CreditCheckByPass);

            ActionRefNo = null;

            if (State != (int)ProcessState.Ready)
            {
                ErrorMessage = "Network not connected.";
                return false;
            }

            Action action = new Action();
            action.ActionNo = 0;
            action.ActionRefNo = Guid.NewGuid().ToString();
            action.ActionType = (int)Action.ActionTypeEnum.Place;
            action.Price = Price;
            action.Quantity = Quantity;

            Order order = new Order();
            order.OrderNoSort = pOrderBook.GetOrderNoSort(0);
            order.Actions.Add(action);
            order.AccountNo = AccountNo;
            order.Side = Side;
            order.StockCode = StockCode;
            order.Price = Price;
            order.Quantity = Quantity;
            order.OrderType = OrderType;
            order.Status = (int)Order.OrderStatusEnum.Sending;
            order.Memo = UserType == UserTypeEnum.Client ? "Internet Order" : "";
            order.ExType = ExType;
            order.DPGW = DPGW;
            order.IsAggregateOrder = AGGO;
            order.OTP = OTP;

            if (pOrderBook.OrderPlacePending)   // Prevent duplicated order due to e.g. delays in network, must wait until the pending order is resolved
            {
                ErrorMessage = "Previous new order send not yet completed.";
                return false;
            }

            pOrderBook.AddUpdate(order);
            pDPOrder.Execute(DataProcessor<Order>.Operation.CodeEnum.DataArrival, null, "", order);

            Dictionary<string, TradeMessageTag> tags = new Dictionary<string, TradeMessageTag>(7);

            tags.Add("ACNO", new TradeMessageTag("ACNO", AccountNo));
            tags.Add("SIDE", new TradeMessageTag("SIDE", Side.ToString()));
            tags.Add("EXC", new TradeMessageTag("EXC", Stock.GetExchangeCode(ExType)));
            tags.Add("STKC", new TradeMessageTag("STKC", StockCode));
            tags.Add("PRIC", new TradeMessageTag("PRIC", Price.ToString("0.000")));
            tags.Add("QTY", new TradeMessageTag("QTY", Quantity.ToString()));
            tags.Add("ORDT", new TradeMessageTag("ORDT", OrderType.ToString()));
            tags.Add("AORN", new TradeMessageTag("AORN", AllOrNothing ? "Y" : "N"));
            tags.Add("SSEL", new TradeMessageTag("SSEL", ShortSell ? "Y" : "N"));
            tags.Add("CCBP", new TradeMessageTag("CCBP", CreditCheckByPass.ToString()));
            tags.Add("REF", new TradeMessageTag("REF", action.ActionRefNo));
            tags.Add("DPGW", new TradeMessageTag("DPGW", DPGW.ToString().ToUpper()));
            tags.Add("AGGO", new TradeMessageTag("AGGO", AGGO ? "Y" : "N"));
            if (OTP != null && OTP.Trim().Length > 0)
                tags.Add("OTP", new TradeMessageTag("OTP", OTP));

            TradeMessage msg = new TradeMessage();
            msg.MessageId = "ACT";
            msg.MessageType = "01";
            msg.Tags = tags;

            SessionMgr.Send(msg);

            string CurrencySign = "$";
            if (ExType == ExchangeTypeEnum.SHG || ExType == ExchangeTypeEnum.SZE)
                CurrencySign = "￥";

            pSystemEventDelegate.Invoke(new SystemEvent("Action", AccountNo + " " + (Side == 'B' ? GetResxString("Buy") : GetResxString("Sell")) + " #" + StockCode + " " + Quantity.ToString("#,##0") + " " + GetResxString("Shares") + " " + CurrencySign + Price.ToString("0.###"),
                SystemEvent.SystemEventAlertType.None, SystemEvent.SystemEventSoundType.None, Color.FromArgb(255, 255, 160)));

            ErrorMessage = null;
            ActionRefNo = action.ActionRefNo;
            return true;
        }

        public bool OrderAmend(int OrderNo, decimal NewPrice, int NewQuantity, int CreditCheckByPass, out string ErrorMessage)
        {
            return OrderAmend(OrderNo, NewPrice, NewQuantity, CreditCheckByPass, "", out ErrorMessage);
        }

        public bool OrderAmend(int OrderNo, decimal NewPrice, int NewQuantity, int CreditCheckByPass, string OTP, out string ErrorMessage)
        {
            CreditCheckByPass = DetermineCreditCheckByPass(CreditCheckByPass);

            if (State != (int)ProcessState.Ready)
            {
                ErrorMessage = "Network not connected.";
                return false;
            }

            Order order = pOrderBook.SetOrderLastAction(OrderNo, "Amend");
            if (order == null)
            {
                ErrorMessage = "Order with action pending.";
                return false;
            }

            pDPOrder.Execute(DataProcessor<Order>.Operation.CodeEnum.DataArrival, null, "", order);

            Dictionary<string, TradeMessageTag> tags = new Dictionary<string, TradeMessageTag>(4);

            tags.Add("ORDN", new TradeMessageTag("ORDN", OrderNo.ToString()));
            tags.Add("NPRI", new TradeMessageTag("NPRI", NewPrice.ToString("0.0000")));
            tags.Add("NQTY", new TradeMessageTag("NQTY", NewQuantity.ToString()));
            tags.Add("CCBP", new TradeMessageTag("CCBP", CreditCheckByPass.ToString()));
            tags.Add("REF", new TradeMessageTag("REF", Guid.NewGuid().ToString()));
            if (OTP != null && OTP.Trim().Length > 0)
                tags.Add("OTP", new TradeMessageTag("OTP", OTP));

            TradeMessage msg = new TradeMessage();
            msg.MessageId = "ACT";
            msg.MessageType = "04";
            msg.Tags = tags;

            SessionMgr.Send(msg);

            string CurrencySign = "$";
            ExchangeTypeEnum ExType = ExchangeTypeEnum.HKG;
            if (order != null)
                ExType = order.ExType;
            if (ExType == ExchangeTypeEnum.SHG || ExType == ExchangeTypeEnum.SZE)
                CurrencySign = "￥";

            pSystemEventDelegate.Invoke(new SystemEvent("Action", order.AccountNo + " " + OrderNo.ToString() + " " + GetResxString("AmendTo") + " " + NewQuantity.ToString("#,##0") + " " + GetResxString("Shares") + " " + CurrencySign + NewPrice.ToString("0.###"),
                SystemEvent.SystemEventAlertType.None, SystemEvent.SystemEventSoundType.None, Color.FromArgb(255, 255, 160)));

            ErrorMessage = null;
            return true;
        }

        public bool OrderCancel(int OrderNo, out string ErrorMessage)
        {
            if (State != (int)ProcessState.Ready)
            {
                ErrorMessage = "Network not connected.";
                return false;
            }

            Order order = pOrderBook.SetOrderLastAction(OrderNo, "Cancel");
            if (order == null)
            {
                ErrorMessage = "Order with action pending.";
                return false;
            }

            pDPOrder.Execute(DataProcessor<Order>.Operation.CodeEnum.DataArrival, null, "", order);

            Dictionary<string, TradeMessageTag> tags = new Dictionary<string, TradeMessageTag>(2);

            tags.Add("ORDN", new TradeMessageTag("ORDN", OrderNo.ToString()));
            tags.Add("REF", new TradeMessageTag("REF", Guid.NewGuid().ToString()));

            TradeMessage msg = new TradeMessage();
            msg.MessageId = "ACT";
            msg.MessageType = "02";
            msg.Tags = tags;

            SessionMgr.Send(msg);

            pSystemEventDelegate.Invoke(new SystemEvent("Action", order.AccountNo + " " + OrderNo.ToString() + " " + GetResxString("Cancel"),
                SystemEvent.SystemEventAlertType.None, SystemEvent.SystemEventSoundType.None, Color.FromArgb(255, 255, 160)));

            ErrorMessage = null;
            return true;
        }

        public bool OrderCancel(List<int> OrderNoList, out string ErrorMessage, out List<int> OrderCancelNotAccepted)
        {
            ErrorMessage = null;
            OrderCancelNotAccepted = null;

            if (State != (int)ProcessState.Ready)
            {
                ErrorMessage = "Network not connected.";
                return false;
            }

            if (OrderNoList == null || OrderNoList.Count <= 0)
            {
                ErrorMessage = "Order cancel list is empty.";
                return false;
            }

            int orderCount = OrderNoList.Count;
            int orderNo;
            Order order = null;
            OrderCancelNotAccepted = new List<int>(orderCount);
            List<TradeMessage> msgList = new List<TradeMessage>(orderCount);

            for (int i = 0; i < orderCount; i++)
            {
                orderNo = OrderNoList[i];
                order = pOrderBook.SetOrderLastAction(orderNo, "Cancel");
                if (order == null)
                {
                    order = pOrderBook.GetByOrderNo(orderNo);
                    if (order == null || order.LastAction != "Cancel")
                    {
                        OrderCancelNotAccepted.Add(orderNo);
                        ErrorMessage = "Some orders with other action pending.";
                    }
                    else
                        pDPOrder.Execute(DataProcessor<Order>.Operation.CodeEnum.DataArrival, null, "", order);
                }
                else
                    pDPOrder.Execute(DataProcessor<Order>.Operation.CodeEnum.DataArrival, null, "", order);

                Dictionary<string, TradeMessageTag> tags = new Dictionary<string, TradeMessageTag>(2);

                tags.Add("ORDN", new TradeMessageTag("ORDN", orderNo.ToString()));
                tags.Add("REF", new TradeMessageTag("REF", Guid.NewGuid().ToString()));

                TradeMessage msg = new TradeMessage();
                msg.MessageId = "ACT";
                msg.MessageType = "02";
                msg.Tags = tags;

                msgList.Add(msg);

                pSystemEventDelegate.Invoke(new SystemEvent("Action", order.AccountNo + " " + orderNo.ToString() + " " + GetResxString("Cancel"),
                    SystemEvent.SystemEventAlertType.None, SystemEvent.SystemEventSoundType.None, Color.FromArgb(255, 255, 160)));
            }

            for (int i = 0; i < msgList.Count; i++)
                SessionMgr.Send(msgList[i]);

            return OrderCancelNotAccepted.Count <= 0 ? true : false;
        }

        public bool OrderReplied(int OrderNo, out string ErrorMessage)
        {
            if (State != (int)ProcessState.Ready)
            {
                ErrorMessage = "Network not connected.";
                return false;
            }

            Dictionary<string, TradeMessageTag> tags = new Dictionary<string, TradeMessageTag>(2);

            tags.Add("ORDN", new TradeMessageTag("ORDN", OrderNo.ToString()));
            tags.Add("REF", new TradeMessageTag("REF", Guid.NewGuid().ToString()));

            TradeMessage msg = new TradeMessage();
            msg.MessageId = "ACT";
            msg.MessageType = "05";
            msg.Tags = tags;

            SessionMgr.Send(msg);

            ErrorMessage = null;
            return true;
        }

        public bool OrderChangeAccount(int OrderNo, string AccountNo, out string ErrorMessage)
        {
            if (State != (int)ProcessState.Ready)
            {
                ErrorMessage = "Network not connected.";
                return false;
            }

            Dictionary<string, TradeMessageTag> tags = new Dictionary<string, TradeMessageTag>(2);

            tags.Add("ORDN", new TradeMessageTag("ORDN", OrderNo.ToString()));
            tags.Add("ACNO", new TradeMessageTag("ACNO", AccountNo != null ? AccountNo : ""));
            tags.Add("REF", new TradeMessageTag("REF", Guid.NewGuid().ToString()));

            TradeMessage msg = new TradeMessage();
            msg.MessageId = "ACT";
            msg.MessageType = "06";
            msg.Tags = tags;

            SessionMgr.Send(msg);

            ErrorMessage = null;
            return true;
        }

        #endregion


        #region "Device Token"

        private string GetDeviceToken(string UserId)
        {
            string devToken = null;
            string devTokenId = null;

            if (pDeviceTokenFilePath != null && pDeviceTokenFilePath.Length > 0 && UserId != null && UserId.Length > 0)
            {
                using (SHA512 sha = SHA512.Create())
                {
                    devTokenId = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes("23OwsfaFJ oWLjJO]LO703I# U%O@#J%TawsEaLfWWJ@W" + UserId + "RlKR3Lv#JRWGF:LgEZSfI#WJR @#U%$(:LF98()&@#_(*%&WE{K.,32")));
                    devTokenId = devTokenId.Remove('=').Replace('/', '.').Replace('+', 'i');
                }

                if (devTokenId != null)
                {
                    Settings dtSettings = new Settings(devTokenId);
                    dtSettings.ReadFromFile(pDeviceTokenFilePath, "DT");

                    devToken = dtSettings[devTokenId];

                    if (devToken != null && devToken.Length <= 0)
                        devToken = null;
                }
            }

            return devToken;
        }

        private void PutDeviceToken(string UserId, string DeviceToken)
        {
            string devTokenId = null;

            if (pDeviceTokenFilePath != null && pDeviceTokenFilePath.Length > 0 && UserId != null && UserId.Length > 0)
            {
                using (SHA512 sha = System.Security.Cryptography.SHA512.Create())
                {
                    devTokenId = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes("23OwsfaFJ oWLjJO]LO703I# U%O@#J%TawsEaLfWWJ@W" + UserId + "RlKR3Lv#JRWGF:LgEZSfI#WJR @#U%$(:LF98()&@#_(*%&WE{K.,32")));
                    devTokenId = devTokenId.Remove('=').Replace('/', '.').Replace('+', 'i');
                }

                if (devTokenId != null)
                {
                    Settings dtSettings = new Settings(devTokenId);

                    dtSettings[devTokenId] = DeviceToken ?? "";

                    dtSettings.SaveToFile(pDeviceTokenFilePath, "DT");
                }
            }
        }

        #endregion


        #region "Resource Management"

        private string GetResxString(string Key)
        {
            try
            {
                return ResManUI.GetString(Key, pCulture);
            }
            catch { }

            return null;
        }

        #endregion

        #region "State Process Delegates"

        private void OnSessionManagerStateChange(int State)
        {
            ProcessEvent.Set();
        }

        protected override void Preprocess(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
            if (Command == ProcessCommand.Start)
            {
                if (SessionMgr.State != (int)SessionManager.ProcessState.Ready)
                {
                    if (NextState == (int)ProcessState.Ready)
                    {
                        // Connection broken, reconnect from SessionLogin state
                        if (pSystemEventDelegate != null)
                        {
                            pSystemEventDelegate.Invoke(
                                new SystemEvent("Network Fail", "Connection Broken",
                                SystemEvent.SystemEventAlertType.Sound, SystemEvent.SystemEventSoundType.NetworkFailure, Color.Empty));
                        }

                        NextState = (int)ProcessState.Login;
                    }
                }
            }
            else if (Command == ProcessCommand.Stop)
            {
                if (NextState != (int)ProcessState.Stopped)
                {
                    NextState = (int)ProcessState.Disconnecting;
                }
            }
        }

        protected override void ProcessStopped(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
            AccountNoListUpdateList.Clear();
            pCentralUserSettings.Clear();
            pOrderBook.Clear();
            orderBookSyncTime = DateTime.MinValue;
            MaxOrderNo = 0;
            pOddLotOrderBook.Clear();
            AccountListUpdateList.Clear();
            AccountNoListUpdateList.Clear();
            pAccountBook.Clear();
            accountLastUpdate = null;
            pAEBook.Clear();
            AEBookUpdateList.Clear();
            MarketDict.Clear();
            pStockBook.Clear();
            pIndexBook.Clear();
            pTransactionChargeBook.Clear();
            MsgInOthers.Clear();
            pSpreadTables.Clear();

            if (Command == ProcessCommand.Start)
            {
                NextState = (int)ProcessState.Login;
            }
        }

        private void ProcessLogin(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
            if (SessionMgr.State == (int)SessionManager.ProcessState.Ready)
            {
                NextState = (int)ProcessState.Ready;

                PutDeviceToken(SessionMgr.UserId, SessionMgr.DeviceToken);

                if (pSystemEventDelegate != null)
                {
                    pSystemEventDelegate.Invoke(new SystemEvent("Network", "Server connected.",
                        SystemEvent.SystemEventAlertType.None, SystemEvent.SystemEventSoundType.NetworkFailure, Color.Empty));
                }
            }
            else if (SessionMgr.LastTradeError == LastErrorEnum.NoError)
            {
                if (!SessionMgr.IsStarted)
                {
                    SessionMgr.DeviceToken = GetDeviceToken(SessionMgr.UserId);
                    SessionMgr.Start();
                }
            }
            else if (!SessionMgr.IsStarted)
            {
                if (SessionMgr.LastTradeError == LastErrorEnum.InvalidVerificationCode || SessionMgr.LastTradeError == LastErrorEnum.SendingVerificationCode)
                {
                    PutDeviceToken(SessionMgr.UserId, null);
                }

                Stop(IProcessStopStyle.NoWait);
            }
        }

        private void ProcessReady(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
            if (StateChanged)
            {
                pStockRelationBook.LoadFromCsv(SessionMgr.StockRelationCsv);

                if (SessionMgr.UserType == UserTypeEnum.AE || SessionMgr.UserType == UserTypeEnum.Client)
                    RequestMarket();
            }

            ProcessReceived();
        }

        private void ProcessDisconnecting(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
            if (SessionMgr.State != (int)SessionManager.ProcessState.Stopped)
            {
                SessionMgr.Stop();
            }

            if (Command == ProcessCommand.Start)
            {   // Auto-Reconnect
                SessionMgr.Start();
                NextState = (int)ProcessState.Login;
                WaitTime = 0;
            }
            else if (Command == ProcessCommand.Stop)
            {
                NextState = (int)ProcessState.Stopped;
                WaitTime = 0;
            }
        }

        #endregion


        #region "Message Processors"

        private void OnMsgArrived(TradeMessage Message)
        {
            if (Message != null)
            {
                MsgInDelegate msgInDelegate;

                if (MsgInDelegates.TryGetValue(Message.MessageId, out msgInDelegate))
                    msgInDelegate.Invoke(Message);

                ProcessEvent.Set();
            }
        }

        private void ProcessReceived()
        {
            MsgInDelegate msgInDelegate;
            TradeMessage msg;

            while ((msg = MsgInOthers.Dequeue()) != null)
            {
                if (MsgInOthersDelegates.TryGetValue(msg.MessageId, out msgInDelegate))
                    msgInDelegate.Invoke(msg);
            }
        }

        private void ProcessReceivedOrderEvent(Order TheOrder)
        {
            if (TheOrder == null) return;

            DateTime EventTime = DateTime.MinValue;
            string EventMessageType = null;
            string EventMessage = null;

            SystemEvent.SystemEventAlertType EventAlertType = SystemEvent.SystemEventAlertType.None;
            SystemEvent.SystemEventSoundType EventSoundType = SystemEvent.SystemEventSoundType.None;

            if (TheOrder.Status == (int)Order.OrderStatusEnum.InvalidTradePass)
            {
                EventAlertType = SystemEvent.SystemEventAlertType.Sound | SystemEvent.SystemEventAlertType.MessageBox;
                EventSoundType = SystemEvent.SystemEventSoundType.Rejected;

                EventMessageType = "Rejected";
                EventMessage = String.Format(GetResxString("MsgInvalidTradePass") +
                    "\n\n" + (TheOrder.Side == 'B' ? GetResxString("Buy") : GetResxString("Sell")) + " " + GetResxString("Stock") + " #" +
                    TheOrder.StockCode + " $" + TheOrder.Price + " " + TheOrder.Quantity.ToString("#,##0") + " " + GetResxString("Shares"));
            }
            else
            {
                if (pOrderBookSynced &&
                    (TheOrder.BeforeChange != null || TheOrder.OrderNo > MaxOrderNo))
                {
                    if ((TheOrder.BeforeChange == null && TheOrder.Filled > 0) ||
                        (TheOrder.BeforeChange != null && TheOrder.Filled > TheOrder.BeforeChange.Filled))
                    {
                        EventAlertType |= SystemEvent.SystemEventAlertType.Sound;
                        EventSoundType = SystemEvent.SystemEventSoundType.Filled;
                    }
                    if (TheOrder.Status == (int)Order.OrderStatusEnum.Cancelled &&
                        (TheOrder.BeforeChange == null || TheOrder.BeforeChange.Status != (int)Order.OrderStatusEnum.Cancelled))
                    {
                        EventAlertType |= SystemEvent.SystemEventAlertType.Sound | SystemEvent.SystemEventAlertType.MessageBox;
                        EventSoundType = SystemEvent.SystemEventSoundType.Rejected;
                    }
                    if (TheOrder.Rejected || TheOrder.StatusMessage != null && TheOrder.StatusMessage.Trim().Length > 0)
                    {
                        if (TheOrder.StatusMessage != null && (
                            TheOrder.StatusMessage.IndexOf("Unfilled", StringComparison.InvariantCultureIgnoreCase) < 0 ||     // remove case for Unfilled quantity cancelled due to market closed
                            TheOrder.StatusMessage.IndexOf("market closed", StringComparison.InvariantCultureIgnoreCase) < 0))
                        {
                            EventAlertType |= SystemEvent.SystemEventAlertType.Sound | SystemEvent.SystemEventAlertType.MessageBox;
                            EventSoundType = SystemEvent.SystemEventSoundType.Rejected;
                        }
                    }
                }

                if ((TheOrder.BeforeChange == null &&
                    (TheOrder.Filled > 0 || TheOrder.Status == (int)Order.OrderStatusEnum.PartiallyCompleted)) ||
                    (TheOrder.BeforeChange != null &&
                    (TheOrder.Filled > TheOrder.BeforeChange.Filled || (TheOrder.Status == (int)Order.OrderStatusEnum.PartiallyCompleted && TheOrder.BeforeChange.Status != (int)Order.OrderStatusEnum.PartiallyCompleted))))
                {
                    string CurrencySign = (TheOrder.ExType == ExchangeTypeEnum.SHG || TheOrder.ExType == ExchangeTypeEnum.SZE) ? "￥" : "$";
                    if (TheOrder.Filled < TheOrder.Quantity)
                    {
                        EventTime = TheOrder.LastDealTime;
                        EventMessageType = TheOrder.Closed ? "Partial" : "More Deal";
                        EventMessage = String.Format("{0} {1} #{2} {3} {4:#,##0} / {5:#,##0} {6}{7}{8}",
                            UserType == UserTypeEnum.Client ? (GetResxString("Order") + " " + TheOrder.OrderNo.ToString()) : (TheOrder.AccountNo + " " + TheOrder.OrderNo.ToString()),
                            GetResxString(TheOrder.Side == 'B' ? "Buy" : "Sell"),
                            TheOrder.StockCodeZeroTrimmed, GetResxString("Filled"), TheOrder.Filled, TheOrder.Quantity,
                            GetResxString("AvgPrice"), CurrencySign, TheOrder.AvgPrice.ToString("0.000"));

                        if (TheOrder.StatusMessage != null && TheOrder.StatusMessage.Trim().Length > 0)
                        {
                            EventMessage += Environment.NewLine + Environment.NewLine + TheOrder.StatusMessage;
                        }
                    }
                    else
                    {
                        EventTime = TheOrder.LastDealTime;
                        EventMessageType = "OK";
                        EventMessage = String.Format("{0} {1} #{2} {3} {4:#,##0} - OK {5}{6}{7}",
                            UserType == UserTypeEnum.Client ? (GetResxString("Order") + " " + TheOrder.OrderNo.ToString()) : (TheOrder.AccountNo + " " + TheOrder.OrderNo.ToString()),
                            GetResxString(TheOrder.Side == 'B' ? "Buy" : "Sell"),
                            TheOrder.StockCodeZeroTrimmed, GetResxString("Filled"), TheOrder.Filled,
                            GetResxString("AvgPrice"), CurrencySign, TheOrder.AvgPrice.ToString("0.000"));
                    }
                }
                else if ((TheOrder.Rejected || TheOrder.Status != (int)Order.OrderStatusEnum.Cancelled && TheOrder.StatusMessage != null && TheOrder.StatusMessage.Trim().Length > 0) &&
                    (TheOrder.BeforeChange == null || TheOrder.Status != TheOrder.BeforeChange.Status || TheOrder.StatusMessage != TheOrder.BeforeChange.StatusMessage))
                {
                    EventMessageType = "Rejected";
                    EventMessage = String.Format("{0} {1} #{2} ${3:0.###} {4:#,##0} {5} {6}.",
                        UserType == UserTypeEnum.Client ? (GetResxString("Order") + " " + TheOrder.OrderNo.ToString()) : (TheOrder.AccountNo + " " + TheOrder.OrderNo.ToString()),
                        GetResxString(TheOrder.Side == 'B' ? "Buy" : "Sell"),
                        TheOrder.StockCodeZeroTrimmed, TheOrder.Price, TheOrder.Quantity, GetResxString("Shares"),
                        GetResxString("HasBeenRejected"), TheOrder.StatusMessage);
                }
                else if (TheOrder.Status == (int)Order.OrderStatusEnum.Cancelled &&
                    (TheOrder.BeforeChange == null || TheOrder.BeforeChange.Status != (int)Order.OrderStatusEnum.Cancelled))
                {
                    EventMessageType = "Cancelled";
                    EventMessage = String.Format("{0} {1} #{2} ${3:0.###} {4:#,##0} {5} {6}.",
                        UserType == UserTypeEnum.Client ? (GetResxString("Order") + " " + TheOrder.OrderNo.ToString()) : (TheOrder.AccountNo + " " + TheOrder.OrderNo.ToString()),
                        GetResxString(TheOrder.Side == 'B' ? "Buy" : "Sell"),
                        TheOrder.StockCodeZeroTrimmed, TheOrder.Price, TheOrder.Quantity, GetResxString("Shares"),
                        GetResxString("HasBeenCancelled"), TheOrder.StatusMessage);
                }
            }

            if (EventMessageType != null)
            {
                pSystemEventDelegate.Invoke(
                    new SystemEvent(EventMessageType, EventMessage, EventTime, EventAlertType, EventSoundType, Color.Empty, TheOrder.OrderNo, TheOrder.OrderNoSort, TheOrder.AECode));
            }
        }

        private int DetermineCreditCheckByPass(int RequestedCreditCheckByPassMode)
        {
            int allowedCreditCheckByPass = 0; // not allow any kind of credit check by pass by default

            string value = null;

            lock (pSettingFormCentralMutex)
            {
                value = pSettingFormCentral["AllowCreditCheckByPass"];
            }

            if (value == null || !int.TryParse(value, out allowedCreditCheckByPass))  // invalid CreditCheckByPass results in not allowing any credit check by pass
                allowedCreditCheckByPass = 0;
            else if (RequestedCreditCheckByPassMode > 0)    // > 0 means requesting some kind of credit checking relaxation
            {
                if (allowedCreditCheckByPass != RequestedCreditCheckByPassMode)    // reject the credit check by pass request if the requested credit check by pass mode is not equal to the allowed credit check by pass mode
                    allowedCreditCheckByPass = 0;
            }

            // Note:
            // RequestedCreditCheckByPassMode <= 0 means to follow pSettingFormCentral["AllowCreditCheckByPass"]

            return allowedCreditCheckByPass;
        }

        #endregion
    }
}
