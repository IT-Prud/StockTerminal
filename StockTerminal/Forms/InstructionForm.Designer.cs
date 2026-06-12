namespace StockTerminal.Forms
{
    partial class InstructionForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InstructionForm));
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.labelStockCode = new System.Windows.Forms.Label();
            this.labelPrice = new System.Windows.Forms.Label();
            this.textBoxPrice = new System.Windows.Forms.TextBox();
            this.labelQuantity = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.labelAccountNo = new System.Windows.Forms.Label();
            this.textBoxAccountNo = new System.Windows.Forms.TextBox();
            this.checkBoxAuction = new System.Windows.Forms.CheckBox();
            this.buttonBuy = new System.Windows.Forms.Button();
            this.buttonSell = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.AccessibleDescription = null;
            this.textBox1.AccessibleName = null;
            resources.ApplyResources(this.textBox1, "textBox1");
            this.textBox1.BackgroundImage = null;
            this.textBox1.Font = null;
            this.textBox1.Name = "textBox1";
            // 
            // labelStockCode
            // 
            this.labelStockCode.AccessibleDescription = null;
            this.labelStockCode.AccessibleName = null;
            resources.ApplyResources(this.labelStockCode, "labelStockCode");
            this.labelStockCode.Font = null;
            this.labelStockCode.Name = "labelStockCode";
            // 
            // labelPrice
            // 
            this.labelPrice.AccessibleDescription = null;
            this.labelPrice.AccessibleName = null;
            resources.ApplyResources(this.labelPrice, "labelPrice");
            this.labelPrice.Font = null;
            this.labelPrice.Name = "labelPrice";
            // 
            // textBoxPrice
            // 
            this.textBoxPrice.AccessibleDescription = null;
            this.textBoxPrice.AccessibleName = null;
            resources.ApplyResources(this.textBoxPrice, "textBoxPrice");
            this.textBoxPrice.BackgroundImage = null;
            this.textBoxPrice.Font = null;
            this.textBoxPrice.Name = "textBoxPrice";
            // 
            // labelQuantity
            // 
            this.labelQuantity.AccessibleDescription = null;
            this.labelQuantity.AccessibleName = null;
            resources.ApplyResources(this.labelQuantity, "labelQuantity");
            this.labelQuantity.Font = null;
            this.labelQuantity.Name = "labelQuantity";
            // 
            // textBox2
            // 
            this.textBox2.AccessibleDescription = null;
            this.textBox2.AccessibleName = null;
            resources.ApplyResources(this.textBox2, "textBox2");
            this.textBox2.BackgroundImage = null;
            this.textBox2.Font = null;
            this.textBox2.Name = "textBox2";
            // 
            // labelAccountNo
            // 
            this.labelAccountNo.AccessibleDescription = null;
            this.labelAccountNo.AccessibleName = null;
            resources.ApplyResources(this.labelAccountNo, "labelAccountNo");
            this.labelAccountNo.Font = null;
            this.labelAccountNo.Name = "labelAccountNo";
            // 
            // textBoxAccountNo
            // 
            this.textBoxAccountNo.AccessibleDescription = null;
            this.textBoxAccountNo.AccessibleName = null;
            resources.ApplyResources(this.textBoxAccountNo, "textBoxAccountNo");
            this.textBoxAccountNo.BackgroundImage = null;
            this.textBoxAccountNo.Font = null;
            this.textBoxAccountNo.Name = "textBoxAccountNo";
            this.textBoxAccountNo.ReadOnly = true;
            // 
            // checkBoxAuction
            // 
            this.checkBoxAuction.AccessibleDescription = null;
            this.checkBoxAuction.AccessibleName = null;
            resources.ApplyResources(this.checkBoxAuction, "checkBoxAuction");
            this.checkBoxAuction.BackgroundImage = null;
            this.checkBoxAuction.Font = null;
            this.checkBoxAuction.Name = "checkBoxAuction";
            this.checkBoxAuction.UseVisualStyleBackColor = true;
            // 
            // buttonBuy
            // 
            this.buttonBuy.AccessibleDescription = null;
            this.buttonBuy.AccessibleName = null;
            resources.ApplyResources(this.buttonBuy, "buttonBuy");
            this.buttonBuy.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.buttonBuy.BackgroundImage = null;
            this.buttonBuy.Font = null;
            this.buttonBuy.Name = "buttonBuy";
            this.buttonBuy.UseVisualStyleBackColor = false;
            // 
            // buttonSell
            // 
            this.buttonSell.AccessibleDescription = null;
            this.buttonSell.AccessibleName = null;
            resources.ApplyResources(this.buttonSell, "buttonSell");
            this.buttonSell.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.buttonSell.BackgroundImage = null;
            this.buttonSell.Font = null;
            this.buttonSell.Name = "buttonSell";
            this.buttonSell.UseVisualStyleBackColor = false;
            // 
            // InstructionForm
            // 
            this.AccessibleDescription = null;
            this.AccessibleName = null;
            resources.ApplyResources(this, "$this");
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.BackgroundImage = null;
            this.Controls.Add(this.buttonSell);
            this.Controls.Add(this.buttonBuy);
            this.Controls.Add(this.checkBoxAuction);
            this.Controls.Add(this.textBoxAccountNo);
            this.Controls.Add(this.labelAccountNo);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.labelQuantity);
            this.Controls.Add(this.textBoxPrice);
            this.Controls.Add(this.labelPrice);
            this.Controls.Add(this.labelStockCode);
            this.Controls.Add(this.textBox1);
            this.Font = null;
            this.Icon = null;
            this.Name = "InstructionForm";
            this.ToolTipText = null;
            this.Load += new System.EventHandler(this.InstructionForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label labelStockCode;
        private System.Windows.Forms.Label labelPrice;
        private System.Windows.Forms.TextBox textBoxPrice;
        private System.Windows.Forms.Label labelQuantity;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Label labelAccountNo;
        private System.Windows.Forms.TextBox textBoxAccountNo;
        private System.Windows.Forms.CheckBox checkBoxAuction;
        private System.Windows.Forms.Button buttonBuy;
        private System.Windows.Forms.Button buttonSell;
    }
}
