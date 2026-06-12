namespace StockTerminal.Forms
{
    partial class StockListBrowserForm
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
            this.webBrowserMain = new System.Windows.Forms.WebBrowser();
            this.SuspendLayout();
            // 
            // webBrowserMain
            // 
            this.webBrowserMain.AllowWebBrowserDrop = false;
            this.webBrowserMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webBrowserMain.IsWebBrowserContextMenuEnabled = false;
            this.webBrowserMain.Location = new System.Drawing.Point(0, 0);
            this.webBrowserMain.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowserMain.Name = "webBrowserMain";
            this.webBrowserMain.ScrollBarsEnabled = false;
            this.webBrowserMain.Size = new System.Drawing.Size(710, 341);
            this.webBrowserMain.TabIndex = 0;
            // 
            // StockListBrowserForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(710, 341);
            this.Controls.Add(this.webBrowserMain);
            this.Name = "StockListBrowserForm";
            this.Text = "StockListBrowserForm";
            this.Shown += new System.EventHandler(this.StockListBrowserForm_Shown);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.StockListBrowserForm_FormClosed);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.WebBrowser webBrowserMain;
    }
}