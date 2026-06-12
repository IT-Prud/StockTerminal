using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Net;
using System.Text;
using System.Windows.Forms;
using TradeDB;
using TradeDB.Net;
using Utils;

namespace StockTerminal.Forms
{
    public partial class LoginForm : StockTerminal.Forms.BaseForm
    {
        private enum LoginFormModeEnum { Login, VerificationCode, ChangePassword };
        private enum VerificationCodeSendMethodEnum { SMS, Email };

        private int LastState = (int)global::TradeDB.ProcessState.Stopped;
        private VerificationCodeSendMethodEnum VerificationCodeSendMethod = VerificationCodeSendMethodEnum.SMS;

        private readonly List<HostEndPoint> LoginHostList = new List<HostEndPoint>(10);

        public LoginForm()
            : this(null, null)
        {
        }

        public LoginForm(CultureInfo Culture, string PersistString)
            : base(Culture, PersistString)
        {
            LayoutLockable = false;

            InitializeComponent();

            ListenConnectionStatus();
        }

        public void ShowMessage(string MessageResxId)
        {
            if (MessageResxId != null)
            {
                string msg = GetResxString(MessageResxId);
                labelMessage.Text = msg;
                AppendLog("Login", msg, "Login", false);
            }
        }

        private bool OnServerCertificateValidationCallBack(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                                    System.Security.Cryptography.X509Certificates.X509Chain chain,
                                    System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true; // Accept any certificate
        }

