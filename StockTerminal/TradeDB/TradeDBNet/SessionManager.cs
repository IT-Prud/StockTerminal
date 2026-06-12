using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using Processors;
using Utils;
using Logger;
using StockTerminal;

namespace TradeDB.Net
{
    public class SessionManager : Processor
    {
        #region "Define delegates for new ProcessState"

        //public new enum ProcessState { Stopped, Connecting, Login, LoginWait, Ready, Disconnecting, MaxState };
        public new enum ProcessState { Stopped, Connecting, Ready, Disconnecting, MaxState };

        protected override void InitializeProcessDelegates()
        {
            ProcessDelegates = new ProcessDelegate[(int)ProcessState.MaxState];
            ProcessDelegates[(int)ProcessState.Stopped] = new ProcessDelegate(ProcessStopped);
            ProcessDelegates[(int)ProcessState.Connecting] = new ProcessDelegate(ProcessConnecting);
            ProcessDelegates[(int)ProcessState.Ready] = new ProcessDelegate(ProcessReady);
            ProcessDelegates[(int)ProcessState.Disconnecting] = new ProcessDelegate(ProcessDisconnecting);
        }

        #endregion

        private readonly TradeConnector pTradeConn = new TradeConnector();
        private readonly QuoteConnector pQuoteConn = new QuoteConnector();
        private readonly UdpData pStockUdpData = new UdpData();
        private bool pStockUdpDataEnable = false;

        public StateChangedDelegate StateChangedQuote;

        private TradeConnector.MsgArrivalDelegate msgArrivalDelegate = null;

        private string pUserId = null;

        private string pLogFolder = null;
        private Log MsgInLog = null;
        private bool pMessageLogEnabled = false;
        private TradeMessage keepAliveMsg = null;


        public SessionManager()
        {
            keepAliveMsg = new TradeMessage();
            keepAliveMsg.MessageId = "LRE";
            keepAliveMsg.MessageType = "05";

            pTradeConn.EncryptionMode = System.Security.Cryptography.CipherMode.CBC;
            pTradeConn.EncryptionBlockSize = 128;
            pTradeConn.EncryptionKeySize = 128;
            pTradeConn.KeepAliveMessage = keepAliveMsg;
            pTradeConn.StateChanged += new StateChangedDelegate(OnTradeConnectorStateChange);
            pTradeConn.MsgArrival += new TradeConnector.MsgArrivalDelegate(OnMsgArrival);

            pQuoteConn.KeepAliveMessage = keepAliveMsg;
            pQuoteConn.StateChanged += new StateChangedDelegate(OnQuoteConnectorStateChange);
            pQuoteConn.MsgArrival += new QuoteConnector.MsgArrivalDelegate(OnMsgArrival);

            pStockUdpData.MsgArrival += new UdpData.MsgArrivalDelegate(OnMsgArrival);
        }


        #region "Class Properties"

        public HostEndPoint CurrentLoginHostTrade
        {
            get { return pTradeConn.CurrentSessionHost; }
        }

        public HostEndPoint CurrentLoginHostQuote
        {
            get { return pTradeConn.CurrentSessionHost; }
        }

        public string UserId
        {
            get { return pUserId; }
            set
            {
                pUserId = value;
                pTradeConn.UserId = value;
                pQuoteConn.UserId = value;
                PrepareLog();
            }
        }

        public string Password
        {
            get { return pTradeConn.Password; }
            set { pTradeConn.Password = value; pQuoteConn.Password = value; }
        }

        public int PasswordForceChange
        {
            get { return pTradeConn.PasswordForceChange; }
        }

        public string VerificationCode
        {
            get { return pTradeConn.VerificationCode; }
            set { pTradeConn.VerificationCode = value; pQuoteConn.VerificationCode = value; }
        }

        public string VerificationCodeSendMethod
        {
            get { return pTradeConn.VerificationCodeSendMethod; }
            set { pTradeConn.VerificationCodeSendMethod = value; pQuoteConn.VerificationCodeSendMethod = value; }
        }

