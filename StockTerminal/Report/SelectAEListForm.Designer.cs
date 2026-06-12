namespace StockTerminal.Report
{
    partial class SelectAEListForm
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
            this.comboBoxAEList = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // comboBoxAEList
            // 
            this.comboBoxAEList.Enabled = false;
            this.comboBoxAEList.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.comboBoxAEList.FormattingEnabled = true;
            this.comboBoxAEList.Location = new System.Drawing.Point(12, 12);
            this.comboBoxAEList.Name = "comboBoxAEList";
            this.comboBoxAEList.Size = new System.Drawing.Size(406, 23);
            this.comboBoxAEList.TabIndex = 0;
            this.comboBoxAEList.SelectionChangeCommitted += new System.EventHandler(this.comboBoxAEList_SelectionChangeCommitted);
            this.comboBoxAEList.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.comboBoxAEList_KeyPress);
            this.comboBoxAEList.KeyDown += new System.Windows.Forms.KeyEventHandler(this.comboBoxAEList_KeyDown);
            // 
            // SelectAEListForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(430, 49);
            this.Controls.Add(this.comboBoxAEList);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SelectAEListForm";
            this.Text = "SelectAEListForm";
            this.Shown += new System.EventHandler(this.SelectAEListForm_Shown);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBoxAEList;
    }
}