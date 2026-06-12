namespace StockTerminal.Forms
{
    partial class LoginForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LoginForm));
            this.panelChangePassword = new System.Windows.Forms.Panel();
            this.textBoxNewPasswordRetype = new System.Windows.Forms.TextBox();
            this.labelNewPasswordRetype = new System.Windows.Forms.Label();
            this.textBoxNewPassword = new System.Windows.Forms.TextBox();
            this.labelNewPassword = new System.Windows.Forms.Label();
            this.buttonSubmitNewPassword = new System.Windows.Forms.Button();
            this.panelVerificationCode = new System.Windows.Forms.Panel();
            this.labelVerificationCodeResendRemind = new System.Windows.Forms.Label();
            this.buttonVerificationCodeResendEmail = new System.Windows.Forms.Button();
            this.buttonVerificationCodeResendSMS = new System.Windows.Forms.Button();
            this.labelVerificationCodeDesc = new System.Windows.Forms.Label();
            this.textBoxVerificationCode = new System.Windows.Forms.TextBox();
            this.labelVerificationCode = new System.Windows.Forms.Label();
            this.buttonSubmitVerificationCode = new System.Windows.Forms.Button();
            this.panelLogin = new System.Windows.Forms.Panel();
            this.labelLoginDesc = new System.Windows.Forms.Label();
            this.textBoxPassword = new StockTerminal.Utils.MyTextBox();
            this.textBoxUserId = new StockTerminal.Utils.MyTextBox();
            this.progressBarLogin = new System.Windows.Forms.ProgressBar();
            this.labelUserId = new System.Windows.Forms.Label();
            this.panelLang = new System.Windows.Forms.Panel();
            this.radioButtonLangCHS = new System.Windows.Forms.RadioButton();
            this.radioButtonLangEN = new System.Windows.Forms.RadioButton();
            this.radioButtonLangCHT = new System.Windows.Forms.RadioButton();
            this.labelPassword = new System.Windows.Forms.Label();
            this.buttonLogin = new System.Windows.Forms.Button();
            this.buttonExit = new System.Windows.Forms.Button();
            this.timerProgress = new System.Windows.Forms.Timer(this.components);
            this.labelMessage = new System.Windows.Forms.Label();
            this.panelChangePassword.SuspendLayout();
            this.panelVerificationCode.SuspendLayout();
            this.panelLogin.SuspendLayout();
            this.panelLang.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelChangePassword
            // 
            this.panelChangePassword.AccessibleDescription = null;
            this.panelChangePassword.AccessibleName = null;
            resources.ApplyResources(this.panelChangePassword, "panelChangePassword");
            this.panelChangePassword.BackgroundImage = null;
            this.panelChangePassword.Controls.Add(this.textBoxNewPasswordRetype);
            this.panelChangePassword.Controls.Add(this.labelNewPasswordRetype);
            this.panelChangePassword.Controls.Add(this.textBoxNewPassword);
            this.panelChangePassword.Controls.Add(this.labelNewPassword);
            this.panelChangePassword.Controls.Add(this.buttonSubmitNewPassword);
            this.panelChangePassword.Font = null;
            this.panelChangePassword.Name = "panelChangePassword";
            // 
            // textBoxNewPasswordRetype
            // 
            this.textBoxNewPasswordRetype.AccessibleDescription = null;
            this.textBoxNewPasswordRetype.AccessibleName = null;
            resources.ApplyResources(this.textBoxNewPasswordRetype, "textBoxNewPasswordRetype");
            this.textBoxNewPasswordRetype.BackgroundImage = null;
            this.textBoxNewPasswordRetype.Name = "textBoxNewPasswordRetype";
            // 
            // labelNewPasswordRetype
            // 
            this.labelNewPasswordRetype.AccessibleDescription = null;
            this.labelNewPasswordRetype.AccessibleName = null;
            resources.ApplyResources(this.labelNewPasswordRetype, "labelNewPasswordRetype");
            this.labelNewPasswordRetype.Name = "labelNewPasswordRetype";
            // 
            // textBoxNewPassword
            // 
            this.textBoxNewPassword.AccessibleDescription = null;
            this.textBoxNewPassword.AccessibleName = null;
            resources.ApplyResources(this.textBoxNewPassword, "textBoxNewPassword");
            this.textBoxNewPassword.BackgroundImage = null;
            this.textBoxNewPassword.Name = "textBoxNewPassword";
            // 
            // labelNewPassword
            // 
            this.labelNewPassword.AccessibleDescription = null;
            this.labelNewPassword.AccessibleName = null;
            resources.ApplyResources(this.labelNewPassword, "labelNewPassword");
            this.labelNewPassword.Name = "labelNewPassword";
            // 
            // buttonSubmitNewPassword
            // 
            this.buttonSubmitNewPassword.AccessibleDescription = null;
            this.buttonSubmitNewPassword.AccessibleName = null;
            resources.ApplyResources(this.buttonSubmitNewPassword, "buttonSubmitNewPassword");
            this.buttonSubmitNewPassword.BackgroundImage = null;
            this.buttonSubmitNewPassword.Name = "buttonSubmitNewPassword";
            this.buttonSubmitNewPassword.UseVisualStyleBackColor = true;
            // 
            // panelVerificationCode
            // 
            this.panelVerificationCode.AccessibleDescription = null;
            this.panelVerificationCode.AccessibleName = null;
            resources.ApplyResources(this.panelVerificationCode, "panelVerificationCode");
            this.panelVerificationCode.BackgroundImage = null;
            this.panelVerificationCode.Controls.Add(this.labelVerificationCodeResendRemind);
            this.panelVerificationCode.Controls.Add(this.buttonVerificationCodeResendEmail);
            this.panelVerificationCode.Controls.Add(this.buttonVerificationCodeResendSMS);
            this.panelVerificationCode.Controls.Add(this.labelVerificationCodeDesc);
            this.panelVerificationCode.Controls.Add(this.textBoxVerificationCode);
            this.panelVerificationCode.Controls.Add(this.labelVerificationCode);
            this.panelVerificationCode.Controls.Add(this.buttonSubmitVerificationCode);
            this.panelVerificationCode.Font = null;
            this.panelVerificationCode.Name = "panelVerificationCode";
            // 
            // labelVerificationCodeResendRemind
            // 
            this.labelVerificationCodeResendRemind.AccessibleDescription = null;
            this.labelVerificationCodeResendRemind.AccessibleName = null;
            resources.ApplyResources(this.labelVerificationCodeResendRemind, "labelVerificationCodeResendRemind");
            this.labelVerificationCodeResendRemind.Font = null;
            this.labelVerificationCodeResendRemind.Name = "labelVerificationCodeResendRemind";
            // 
            // buttonVerificationCodeResendEmail
            // 
            this.buttonVerificationCodeResendEmail.AccessibleDescription = null;
            this.buttonVerificationCodeResendEmail.AccessibleName = null;
            resources.ApplyResources(this.buttonVerificationCodeResendEmail, "buttonVerificationCodeResendEmail");
            this.buttonVerificationCodeResendEmail.BackgroundImage = null;
            this.buttonVerificationCodeResendEmail.Font = null;
            this.buttonVerificationCodeResendEmail.Name = "buttonVerificationCodeResendEmail";
            this.buttonVerificationCodeResendEmail.UseVisualStyleBackColor = true;
            this.buttonVerificationCodeResendEmail.Click += new System.EventHandler(this.buttonVerificationCodeResendEmail_Click);
            // 
            // buttonVerificationCodeResendSMS
            // 
            this.buttonVerificationCodeResendSMS.AccessibleDescription = null;
            this.buttonVerificationCodeResendSMS.AccessibleName = null;
            resources.ApplyResources(this.buttonVerificationCodeResendSMS, "buttonVerificationCodeResendSMS");
            this.buttonVerificationCodeResendSMS.BackgroundImage = null;
            this.buttonVerificationCodeResendSMS.Font = null;
            this.buttonVerificationCodeResendSMS.Name = "buttonVerificationCodeResendSMS";
            this.buttonVerificationCodeResendSMS.UseVisualStyleBackColor = true;
            this.buttonVerificationCodeResendSMS.Click += new System.EventHandler(this.buttonVerificationCodeResendSMS_Click);
            // 
            // labelVerificationCodeDesc
            // 
            this.labelVerificationCodeDesc.AccessibleDescription = null;
            this.labelVerificationCodeDesc.AccessibleName = null;
            resources.ApplyResources(this.labelVerificationCodeDesc, "labelVerificationCodeDesc");
            this.labelVerificationCodeDesc.Name = "labelVerificationCodeDesc";
            // 
            // textBoxVerificationCode
            // 
            this.textBoxVerificationCode.AccessibleDescription = null;
            this.textBoxVerificationCode.AccessibleName = null;
            resources.ApplyResources(this.textBoxVerificationCode, "textBoxVerificationCode");
            this.textBoxVerificationCode.BackgroundImage = null;
            this.textBoxVerificationCode.Name = "textBoxVerificationCode";
            this.textBoxVerificationCode.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxVerificationCode_KeyPress);
            // 
            // labelVerificationCode
            // 
            this.labelVerificationCode.AccessibleDescription = null;
            this.labelVerificationCode.AccessibleName = null;
            resources.ApplyResources(this.labelVerificationCode, "labelVerificationCode");
            this.labelVerificationCode.Name = "labelVerificationCode";
            // 
            // buttonSubmitVerificationCode
            // 
            this.buttonSubmitVerificationCode.AccessibleDescription = null;
            this.buttonSubmitVerificationCode.AccessibleName = null;
            resources.ApplyResources(this.buttonSubmitVerificationCode, "buttonSubmitVerificationCode");
            this.buttonSubmitVerificationCode.BackgroundImage = null;
            this.buttonSubmitVerificationCode.Name = "buttonSubmitVerificationCode";
            this.buttonSubmitVerificationCode.UseVisualStyleBackColor = true;
            this.buttonSubmitVerificationCode.Click += new System.EventHandler(this.buttonSubmitVerificationCode_Click);
            // 
            // panelLogin
            // 
            this.panelLogin.AccessibleDescription = null;
            this.panelLogin.AccessibleName = null;
            resources.ApplyResources(this.panelLogin, "panelLogin");
            this.panelLogin.BackgroundImage = null;
            this.panelLogin.Controls.Add(this.labelLoginDesc);
            this.panelLogin.Controls.Add(this.textBoxPassword);
            this.panelLogin.Controls.Add(this.textBoxUserId);
            this.panelLogin.Controls.Add(this.progressBarLogin);
            this.panelLogin.Controls.Add(this.labelUserId);
            this.panelLogin.Controls.Add(this.panelLang);
            this.panelLogin.Controls.Add(this.labelPassword);
            this.panelLogin.Controls.Add(this.buttonLogin);
            this.panelLogin.Font = null;
            this.panelLogin.Name = "panelLogin";
            // 
            // labelLoginDesc
            // 
            this.labelLoginDesc.AccessibleDescription = null;
            this.labelLoginDesc.AccessibleName = null;
            resources.ApplyResources(this.labelLoginDesc, "labelLoginDesc");
            this.labelLoginDesc.Name = "labelLoginDesc";
            // 
            // textBoxPassword
            // 
            this.textBoxPassword.AccessibleDescription = null;
            this.textBoxPassword.AccessibleName = null;
            resources.ApplyResources(this.textBoxPassword, "textBoxPassword");
            this.textBoxPassword.BackgroundImage = null;
            this.textBoxPassword.Name = "textBoxPassword";
            this.textBoxPassword.UseSystemPasswordChar = true;
            this.textBoxPassword.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxPassword_KeyPress);
            // 
            // textBoxUserId
            // 
            this.textBoxUserId.AccessibleDescription = null;
            this.textBoxUserId.AccessibleName = null;
            resources.ApplyResources(this.textBoxUserId, "textBoxUserId");
            this.textBoxUserId.BackgroundImage = null;
            this.textBoxUserId.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.textBoxUserId.Name = "textBoxUserId";
            this.textBoxUserId.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxUserId_KeyPress);
            // 
            // progressBarLogin
            // 
            this.progressBarLogin.AccessibleDescription = null;
            this.progressBarLogin.AccessibleName = null;
            resources.ApplyResources(this.progressBarLogin, "progressBarLogin");
            this.progressBarLogin.BackgroundImage = null;
            this.progressBarLogin.Font = null;
            this.progressBarLogin.Maximum = 20;
            this.progressBarLogin.Name = "progressBarLogin";
            // 
            // labelUserId
            // 
            this.labelUserId.AccessibleDescription = null;
            this.labelUserId.AccessibleName = null;
            resources.ApplyResources(this.labelUserId, "labelUserId");
            this.labelUserId.Name = "labelUserId";
            // 
            // panelLang
            // 
            this.panelLang.AccessibleDescription = null;
            this.panelLang.AccessibleName = null;
            resources.ApplyResources(this.panelLang, "panelLang");
            this.panelLang.BackgroundImage = null;
            this.panelLang.Controls.Add(this.radioButtonLangCHS);
            this.panelLang.Controls.Add(this.radioButtonLangEN);
            this.panelLang.Controls.Add(this.radioButtonLangCHT);
            this.panelLang.Font = null;
            this.panelLang.Name = "panelLang";
            // 
            // radioButtonLangCHS
            // 
            this.radioButtonLangCHS.AccessibleDescription = null;
            this.radioButtonLangCHS.AccessibleName = null;
            resources.ApplyResources(this.radioButtonLangCHS, "radioButtonLangCHS");
            this.radioButtonLangCHS.BackgroundImage = null;
            this.radioButtonLangCHS.Name = "radioButtonLangCHS";
            this.radioButtonLangCHS.TabStop = true;
            this.radioButtonLangCHS.UseVisualStyleBackColor = true;
            this.radioButtonLangCHS.CheckedChanged += new System.EventHandler(this.radioButtonLangCHS_CheckedChanged);
            // 
            // radioButtonLangEN
            // 
            this.radioButtonLangEN.AccessibleDescription = null;
            this.radioButtonLangEN.AccessibleName = null;
            resources.ApplyResources(this.radioButtonLangEN, "radioButtonLangEN");
            this.radioButtonLangEN.BackgroundImage = null;
            this.radioButtonLangEN.Name = "radioButtonLangEN";
            this.radioButtonLangEN.TabStop = true;
            this.radioButtonLangEN.UseVisualStyleBackColor = true;
            this.radioButtonLangEN.CheckedChanged += new System.EventHandler(this.radioButtonLangEN_CheckedChanged);
            // 
            // radioButtonLangCHT
            // 
            this.radioButtonLangCHT.AccessibleDescription = null;
            this.radioButtonLangCHT.AccessibleName = null;
            resources.ApplyResources(this.radioButtonLangCHT, "radioButtonLangCHT");
            this.radioButtonLangCHT.BackgroundImage = null;
            this.radioButtonLangCHT.Name = "radioButtonLangCHT";
            this.radioButtonLangCHT.TabStop = true;
            this.radioButtonLangCHT.UseVisualStyleBackColor = true;
            this.radioButtonLangCHT.CheckedChanged += new System.EventHandler(this.radioButtonLangCHT_CheckedChanged);
            // 
            // labelPassword
            // 
            this.labelPassword.AccessibleDescription = null;
            this.labelPassword.AccessibleName = null;
            resources.ApplyResources(this.labelPassword, "labelPassword");
            this.labelPassword.Name = "labelPassword";
            // 
            // buttonLogin
            // 
            this.buttonLogin.AccessibleDescription = null;
            this.buttonLogin.AccessibleName = null;
            resources.ApplyResources(this.buttonLogin, "buttonLogin");
            this.buttonLogin.BackgroundImage = null;
            this.buttonLogin.Name = "buttonLogin";
            this.buttonLogin.UseVisualStyleBackColor = true;
            this.buttonLogin.Click += new System.EventHandler(this.buttonLogin_Click);
            // 
            // buttonExit
            // 
            this.buttonExit.AccessibleDescription = null;
            this.buttonExit.AccessibleName = null;
            resources.ApplyResources(this.buttonExit, "buttonExit");
            this.buttonExit.BackgroundImage = null;
            this.buttonExit.Font = null;
            this.buttonExit.Name = "buttonExit";
            this.buttonExit.UseVisualStyleBackColor = true;
            this.buttonExit.Click += new System.EventHandler(this.buttonExit_Click);
            // 
            // timerProgress
            // 
            this.timerProgress.Interval = 500;
            this.timerProgress.Tick += new System.EventHandler(this.timerProgress_Tick);
            // 
            // labelMessage
            // 
            this.labelMessage.AccessibleDescription = null;
            this.labelMessage.AccessibleName = null;
            resources.ApplyResources(this.labelMessage, "labelMessage");
            this.labelMessage.Name = "labelMessage";
            // 
            // LoginForm
            // 
            this.AccessibleDescription = null;
            this.AccessibleName = null;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = null;
            this.Controls.Add(this.buttonExit);
            this.Controls.Add(this.labelMessage);
            this.Controls.Add(this.panelChangePassword);
            this.Controls.Add(this.panelVerificationCode);
            this.Controls.Add(this.panelLogin);
            this.Font = null;
            this.Icon = null;
            this.Name = "LoginForm";
            this.ToolTipText = null;
            this.Load += new System.EventHandler(this.LoginForm_Load);
            this.Shown += new System.EventHandler(this.LoginForm_Shown);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.LoginForm_FormClosing);
            this.Resize += new System.EventHandler(this.LoginForm_Resize);
            this.panelChangePassword.ResumeLayout(false);
            this.panelChangePassword.PerformLayout();
            this.panelVerificationCode.ResumeLayout(false);
            this.panelVerificationCode.PerformLayout();
            this.panelLogin.ResumeLayout(false);
            this.panelLogin.PerformLayout();
            this.panelLang.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelChangePassword;
        private System.Windows.Forms.TextBox textBoxNewPasswordRetype;
        private System.Windows.Forms.Label labelNewPasswordRetype;
        private System.Windows.Forms.TextBox textBoxNewPassword;
        private System.Windows.Forms.Label labelNewPassword;
        private System.Windows.Forms.Button buttonSubmitNewPassword;
        private System.Windows.Forms.Panel panelVerificationCode;
        private System.Windows.Forms.Button buttonVerificationCodeResendEmail;
        private System.Windows.Forms.Button buttonVerificationCodeResendSMS;
        private System.Windows.Forms.Label labelVerificationCodeDesc;
        private System.Windows.Forms.TextBox textBoxVerificationCode;
        private System.Windows.Forms.Label labelVerificationCode;
        private System.Windows.Forms.Button buttonSubmitVerificationCode;
        private System.Windows.Forms.Panel panelLogin;
        private System.Windows.Forms.Label labelLoginDesc;
        private StockTerminal.Utils.MyTextBox textBoxPassword;
        private StockTerminal.Utils.MyTextBox textBoxUserId;
        private System.Windows.Forms.ProgressBar progressBarLogin;
        private System.Windows.Forms.Label labelUserId;
        private System.Windows.Forms.Panel panelLang;
        private System.Windows.Forms.RadioButton radioButtonLangCHS;
        private System.Windows.Forms.RadioButton radioButtonLangEN;
        private System.Windows.Forms.RadioButton radioButtonLangCHT;
        private System.Windows.Forms.Label labelPassword;
        private System.Windows.Forms.Button buttonLogin;
        private System.Windows.Forms.Button buttonExit;
        private System.Windows.Forms.Timer timerProgress;
        private System.Windows.Forms.Label labelMessage;
        private System.Windows.Forms.Label labelVerificationCodeResendRemind;
    }
}