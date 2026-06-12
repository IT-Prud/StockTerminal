using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web;
using Processors;
using Utils;
using Logger;
using StockTerminal;

namespace TradeDB.Net
{
    public partial class TradeConnector : Processor
    {
        #region "Define delegates for new ProcessState"

        public new enum ProcessState { Stopped, SessionLogin, Connecting, ServerLogin, Loading, Ready, Disconnecting, MaxState };

        protected override void InitializeProcessDelegates()
        {
            ProcessDelegates = new ProcessDelegate[(int)ProcessState.MaxState];
            ProcessDelegates[(int)ProcessState.Stopped] = new ProcessDelegate(ProcessStopped);
            ProcessDelegates[(int)ProcessState.SessionLogin] = new ProcessDelegate(ProcessSessionLogin);
            ProcessDelegates[(int)ProcessState.Connecting] = new ProcessDelegate(ProcessConnecting);
            ProcessDelegates[(int)ProcessState.ServerLogin] = new ProcessDelegate(ProcessServerLogin);
            ProcessDelegates[(int)ProcessState.Loading] = new ProcessDelegate(ProcessLoading);
            ProcessDelegates[(int)ProcessState.Ready] = new ProcessDelegate(ProcessReady);
            ProcessDelegates[(int)ProcessState.Disconnecting] = new ProcessDelegate(ProcessDisconnecting);
        }

        #endregion


        public delegate void MsgArrivalDelegate(TradeMessage Message);

        public MsgArrivalDelegate MsgArrival = null;

        public TradeMessage KeepAliveMessage = null;

        private int TimeLastBroken = -100000;
        private int LoginAttemptCount = 0;  // reset to zero if login success (State == Ready)
        private int MaxLoginAttemptCount = 0;   // normally will be set to the number of session hosts but will be increased to (number of session hosts x 10) during reconnection

        private readonly ServiceAccessPointManager sapMgr = new ServiceAccessPointManager();

        private string pUserId = null;
        private string pPassword = null;
        private string pPasswordSalt = null;
        private int pPasswordForceChange = 0;
        private string pVerificationCode = null;
        private string pVerificationCodeSendMethod = null;
        private string pDeviceToken = null;
        private string pUserSystemName = null;
        private string pUserSystemVersion = null;
        private string pIPType = null;
        private string pDeploymentGroupName = null;
        private string pEmail = null;
        private string pSMSNo = null;
        private string pStockRelationCsv = null;
        private LastErrorEnum pLastError = LastErrorEnum.NoError;
        private int pSessionId = -1;
        private UserTypeEnum pUserType = UserTypeEnum.Client;
        private int ServerLoginState = 0;
        private readonly Queue<TradeMessage> SessionMessageQueue = new Queue<TradeMessage>(20);
        private DateTime ServerTimeLast = DateTime.MinValue;
        private int ServerTimeLocalTick = -10000;
        private bool ServerClientTimeDiffValid = false; // once a LRE05 with valid time arrived, this value will be true

        private int SendLock = 0;

        private readonly RijndaelManaged AESCrypto = new RijndaelManaged();
        private ICryptoTransform Encryptor = null;
        private ICryptoTransform Decryptor = null;

        private readonly MessageRaw.MessageRawPool MsgRawPool = new MessageRaw.MessageRawPool();
        private readonly QueueLockFree<MessageRaw> MsgRawQueue = new QueueLockFree<MessageRaw>();
        
        private readonly DirectBuffer ReceiveBuffer = new DirectBuffer(1048576);
        private readonly DirectBuffer MsgBuffer = new DirectBuffer(1048576);
		private readonly byte[] DcpBuffer = new byte[2097152];
        private int TimeLastReceive = 0;

        private readonly TcpClientSocket TcpSock = new TcpClientSocket();
        private int TimeLastSend = 0;

        private string pLogFolder = null;
        private Log DataInLog = null;
        private Log DataOutLog = null;
        public bool DataLogEnabled = false;

        private int MsgSeqNum = 0;


        public TradeConnector()
        {
            TcpSock.ConnectComplete += OnSocketConnectComplete;
            TcpSock.DataArrival += OnSocketDataArrival;

            TcpSock.MsgRawPool = MsgRawPool;

            PrepareCrypters();
        }

        public HostEndPoint CurrentSessionHost
        {
            get { return sapMgr.CurrentSessionHost; }
        }

        public string UserId
        {
            get { return pUserId; }
            set
            {
                pUserId = value;
            }
        }

