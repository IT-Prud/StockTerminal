namespace StockTerminal.Forms
{
    partial class SnapShotQuoteForm
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
            this.webBrowserQuote = new System.Windows.Forms.WebBrowser();
            this.SuspendLayout();
            // 
            // webBrowserQuote
            // 
            this.webBrowserQuote.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webBrowserQuote.Location = new System.Drawing.Point(0, 0);
            this.webBrowserQuote.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowserQuote.Name = "webBrowserQuote";
            this.webBrowserQuote.Size = new System.Drawing.Size(587, 564);
            this.webBrowserQuote.TabIndex = 0;
            // 
            // SnapShotQuoteForm
            // 
            this.ClientSize = new System.Drawing.Size(587, 564);
            this.Controls.Add(this.webBrowserQuote);
            this.Name = "SnapShotQuoteForm";
            this.Text = "Snap Shot Quote Form";
            this.Shown += new System.EventHandler(this.SnapShotQuoteForm_Shown);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.SnapShotQuoteForm_FormClosed);
            this.DockStateChanged += new System.EventHandler(this.SnapShotQuoteForm_DockStateChanged);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.WebBrowser webBrowserQuote;
    }
}