        public string DeviceToken
        {
            get { return pTradeConn.DeviceToken; }
            set { pTradeConn.DeviceToken = value; pQuoteConn.DeviceToken = value; }
        }

        public string UserSystemName
        {
            get { return pTradeConn.UserSystemName; }
            set { pTradeConn.UserSystemName = value; pQuoteConn.UserSystemName = value; }
        }

        public string UserSystemVersion
        {
            get { return pTradeConn.UserSystemVersion; }
            set { pTradeConn.UserSystemVersion = value; pQuoteConn.UserSystemVersion = value; }
        }

        public string IPType
        {
            get { return pTradeConn.IPType; }
        }

        public string DeploymentGroupName
        {
            get { return pTradeConn.DeploymentGroupName; }
            set { pTradeConn.DeploymentGroupName = value; pQuoteConn.DeploymentGroupName = value; }
        }

        public string Email
        {
            get { return pTradeConn.Email; }
        }

        public string SMSNo
        {
            get { return pTradeConn.SMSNo; }
        }

        public string StockRelationCsv
        {
            get { return pTradeConn.StockRelationCsv; }
        }

        public UserTypeEnum UserType
        {
            get { return pTradeConn.UserType; }
        }

        public IPEndPoint StockUdpEP
        {
            get { return pStockUdpData.TheIPEndPoint; }
            set { pStockUdpData.TheIPEndPoint = value; }
        }

        public bool StockUdpDataEnable
        {
            get { return pStockUdpDataEnable; }
            set { pStockUdpDataEnable = value; }
        }

        public bool MessageLogEnabled
        {
            get { return pMessageLogEnabled; }
            set
            {
                pMessageLogEnabled = value;
                if (pTradeConn != null) pTradeConn.DataLogEnabled = pMessageLogEnabled;
                if (pQuoteConn != null) pQuoteConn.DataLogEnabled = pMessageLogEnabled;
            }
        }

        public string LogFolder
        {
            get { return pLogFolder; }
            set
            {
                pLogFolder = value;
                PrepareLog();
            }
        }

        public int QuoteState
        {
            get { return pQuoteConn.State; }
        }

        public int TradeState
        {
            get { return State; }
        }

        public LastErrorEnum LastTradeError
        {
            get { return pTradeConn.LastError; }
        }

        public LastErrorEnum LastQuoteError
        {
            get { return pQuoteConn.LastError; }
        }

        public DateTime ServerTime
        {
            get { return pTradeConn.ServerTime > pQuoteConn.ServerTime ? pTradeConn.ServerTime : pQuoteConn.ServerTime; }
        }

        public SpreadTableSet SpreadTables
        {
            get { return pQuoteConn.SpreadTables; }
            set { pQuoteConn.SpreadTables = value; }
        }

        #endregion


        #region "Class Methods"

        public void AddLoginHostTrade(HostEndPoint HostEP)
        {
            pTradeConn.AddSessionHost(HostEP);
        }

        public void ClearLoginHostTrade()
        {
            pTradeConn.ClearSessionHost();
        }

        public void AddLoginHostQuote(HostEndPoint HostEP)
        {
            pQuoteConn.AddSessionHost(HostEP);
        }

        public void ClearLoginHostQuote()
        {
            pQuoteConn.ClearSessionHost();
        }

        public void ListenMessage(TradeConnector.MsgArrivalDelegate msgArrivalDelegate)
        {
            if (msgArrivalDelegate == null) return;

            if (this.msgArrivalDelegate == null)
            {
                this.msgArrivalDelegate = msgArrivalDelegate;
            }
            else
            {
                this.msgArrivalDelegate += msgArrivalDelegate;
            }
        }

        public void Send(TradeMessage Msg)
        {
            if (Msg != null)
            {
                if (Msg.MessageId == "SRE")
                {
                    pQuoteConn.Send(Msg);
                }
                else
                {
                    pTradeConn.Send(Msg);
                }
            }
        }

