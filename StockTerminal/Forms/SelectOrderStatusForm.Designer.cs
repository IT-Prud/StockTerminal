namespace StockTerminal.Report
{
    partial class SelectOrderStatusForm
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
            this.comboBoxStatusList = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // comboBoxStatusList
            // 
            this.comboBoxStatusList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxStatusList.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.comboBoxStatusList.FormattingEnabled = true;
            this.comboBoxStatusList.Location = new System.Drawing.Point(3, 12);
            this.comboBoxStatusList.Name = "comboBoxStatusList";
            this.comboBoxStatusList.Size = new System.Drawing.Size(406, 23);
            this.comboBoxStatusList.TabIndex = 1;
            this.comboBoxStatusList.SelectionChangeCommitted += new System.EventHandler(this.comboBoxStatusList_SelectionChangeCommitted);
            this.comboBoxStatusList.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.comboBoxStatusList_KeyPress);
            this.comboBoxStatusList.KeyDown += new System.Windows.Forms.KeyEventHandler(this.comboBoxStatusList_KeyDown);
            // 
            // SelectOrderStatusForm
            // 
            this.ClientSize = new System.Drawing.Size(418, 51);
            this.Controls.Add(this.comboBoxStatusList);
            this.Name = "SelectOrderStatusForm";
            this.Text = "Select Order Status Form";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBoxStatusList;
    }
}