        public string Password
        {
            get { return pPassword; }
            set { pPassword = value; }
        }

        public string PasswordSalt
        {
            get { return pPasswordSalt; }
            set { pPasswordSalt = value; }
        }

        public int PasswordForceChange
        {
            get { return pPasswordForceChange; }
        }

        public string IPType
        {
            get { return pIPType; }
        }

        public string VerificationCode
        {
            get { return pVerificationCode; }
            set { pVerificationCode = value; }
        }

        public string VerificationCodeSendMethod
        {
            get { return pVerificationCodeSendMethod; }
            set { pVerificationCodeSendMethod = value; }
        }

        public string DeviceToken
        {
            get { return pDeviceToken; }
            set { pDeviceToken = value; }
        }

        public string UserSystemName
        {
            get { return pUserSystemName; }
            set { pUserSystemName = value; }
        }

        public string UserSystemVersion
        {
            get { return pUserSystemVersion; }
            set { pUserSystemVersion = value; }
        }

        public string DeploymentGroupName
        {
            get { return pDeploymentGroupName; }
            set { pDeploymentGroupName = value; }
        }

        public string Email
        {
            get { return pEmail; }
        }

        public string SMSNo
        {
            get { return pSMSNo; }
        }

        public string StockRelationCsv
        {
            get { return pStockRelationCsv; }
        }

        public UserTypeEnum UserType
        {
            get { return pUserId != "" ? pUserType : UserTypeEnum.Quote; }
        }

        public int EncryptionBlockSize
        {
            get { return AESCrypto.BlockSize; }
            set { AESCrypto.BlockSize = value; }
        }

        public int EncryptionKeySize
        {
            get { return AESCrypto.KeySize; }
            set { AESCrypto.KeySize = value; }
        }

        public CipherMode EncryptionMode
        {
            get { return AESCrypto.Mode; }
            set { AESCrypto.Mode = value; }
        }

