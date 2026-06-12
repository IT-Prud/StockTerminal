using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using TradeDB;
using System.Globalization;
using StockTerminal.Utils;
using System.Collections;

namespace StockTerminal.Report
{
    public partial class SelectAEListForm : StockTerminal.Forms.BaseForm
    {
        public bool IsDetailReport = false;
        private object AEToBeFilledListMutex = new object();
        private DockPanel dockPanelMain;
        private SortedList sortedAEList = new SortedList();
        public Utils.Utils.OrderStatusState OS = Utils.Utils.OrderStatusState.All;

        public SelectAEListForm(CultureInfo Culture, string PersistString, DockPanel dockPanelMain)
            : base(Culture, PersistString)
        {
            InitializeComponent();
            LayoutLockable = false;
            this.dockPanelMain = dockPanelMain;
        }

        private void SelectAEListForm_Shown(object sender, EventArgs e)
        {
            comboBoxAEList.AutoCompleteSource = AutoCompleteSource.None;
            comboBoxAEList.AutoCompleteMode = AutoCompleteMode.None;
            comboBoxAEList.Sorted = false;
            comboBoxAEList.Text = GetResxString("Loading");
            comboBoxAEList.SuspendLayout();

            ListenAE();
        }

        private void comboBoxAEList_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (comboBoxAEList != null && comboBoxAEList.SelectedItem != null)
                NewReportForm(((ComboBoxItem)comboBoxAEList.SelectedItem).Value.ToString());
        }

        protected override void OnAE(List<AccountExecutive> AEList)
        {
            if (AEList == null || AEList.Count < 1)
                return;

            // If only 1 AE arrived, then no need to select, straightly show the report
            if (AEList.Count == 1 && comboBoxAEList != null && AEList[0] != null)
            {
                comboBoxAEList.Text = AEList[0].ToString();
                NewReportForm(AEList[0].Code.ToString());
                return;
            }

            lock (AEToBeFilledListMutex)
            {
                foreach (AccountExecutive AE in AEList)
                    if (AE != null)
                        sortedAEList.Add(AE.Code, AE.Code + " " + AE.Name);

                foreach (DictionaryEntry AE in sortedAEList)
                    comboBoxAEList.Items.Insert(sortedAEList.IndexOfKey(AE.Key), new ComboBoxItem(AE.Value.ToString(), AE.Key.ToString()));
                
                if (!comboBoxAEList.Enabled)
                {
                    if (comboBoxAEList.SelectedIndex <= 0) 
                        comboBoxAEList.Text = "";
                    comboBoxAEList.Enabled = true;
                    comboBoxAEList.AutoCompleteSource = AutoCompleteSource.ListItems;
                    comboBoxAEList.AutoCompleteMode = AutoCompleteMode.Suggest;
                    comboBoxAEList.ResumeLayout();
                    comboBoxAEList.Focus();
                }
            }
        }

        private void comboBoxAEList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && comboBoxAEList != null && comboBoxAEList.Items != null)
            {
                string strAE = comboBoxAEList.Text;
                int keyFound = sortedAEList.IndexOfKey(strAE);
                if (keyFound >= 0)
                {
                    NewReportForm(((ComboBoxItem)comboBoxAEList.Items[keyFound]).Value.ToString());
                    return;
                }

                int valueFound = sortedAEList.IndexOfValue(strAE);
                if (valueFound >= 0)
                    NewReportForm(((ComboBoxItem)comboBoxAEList.Items[valueFound]).Value.ToString());
            }
        }

        private void NewReportForm(string AE)
        {
            if (IsDetailReport)
            {
                Report.DailyReportDetailsForm form = new DailyReportDetailsForm(this.Culture, null, AE);
                form.OS = this.OS;
                form.Show(dockPanelMain, DockState.Float, new Rectangle(600, 500, form.Width, form.Height));
            }
            else
            {
                Report.DailyReportForm form = new StockTerminal.Report.DailyReportForm(this.Culture, null, AE);
                form.Show(dockPanelMain, DockState.Float, new Rectangle(400, 400, form.Width, form.Height));
            }
            this.IsProgramClosing = true;
            this.Close();
            this.Dispose();
        }

        private void comboBoxAEList_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar >= 97 && e.KeyChar <= 122)
                e.KeyChar = (char)(e.KeyChar & 223);
            else if (e.KeyChar != 8 && (e.KeyChar < 48 || (e.KeyChar > 57 && e.KeyChar < 65) || e.KeyChar > 90))
                e.Handled = true;
        }
    }
}
