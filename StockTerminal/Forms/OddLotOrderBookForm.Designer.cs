namespace StockTerminal.Forms
{
    partial class OddLotOrderBookForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OddLotOrderBookForm));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.checkBoxSoundAlert = new System.Windows.Forms.CheckBox();
            this.labelSide = new System.Windows.Forms.Label();
            this.comboBoxShowSide = new System.Windows.Forms.ComboBox();
            this.buttonClrSrchSetting = new System.Windows.Forms.Button();
            this.buttonApplySrchSetting = new System.Windows.Forms.Button();
            this.textBoxStkCode = new System.Windows.Forms.TextBox();
            this.labelStkCodeTarget = new System.Windows.Forms.Label();
            this.labelStkCode = new System.Windows.Forms.Label();
            this.dataGridViewOddLot = new System.Windows.Forms.DataGridView();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewOddLot)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.checkBoxSoundAlert);
            this.groupBox1.Controls.Add(this.labelSide);
            this.groupBox1.Controls.Add(this.comboBoxShowSide);
            this.groupBox1.Controls.Add(this.buttonClrSrchSetting);
            this.groupBox1.Controls.Add(this.buttonApplySrchSetting);
            this.groupBox1.Controls.Add(this.textBoxStkCode);
            this.groupBox1.Controls.Add(this.labelStkCodeTarget);
            this.groupBox1.Controls.Add(this.labelStkCode);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // checkBoxSoundAlert
            // 
            resources.ApplyResources(this.checkBoxSoundAlert, "checkBoxSoundAlert");
            this.checkBoxSoundAlert.Checked = true;
            this.checkBoxSoundAlert.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxSoundAlert.Name = "checkBoxSoundAlert";
            this.checkBoxSoundAlert.UseVisualStyleBackColor = true;
            this.checkBoxSoundAlert.CheckedChanged += new System.EventHandler(this.checkBoxSoundAlert_CheckedChanged);
            // 
            // labelSide
            // 
            resources.ApplyResources(this.labelSide, "labelSide");
            this.labelSide.BackColor = System.Drawing.Color.Cyan;
            this.labelSide.Name = "labelSide";
            // 
            // comboBoxShowSide
            // 
            this.comboBoxShowSide.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxShowSide.FormattingEnabled = true;
            resources.ApplyResources(this.comboBoxShowSide, "comboBoxShowSide");
            this.comboBoxShowSide.Name = "comboBoxShowSide";
            this.comboBoxShowSide.SelectionChangeCommitted += new System.EventHandler(this.comboBoxShowSide_SelectionChangeCommitted);
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
            // textBoxStkCode
            // 
            resources.ApplyResources(this.textBoxStkCode, "textBoxStkCode");
            this.textBoxStkCode.Name = "textBoxStkCode";
            this.textBoxStkCode.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxStkCode_KeyDown);
            this.textBoxStkCode.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxStkCode_KeyPress);
            // 
            // labelStkCodeTarget
            // 
            this.labelStkCodeTarget.BackColor = System.Drawing.Color.Cyan;
            resources.ApplyResources(this.labelStkCodeTarget, "labelStkCodeTarget");
            this.labelStkCodeTarget.Name = "labelStkCodeTarget";
            // 
            // labelStkCode
            // 
            resources.ApplyResources(this.labelStkCode, "labelStkCode");
            this.labelStkCode.BackColor = System.Drawing.Color.Cyan;
            this.labelStkCode.Name = "labelStkCode";
            // 
            // dataGridViewOddLot
            // 
            this.dataGridViewOddLot.AllowUserToAddRows = false;
            this.dataGridViewOddLot.AllowUserToDeleteRows = false;
            this.dataGridViewOddLot.AllowUserToResizeRows = false;
            this.dataGridViewOddLot.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridViewOddLot.BackgroundColor = System.Drawing.SystemColors.Window;
            resources.ApplyResources(this.dataGridViewOddLot, "dataGridViewOddLot");
            this.dataGridViewOddLot.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dataGridViewOddLot.EnableHeadersVisualStyles = false;
            this.dataGridViewOddLot.MultiSelect = false;
            this.dataGridViewOddLot.Name = "dataGridViewOddLot";
            this.dataGridViewOddLot.ReadOnly = true;
            this.dataGridViewOddLot.RowHeadersVisible = false;
            this.dataGridViewOddLot.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.dataGridViewOddLot.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.dataGridViewOddLot.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewOddLot_CellClick);
            // 
            // OddLotOrderBookForm
            // 
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.dataGridViewOddLot);
            this.Controls.Add(this.groupBox1);
            this.Name = "OddLotOrderBookForm";
            this.Shown += new System.EventHandler(this.OddLotOrderBookForm_Shown);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewOddLot)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button buttonClrSrchSetting;
        private System.Windows.Forms.Button buttonApplySrchSetting;
        private System.Windows.Forms.TextBox textBoxStkCode;
        private System.Windows.Forms.Label labelStkCodeTarget;
        private System.Windows.Forms.Label labelStkCode;
        private System.Windows.Forms.Label labelSide;
        private System.Windows.Forms.ComboBox comboBoxShowSide;
        private System.Windows.Forms.DataGridView dataGridViewOddLot;
        private System.Windows.Forms.CheckBox checkBoxSoundAlert;
    }
}