        protected override void OnConnectionStatus(int State)
        {
            // public new enum ProcessState { Stopped, Connecting, Login, Loading, Ready, Disconnecting, MaxState };

            switch (State)
            {
                case (int)global::TradeDB.ProcessState.Stopped:
                    LockScreen(false);
                    if (LastState == (int)global::TradeDB.ProcessState.Login)
                    {
                        switch (TradeDB.SessionLastTradeError)
                        {
                            case LastErrorEnum.InvalidPassword:
                                AppendLog("Login", "Invalid User Id or Password", "Login", false);
                                labelMessage.Text = GetResxString("Login_Message_InvalidUserIdPassword");
                                textBoxUserId.Text = "";
                                textBoxPassword.Text = "";
                                textBoxVerificationCode.Text = "";
                                SetLoginFormMode(LoginFormModeEnum.Login);
                                break;

                            case LastErrorEnum.InvalidVerificationCode:
                                AppendLog("Login", "Invalid VerificationCode", "Login", false);
                                if (TradeDB.VerificationCode != null && TradeDB.VerificationCode.Length > 0)
                                {
                                    labelMessage.Text = GetResxString("Login_Message_InvalidVerificationCode");
                                }
                                else if (TradeDB != null)
                                {
                                    if (VerificationCodeSendMethod == VerificationCodeSendMethodEnum.SMS)
                                        labelMessage.Text = GetResxString("Login_Message_SendingVerificationCodeSMS") + " " + TradeDB.SMSNo;
                                    else if (VerificationCodeSendMethod == VerificationCodeSendMethodEnum.Email)
                                        labelMessage.Text = GetResxString("Login_Message_SendingVerificationCodeEmail") + " " + TradeDB.Email;
                                    else
                                        labelMessage.Text = "";
                                }
                                textBoxVerificationCode.Text = "";
                                SetLoginFormMode(LoginFormModeEnum.VerificationCode);
                                break;

                            case LastErrorEnum.SendingVerificationCode:
                                AppendLog("Login", "Sending VerificationCode", "Login", false);
                                if (TradeDB != null)
                                {
                                    if (VerificationCodeSendMethod == VerificationCodeSendMethodEnum.SMS)
                                        labelMessage.Text = GetResxString("Login_Message_SendingVerificationCodeSMS") + " " + TradeDB.SMSNo;
                                    else if (VerificationCodeSendMethod == VerificationCodeSendMethodEnum.Email)
                                        labelMessage.Text = GetResxString("Login_Message_SendingVerificationCodeEmail") + " " + TradeDB.Email;
                                    else
                                        labelMessage.Text = "";
                                }
                                textBoxVerificationCode.Text = "";
                                SetLoginFormMode(LoginFormModeEnum.VerificationCode);
                                break;

                            case LastErrorEnum.ServiceUnavailable:
                                AppendLog("Login", "Service unavailable", "Login", false);
                                labelMessage.Text = GetResxString("Login_Message_ServiceUnavailable");
                                textBoxUserId.Text = "";
                                textBoxPassword.Text = "";
                                textBoxVerificationCode.Text = "";
                                SetLoginFormMode(LoginFormModeEnum.Login);
                                break;

                            case LastErrorEnum.PasswordForceChange:
                                AppendLog("Login", "Password force to be changed", "Login", false);
                                string cultureStr = Culture.ToString();
                                string langID = (cultureStr == "zh-CHS" || cultureStr == "zh-CHT") ? "TC" : "EN";
                                string destWeb = "https://www.pru.hk/ETrade/Secured/Session/Login.asp?Action=LOGIN&LangID=" + langID + "&UserID=" + TradeDB.UserId + "&Password=" + TradeDB.Password + "&Redirect=PASSWORD";
                                System.Diagnostics.Process.Start(@destWeb);
                                labelMessage.Text = GetResxString("Login_Message_PasswordForceChange");
                                textBoxUserId.Text = "";
                                textBoxPassword.Text = "";
                                textBoxVerificationCode.Text = "";
                                SetLoginFormMode(LoginFormModeEnum.ChangePassword);
                                break;

                            case LastErrorEnum.MaxLoginAttemptReached:
                            case LastErrorEnum.LoginNoResponse:
                                AppendLog("Login", "Failed to connect server.", "Login", false);
                                labelMessage.Text = GetResxString("Login_Message_FailedToConnectServer");
                                textBoxUserId.Text = "";
                                textBoxPassword.Text = "";
                                textBoxVerificationCode.Text = "";
                                SetLoginFormMode(LoginFormModeEnum.Login);
                                break;

                            case LastErrorEnum.LoadDataFailed:
                                AppendLog("Login", "Failed to load data.", "Login", false);
                                labelMessage.Text = GetResxString("Login_Message_FailedToLoadData");
                                textBoxUserId.Text = "";
                                textBoxPassword.Text = "";
                                textBoxVerificationCode.Text = "";
                                SetLoginFormMode(LoginFormModeEnum.Login);
                                break;

                            default:
                                AppendLog("Login", "Invalid User Id or Password", "Login", false);
                                labelMessage.Text = GetResxString("Login_Message_InvalidUserIdPassword");
                                textBoxUserId.Text = "";
                                textBoxPassword.Text = "";
                                textBoxVerificationCode.Text = "";
                                SetLoginFormMode(LoginFormModeEnum.Login);
                                break;
                        }
                    }
                    else if (LastState == (int)global::TradeDB.ProcessState.Connecting)
                    {
                        AppendLog("Login", "Failed to connect server.", "Login", false);
                        labelMessage.Text = GetResxString("Login_Message_FailedToConnectServer");
                        textBoxUserId.Text = "";
                        textBoxPassword.Text = "";
                        textBoxVerificationCode.Text = "";
                        SetLoginFormMode(LoginFormModeEnum.Login);
                    }
                    break;

                case (int)global::TradeDB.ProcessState.Login:
                    AppendLog("Login", "Login in progress...", "Login", false);
                    labelMessage.Text = GetResxString("Login_Message_Login");
                    textBoxVerificationCode.Text = "";
                    break;

                case (int)global::TradeDB.ProcessState.Connecting:
                    AppendLog("Login", "Connecting...", "Login", false);
                    labelMessage.Text = GetResxString("Login_Message_Connecting");
                    textBoxUserId.Text = "";
                    textBoxPassword.Text = "";
                    textBoxVerificationCode.Text = "";
                    break;

                case (int)global::TradeDB.ProcessState.Ready:
                    AppendLog("Login", "Login Success", "Login", false);
                    labelMessage.Text = "";
                    textBoxUserId.Text = "";
                    textBoxPassword.Text = "";
                    textBoxVerificationCode.Text = "";
                    LockScreen(false);
                    this.Close();
                    break;

                default:
                    labelMessage.Text = "";
                    textBoxUserId.Text = "";
                    textBoxPassword.Text = "";
                    textBoxVerificationCode.Text = "";
                    textBoxNewPassword.Text = "";
                    textBoxNewPasswordRetype.Text = "";
                    LockScreen(false);
                    break;
            }
            LastState = State;
        }

