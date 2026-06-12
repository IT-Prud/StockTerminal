namespace StockTerminal.Forms
{
    partial class DiscretionConfirmForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DiscretionConfirmForm));
            this.labelUnauthorized = new System.Windows.Forms.Label();
            this.buttonClientConfirmed = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // labelUnauthorized
            // 
            this.labelUnauthorized.AccessibleDescription = null;
            this.labelUnauthorized.AccessibleName = null;
            resources.ApplyResources(this.labelUnauthorized, "labelUnauthorized");
            this.labelUnauthorized.Name = "labelUnauthorized";
            // 
            // buttonClientConfirmed
            // 
            this.buttonClientConfirmed.AccessibleDescription = null;
            this.buttonClientConfirmed.AccessibleName = null;
            resources.ApplyResources(this.buttonClientConfirmed, "buttonClientConfirmed");
            this.buttonClientConfirmed.BackgroundImage = null;
            this.buttonClientConfirmed.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonClientConfirmed.Name = "buttonClientConfirmed";
            this.buttonClientConfirmed.UseVisualStyleBackColor = true;
            // 
            // buttonCancel
            // 
            this.buttonCancel.AccessibleDescription = null;
            this.buttonCancel.AccessibleName = null;
            resources.ApplyResources(this.buttonCancel, "buttonCancel");
            this.buttonCancel.BackgroundImage = null;
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // DiscretionConfirmForm
            // 
            this.AccessibleDescription = null;
            this.AccessibleName = null;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = null;
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonClientConfirmed);
            this.Controls.Add(this.labelUnauthorized);
            this.Font = null;
            this.Icon = null;
            this.Name = "DiscretionConfirmForm";
            this.ToolTipText = null;
            this.Load += new System.EventHandler(this.DiscretionConfirmForm_Load);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.DiscretionConfirmForm_FormClosed);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.DiscretionConfirmForm_KeyDown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelUnauthorized;
        private System.Windows.Forms.Button buttonClientConfirmed;
        private System.Windows.Forms.Button buttonCancel;
    }
}