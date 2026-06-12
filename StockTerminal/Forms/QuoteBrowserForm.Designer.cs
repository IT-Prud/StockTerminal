namespace StockTerminal.Forms
{
    partial class QuoteBrowserForm
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
            this.webBrowserMain = new System.Windows.Forms.WebBrowser();
            this.timerAttachEvent = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // webBrowserMain
            // 
            this.webBrowserMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webBrowserMain.Location = new System.Drawing.Point(0, 0);
            this.webBrowserMain.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowserMain.Name = "webBrowserMain";
            this.webBrowserMain.Size = new System.Drawing.Size(710, 341);
            this.webBrowserMain.TabIndex = 0;
            this.webBrowserMain.DocumentCompleted += new System.Windows.Forms.WebBrowserDocumentCompletedEventHandler(this.webBrowserMain_DocumentCompleted);
            // 
            // timerAttachEvent
            // 
            this.timerAttachEvent.Interval = 10000;
            this.timerAttachEvent.Tick += new System.EventHandler(this.timerAttachEvent_Tick);
            // 
            // QuoteBrowserForm
            // 
            this.ClientSize = new System.Drawing.Size(710, 341);
            this.Controls.Add(this.webBrowserMain);
            this.Name = "QuoteBrowserForm";
            this.TabText = "Base Form 3";
            this.Text = "Base Form 3";
            this.Shown += new System.EventHandler(this.QuoteBrowserForm_Shown);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.QuoteBrowserForm_FormClosed);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.WebBrowser webBrowserMain;
        private System.Windows.Forms.Timer timerAttachEvent;
    }
}
