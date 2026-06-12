namespace StockTerminal.Forms
{
    partial class PresetAcc
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PresetAcc));
            this.dataGridViewPresetAcc = new System.Windows.Forms.DataGridView();
            this.PresetNo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.AccountNo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewPresetAcc)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridViewPresetAcc
            // 
            this.dataGridViewPresetAcc.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewPresetAcc.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.PresetNo,
            this.AccountNo});
            resources.ApplyResources(this.dataGridViewPresetAcc, "dataGridViewPresetAcc");
            this.dataGridViewPresetAcc.MultiSelect = false;
            this.dataGridViewPresetAcc.Name = "dataGridViewPresetAcc";
            this.dataGridViewPresetAcc.RowHeadersVisible = false;
            this.dataGridViewPresetAcc.RowTemplate.Height = 24;
            this.dataGridViewPresetAcc.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewPresetAcc_CellContentClick);
            // 
            // PresetNo
            // 
            resources.ApplyResources(this.PresetNo, "PresetNo");
            this.PresetNo.Name = "PresetNo";
            // 
            // AccountNo
            // 
            resources.ApplyResources(this.AccountNo, "AccountNo");
            this.AccountNo.Name = "AccountNo";
            // 
            // PresetAcc
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.dataGridViewPresetAcc);
            this.Name = "PresetAcc";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewPresetAcc)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridViewPresetAcc;
        private System.Windows.Forms.DataGridViewTextBoxColumn PresetNo;
        private System.Windows.Forms.DataGridViewTextBoxColumn AccountNo;
    }
}