        private void LockScreen(bool DoLock)
        {
            if (DoLock)
            {
                progressBarLogin.Visible = true;
                textBoxUserId.Enabled = false;
                textBoxPassword.Enabled = false;
                textBoxVerificationCode.Enabled = false;
                textBoxNewPassword.Enabled = false;
                textBoxNewPasswordRetype.Enabled = false;
                buttonLogin.Enabled = false;
                buttonSubmitVerificationCode.Enabled = false;
                buttonSubmitNewPassword.Enabled = false;
                radioButtonLangEN.Enabled = false;
                radioButtonLangCHT.Enabled = false;
                radioButtonLangCHS.Enabled = false;
                progressBarLogin.Value = progressBarLogin.Minimum;
                timerProgress.Interval = 100;
                timerProgress.Enabled = true;
            }
            else
            {
                timerProgress.Enabled = false;
                progressBarLogin.Visible = false;
                textBoxUserId.Enabled = true;
                textBoxPassword.Enabled = true;
                textBoxVerificationCode.Enabled = true;
                textBoxNewPassword.Enabled = true;
                textBoxNewPasswordRetype.Enabled = true;
                buttonLogin.Enabled = true;
                buttonSubmitVerificationCode.Enabled = true;
                buttonSubmitNewPassword.Enabled = true;
                radioButtonLangEN.Enabled = true;
                radioButtonLangCHT.Enabled = true;
                radioButtonLangCHS.Enabled = true;
            }
        }

        private void SetLoginFormMode(LoginFormModeEnum LoginFormMode)
        {
            switch (LoginFormMode)
            {
                case LoginFormModeEnum.Login:
                    panelLogin.Visible = true;
                    panelVerificationCode.Visible = false;
                    panelChangePassword.Visible = false;
                    textBoxUserId.Focus();
                    break;

                case LoginFormModeEnum.VerificationCode:
                    panelLogin.Visible = false;
                    panelVerificationCode.Visible = true;
                    panelChangePassword.Visible = false;
                    textBoxVerificationCode.Focus();
                    if (TradeDB != null)
                    {
                        if (TradeDB.SMSNo != null && TradeDB.SMSNo.Length > 0)
                        {
                            buttonVerificationCodeResendSMS.Text = GetResxString("Login_Message_ResendVerificationCodeSMS") + " " + TradeDB.SMSNo;
                            buttonVerificationCodeResendSMS.Visible = true;
                        }
                        else
                            buttonVerificationCodeResendSMS.Visible = false;

                        if (TradeDB.Email != null && TradeDB.Email.Length > 0)
                        {
                            buttonVerificationCodeResendEmail.Text = GetResxString("Login_Message_ResendVerificationCodeEmail") + " " + TradeDB.Email;
                            buttonVerificationCodeResendEmail.Visible = true;
                        }
                        else
                            buttonVerificationCodeResendEmail.Visible = false;
                    }
                    break;

                case LoginFormModeEnum.ChangePassword:
                    panelLogin.Visible = false;
                    panelVerificationCode.Visible = false;
                    panelChangePassword.Visible = true;
                    textBoxNewPassword.Focus();
                    break;
            }
        }

