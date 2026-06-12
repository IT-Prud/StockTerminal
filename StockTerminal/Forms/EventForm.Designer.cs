namespace StockTerminal.Forms
{
    partial class EventForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EventForm));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.panelEvent = new System.Windows.Forms.Panel();
            this.panelCheckBox = new System.Windows.Forms.Panel();
            this.checkBoxPrintDeal = new System.Windows.Forms.CheckBox();
            this.checkBoxRejectAlert = new System.Windows.Forms.CheckBox();
            this.checkBoxCancelAlert = new System.Windows.Forms.CheckBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.dataGridViewEvent = new System.Windows.Forms.DataGridView();
            this.Time = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.EventType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.EventMessage = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.clearAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.timerSortGrid = new System.Windows.Forms.Timer(this.components);
            this.panelEvent.SuspendLayout();
            this.panelCheckBox.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewEvent)).BeginInit();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelEvent
            // 
            this.panelEvent.Controls.Add(this.panelCheckBox);
            this.panelEvent.Controls.Add(this.panel1);
            resources.ApplyResources(this.panelEvent, "panelEvent");
            this.panelEvent.Name = "panelEvent";
            // 
            // panelCheckBox
            // 
            this.panelCheckBox.Controls.Add(this.checkBoxPrintDeal);
            this.panelCheckBox.Controls.Add(this.checkBoxRejectAlert);
            this.panelCheckBox.Controls.Add(this.checkBoxCancelAlert);
            resources.ApplyResources(this.panelCheckBox, "panelCheckBox");
            this.panelCheckBox.Name = "panelCheckBox";
            // 
            // checkBoxPrintDeal
            // 
            resources.ApplyResources(this.checkBoxPrintDeal, "checkBoxPrintDeal");
            this.checkBoxPrintDeal.Name = "checkBoxPrintDeal";
            this.checkBoxPrintDeal.UseVisualStyleBackColor = true;
            // 
            // checkBoxRejectAlert
            // 
            resources.ApplyResources(this.checkBoxRejectAlert, "checkBoxRejectAlert");
            this.checkBoxRejectAlert.Checked = true;
            this.checkBoxRejectAlert.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxRejectAlert.Name = "checkBoxRejectAlert";
            this.checkBoxRejectAlert.UseVisualStyleBackColor = true;
            this.checkBoxRejectAlert.CheckedChanged += new System.EventHandler(this.checkBoxRejectAlert_CheckedChanged);
            // 
            // checkBoxCancelAlert
            // 
            resources.ApplyResources(this.checkBoxCancelAlert, "checkBoxCancelAlert");
            this.checkBoxCancelAlert.Checked = true;
            this.checkBoxCancelAlert.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxCancelAlert.Name = "checkBoxCancelAlert";
            this.checkBoxCancelAlert.UseVisualStyleBackColor = true;
            this.checkBoxCancelAlert.CheckedChanged += new System.EventHandler(this.checkBoxCancelAlert_CheckedChanged);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.dataGridViewEvent);
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.Name = "panel1";
            // 
            // dataGridViewEvent
            // 
            this.dataGridViewEvent.AllowUserToAddRows = false;
            this.dataGridViewEvent.AllowUserToDeleteRows = false;
            this.dataGridViewEvent.AllowUserToResizeRows = false;
            this.dataGridViewEvent.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridViewEvent.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dataGridViewEvent.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dataGridViewEvent.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Time,
            this.EventType,
            this.EventMessage});
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 7F);
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridViewEvent.DefaultCellStyle = dataGridViewCellStyle3;
            resources.ApplyResources(this.dataGridViewEvent, "dataGridViewEvent");
            this.dataGridViewEvent.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dataGridViewEvent.Name = "dataGridViewEvent";
            this.dataGridViewEvent.ReadOnly = true;
            this.dataGridViewEvent.RowHeadersVisible = false;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.dataGridViewEvent.RowsDefaultCellStyle = dataGridViewCellStyle4;
            this.dataGridViewEvent.RowTemplate.Height = 24;
            this.dataGridViewEvent.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridViewEvent.CellMouseDoubleClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridViewEvent_CellMouseDoubleClick);
            // 
            // Time
            // 
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            this.Time.DefaultCellStyle = dataGridViewCellStyle2;
            this.Time.FillWeight = 20F;
            resources.ApplyResources(this.Time, "Time");
            this.Time.Name = "Time";
            this.Time.ReadOnly = true;
            this.Time.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // EventType
            // 
            this.EventType.FillWeight = 25F;
            resources.ApplyResources(this.EventType, "EventType");
            this.EventType.Name = "EventType";
            this.EventType.ReadOnly = true;
            this.EventType.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // EventMessage
            // 
            this.EventMessage.FillWeight = 204.9864F;
            resources.ApplyResources(this.EventMessage, "EventMessage");
            this.EventMessage.Name = "EventMessage";
            this.EventMessage.ReadOnly = true;
            this.EventMessage.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.clearAllToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            resources.ApplyResources(this.contextMenuStrip1, "contextMenuStrip1");
            // 
            // clearAllToolStripMenuItem
            // 
            this.clearAllToolStripMenuItem.Name = "clearAllToolStripMenuItem";
            resources.ApplyResources(this.clearAllToolStripMenuItem, "clearAllToolStripMenuItem");
            this.clearAllToolStripMenuItem.Click += new System.EventHandler(this.clearAllToolStripMenuItem_Click);
            // 
            // timerSortGrid
            // 
            this.timerSortGrid.Enabled = true;
            this.timerSortGrid.Interval = 1500;
            this.timerSortGrid.Tick += new System.EventHandler(this.timerSortGrid_Tick);
            // 
            // EventForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CloseButton = false;
            this.Controls.Add(this.panelEvent);
            this.Name = "EventForm";
            this.Shown += new System.EventHandler(this.EventForm_Shown);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.EventForm_FormClosing);
            this.Resize += new System.EventHandler(this.EventForm_Resize);
            this.panelEvent.ResumeLayout(false);
            this.panelCheckBox.ResumeLayout(false);
            this.panelCheckBox.PerformLayout();
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewEvent)).EndInit();
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelEvent;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem clearAllToolStripMenuItem;
        private System.Windows.Forms.DataGridView dataGridViewEvent;
        private System.Windows.Forms.Timer timerSortGrid;
        private System.Windows.Forms.Panel panelCheckBox;
        private System.Windows.Forms.CheckBox checkBoxRejectAlert;
        private System.Windows.Forms.CheckBox checkBoxCancelAlert;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Time;
        private System.Windows.Forms.DataGridViewTextBoxColumn EventType;
        private System.Windows.Forms.DataGridViewTextBoxColumn EventMessage;
        private System.Windows.Forms.CheckBox checkBoxPrintDeal;
    }
}