        #endregion


        #region "State Process Delegates"

        protected override void Preprocess(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
            if (Command == ProcessCommand.Start)
            {
                if (UserType != UserTypeEnum.Quote && pTradeConn.State != (int)TradeConnector.ProcessState.Ready)
                {
                    if (NextState == (int)ProcessState.Ready)
                    {
                        // Connection broken, reconnect from SessionLogin state
                        NextState = (int)ProcessState.Connecting;
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
            if (Command == ProcessCommand.Start)
            {
                NextState = (int)ProcessState.Connecting;
            }

            if (pMessageLogEnabled)
            {
                if (MsgInLog != null)
                {
                    MsgInLog.Purge(7);
                    MsgInLog.Close();
                }
            }
        }

        private void ProcessConnecting(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
            if (UserType == UserTypeEnum.Quote || pTradeConn.State == (int)TradeConnector.ProcessState.Ready)
            {
                if (!pQuoteConn.IsStarted)
                    pQuoteConn.Start();

                NextState = (int)ProcessState.Ready;
            }
            else if (pTradeConn.LastError == LastErrorEnum.NoError)
            {
                if (!pTradeConn.IsStarted)
                    pTradeConn.Start();
            }
            else if (!pTradeConn.IsStarted)
            {
                Stop(IProcessStopStyle.NoWait);
            }
        }

        private void ProcessReady(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
            if (UserType != UserTypeEnum.Quote && pTradeConn.State != (int)TradeConnector.ProcessState.Ready)
            {
                NextState = (int)ProcessState.Disconnecting;
            }
            else if (StateChanged) // && !pQuoteConn.IsStarted)
            {
                if (pStockUdpDataEnable)
                    pStockUdpData.Start();
            }
        }

        private void ProcessDisconnecting(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
            if (Command == ProcessCommand.Start)
            {
                NextState = (int)ProcessState.Connecting;         // Auto-Reconnect
            }
            else if (Command == ProcessCommand.Stop)
            {
                if (pTradeConn.IsStarted) pTradeConn.Stop(IProcessStopStyle.NoWait);
                if (pQuoteConn.IsStarted) pQuoteConn.Stop(IProcessStopStyle.NoWait);
                if (pStockUdpData.IsStarted) pStockUdpData.Stop(IProcessStopStyle.NoWait);

                if (pTradeConn.State == (int)TradeConnector.ProcessState.Stopped &&
                    pQuoteConn.State == (int)QuoteConnector.ProcessState.Stopped &&
                    pStockUdpData.State == (int)UdpData.ProcessState.Stopped)
                {
                    NextState = (int)ProcessState.Stopped;
                }
                else
                {
                    WaitTime = 20000000L;
                }
            }
        }

        #endregion


        private void PrepareLog()
        {
            string path = null;
            
            if (pLogFolder != null)
            {
                path = (pUserId != null && pUserId.Length > 0) ? Path.Combine(pLogFolder, pUserId) : pLogFolder;
                pTradeConn.LogFolder = path;
                pQuoteConn.LogFolder = path;
                MsgInLog = Program.TheLogProcessor.Open("MessageInLog", Path.Combine(path, @"__MessageLog\"), "In", ".log");
            }
            else if (MsgInLog != null)
            {
                pTradeConn.LogFolder = null;
                pQuoteConn.LogFolder = null;
                MsgInLog.Close();
                MsgInLog.Dispose();
                MsgInLog = null;
            }
        }

        private void OnTradeConnectorStateChange(int State)
        {
            ProcessEvent.Set();
        }

        private void OnQuoteConnectorStateChange(int State)
        {
            if (StateChangedQuote != null)
            {
                StateChangedQuote.Invoke(State);
            }
        }

        private void OnMsgArrival(TradeMessage Message)
        {
            msgArrivalDelegate.Invoke(Message);
        }
    }
}