        private void DoResize()
        {
            int v;

            v = this.Width - panelLogin.Width;
            if (v < 0) v = 0;
            v >>= 1;
            panelLogin.Left = v;

            v = this.Height - panelLogin.Height;
            if (v < 0) v = 0;
            v >>= 1;
            panelLogin.Top = v;

            v = this.Width - panelVerificationCode.Width;
            if (v < 0) v = 0;
            v >>= 1;
            panelVerificationCode.Left = v;

            v = this.Height - panelVerificationCode.Height;
            if (v < 0) v = 0;
            v >>= 1;
            panelVerificationCode.Top = v;

            v = this.Width - panelChangePassword.Width;
            if (v < 0) v = 0;
            v >>= 1;
            panelChangePassword.Left = v;

            v = this.Height - panelChangePassword.Height;
            if (v < 0) v = 0;
            v >>= 1;
            panelChangePassword.Top = v;

            labelMessage.Left = panelLogin.Left;
            labelMessage.Top = panelLogin.Top + 50;

            buttonExit.Left = panelLogin.Left + panelLogin.Width - buttonExit.Width;
            buttonExit.Top = panelLogin.Top + panelLogin.Height - buttonExit.Height;
        }

        private void Login()
        {
            ITradeDB tdb = null;    // make sure UserId is set before assigning to TradeDB of BaseForm for the log to write to the proper folder for that UserId
            IPAddress StockUdpIP;
            int StockUdpPort = 0;

            LastState = (int)global::TradeDB.ProcessState.Stopped;

            if (Program.QuoteOnly == "2")
            {
                if (textBoxUserId.Text.Trim() == "" && textBoxPassword.Text.Trim() == "")
                {
                    switch (Instance.RegisterInstance())
                    {
                        case 1: // Success
                            break;

                        case -1:
                        case -2:
                            AppendLog("Login", GetResxString("Login_Message_InvalidUserIdPassword"), "Login", false);
                            labelMessage.Text = GetResxString("Login_Message_InvalidUserIdPassword");
                            return;
                    }
                }
            }
            else if (Program.QuoteOnly != "1")
            {
                if (textBoxUserId.Text.Trim() == "" || textBoxPassword.Text.Trim() == "")
                {
                    AppendLog("Login", GetResxString("Login_Message_InvalidUserIdPassword"), "Login", false);
                    labelMessage.Text = GetResxString("Login_Message_InvalidUserIdPassword");
                    return;
                }
            }

            if (TradeDB != null)
            {
                TradeDB.Disconnect();
                TradeDB = null;
            }
            tdb = new TradeDBNet();

            for (int i = 0; i < LoginHostList.Count; i++)
            {
                tdb.AddLoginHostTrade(LoginHostList[i]);
                tdb.AddLoginHostQuote(LoginHostList[i]);
            }

            if (SettingsTradeDB["StockUdpEnable"] == "1" &&
                IPAddress.TryParse(SettingsTradeDB["StockUdpIP"], out StockUdpIP) &&
                int.TryParse(SettingsTradeDB["StockUdpPort"], out StockUdpPort) && StockUdpPort > 0 && StockUdpPort <= 65535)
            {
                tdb.StockUdpDataEP = new IPEndPoint(StockUdpIP, StockUdpPort);
                tdb.StockUdpDataEnable = true;
                //StopExtraSubscriptionStock = false;
            }
            else
            {
                //StopExtraSubscriptionStock = true;
            }

            LockScreen(true);
            TradeDB = tdb;
            tdb.UserId = textBoxUserId.Text.ToUpper().Trim();
            tdb.Password = textBoxPassword.Text.ToUpper().Trim();
            tdb.VerificationCode = textBoxVerificationCode.Text.Trim();
            switch (VerificationCodeSendMethod)
            {
                case VerificationCodeSendMethodEnum.Email: tdb.VerificationCodeSendMethod = "Email"; break;
                default: tdb.VerificationCodeSendMethod = "SMS"; break;
            }
            tdb.UserSystemName = Application.ProductName;
            tdb.UserSystemVersion = Application.ProductVersion;
            tdb.DeploymentGroupName = DeploymentGroupName;
            tdb.MessageLogEnabled = SettingsTradeDB["LogMessage328"] == "1";
            tdb.Connect();
        }

