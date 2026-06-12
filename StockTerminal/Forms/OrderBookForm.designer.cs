namespace StockTerminal.Forms
{
    partial class OrderBookForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OrderBookForm));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            this.panelTop = new System.Windows.Forms.Panel();
            this.labelTtlDealPrice = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.labelTtlDealNo = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.labelTtlOrderPriceVal = new System.Windows.Forms.Label();
            this.labelTtlOrderPrice = new System.Windows.Forms.Label();
            this.labelTtlOrdersVal = new System.Windows.Forms.Label();
            this.labelTtlOrders = new System.Windows.Forms.Label();
            this.myGroupBoxSrch = new StockTerminal.Utils.myGroupBox();
            this.buttonClrSrchSetting = new System.Windows.Forms.Button();
            this.buttonApplySrchSetting = new System.Windows.Forms.Button();
            this.comboBoxAccount = new System.Windows.Forms.ComboBox();
            this.labelSellAvgPrice = new System.Windows.Forms.Label();
            this.textBoxStkCode = new System.Windows.Forms.TextBox();
            this.labelSellFilled = new System.Windows.Forms.Label();
            this.labelStkCodeTarget = new System.Windows.Forms.Label();
            this.labelStkCode = new System.Windows.Forms.Label();
            this.labelSellTtlQty = new System.Windows.Forms.Label();
            this.labelAccTarget = new System.Windows.Forms.Label();
            this.labelAcc = new System.Windows.Forms.Label();
            this.labelBuyAvgPrice = new System.Windows.Forms.Label();
            this.labelAETarget = new System.Windows.Forms.Label();
            this.checkBoxEnableSrch = new System.Windows.Forms.CheckBox();
            this.labelBuyFilled = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.labelAE = new System.Windows.Forms.Label();
            this.labelBuyTtlQty = new System.Windows.Forms.Label();
            this.comboBoxAE = new System.Windows.Forms.ComboBox();
            this.labelBuy = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.comboBoxShowEx = new System.Windows.Forms.ComboBox();
            this.panelChkBox = new System.Windows.Forms.Panel();
            this.checkBoxGrayStock = new System.Windows.Forms.CheckBox();
            this.labelXchg = new System.Windows.Forms.Label();
            this.checkBoxShowDeal = new System.Windows.Forms.CheckBox();
            this.checkBoxShowAEOrders = new System.Windows.Forms.CheckBox();
            this.checkBoxShowInternetOrders = new System.Windows.Forms.CheckBox();
            this.checkBoxShowSearch = new System.Windows.Forms.CheckBox();
            this.panelBottom = new System.Windows.Forms.Panel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.labelPlacedByVal = new System.Windows.Forms.Label();
            this.labelPlacedBy = new System.Windows.Forms.Label();
            this.labelCurrency = new System.Windows.Forms.Label();
            this.labelRemark = new System.Windows.Forms.Label();
            this.buttonGetTransacCharge = new System.Windows.Forms.Button();
            this.labelTransacChargeVal = new System.Windows.Forms.Label();
            this.labelInvalidMsgVal = new System.Windows.Forms.Label();
            this.splitter2 = new System.Windows.Forms.Splitter();
            this.dataGridViewDeal = new System.Windows.Forms.DataGridView();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.panelMiddle = new System.Windows.Forms.Panel();
            this.tabControlOrders = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.dataGridViewPendOrder = new System.Windows.Forms.DataGridView();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.dataGridViewCompletedOrder = new System.Windows.Forms.DataGridView();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.dataGridViewRepliedOrder = new System.Windows.Forms.DataGridView();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.dataGridViewAllOrder = new System.Windows.Forms.DataGridView();
            this.SortGridTimer = new System.Windows.Forms.Timer(this.components);
            this.panelTop.SuspendLayout();
            this.myGroupBoxSrch.SuspendLayout();
            this.panelChkBox.SuspendLayout();
            this.panelBottom.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewDeal)).BeginInit();
            this.panelMiddle.SuspendLayout();
            this.tabControlOrders.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewPendOrder)).BeginInit();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewCompletedOrder)).BeginInit();
            this.tabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewRepliedOrder)).BeginInit();
            this.tabPage4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewAllOrder)).BeginInit();
            this.SuspendLayout();
            // 
            // panelTop
            // 
            this.panelTop.BackColor = System.Drawing.SystemColors.Control;
            this.panelTop.Controls.Add(this.labelTtlDealPrice);
            this.panelTop.Controls.Add(this.label4);
            this.panelTop.Controls.Add(this.labelTtlDealNo);
            this.panelTop.Controls.Add(this.label2);
            this.panelTop.Controls.Add(this.labelTtlOrderPriceVal);
            this.panelTop.Controls.Add(this.labelTtlOrderPrice);
            this.panelTop.Controls.Add(this.labelTtlOrdersVal);
            this.panelTop.Controls.Add(this.labelTtlOrders);
            this.panelTop.Controls.Add(this.myGroupBoxSrch);
            resources.ApplyResources(this.panelTop, "panelTop");
            this.panelTop.Name = "panelTop";
            // 
            // labelTtlDealPrice
            // 
            resources.ApplyResources(this.labelTtlDealPrice, "labelTtlDealPrice");
            this.labelTtlDealPrice.Name = "labelTtlDealPrice";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // labelTtlDealNo
            // 
            resources.ApplyResources(this.labelTtlDealNo, "labelTtlDealNo");
            this.labelTtlDealNo.Name = "labelTtlDealNo";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // labelTtlOrderPriceVal
            // 
            resources.ApplyResources(this.labelTtlOrderPriceVal, "labelTtlOrderPriceVal");
            this.labelTtlOrderPriceVal.Name = "labelTtlOrderPriceVal";
            // 
            // labelTtlOrderPrice
            // 
            resources.ApplyResources(this.labelTtlOrderPrice, "labelTtlOrderPrice");
            this.labelTtlOrderPrice.Name = "labelTtlOrderPrice";
            // 
            // labelTtlOrdersVal
            // 
            resources.ApplyResources(this.labelTtlOrdersVal, "labelTtlOrdersVal");
            this.labelTtlOrdersVal.Name = "labelTtlOrdersVal";
            // 
            // labelTtlOrders
            // 
            resources.ApplyResources(this.labelTtlOrders, "labelTtlOrders");
            this.labelTtlOrders.Name = "labelTtlOrders";
            // 
            // myGroupBoxSrch
            // 
            this.myGroupBoxSrch.BorderColor = System.Drawing.Color.Black;
            this.myGroupBoxSrch.Controls.Add(this.buttonClrSrchSetting);
            this.myGroupBoxSrch.Controls.Add(this.buttonApplySrchSetting);
            this.myGroupBoxSrch.Controls.Add(this.comboBoxAccount);
            this.myGroupBoxSrch.Controls.Add(this.labelSellAvgPrice);
            this.myGroupBoxSrch.Controls.Add(this.textBoxStkCode);
            this.myGroupBoxSrch.Controls.Add(this.labelSellFilled);
            this.myGroupBoxSrch.Controls.Add(this.labelStkCodeTarget);
            this.myGroupBoxSrch.Controls.Add(this.labelStkCode);
            this.myGroupBoxSrch.Controls.Add(this.labelSellTtlQty);
            this.myGroupBoxSrch.Controls.Add(this.labelAccTarget);
            this.myGroupBoxSrch.Controls.Add(this.labelAcc);
            this.myGroupBoxSrch.Controls.Add(this.labelBuyAvgPrice);
            this.myGroupBoxSrch.Controls.Add(this.labelAETarget);
            this.myGroupBoxSrch.Controls.Add(this.checkBoxEnableSrch);
            this.myGroupBoxSrch.Controls.Add(this.labelBuyFilled);
            this.myGroupBoxSrch.Controls.Add(this.label5);
            this.myGroupBoxSrch.Controls.Add(this.labelAE);
            this.myGroupBoxSrch.Controls.Add(this.labelBuyTtlQty);
            this.myGroupBoxSrch.Controls.Add(this.comboBoxAE);
            this.myGroupBoxSrch.Controls.Add(this.labelBuy);
            this.myGroupBoxSrch.Controls.Add(this.label7);
            this.myGroupBoxSrch.Controls.Add(this.label3);
            this.myGroupBoxSrch.Controls.Add(this.label6);
            resources.ApplyResources(this.myGroupBoxSrch, "myGroupBoxSrch");
            this.myGroupBoxSrch.Name = "myGroupBoxSrch";
            this.myGroupBoxSrch.TabStop = false;
            // 
            // buttonClrSrchSetting
            // 
            this.buttonClrSrchSetting.BackColor = System.Drawing.Color.Lime;
            resources.ApplyResources(this.buttonClrSrchSetting, "buttonClrSrchSetting");
            this.buttonClrSrchSetting.Name = "buttonClrSrchSetting";
            this.buttonClrSrchSetting.UseVisualStyleBackColor = false;
            this.buttonClrSrchSetting.Click += new System.EventHandler(this.buttonClrSrchSetting_Click);
            // 
            // buttonApplySrchSetting
            // 
            this.buttonApplySrchSetting.BackColor = System.Drawing.Color.Lime;
            resources.ApplyResources(this.buttonApplySrchSetting, "buttonApplySrchSetting");
            this.buttonApplySrchSetting.Name = "buttonApplySrchSetting";
            this.buttonApplySrchSetting.UseVisualStyleBackColor = false;
            this.buttonApplySrchSetting.Click += new System.EventHandler(this.buttonApplySrchSetting_Click);
            // 
            // comboBoxAccount
            // 
            resources.ApplyResources(this.comboBoxAccount, "comboBoxAccount");
            this.comboBoxAccount.Name = "comboBoxAccount";
            this.comboBoxAccount.SelectionChangeCommitted += new System.EventHandler(this.comboBoxAccount_SelectionChangeCommitted);
            this.comboBoxAccount.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.comboBoxAccount_KeyPress);
            this.comboBoxAccount.KeyDown += new System.Windows.Forms.KeyEventHandler(this.comboBoxAccount_KeyDown);
            // 
            // labelSellAvgPrice
            // 
            this.labelSellAvgPrice.BackColor = System.Drawing.Color.LightPink;
            resources.ApplyResources(this.labelSellAvgPrice, "labelSellAvgPrice");
            this.labelSellAvgPrice.Name = "labelSellAvgPrice";
            // 
            // textBoxStkCode
            // 
            resources.ApplyResources(this.textBoxStkCode, "textBoxStkCode");
            this.textBoxStkCode.Name = "textBoxStkCode";
            this.textBoxStkCode.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxStkCode_KeyDown);
            this.textBoxStkCode.MouseClick += new System.Windows.Forms.MouseEventHandler(this.textBoxStkCode_MouseClick);
            this.textBoxStkCode.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxStkCode_KeyPress);
            // 
            // labelSellFilled
            // 
            this.labelSellFilled.BackColor = System.Drawing.Color.LightPink;
            resources.ApplyResources(this.labelSellFilled, "labelSellFilled");
            this.labelSellFilled.Name = "labelSellFilled";
            // 
            // labelStkCodeTarget
            // 
            this.labelStkCodeTarget.BackColor = System.Drawing.Color.Cyan;
            resources.ApplyResources(this.labelStkCodeTarget, "labelStkCodeTarget");
            this.labelStkCodeTarget.Name = "labelStkCodeTarget";
            this.labelStkCodeTarget.TextChanged += new System.EventHandler(this.labelStkCodeTarget_TextChanged);
            // 
            // labelStkCode
            // 
            resources.ApplyResources(this.labelStkCode, "labelStkCode");
            this.labelStkCode.BackColor = System.Drawing.Color.Cyan;
            this.labelStkCode.Name = "labelStkCode";
            // 
            // labelSellTtlQty
            // 
            this.labelSellTtlQty.BackColor = System.Drawing.Color.LightPink;
            resources.ApplyResources(this.labelSellTtlQty, "labelSellTtlQty");
            this.labelSellTtlQty.Name = "labelSellTtlQty";
            // 
            // labelAccTarget
            // 
            this.labelAccTarget.BackColor = System.Drawing.Color.Cyan;
            resources.ApplyResources(this.labelAccTarget, "labelAccTarget");
            this.labelAccTarget.Name = "labelAccTarget";
            this.labelAccTarget.TextChanged += new System.EventHandler(this.labelAccTarget_TextChanged);
            // 
            // labelAcc
            // 
            resources.ApplyResources(this.labelAcc, "labelAcc");
            this.labelAcc.BackColor = System.Drawing.Color.Cyan;
            this.labelAcc.Name = "labelAcc";
            // 
            // labelBuyAvgPrice
            // 
            resources.ApplyResources(this.labelBuyAvgPrice, "labelBuyAvgPrice");
            this.labelBuyAvgPrice.Name = "labelBuyAvgPrice";
            // 
            // labelAETarget
            // 
            this.labelAETarget.BackColor = System.Drawing.Color.Cyan;
            resources.ApplyResources(this.labelAETarget, "labelAETarget");
            this.labelAETarget.Name = "labelAETarget";
            this.labelAETarget.TextChanged += new System.EventHandler(this.labelAETarget_TextChanged);
            // 
            // checkBoxEnableSrch
            // 
            this.checkBoxEnableSrch.BackColor = System.Drawing.Color.Lime;
            resources.ApplyResources(this.checkBoxEnableSrch, "checkBoxEnableSrch");
            this.checkBoxEnableSrch.Name = "checkBoxEnableSrch";
            this.checkBoxEnableSrch.UseVisualStyleBackColor = false;
            this.checkBoxEnableSrch.Click += new System.EventHandler(this.checkBoxEnableSrch_Click);
            this.checkBoxEnableSrch.CheckedChanged += new System.EventHandler(this.checkBoxEnableSrch_CheckedChanged);
            // 
            // labelBuyFilled
            // 
            resources.ApplyResources(this.labelBuyFilled, "labelBuyFilled");
            this.labelBuyFilled.Name = "labelBuyFilled";
            // 
            // label5
            // 
            this.label5.BackColor = System.Drawing.Color.Lime;
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // labelAE
            // 
            resources.ApplyResources(this.labelAE, "labelAE");
            this.labelAE.BackColor = System.Drawing.Color.Cyan;
            this.labelAE.Name = "labelAE";
            // 
            // labelBuyTtlQty
            // 
            resources.ApplyResources(this.labelBuyTtlQty, "labelBuyTtlQty");
            this.labelBuyTtlQty.Name = "labelBuyTtlQty";
            // 
            // comboBoxAE
            // 
            resources.ApplyResources(this.comboBoxAE, "comboBoxAE");
            this.comboBoxAE.Name = "comboBoxAE";
            this.comboBoxAE.Sorted = true;
            this.comboBoxAE.SelectionChangeCommitted += new System.EventHandler(this.comboBoxAE_SelectionChangeCommitted);
            this.comboBoxAE.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.comboBoxAE_KeyPress);
            this.comboBoxAE.KeyDown += new System.Windows.Forms.KeyEventHandler(this.comboBoxAE_KeyDown);
            // 
            // labelBuy
            // 
            resources.ApplyResources(this.labelBuy, "labelBuy");
            this.labelBuy.Name = "labelBuy";
            // 
            // label7
            // 
            this.label7.BackColor = System.Drawing.Color.Lime;
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            // 
            // label3
            // 
            this.label3.BackColor = System.Drawing.Color.LightPink;
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // label6
            // 
            this.label6.BackColor = System.Drawing.Color.Lime;
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // comboBoxShowEx
            // 
            resources.ApplyResources(this.comboBoxShowEx, "comboBoxShowEx");
            this.comboBoxShowEx.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxShowEx.FormattingEnabled = true;
            this.comboBoxShowEx.Name = "comboBoxShowEx";
            this.comboBoxShowEx.SelectionChangeCommitted += new System.EventHandler(this.comboBoxShowEx_SelectionChangeCommitted);
            // 
            // panelChkBox
            // 
            this.panelChkBox.Controls.Add(this.checkBoxGrayStock);
            this.panelChkBox.Controls.Add(this.labelXchg);
            this.panelChkBox.Controls.Add(this.comboBoxShowEx);
            this.panelChkBox.Controls.Add(this.checkBoxShowDeal);
            this.panelChkBox.Controls.Add(this.checkBoxShowAEOrders);
            this.panelChkBox.Controls.Add(this.checkBoxShowInternetOrders);
            this.panelChkBox.Controls.Add(this.checkBoxShowSearch);
            resources.ApplyResources(this.panelChkBox, "panelChkBox");
            this.panelChkBox.Name = "panelChkBox";
            // 
            // checkBoxGrayStock
            // 
            resources.ApplyResources(this.checkBoxGrayStock, "checkBoxGrayStock");
            this.checkBoxGrayStock.Name = "checkBoxGrayStock";
            this.checkBoxGrayStock.UseVisualStyleBackColor = true;
            this.checkBoxGrayStock.CheckedChanged += new System.EventHandler(this.checkBoxGrayStock_CheckedChanged);
            // 
            // labelXchg
            // 
            resources.ApplyResources(this.labelXchg, "labelXchg");
            this.labelXchg.Name = "labelXchg";
            // 
            // checkBoxShowDeal
            // 
            resources.ApplyResources(this.checkBoxShowDeal, "checkBoxShowDeal");
            this.checkBoxShowDeal.Checked = true;
            this.checkBoxShowDeal.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxShowDeal.Name = "checkBoxShowDeal";
            this.checkBoxShowDeal.UseVisualStyleBackColor = true;
            this.checkBoxShowDeal.Click += new System.EventHandler(this.checkBoxShowDeal_Click);
            // 
            // checkBoxShowAEOrders
            // 
            resources.ApplyResources(this.checkBoxShowAEOrders, "checkBoxShowAEOrders");
            this.checkBoxShowAEOrders.Name = "checkBoxShowAEOrders";
            this.checkBoxShowAEOrders.UseVisualStyleBackColor = true;
            this.checkBoxShowAEOrders.Click += new System.EventHandler(this.checkBoxShowAEOrders_Click);
            this.checkBoxShowAEOrders.CheckedChanged += new System.EventHandler(this.checkBoxShowAEOrders_CheckedChanged);
            // 
            // checkBoxShowInternetOrders
            // 
            resources.ApplyResources(this.checkBoxShowInternetOrders, "checkBoxShowInternetOrders");
            this.checkBoxShowInternetOrders.Name = "checkBoxShowInternetOrders";
            this.checkBoxShowInternetOrders.UseVisualStyleBackColor = true;
            this.checkBoxShowInternetOrders.Click += new System.EventHandler(this.checkBoxShowInternetOrders_Click);
            this.checkBoxShowInternetOrders.CheckedChanged += new System.EventHandler(this.checkBoxShowInternetOrders_CheckedChanged);
            // 
            // checkBoxShowSearch
            // 
            resources.ApplyResources(this.checkBoxShowSearch, "checkBoxShowSearch");
            this.checkBoxShowSearch.Name = "checkBoxShowSearch";
            this.checkBoxShowSearch.UseVisualStyleBackColor = true;
            this.checkBoxShowSearch.Click += new System.EventHandler(this.checkBoxShowSearch_Click);
            // 
            // panelBottom
            // 
            this.panelBottom.Controls.Add(this.panel1);
            this.panelBottom.Controls.Add(this.splitter2);
            this.panelBottom.Controls.Add(this.dataGridViewDeal);
            resources.ApplyResources(this.panelBottom, "panelBottom");
            this.panelBottom.Name = "panelBottom";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.labelPlacedByVal);
            this.panel1.Controls.Add(this.labelPlacedBy);
            this.panel1.Controls.Add(this.labelCurrency);
            this.panel1.Controls.Add(this.labelRemark);
            this.panel1.Controls.Add(this.buttonGetTransacCharge);
            this.panel1.Controls.Add(this.labelTransacChargeVal);
            this.panel1.Controls.Add(this.labelInvalidMsgVal);
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.Name = "panel1";
            // 
            // labelPlacedByVal
            // 
            resources.ApplyResources(this.labelPlacedByVal, "labelPlacedByVal");
            this.labelPlacedByVal.Name = "labelPlacedByVal";
            // 
            // labelPlacedBy
            // 
            resources.ApplyResources(this.labelPlacedBy, "labelPlacedBy");
            this.labelPlacedBy.Name = "labelPlacedBy";
            // 
            // labelCurrency
            // 
            resources.ApplyResources(this.labelCurrency, "labelCurrency");
            this.labelCurrency.ForeColor = System.Drawing.Color.Red;
            this.labelCurrency.Name = "labelCurrency";
            // 
            // labelRemark
            // 
            this.labelRemark.BackColor = System.Drawing.Color.White;
            this.labelRemark.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.labelRemark, "labelRemark");
            this.labelRemark.Name = "labelRemark";
            // 
            // buttonGetTransacCharge
            // 
            resources.ApplyResources(this.buttonGetTransacCharge, "buttonGetTransacCharge");
            this.buttonGetTransacCharge.Name = "buttonGetTransacCharge";
            this.buttonGetTransacCharge.UseVisualStyleBackColor = true;
            this.buttonGetTransacCharge.Click += new System.EventHandler(this.buttonGetTransacCharge_Click);
            // 
            // labelTransacChargeVal
            // 
            resources.ApplyResources(this.labelTransacChargeVal, "labelTransacChargeVal");
            this.labelTransacChargeVal.Name = "labelTransacChargeVal";
            // 
            // labelInvalidMsgVal
            // 
            resources.ApplyResources(this.labelInvalidMsgVal, "labelInvalidMsgVal");
            this.labelInvalidMsgVal.Name = "labelInvalidMsgVal";
            // 
            // splitter2
            // 
            this.splitter2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.splitter2, "splitter2");
            this.splitter2.Name = "splitter2";
            this.splitter2.TabStop = false;
            this.splitter2.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitter2_SplitterMoved);
            // 
            // dataGridViewDeal
            // 
            this.dataGridViewDeal.AllowUserToAddRows = false;
            this.dataGridViewDeal.AllowUserToDeleteRows = false;
            this.dataGridViewDeal.AllowUserToResizeRows = false;
            this.dataGridViewDeal.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridViewDeal.BackgroundColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridViewDeal.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            resources.ApplyResources(this.dataGridViewDeal, "dataGridViewDeal");
            this.dataGridViewDeal.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dataGridViewDeal.MultiSelect = false;
            this.dataGridViewDeal.Name = "dataGridViewDeal";
            this.dataGridViewDeal.ReadOnly = true;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridViewDeal.RowHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dataGridViewDeal.RowHeadersVisible = false;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.dataGridViewDeal.RowsDefaultCellStyle = dataGridViewCellStyle3;
            this.dataGridViewDeal.RowTemplate.Height = 24;
            this.dataGridViewDeal.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridViewDeal.CellMouseDown += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridViewDeal_CellMouseDown);
            // 
            // splitter1
            // 
            resources.ApplyResources(this.splitter1, "splitter1");
            this.splitter1.Name = "splitter1";
            this.splitter1.TabStop = false;
            this.splitter1.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitter1_SplitterMoved);
            // 
            // panelMiddle
            // 
            this.panelMiddle.Controls.Add(this.tabControlOrders);
            resources.ApplyResources(this.panelMiddle, "panelMiddle");
            this.panelMiddle.Name = "panelMiddle";
            // 
            // tabControlOrders
            // 
            this.tabControlOrders.Controls.Add(this.tabPage1);
            this.tabControlOrders.Controls.Add(this.tabPage2);
            this.tabControlOrders.Controls.Add(this.tabPage3);
            this.tabControlOrders.Controls.Add(this.tabPage4);
            resources.ApplyResources(this.tabControlOrders, "tabControlOrders");
            this.tabControlOrders.Multiline = true;
            this.tabControlOrders.Name = "tabControlOrders";
            this.tabControlOrders.SelectedIndex = 3;
            this.tabControlOrders.SelectedIndexChanged += new System.EventHandler(this.tabControlOrders_SelectedIndexChanged);
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.dataGridViewPendOrder);
            resources.ApplyResources(this.tabPage1, "tabPage1");
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // dataGridViewPendOrder
            // 
            this.dataGridViewPendOrder.AllowUserToAddRows = false;
            this.dataGridViewPendOrder.AllowUserToDeleteRows = false;
            this.dataGridViewPendOrder.AllowUserToResizeRows = false;
            this.dataGridViewPendOrder.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridViewPendOrder.BackgroundColor = System.Drawing.SystemColors.Window;
            resources.ApplyResources(this.dataGridViewPendOrder, "dataGridViewPendOrder");
            this.dataGridViewPendOrder.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dataGridViewPendOrder.Cursor = System.Windows.Forms.Cursors.Default;
            this.dataGridViewPendOrder.EnableHeadersVisualStyles = false;
            this.dataGridViewPendOrder.MultiSelect = false;
            this.dataGridViewPendOrder.Name = "dataGridViewPendOrder";
            this.dataGridViewPendOrder.ReadOnly = true;
            this.dataGridViewPendOrder.RowHeadersVisible = false;
            this.dataGridViewPendOrder.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.dataGridViewPendOrder.RowTemplate.Height = 24;
            this.dataGridViewPendOrder.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.dataGridViewPendOrder.CellMouseUp += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView_CellMouseUp);
            this.dataGridViewPendOrder.CellMouseDown += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView_CellMouseDown);
            this.dataGridViewPendOrder.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewOrder_CellClick);
            this.dataGridViewPendOrder.CellMouseDoubleClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView_CellMouseDoubleClick);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.dataGridViewCompletedOrder);
            resources.ApplyResources(this.tabPage2, "tabPage2");
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // dataGridViewCompletedOrder
            // 
            this.dataGridViewCompletedOrder.AllowUserToAddRows = false;
            this.dataGridViewCompletedOrder.AllowUserToDeleteRows = false;
            this.dataGridViewCompletedOrder.AllowUserToResizeRows = false;
            this.dataGridViewCompletedOrder.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridViewCompletedOrder.BackgroundColor = System.Drawing.SystemColors.Window;
            resources.ApplyResources(this.dataGridViewCompletedOrder, "dataGridViewCompletedOrder");
            this.dataGridViewCompletedOrder.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dataGridViewCompletedOrder.EnableHeadersVisualStyles = false;
            this.dataGridViewCompletedOrder.MultiSelect = false;
            this.dataGridViewCompletedOrder.Name = "dataGridViewCompletedOrder";
            this.dataGridViewCompletedOrder.ReadOnly = true;
            this.dataGridViewCompletedOrder.RowHeadersVisible = false;
            this.dataGridViewCompletedOrder.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.dataGridViewCompletedOrder.RowTemplate.Height = 24;
            this.dataGridViewCompletedOrder.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.dataGridViewCompletedOrder.CellMouseUp += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView_CellMouseUp);
            this.dataGridViewCompletedOrder.CellMouseDown += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView_CellMouseDown);
            this.dataGridViewCompletedOrder.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewOrder_CellClick);
            this.dataGridViewCompletedOrder.CellMouseDoubleClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView_CellMouseDoubleClick);
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.dataGridViewRepliedOrder);
            resources.ApplyResources(this.tabPage3, "tabPage3");
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // dataGridViewRepliedOrder
            // 
            this.dataGridViewRepliedOrder.AllowUserToAddRows = false;
            this.dataGridViewRepliedOrder.AllowUserToDeleteRows = false;
            this.dataGridViewRepliedOrder.AllowUserToResizeRows = false;
            this.dataGridViewRepliedOrder.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridViewRepliedOrder.BackgroundColor = System.Drawing.SystemColors.Window;
            resources.ApplyResources(this.dataGridViewRepliedOrder, "dataGridViewRepliedOrder");
            this.dataGridViewRepliedOrder.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dataGridViewRepliedOrder.EnableHeadersVisualStyles = false;
            this.dataGridViewRepliedOrder.MultiSelect = false;
            this.dataGridViewRepliedOrder.Name = "dataGridViewRepliedOrder";
            this.dataGridViewRepliedOrder.ReadOnly = true;
            this.dataGridViewRepliedOrder.RowHeadersVisible = false;
            this.dataGridViewRepliedOrder.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.dataGridViewRepliedOrder.RowTemplate.Height = 24;
            this.dataGridViewRepliedOrder.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.dataGridViewRepliedOrder.CellMouseUp += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView_CellMouseUp);
            this.dataGridViewRepliedOrder.CellMouseDown += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView_CellMouseDown);
            this.dataGridViewRepliedOrder.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewOrder_CellClick);
            this.dataGridViewRepliedOrder.CellMouseDoubleClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView_CellMouseDoubleClick);
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.dataGridViewAllOrder);
            resources.ApplyResources(this.tabPage4, "tabPage4");
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // dataGridViewAllOrder
            // 
            this.dataGridViewAllOrder.AllowUserToAddRows = false;
            this.dataGridViewAllOrder.AllowUserToDeleteRows = false;
            this.dataGridViewAllOrder.AllowUserToResizeRows = false;
            this.dataGridViewAllOrder.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridViewAllOrder.BackgroundColor = System.Drawing.SystemColors.Window;
            resources.ApplyResources(this.dataGridViewAllOrder, "dataGridViewAllOrder");
            this.dataGridViewAllOrder.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dataGridViewAllOrder.EnableHeadersVisualStyles = false;
            this.dataGridViewAllOrder.MultiSelect = false;
            this.dataGridViewAllOrder.Name = "dataGridViewAllOrder";
            this.dataGridViewAllOrder.ReadOnly = true;
            this.dataGridViewAllOrder.RowHeadersVisible = false;
            this.dataGridViewAllOrder.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.dataGridViewAllOrder.RowTemplate.Height = 24;
            this.dataGridViewAllOrder.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.dataGridViewAllOrder.CellMouseUp += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView_CellMouseUp);
            this.dataGridViewAllOrder.CellMouseDown += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView_CellMouseDown);
            this.dataGridViewAllOrder.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewOrder_CellClick);
            this.dataGridViewAllOrder.CellMouseDoubleClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView_CellMouseDoubleClick);
            // 
            // SortGridTimer
            // 
            this.SortGridTimer.Enabled = true;
            this.SortGridTimer.Tick += new System.EventHandler(this.SortGridTimer_Tick);
            // 
            // OrderBookForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.panelChkBox);
            this.Controls.Add(this.panelMiddle);
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this.panelBottom);
            this.Controls.Add(this.panelTop);
            this.Name = "OrderBookForm";
            this.Shown += new System.EventHandler(this.OrderBookForm_Shown);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.OrderBookForm_FormClosed);
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.myGroupBoxSrch.ResumeLayout(false);
            this.myGroupBoxSrch.PerformLayout();
            this.panelChkBox.ResumeLayout(false);
            this.panelChkBox.PerformLayout();
            this.panelBottom.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewDeal)).EndInit();
            this.panelMiddle.ResumeLayout(false);
            this.tabControlOrders.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewPendOrder)).EndInit();
            this.tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewCompletedOrder)).EndInit();
            this.tabPage3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewRepliedOrder)).EndInit();
            this.tabPage4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewAllOrder)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.DataGridView dataGridViewDeal;
        private System.Windows.Forms.Splitter splitter1;
        private System.Windows.Forms.Panel panelMiddle;
        private System.Windows.Forms.TabControl tabControlOrders;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.DataGridView dataGridViewPendOrder;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.DataGridView dataGridViewCompletedOrder;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.DataGridView dataGridViewRepliedOrder;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.DataGridView dataGridViewAllOrder;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Splitter splitter2;
        private System.Windows.Forms.Label labelInvalidMsgVal;
        private System.Windows.Forms.Button buttonGetTransacCharge;
        private System.Windows.Forms.Label labelTransacChargeVal;
        private System.Windows.Forms.Timer SortGridTimer;
        private System.Windows.Forms.Label labelAE;
        private System.Windows.Forms.CheckBox checkBoxEnableSrch;
        private System.Windows.Forms.ComboBox comboBoxAE;
        private StockTerminal.Utils.myGroupBox myGroupBoxSrch;
        private System.Windows.Forms.Label labelRemark;
        private System.Windows.Forms.Label labelAETarget;
        private System.Windows.Forms.Label labelTtlOrders;
        private System.Windows.Forms.Label labelTtlOrdersVal;
        private System.Windows.Forms.Label labelTtlOrderPriceVal;
        private System.Windows.Forms.Label labelTtlOrderPrice;
        private System.Windows.Forms.Panel panelChkBox;
        private System.Windows.Forms.CheckBox checkBoxShowSearch;
        private System.Windows.Forms.CheckBox checkBoxShowInternetOrders;
        private System.Windows.Forms.CheckBox checkBoxShowAEOrders;
        private System.Windows.Forms.CheckBox checkBoxShowDeal;
        private System.Windows.Forms.Label labelTtlDealPrice;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label labelTtlDealNo;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label labelAccTarget;
        private System.Windows.Forms.Label labelAcc;
        private System.Windows.Forms.Label labelBuy;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label labelStkCodeTarget;
        private System.Windows.Forms.Label labelStkCode;
        private System.Windows.Forms.TextBox textBoxStkCode;
        private System.Windows.Forms.Label labelBuyTtlQty;
        private System.Windows.Forms.Label labelBuyAvgPrice;
        private System.Windows.Forms.Label labelBuyFilled;
        private System.Windows.Forms.Label labelSellAvgPrice;
        private System.Windows.Forms.Label labelSellFilled;
        private System.Windows.Forms.Label labelSellTtlQty;
        private System.Windows.Forms.ComboBox comboBoxAccount;
        private System.Windows.Forms.Label labelCurrency;
        private System.Windows.Forms.Label labelPlacedBy;
        private System.Windows.Forms.Label labelPlacedByVal;
        private System.Windows.Forms.ComboBox comboBoxShowEx;
        private System.Windows.Forms.Label labelXchg;
        private System.Windows.Forms.Button buttonApplySrchSetting;
        private System.Windows.Forms.Button buttonClrSrchSetting;
        private System.Windows.Forms.CheckBox checkBoxGrayStock;


    }
}
