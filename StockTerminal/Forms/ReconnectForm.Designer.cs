namespace StockTerminal.Forms
{
    partial class ReconnectForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ReconnectForm));
            this.buttonLogout = new System.Windows.Forms.Button();
            this.panelButtons = new System.Windows.Forms.Panel();
            this.labelStatus = new System.Windows.Forms.Label();
            this.progressBarReconnect = new System.Windows.Forms.ProgressBar();
            this.labelMessage = new System.Windows.Forms.Label();
            this.timerProgressReconnect = new System.Windows.Forms.Timer(this.components);
            this.pictureBoxIcon = new System.Windows.Forms.PictureBox();
            this.panelButtons.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonLogout
            // 
            resources.ApplyResources(this.buttonLogout, "buttonLogout");
            this.buttonLogout.Name = "buttonLogout";
            this.buttonLogout.TabStop = false;
            this.buttonLogout.UseVisualStyleBackColor = true;
            this.buttonLogout.Click += new System.EventHandler(this.buttonLogout_Click);
            // 
            // panelButtons
            // 
            this.panelButtons.BackColor = System.Drawing.SystemColors.Control;
            this.panelButtons.Controls.Add(this.buttonLogout);
            resources.ApplyResources(this.panelButtons, "panelButtons");
            this.panelButtons.Name = "panelButtons";
            // 
            // labelStatus
            // 
            resources.ApplyResources(this.labelStatus, "labelStatus");
            this.labelStatus.Name = "labelStatus";
            // 
            // progressBarReconnect
            // 
            this.progressBarReconnect.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(this.progressBarReconnect, "progressBarReconnect");
            this.progressBarReconnect.Name = "progressBarReconnect";
            // 
            // labelMessage
            // 
            resources.ApplyResources(this.labelMessage, "labelMessage");
            this.labelMessage.Name = "labelMessage";
            // 
            // timerProgressReconnect
            // 
            this.timerProgressReconnect.Interval = 500;
            this.timerProgressReconnect.Tick += new System.EventHandler(this.timerProgressReconnect_Tick);
            // 
            // pictureBoxIcon
            // 
            resources.ApplyResources(this.pictureBoxIcon, "pictureBoxIcon");
            this.pictureBoxIcon.Name = "pictureBoxIcon";
            this.pictureBoxIcon.TabStop = false;
            // 
            // ReconnectForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ControlBox = false;
            this.Controls.Add(this.pictureBoxIcon);
            this.Controls.Add(this.labelStatus);
            this.Controls.Add(this.labelMessage);
            this.Controls.Add(this.progressBarReconnect);
            this.Controls.Add(this.panelButtons);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "ReconnectForm";
            this.Shown += new System.EventHandler(this.ReconnectForm_Shown);
            this.Leave += new System.EventHandler(this.ReconnectForm_Leave);
            this.Resize += new System.EventHandler(this.ReconnectForm_Resize);
            this.panelButtons.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxIcon)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonLogout;
        private System.Windows.Forms.Panel panelButtons;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.ProgressBar progressBarReconnect;
        private System.Windows.Forms.Label labelMessage;
        private System.Windows.Forms.Timer timerProgressReconnect;
        private System.Windows.Forms.PictureBox pictureBoxIcon;
    }
}