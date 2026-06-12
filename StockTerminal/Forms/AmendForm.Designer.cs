namespace StockTerminal.Forms
{
    partial class AmendForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AmendForm));
            this.labelAmount = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.numUpDownNewStkPrice = new System.Windows.Forms.NumericUpDown();
            this.checkBoxAuction = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.numUpDownNewStockQty = new System.Windows.Forms.NumericUpDown();
            this.checkBoxLmt = new System.Windows.Forms.CheckBox();
            this.textBoxLmt = new System.Windows.Forms.TextBox();
            this.labelBuySell = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.labelStockCode = new System.Windows.Forms.Label();
            this.buttonGo = new System.Windows.Forms.Button();
            this.labelStockName = new System.Windows.Forms.Label();
            this.labelOrderNum = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.labelOutstandQty = new System.Windows.Forms.Label();
            this.labelFilledQty = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.myGroupBox1 = new StockTerminal.Utils.myGroupBox();
            this.labelExchange = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.labelStockQty = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.labelStockPrice = new System.Windows.Forms.Label();
            this.myGroupBox3 = new StockTerminal.Utils.myGroupBox();
            this.myGroupBox4 = new StockTerminal.Utils.myGroupBox();
            this.labelCurrency = new System.Windows.Forms.Label();
            this.labelPartialFilled = new System.Windows.Forms.Label();
            this.pictureBoxStockPriceCross = new System.Windows.Forms.PictureBox();
            this.pictureBoxStockQtyCross = new System.Windows.Forms.PictureBox();
            this.pictureBoxStockPriceTick = new System.Windows.Forms.PictureBox();
            this.pictureBoxStockQtyTick = new System.Windows.Forms.PictureBox();
            this.textBoxNewStkQty = new StockTerminal.Utils.MyTextBox();
            this.textBoxNewStkPrice = new StockTerminal.Utils.MyTextBox();
            this.vScrollBarQty = new System.Windows.Forms.VScrollBar();
            this.vScrollBarPrice = new System.Windows.Forms.VScrollBar();
            this.buttonMax = new System.Windows.Forms.Button();
            this.label20 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.labelAccountNo = new System.Windows.Forms.Label();
            this.myTextBoxAccountno = new StockTerminal.Utils.MyTextBox();
            ((System.ComponentModel.ISupportInitialize)(this.numUpDownNewStkPrice)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numUpDownNewStockQty)).BeginInit();
            this.myGroupBox1.SuspendLayout();
            this.myGroupBox3.SuspendLayout();
            this.myGroupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxStockPriceCross)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxStockQtyCross)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxStockPriceTick)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxStockQtyTick)).BeginInit();
            this.SuspendLayout();
            // 
            // labelAmount
            // 
            resources.ApplyResources(this.labelAmount, "labelAmount");
            this.labelAmount.Name = "labelAmount";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // numUpDownNewStkPrice
            // 
            this.numUpDownNewStkPrice.DecimalPlaces = 3;
            resources.ApplyResources(this.numUpDownNewStkPrice, "numUpDownNewStkPrice");
            this.numUpDownNewStkPrice.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numUpDownNewStkPrice.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.numUpDownNewStkPrice.Name = "numUpDownNewStkPrice";
            this.numUpDownNewStkPrice.Value = new decimal(new int[] {
            118900,
            0,
            0,
            196608});
            this.numUpDownNewStkPrice.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.numUpDownNewStkPrice_KeyPress);
            // 
            // checkBoxAuction
            // 
            resources.ApplyResources(this.checkBoxAuction, "checkBoxAuction");
            this.checkBoxAuction.Name = "checkBoxAuction";
            this.checkBoxAuction.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // numUpDownNewStockQty
            // 
            resources.ApplyResources(this.numUpDownNewStockQty, "numUpDownNewStockQty");
            this.numUpDownNewStockQty.Maximum = new decimal(new int[] {
            9999999,
            0,
            0,
            0});
            this.numUpDownNewStockQty.Name = "numUpDownNewStockQty";
            this.numUpDownNewStockQty.Value = new decimal(new int[] {
            1600,
            0,
            0,
            0});
            this.numUpDownNewStockQty.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.numUpDownNewStockQty_KeyPress);
            // 
            // checkBoxLmt
            // 
            resources.ApplyResources(this.checkBoxLmt, "checkBoxLmt");
            this.checkBoxLmt.Name = "checkBoxLmt";
            this.checkBoxLmt.UseVisualStyleBackColor = true;
            // 
            // textBoxLmt
            // 
            resources.ApplyResources(this.textBoxLmt, "textBoxLmt");
            this.textBoxLmt.Name = "textBoxLmt";
            // 
            // labelBuySell
            // 
            resources.ApplyResources(this.labelBuySell, "labelBuySell");
            this.labelBuySell.Name = "labelBuySell";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // labelStockCode
            // 
            resources.ApplyResources(this.labelStockCode, "labelStockCode");
            this.labelStockCode.Name = "labelStockCode";
            // 
            // buttonGo
            // 
            this.buttonGo.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(this.buttonGo, "buttonGo");
            this.buttonGo.Name = "buttonGo";
            this.buttonGo.UseVisualStyleBackColor = false;
            this.buttonGo.Click += new System.EventHandler(this.buttonGo_Click);
            // 
            // labelStockName
            // 
            resources.ApplyResources(this.labelStockName, "labelStockName");
            this.labelStockName.Name = "labelStockName";
            // 
            // labelOrderNum
            // 
            resources.ApplyResources(this.labelOrderNum, "labelOrderNum");
            this.labelOrderNum.Name = "labelOrderNum";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.Name = "label8";
            // 
            // labelOutstandQty
            // 
            resources.ApplyResources(this.labelOutstandQty, "labelOutstandQty");
            this.labelOutstandQty.Name = "labelOutstandQty";
            // 
            // labelFilledQty
            // 
            resources.ApplyResources(this.labelFilledQty, "labelFilledQty");
            this.labelFilledQty.Name = "labelFilledQty";
            // 
            // label16
            // 
            resources.ApplyResources(this.label16, "label16");
            this.label16.Name = "label16";
            // 
            // label15
            // 
            resources.ApplyResources(this.label15, "label15");
            this.label15.Name = "label15";
            // 
            // myGroupBox1
            // 
            this.myGroupBox1.BorderColor = System.Drawing.Color.Black;
            this.myGroupBox1.Controls.Add(this.labelExchange);
            this.myGroupBox1.Controls.Add(this.label7);
            this.myGroupBox1.Controls.Add(this.labelStockQty);
            this.myGroupBox1.Controls.Add(this.label6);
            this.myGroupBox1.Controls.Add(this.labelStockPrice);
            this.myGroupBox1.Controls.Add(this.label8);
            this.myGroupBox1.Controls.Add(this.label5);
            this.myGroupBox1.Controls.Add(this.labelStockCode);
            this.myGroupBox1.Controls.Add(this.labelBuySell);
            this.myGroupBox1.Controls.Add(this.labelStockName);
            this.myGroupBox1.Controls.Add(this.label2);
            resources.ApplyResources(this.myGroupBox1, "myGroupBox1");
            this.myGroupBox1.Name = "myGroupBox1";
            this.myGroupBox1.TabStop = false;
            this.myGroupBox1.Enter += new System.EventHandler(this.myGroupBox1_Enter);
            // 
            // labelExchange
            // 
            resources.ApplyResources(this.labelExchange, "labelExchange");
            this.labelExchange.Name = "labelExchange";
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            // 
            // labelStockQty
            // 
            resources.ApplyResources(this.labelStockQty, "labelStockQty");
            this.labelStockQty.Name = "labelStockQty";
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // labelStockPrice
            // 
            resources.ApplyResources(this.labelStockPrice, "labelStockPrice");
            this.labelStockPrice.Name = "labelStockPrice";
            // 
            // myGroupBox3
            // 
            this.myGroupBox3.BorderColor = System.Drawing.Color.Black;
            this.myGroupBox3.Controls.Add(this.labelFilledQty);
            this.myGroupBox3.Controls.Add(this.label15);
            resources.ApplyResources(this.myGroupBox3, "myGroupBox3");
            this.myGroupBox3.Name = "myGroupBox3";
            this.myGroupBox3.TabStop = false;
            // 
            // myGroupBox4
            // 
            this.myGroupBox4.BorderColor = System.Drawing.Color.MediumSlateBlue;
            this.myGroupBox4.Controls.Add(this.labelCurrency);
            this.myGroupBox4.Controls.Add(this.labelPartialFilled);
            this.myGroupBox4.Controls.Add(this.pictureBoxStockPriceCross);
            this.myGroupBox4.Controls.Add(this.pictureBoxStockQtyCross);
            this.myGroupBox4.Controls.Add(this.pictureBoxStockPriceTick);
            this.myGroupBox4.Controls.Add(this.pictureBoxStockQtyTick);
            this.myGroupBox4.Controls.Add(this.textBoxNewStkQty);
            this.myGroupBox4.Controls.Add(this.textBoxNewStkPrice);
            this.myGroupBox4.Controls.Add(this.vScrollBarQty);
            this.myGroupBox4.Controls.Add(this.vScrollBarPrice);
            this.myGroupBox4.Controls.Add(this.buttonMax);
            this.myGroupBox4.Controls.Add(this.label20);
            this.myGroupBox4.Controls.Add(this.buttonGo);
            this.myGroupBox4.Controls.Add(this.labelAmount);
            this.myGroupBox4.Controls.Add(this.label3);
            this.myGroupBox4.Controls.Add(this.label4);
            resources.ApplyResources(this.myGroupBox4, "myGroupBox4");
            this.myGroupBox4.Name = "myGroupBox4";
            this.myGroupBox4.TabStop = false;
            // 
            // labelCurrency
            // 
            resources.ApplyResources(this.labelCurrency, "labelCurrency");
            this.labelCurrency.ForeColor = System.Drawing.Color.Red;
            this.labelCurrency.Name = "labelCurrency";
            // 
            // labelPartialFilled
            // 
            resources.ApplyResources(this.labelPartialFilled, "labelPartialFilled");
            this.labelPartialFilled.ForeColor = System.Drawing.Color.Red;
            this.labelPartialFilled.Name = "labelPartialFilled";
            this.labelPartialFilled.Click += new System.EventHandler(this.labelPartialFilled_Click);
            // 
            // pictureBoxStockPriceCross
            // 
            resources.ApplyResources(this.pictureBoxStockPriceCross, "pictureBoxStockPriceCross");
            this.pictureBoxStockPriceCross.Name = "pictureBoxStockPriceCross";
            this.pictureBoxStockPriceCross.TabStop = false;
            this.pictureBoxStockPriceCross.Click += new System.EventHandler(this.pictureBoxStockPriceCross_Click);
            // 
            // pictureBoxStockQtyCross
            // 
            resources.ApplyResources(this.pictureBoxStockQtyCross, "pictureBoxStockQtyCross");
            this.pictureBoxStockQtyCross.Name = "pictureBoxStockQtyCross";
            this.pictureBoxStockQtyCross.TabStop = false;
            // 
            // pictureBoxStockPriceTick
            // 
            resources.ApplyResources(this.pictureBoxStockPriceTick, "pictureBoxStockPriceTick");
            this.pictureBoxStockPriceTick.Name = "pictureBoxStockPriceTick";
            this.pictureBoxStockPriceTick.TabStop = false;
            // 
            // pictureBoxStockQtyTick
            // 
            resources.ApplyResources(this.pictureBoxStockQtyTick, "pictureBoxStockQtyTick");
            this.pictureBoxStockQtyTick.Name = "pictureBoxStockQtyTick";
            this.pictureBoxStockQtyTick.TabStop = false;
            // 
            // textBoxNewStkQty
            // 
            resources.ApplyResources(this.textBoxNewStkQty, "textBoxNewStkQty");
            this.textBoxNewStkQty.Name = "textBoxNewStkQty";
            this.textBoxNewStkQty.TextChanged += new System.EventHandler(this.textBoxNewStkQty_TextChanged);
            this.textBoxNewStkQty.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxNewStkPriceQty_KeyDown);
            this.textBoxNewStkQty.Leave += new System.EventHandler(this.textBoxNewStockPriceQty_Leave);
            this.textBoxNewStkQty.KeyUp += new System.Windows.Forms.KeyEventHandler(this.textBoxNewStkQty_KeyUp);
            this.textBoxNewStkQty.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxNewStkQty_KeyPress);
            // 
            // textBoxNewStkPrice
            // 
            resources.ApplyResources(this.textBoxNewStkPrice, "textBoxNewStkPrice");
            this.textBoxNewStkPrice.Name = "textBoxNewStkPrice";
            this.textBoxNewStkPrice.TextChanged += new System.EventHandler(this.textBoxNewStkPrice_TextChanged);
            this.textBoxNewStkPrice.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxNewStkPriceQty_KeyDown);
            this.textBoxNewStkPrice.Leave += new System.EventHandler(this.textBoxNewStockPriceQty_Leave);
            this.textBoxNewStkPrice.KeyUp += new System.Windows.Forms.KeyEventHandler(this.textBoxNewStkPrice_KeyUp);
            this.textBoxNewStkPrice.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxNewStkPrice_KeyPress);
            // 
            // vScrollBarQty
            // 
            resources.ApplyResources(this.vScrollBarQty, "vScrollBarQty");
            this.vScrollBarQty.Name = "vScrollBarQty";
            this.vScrollBarQty.Scroll += new System.Windows.Forms.ScrollEventHandler(this.vScrollBarQty_Scroll);
            // 
            // vScrollBarPrice
            // 
            resources.ApplyResources(this.vScrollBarPrice, "vScrollBarPrice");
            this.vScrollBarPrice.Name = "vScrollBarPrice";
            this.vScrollBarPrice.Scroll += new System.Windows.Forms.ScrollEventHandler(this.vScrollBarPrice_Scroll);
            // 
            // buttonMax
            // 
            this.buttonMax.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(this.buttonMax, "buttonMax");
            this.buttonMax.Name = "buttonMax";
            this.buttonMax.UseVisualStyleBackColor = false;
            this.buttonMax.Click += new System.EventHandler(this.buttonMax_Click);
            // 
            // label20
            // 
            resources.ApplyResources(this.label20, "label20");
            this.label20.Name = "label20";
            // 
            // label9
            // 
            resources.ApplyResources(this.label9, "label9");
            this.label9.Name = "label9";
            // 
            // labelAccountNo
            // 
            resources.ApplyResources(this.labelAccountNo, "labelAccountNo");
            this.labelAccountNo.Name = "labelAccountNo";
            this.labelAccountNo.DoubleClick += new System.EventHandler(this.labelAccountNo_DoubleClick);
            this.labelAccountNo.Click += new System.EventHandler(this.labelAccountNo_Click);
            // 
            // myTextBoxAccountno
            // 
            resources.ApplyResources(this.myTextBoxAccountno, "myTextBoxAccountno");
            this.myTextBoxAccountno.Name = "myTextBoxAccountno";
            this.myTextBoxAccountno.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.myTextBoxAccountno_KeyPress);
            // 
            // AmendForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.PapayaWhip;
            this.Controls.Add(this.myTextBoxAccountno);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.labelAccountNo);
            this.Controls.Add(this.labelOutstandQty);
            this.Controls.Add(this.myGroupBox4);
            this.Controls.Add(this.myGroupBox3);
            this.Controls.Add(this.myGroupBox1);
            this.Controls.Add(this.label16);
            this.Controls.Add(this.numUpDownNewStkPrice);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.numUpDownNewStockQty);
            this.Controls.Add(this.labelOrderNum);
            this.Controls.Add(this.checkBoxLmt);
            this.Controls.Add(this.textBoxLmt);
            this.Controls.Add(this.checkBoxAuction);
            this.Name = "AmendForm";
            this.Load += new System.EventHandler(this.AmendForm_Load);
            this.Shown += new System.EventHandler(this.AmendForm_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.numUpDownNewStkPrice)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numUpDownNewStockQty)).EndInit();
            this.myGroupBox1.ResumeLayout(false);
            this.myGroupBox1.PerformLayout();
            this.myGroupBox3.ResumeLayout(false);
            this.myGroupBox3.PerformLayout();
            this.myGroupBox4.ResumeLayout(false);
            this.myGroupBox4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxStockPriceCross)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxStockQtyCross)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxStockPriceTick)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxStockQtyTick)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelStockName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown numUpDownNewStkPrice;
        private System.Windows.Forms.CheckBox checkBoxAuction;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown numUpDownNewStockQty;
        private System.Windows.Forms.CheckBox checkBoxLmt;
        private System.Windows.Forms.TextBox textBoxLmt;
        private System.Windows.Forms.Button buttonGo;
        private System.Windows.Forms.Label labelStockCode;
        private System.Windows.Forms.Label labelAmount;
        private System.Windows.Forms.Label labelOrderNum;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label labelBuySell;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label labelOutstandQty;
        private System.Windows.Forms.Label labelFilledQty;
        private StockTerminal.Utils.myGroupBox myGroupBox1;
        private StockTerminal.Utils.myGroupBox myGroupBox3;
        private StockTerminal.Utils.myGroupBox myGroupBox4;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.Button buttonMax;
        private System.Windows.Forms.VScrollBar vScrollBarQty;
        private System.Windows.Forms.VScrollBar vScrollBarPrice;
        private StockTerminal.Utils.MyTextBox textBoxNewStkPrice;
        private StockTerminal.Utils.MyTextBox textBoxNewStkQty;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label labelStockPrice;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label labelStockQty;
        private System.Windows.Forms.PictureBox pictureBoxStockPriceCross;
        private System.Windows.Forms.PictureBox pictureBoxStockQtyCross;
        private System.Windows.Forms.PictureBox pictureBoxStockPriceTick;
        private System.Windows.Forms.PictureBox pictureBoxStockQtyTick;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label labelAccountNo;
        private StockTerminal.Utils.MyTextBox myTextBoxAccountno;
        private System.Windows.Forms.Label labelPartialFilled;
        private System.Windows.Forms.Label labelCurrency;
        private System.Windows.Forms.Label labelExchange;
    }
}