        private void buttonExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void buttonLogin_Click(object sender, EventArgs e)
        {
            Login();
        }

        private void buttonSubmitVerificationCode_Click(object sender, EventArgs e)
        {
            Login();
        }

        private void buttonVerificationCodeResendSMS_Click(object sender, EventArgs e)
        {
            VerificationCodeSendMethod = VerificationCodeSendMethodEnum.SMS;
            textBoxVerificationCode.Text = "";
            Login();
        }

        private void buttonVerificationCodeResendEmail_Click(object sender, EventArgs e)
        {
            VerificationCodeSendMethod = VerificationCodeSendMethodEnum.Email;
            textBoxVerificationCode.Text = "";
            Login();
        }

        private void radioButtonLangEN_CheckedChanged(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
            {
                CultureInfo ci = new CultureInfo("en-US");
                this.Culture = ci;
                (this.MdiParent as BaseForm).Culture = ci;
                DoResize(); // some times changing language will reset the panel position without issuing the _Resize event
            }
        }

        private void radioButtonLangCHT_CheckedChanged(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
            {
                CultureInfo ci = new CultureInfo("zh-CHT");
                this.Culture = ci;
                (this.MdiParent as BaseForm).Culture = ci;
                DoResize(); // some times changing language will reset the panel position without issuing the _Resize event
            }
        }

        private void radioButtonLangCHS_CheckedChanged(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
            {
                CultureInfo ci = new CultureInfo("zh-CHS");
                this.Culture = ci;
                (this.MdiParent as BaseForm).Culture = ci;
                DoResize(); // some times changing language will reset the panel position without issuing the _Resize event
            }
        }

        private void LoginForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && (TradeDB == null || TradeDB.TradeState != (int)ProcessState.Ready)) e.Cancel = true;
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            SetLoginFormMode(LoginFormModeEnum.Login);

            //if (this.Culture == null)
            //{
            //    switch (CultureInfo.CurrentCulture.ToString())
            //    {
            //        case "zh-CN": this.Culture = new CultureInfo("zh-CHS"); break;
            //        case "zh-CHS": this.Culture = new CultureInfo("zh-CHS"); break;
            //        case "zh-CHT": this.Culture = new CultureInfo("zh-CHT"); break;
            //        case "zh-HK": this.Culture = new CultureInfo("zh-CHT"); break;
            //        case "zh-MO": this.Culture = new CultureInfo("zh-CHT"); break;
            //        case "zh-SG": this.Culture = new CultureInfo("zh-CHT"); break;
            //        case "zh-TW": this.Culture = new CultureInfo("zh-CHT"); break;
            //        default: this.Culture = new CultureInfo("en-US"); break;
            //    }
            //}
            if (this.Culture == null)
            {
                CultureInfo ci = null;
                switch (CultureInfo.CurrentCulture.ToString())
                {
                    case "zh-CN": ci = new CultureInfo("zh-CHS"); break;
                    case "zh-CHS": ci = new CultureInfo("zh-CHS"); break;
                    case "zh-CHT": ci = new CultureInfo("zh-CHT"); break;
                    case "zh-HK": ci = new CultureInfo("zh-CHT"); break;
                    case "zh-MO": ci = new CultureInfo("zh-CHT"); break;
                    case "zh-SG": ci = new CultureInfo("zh-CHT"); break;
                    case "zh-TW": ci = new CultureInfo("zh-CHT"); break;
                    default: ci = new CultureInfo("en-US"); break;
                }

                if (ci != null)
                {
                    this.Culture = ci;
                    (this.MdiParent as BaseForm).Culture = ci;
                }
            }

