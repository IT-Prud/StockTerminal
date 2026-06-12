namespace StockTerminal.Forms
{
    partial class GrayStockQuoteForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GrayStockQuoteForm));
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.timerRefresh = new System.Windows.Forms.Timer(this.components);
            this.StockCodeComboBox = new System.Windows.Forms.ComboBox();
            this.labelStkCodeTarget = new System.Windows.Forms.Label();
            this.labelStkCode = new System.Windows.Forms.Label();
            this.panelTop = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.panelTop.SuspendLayout();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            resources.ApplyResources(this.pictureBox1, "pictureBox1");
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.TabStop = false;
            // 
            // timerRefresh
            // 
            this.timerRefresh.Interval = 2000;
            this.timerRefresh.Tick += new System.EventHandler(this.timerRefresh_Tick);
            // 
            // StockCodeComboBox
            // 
            resources.ApplyResources(this.StockCodeComboBox, "StockCodeComboBox");
            this.StockCodeComboBox.Name = "StockCodeComboBox";
            this.StockCodeComboBox.SelectionChangeCommitted += new System.EventHandler(this.StockCodeComboBox_SelectionChangeCommitted);
            this.StockCodeComboBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.StockCodeComboBox_KeyDown);
            // 
            // labelStkCodeTarget
            // 
            this.labelStkCodeTarget.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(this.labelStkCodeTarget, "labelStkCodeTarget");
            this.labelStkCodeTarget.Name = "labelStkCodeTarget";
            // 
            // labelStkCode
            // 
            resources.ApplyResources(this.labelStkCode, "labelStkCode");
            this.labelStkCode.BackColor = System.Drawing.SystemColors.Control;
            this.labelStkCode.Name = "labelStkCode";
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.labelStkCode);
            this.panelTop.Controls.Add(this.StockCodeComboBox);
            this.panelTop.Controls.Add(this.labelStkCodeTarget);
            resources.ApplyResources(this.panelTop, "panelTop");
            this.panelTop.Name = "panelTop";
            // 
            // GrayStockQuoteForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.panelTop);
            this.Name = "GrayStockQuoteForm";
            this.Load += new System.EventHandler(this.GrayStockQuoteForm_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.GrayStockQuoteForm_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Timer timerRefresh;
        private System.Windows.Forms.ComboBox StockCodeComboBox;
        private System.Windows.Forms.Label labelStkCodeTarget;
        private System.Windows.Forms.Label labelStkCode;
        private System.Windows.Forms.Panel panelTop;
    }
}