using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using StockTerminal.Utils;
using WeifenLuo.WinFormsUI.Docking;
using System.Globalization;

namespace StockTerminal.Report
{
    public partial class SelectOrderStatusForm : StockTerminal.Forms.BaseForm
    {
        public bool IsDetailReport = false;
        private DockPanel dockPanelMain;

        public SelectOrderStatusForm(CultureInfo Culture, string PersistString, DockPanel dockPanelMain)
            : base(Culture, PersistString)
        {
            InitializeComponent();
            LayoutLockable = false;
            this.dockPanelMain = dockPanelMain;
            Init();
        }

        //public SelectOrderStatusForm()
        //{
        //    InitializeComponent();
        //}

        private void Init()
        {
            comboBoxStatusList.Items.Insert((int)Utils.Utils.OrderStatusState.All, new ComboBoxItem("All", "All"));
            comboBoxStatusList.Items.Insert((int)Utils.Utils.OrderStatusState.Filled, new ComboBoxItem("Filled", "Filled"));
            comboBoxStatusList.Items.Insert((int)Utils.Utils.OrderStatusState.Queue, new ComboBoxItem("Queue", "Queue"));
            comboBoxStatusList.Items.Insert((int)Utils.Utils.OrderStatusState.CancelRejected, new ComboBoxItem("CancelReject", "CancelReject"));
        }

        private void comboBoxStatusList_KeyDown(object sender, KeyEventArgs e)
        {
            //if (e.KeyCode == Keys.Enter && comboBoxStatusList != null && comboBoxStatusList.Items != null)
        }

        private void comboBoxStatusList_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar >= 97 && e.KeyChar <= 122)
                e.KeyChar = (char)(e.KeyChar & 223);
            else if (e.KeyChar != 8 && (e.KeyChar < 48 || (e.KeyChar > 57 && e.KeyChar < 65) || e.KeyChar > 90))
                e.Handled = true;
        }

        private void comboBoxStatusList_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (comboBoxStatusList != null && comboBoxStatusList.SelectedItem != null)
            {
                int keyFound = comboBoxStatusList.SelectedIndex;

                Report.SelectAEListForm form = new StockTerminal.Report.SelectAEListForm(this.Culture, null, dockPanelMain);
                switch (keyFound)
                {
                    case 0: form.OS = Utils.Utils.OrderStatusState.All; break;
                    case 1: form.OS = Utils.Utils.OrderStatusState.Filled; break;
                    case 2: form.OS = Utils.Utils.OrderStatusState.Queue; break;
                    case 3: form.OS = Utils.Utils.OrderStatusState.CancelRejected; break;
                }
                form.IsDetailReport = this.IsDetailReport;
                form.Show(dockPanelMain, DockState.Float, new Rectangle(200, 200, form.Width, form.Height));
                this.Dispose();
            }
        }
    }
}