            switch (this.Culture.Name)
            {
                case "zh-CHS":
                    radioButtonLangCHS.Checked = true;
                    break;

                case "zh-CHT":
                    radioButtonLangCHT.Checked = true;
                    break;

                default:
                    radioButtonLangEN.Checked = true;
                    break;
            }
        }

        private void LoginForm_Shown(object sender, EventArgs e)
        {
            SettingsTradeDB.ReadFromFile(INIFile, "TradeDB");
            SettingsForms.ReadFromFile(INIFile, "Forms");
            SettingsWeb.ReadFromFile(INIFile, "Web");

            if (SettingsTradeDB["AllowOldSecurityProtocol"] != "1")
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)(0xC00);   // TLS 1.2

            if (SettingsTradeDB["ByPassCertValidation"] == "1")
                ServicePointManager.ServerCertificateValidationCallback += new System.Net.Security.RemoteCertificateValidationCallback(OnServerCertificateValidationCallBack);
            else
                ServicePointManager.ServerCertificateValidationCallback = null;

            LoginHostList.Clear();

            string hostName;
            string portStr;
            int port;

            for (int i = 0; i < 5; i++)
            {
                hostName = SettingsTradeDB[(i + 1).ToString("LoginHost0#")];
                portStr = SettingsTradeDB[(i + 1).ToString("LoginPort0#")];

                if (hostName != null && hostName.Length > 0 &&
                    portStr != null && portStr.Length > 0 &&
                    int.TryParse(portStr, out port))
                {
                    LoginHostList.Add(new HostEndPoint(hostName, port));
                }
            }

            if (LoginHostList.Count <= 0)
            {
                // set default hosts and ports only if all host port settings are empty to prevent confusion caused by mixing up testing host with default production host
                LoginHostList.Add(new HostEndPoint("www.pru.hk", 443));
                LoginHostList.Add(new HostEndPoint("www.pru.com.hk", 443));
            }

            MainForm theMainForm = (GetFormByFormType(typeof(MainForm))[0] as MainForm);
            theMainForm.UpdateStatusLabels();

            LockScreen(false);

            SetLoginFormMode(LoginFormModeEnum.Login);

            if (Program.QuoteOnly == "1")
                Login();
        }

        private void LoginForm_Resize(object sender, EventArgs e)
        {
            DoResize();
        }

        private void textBoxUserId_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (textBoxUserId.Text.Trim().Length <= 0)
                {
                    textBoxUserId.Focus();
                }
                else
                {
                    textBoxPassword.Focus();
                    textBoxPassword.SelectAll();
                }
                e.Handled = true;
            }
        }

        private void textBoxPassword_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (textBoxUserId.Text.Trim().Length <= 0)
                {
                    textBoxUserId.Focus();
                }
                else if (textBoxPassword.Text.Length <= 0)
                {
                    textBoxPassword.Focus();
                }
                else
                {
                    Login();
                }
                e.Handled = true;
            }
        }

        private void textBoxVerificationCode_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (textBoxVerificationCode.Text.Trim().Length > 0)
                {
                    Login();
                }
            }
        }

        private void comboBoxMode_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (textBoxUserId.Text.Trim().Length <= 0)
                {
                    textBoxUserId.Focus();
                }
                else if (textBoxPassword.Text.Length <= 0)
                {
                    textBoxPassword.Focus();
                }
                else
                {
                    Login();
                }
            }
        }

        private void timerProgress_Tick(object sender, EventArgs e)
        {
            timerProgress.Enabled = false;
            int k = progressBarLogin.Value;
            k++;
            if (k >= progressBarLogin.Maximum) k = progressBarLogin.Minimum;
            progressBarLogin.Value = k;
            timerProgress.Interval = 500;
            timerProgress.Enabled = true;
        }
    }
}