        public string LogFolder
        {
            get { return pLogFolder; }
            set
            {
                pLogFolder = value;
                if (pLogFolder != null)
                {
                    DataInLog = Program.TheLogProcessor.Open("SocketTradeInLog", Path.Combine(pLogFolder, @"__SocketLog\Trade\"), "In", ".log");
                    DataInLog.FlushAfterAppend = true;
                    DataOutLog = Program.TheLogProcessor.Open("SocketTradeOutLog", Path.Combine(pLogFolder, @"__SocketLog\Trade\"), "Out", ".log");
                    DataOutLog.FlushAfterAppend = true;
                }
                else
                {
                    if (DataInLog != null)
                    {
                        DataInLog.Close();
                        DataInLog.Dispose();
                        DataInLog = null;
                    }
                    if (DataOutLog != null)
                    {
                        DataOutLog.Close();
                        DataOutLog.Dispose();
                        DataOutLog = null;
                    }
                }
            }
        }

        public DateTime ServerTime
        {
            get
            {
                if (ServerClientTimeDiffValid == true && State == (int)ProcessState.Ready)
                {
                    return ServerTimeLast.AddMilliseconds(ProcessTimer.TickCount - ServerTimeLocalTick);
                }
                return DateTime.MinValue;
            }
        }

        public LastErrorEnum LastError
        {
            get { return pLastError; }
        }

        public void AddSessionHost(HostEndPoint HostEP)
        {
            sapMgr.AddSessionHost(HostEP);

            if (MaxLoginAttemptCount >= 0)
                MaxLoginAttemptCount = sapMgr.SessionHostCount;
        }

        public void ClearSessionHost()
        {
            sapMgr.ClearSessionHost();

            if (MaxLoginAttemptCount > 0)
                MaxLoginAttemptCount = 0;
        }

        public void SendPlain(TradeMessage Message)
        {
            if (Message != null)
            {
                string msgSeqNumStr;
                string errorMessage;
                MessageRaw msgRaw;

                msgSeqNumStr = (Interlocked.Increment(ref MsgSeqNum) & 0x3ffffff).ToString("00000000");
                Message.SourceId = msgSeqNumStr.Substring(0, 4);
                Message.DestinationId = msgSeqNumStr.Substring(4, 4);

                msgRaw = Message.GetRaw(MsgRawPool);

                if (TcpSock.Send(msgRaw, out errorMessage))
                    AppendOutLog("Sent", Encoding.ASCII.GetString(msgRaw.Data, 0, msgRaw.Count));
                else
                    AppendOutLog("SendError", Encoding.ASCII.GetString(msgRaw.Data, 0, msgRaw.Count) + " - " + errorMessage);

                TimeLastSend = ProcessTimer.TickCount;
            }
        }

        public void Send(TradeMessage Message)
        {
            if (Message != null)
            {
                string msgSeqNumStr;
                string errorMessage;
                MessageRaw msgRaw;
                int rawSize, encLength;
                byte[] encPreBuffer;

                rawSize = Message.TotalLength;
                encLength = rawSize + (16 - (rawSize & 15) & 15);

                encPreBuffer = new byte[encLength];

                msgRaw = MsgRawPool.Get(encLength);
                msgRaw.Count = encLength;

                msgSeqNumStr = (Interlocked.Increment(ref MsgSeqNum) & 0x3ffffff).ToString("00000000");
                Message.SourceId = msgSeqNumStr.Substring(0, 4);
                Message.DestinationId = msgSeqNumStr.Substring(4, 4);

                Message.ToRaw(encPreBuffer, 0);

                while (Interlocked.CompareExchange(ref SendLock, 1, 0) != 0) ;

                try
                {
                    Encryptor.TransformBlock(encPreBuffer, 0, encLength, msgRaw.Data, 0);

                    if (TcpSock.Send(msgRaw, out errorMessage))
                        AppendOutLog("Sent", Encoding.ASCII.GetString(encPreBuffer, 0, rawSize));
                    else
                        AppendOutLog("SendError", Encoding.ASCII.GetString(encPreBuffer, 0, rawSize) + " - " + errorMessage);

                    TimeLastSend = ProcessTimer.TickCount;
                }
                catch (Exception exp)
                {
                    AppendOutLog("EncryptError", Encoding.ASCII.GetString(encPreBuffer, 0, rawSize) + "; " + exp.Message);
                }
                finally
                {
                    Interlocked.Exchange(ref SendLock, 0);
                }
            }
        }

        #region "Process Events"

        protected override void Preprocess(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
            if (NextState == (int)ProcessState.Stopped)
            {
                if (Command == ProcessCommand.Start) NextState = (int)ProcessState.SessionLogin;
            }
            else if (Command == ProcessCommand.Stop)
            {
                NextState = (int)ProcessState.Disconnecting;
            }
        }

        protected override void ProcessStopped(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
            if (DataLogEnabled)
            {
                if (DataInLog != null)
                {
                    DataInLog.Purge(7);
                    DataInLog.Close();  // ensure file handle is properly closed
                }
                if (DataOutLog != null)
                {
                    DataOutLog.Purge(7);
                    DataOutLog.Close(); // ensure file handle is properly closed
                }
            }

            pSessionId = -1;
            pIPType = null;
            pUserType = UserTypeEnum.Client;
            pEmail = null;
            pSMSNo = null;
            pStockRelationCsv = null;
            TcpSock.RemoteEP = null;
        }

        private void ProcessSessionLogin(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
            int LastBrokenElapsed;
            int LastBrokenReloginDelay = (new Random()).Next(2000, 5000);

            if (StateChanged)
                pLastError = LastErrorEnum.NoError;

            if (pLastError == LastErrorEnum.InvalidPassword || pLastError == LastErrorEnum.SendingVerificationCode || pLastError == LastErrorEnum.InvalidVerificationCode)
            {
                Stop(IProcessStopStyle.NoWait);
            }
            else if (StateChanged && (LastBrokenElapsed = (ProcessTimer.TickCount - TimeLastBroken)) < LastBrokenReloginDelay)
            {
                // Connection broken, wait 2~5 seconds before going to SessionLogin to prevent previous heartbeat not yet expired.
                WaitTime = (LastBrokenReloginDelay - LastBrokenElapsed) * 10000L;
            }
            else if (LoginAttemptCount < MaxLoginAttemptCount)
            {
                if (LoginAttemptCount == 0)
                {
                    sapMgr.SetOrigin(); // keep the first tried login host so that later can insert delay time when between repeat cycles.
                    sapMgr.ClearAttemptedServer();
                }

                int LoginTimeLastAttempt = ProcessTimer.TickCount;
                LoginAttemptCount++;

                if (GetPasswordSalt() == 0)
                {
                    switch (SessionLogin())
                    {
                        case 0:
                            NextState = (int)ProcessState.Connecting;
                            break;

                        case -1000:
                            pLastError = LastErrorEnum.LoginNoResponse;
                            WaitTime = sapMgr.GoNextSessionHost() ?
                                Math.Max(0, Math.Min(8000, 8000 - (ProcessTimer.TickCount - LoginTimeLastAttempt))) * 10000L : 0; // force wait for 8 seconds before stop to reduce the rate of brute force attack
                            break;

                        case -2000: // Invalid UserID or Password
                            pLastError = LastErrorEnum.InvalidPassword;
                            WaitTime = Math.Max(0, Math.Min(8000, 8000 - (ProcessTimer.TickCount - LoginTimeLastAttempt))) * 10000L;
                            break;

                        case -4000: // Service unavailable
                            pLastError = LastErrorEnum.ServiceUnavailable;
                            WaitTime = sapMgr.GoNextSessionHost() ?
                                Math.Max(0, Math.Min(8000, 8000 - (ProcessTimer.TickCount - LoginTimeLastAttempt))) * 10000L : 0; // force wait for 8 seconds before stop to reduce the rate of brute force attack
                            break;

                        case -5000: // Invalid 2nd factor
                            pLastError = LastErrorEnum.InvalidVerificationCode;
                            WaitTime = 0;
                            break;

                        case -6000: // Sending VerificationCode
                            pLastError = LastErrorEnum.SendingVerificationCode;
                            WaitTime = 0;
                            break;

                        default:
                            pLastError = LastErrorEnum.InvalidPassword;
                            LoginAttemptCount = int.MaxValue;       // ensure no retry, do not call stop() because this will allow user retry immediately
                            WaitTime = Math.Max(0, Math.Min(8000, 8000 - (ProcessTimer.TickCount - LoginTimeLastAttempt))) * 10000L;  // force wait for 8 seconds before stop to reduce the rate of brute force attack
                            break;
                    }
                }
                else
                {
                    pLastError = LastErrorEnum.LoginNoResponse;
                    WaitTime = sapMgr.GoNextSessionHost() ?
                        Math.Max(0, Math.Min(8000, 8000 - (ProcessTimer.TickCount - LoginTimeLastAttempt))) * 10000L : 0; // force wait for 8 seconds before stop to reduce the rate of brute force attack
                }
            }
            else
            {
                pLastError = LastErrorEnum.MaxLoginAttemptReached;
                WaitTime = 0;
                Stop(IProcessStopStyle.NoWait);
            }
        }

        private void ProcessConnecting(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
            string errorMessage;

            if (StateChanged)
            {
                PrepareCrypters();
                MsgRawQueue.Clear();
                ReceiveBuffer.Clear();
                MsgBuffer.Clear();
            }

            if (!TcpSock.IsConnected)
            {
                if (StateChanged)
                {
                    sapMgr.AddAttemptedServer(TcpSock.RemoteEP);

                    TcpSock.Close();

                    if (!TcpSock.Connect(out errorMessage))
                        AppendOutLog("Connect Error", TcpSock.RemoteEP.ToString() + " - " + errorMessage);
                }
                else
                {
                    NextState = (int)ProcessState.SessionLogin;
                }
            }
            else
            {
                TimeLastSend = ProcessTimer.TickCount;
                ServerLoginState = 0;
                NextState = (int)ProcessState.ServerLogin;
                TcpSock.BeginReceive();
            }
        }

        private void ProcessServerLogin(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
            string errorMessage;

            switch (ServerLoginState)
            {
                case 0:
                    SessionMessageQueue.Clear();
                    ServerLoginState = 1;
                    if (ServerLoginSessionID())
                    {
                        TimeLastReceive = ProcessTimer.TickCount;
                        WaitTime = 50000000L;
                    }
                    else
                    {
                        NextState = (int)ProcessState.Disconnecting;
                    }
                    break;

                case 1:
                    if (!ProcessReceived(out errorMessage))
                    {
                        TimeLastBroken = ProcessTimer.TickCount;
                        NextState = (int)ProcessState.Disconnecting;
                    }
                    else if (ProcessSessionMessage(NextState))
                    {
                        WaitTime = 0;
                    }
                    else if (!TcpSock.IsConnected || (ProcessTimer.TickCount - TimeLastReceive) > 3000)
                    {
                        TimeLastBroken = ProcessTimer.TickCount;
                        NextState = (int)ProcessState.Disconnecting;
                    }
                    else
                    {
                        WaitTime = 30000000L;
                    }
                    break;

                case 2:
                    ServerLoginState = 3;
                    if (ServerLoginUserID())
                    {
                        WaitTime = 30000000L;
                    }
                    else
                    {
                        NextState = (int)ProcessState.Disconnecting;
                    }
                    break;

                case 3:
                    if (!ProcessReceived(out errorMessage))
                    {
                        TimeLastBroken = ProcessTimer.TickCount;
                        NextState = (int)ProcessState.Disconnecting;
                    }
                    else if (ProcessSessionMessage(NextState))
                    {
                        WaitTime = 0;
                    }
                    else if (!TcpSock.IsConnected || (ProcessTimer.TickCount - TimeLastReceive) > 3000)
                    {
                        TimeLastBroken = ProcessTimer.TickCount;
                        NextState = (int)ProcessState.Disconnecting;
                    }
                    else
                    {
                        WaitTime = 30000000L;
                    }
                    break;

                case 4:
                    NextState = (int)ProcessState.Loading;
                    break;

                case 6: // Force Logout
                    Stop(IProcessStopStyle.NoWait);
                    break;

                default: // Fatal Error
                    NextState = (int)ProcessState.Disconnecting;
                    break;
            }
        }

        private void ProcessLoading(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
            if (!LoadStockRelation())
            {
                if (NextSubState < 2)
                {
                    NextSubState++;
                    WaitTime = 10000000L;
                }
                else
                {
                    pLastError = LastErrorEnum.LoadDataFailed;
                    Stop(IProcessStopStyle.NoWait);
                }
            }
            else
                NextState = (int)ProcessState.Ready;
        }

        private void ProcessReady(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
            string errorMessage;

            if (StateChanged)
            {
                LoginAttemptCount = 0;
                MaxLoginAttemptCount = sapMgr.SessionHostCount * 10;  // in case of reconnection
            }

            if (pLastError != LastErrorEnum.ForceLogout && TcpSock.IsConnected)
            {
                if (!ProcessReceived(out errorMessage))
                {
                    AppendInLog("ProcessReady", errorMessage);
                    TimeLastBroken = ProcessTimer.TickCount;
                    NextState = (int)ProcessState.Disconnecting;
                }
                else
                {
                    ProcessSessionMessage(NextState);
                    WaitTime = KeepAlive();

                    if ((ProcessTimer.TickCount - TimeLastReceive) > 33000)
                    {
                        TimeLastBroken = ProcessTimer.TickCount;
                        NextState = (int)ProcessState.Disconnecting;
                    }
                }
            }
            else if (pLastError == LastErrorEnum.ForceLogout)
            {
                Stop(IProcessStopStyle.NoWait);
            }
            else
            {
                TimeLastBroken = ProcessTimer.TickCount;
                NextState = (int)ProcessState.Disconnecting;
            }
        }

        private void ProcessDisconnecting(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
            TcpSock.Close();

            if (Command == ProcessCommand.Start)
            {
                NextState = (int)ProcessState.SessionLogin;
            }
            else
            {
                NextState = (int)ProcessState.Stopped;

                DataInLog.Close();
                DataOutLog.Close();
            }
        }

        #endregion


        private void PrepareCrypters()
        {
            RijndaelManaged aes = new RijndaelManaged();
            aes.KeySize = AESCrypto.KeySize;
            aes.Mode = AESCrypto.Mode;
            aes.BlockSize = AESCrypto.BlockSize;
            aes.IV = AESCrypto.IV;
            aes.Key = AESCrypto.Key;
            aes.Padding = PaddingMode.None;

            Encryptor = aes.CreateEncryptor();
            Decryptor = aes.CreateDecryptor();
        }

        private long KeepAlive()
        {
            int timeElapsed;
            long waitTime = -1;

            if (KeepAliveMessage != null)
            {
                timeElapsed = ProcessTimer.TickCount - TimeLastSend;
                if (timeElapsed >= 5000)
                {
                    Send(KeepAliveMessage);
                    waitTime = 50000000L;
                }
                else
                {
                    waitTime = 50000000L - timeElapsed * 10000L;
                    if (waitTime > 50000000L) waitTime = 50000000L;
                }
            }

            return waitTime;
        }

        private void AppendInLog(string MessageType, string Message)
        {
            if (DataLogEnabled && DataInLog != null)
            DataInLog.Append(MessageType, Message);
        }

        private void AppendOutLog(string MessageType, string Message)
        {
            if (DataLogEnabled && DataOutLog != null)
                DataOutLog.Append(MessageType, Message);
        }
    }
}
