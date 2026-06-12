using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using TradeDB;
using System.Globalization;
using WeifenLuo.WinFormsUI.Docking;
using System.IO;
using Utils;

namespace StockTerminal.Forms
{
    public partial class OddLotOrderBookForm : StockTerminal.Forms.BaseForm
    {
        private Dictionary<string, DataGridViewRow> OddLotGridDict = new Dictionary<string, DataGridViewRow>(20);
        private readonly object OddLotGridDictMutex = new object();

        private int iOrderPriceColIdx = 3;
        private readonly object GridClickMutex = new object();

        public OddLotOrderBookForm(CultureInfo Culture, string PersistString, DockPanel dockPanelMain)
            : base(Culture, PersistString)
        {
            InitializeComponent();

            //pEventDoubleClickedDelegate = new EventForm.EventDoubleClickedDelegate(OnEventFormDoubleClicked);
            //AddEventDoubleClickedDelegate();

            //this.dockPanelMain = dockPanelMain;
            //blinkCellData = new List<CellData>();
            //ListenFormListChange();
        }

        private readonly string ShowAll = "ALL", ShowBuyOnly = "Buy", ShowSellOnly = "Sell";
        private void OddLotOrderBookForm_Shown(object sender, EventArgs e)
        {
            comboBoxShowSide.Items.AddRange(new string[] { ShowAll, ShowBuyOnly, ShowSellOnly });
            comboBoxShowSide.SelectedIndex = 0;
            InitGrid();
        }

        private void buttonApplySrchSetting_Click(object sender, EventArgs e)
        {
            int iStkCode = 0;
            string strStkCode = textBoxStkCode.Text.Trim();
            if (int.TryParse(strStkCode, out iStkCode) && strStkCode != "")
                labelStkCodeTarget.Text = textBoxStkCode.Text;
            else
            {
                //if (textBoxStkCode != null)
                //    dataGridViewOddLot.Columns[strOrderCol_StkCode].HeaderCell.Style.BackColor = SystemColors.Control;
                labelStkCodeTarget.Text = "";
            }
            PerformSearch();
        }

        private void buttonClrSrchSetting_Click(object sender, EventArgs e)
        {
            dataGridViewOddLot.Rows.Clear();
            lock (OddLotGridDictMutex)
            {
                OddLotGridDict.Clear();
            }
            comboBoxShowSide.SelectedIndex = 0;
            labelStkCodeTarget.Text = "";
            PerformSearch();
            textBoxStkCode.Focus();
        }

        DateTime TimeSearchStart = DateTime.MinValue;
        private void PerformSearch()
        {
            dataGridViewOddLot.Rows.Clear();
            lock (OddLotGridDictMutex)
            {
                OddLotGridDict.Clear();
            }
            TimeSearchStart = DateTime.Now;
            ListenOddLotOrder(new List<string> { "HKG~" + labelStkCodeTarget.Text.Trim() }, false);
            textBoxStkCode.Text = "";
            textBoxStkCode.Focus();
        }

        private void InitGrid()
        {
            string strOrderCol_OrderNo = "Orderno", strOrderCol_SubmitBrokerNo = "SubmitBrokerNo", strOrderCol_OrderSide = "Order_Side",
                strOrderCol_StkCode = "StockCode", strOrderCol_OrderPrice = "Order_Price", strOrderCol_OrderQty = "Order_Qty";
            dataGridViewOddLot.ColumnCount = 6; // , strOrderCol_OrderType = "OrderType"
            dataGridViewOddLot.BackgroundColor = Color.AliceBlue;
            dataGridViewOddLot.Columns[0].Name = strOrderCol_OrderNo;
            dataGridViewOddLot.Columns[1].Name = strOrderCol_OrderSide;
            dataGridViewOddLot.Columns[2].Name = strOrderCol_StkCode;
            dataGridViewOddLot.Columns[iOrderPriceColIdx].Name = strOrderCol_OrderPrice;
            dataGridViewOddLot.Columns[4].Name = strOrderCol_OrderQty;
            dataGridViewOddLot.Columns[5].Name = strOrderCol_SubmitBrokerNo;
            //dataGridViewOddLot.Columns[6].Name = strOrderCol_OrderType;

            dataGridViewOddLot.Columns[0].HeaderText = "Order No.";
            dataGridViewOddLot.Columns[1].HeaderText = "Side";
            dataGridViewOddLot.Columns[2].HeaderText = "Code";
            dataGridViewOddLot.Columns[iOrderPriceColIdx].HeaderText = "Price";
            dataGridViewOddLot.Columns[4].HeaderText = "Qty";
            dataGridViewOddLot.Columns[5].HeaderText = "Broker No.";
            //dataGridViewOddLot.Columns[6].HeaderText = "OrderType";

            for(int i=0; i < dataGridViewOddLot.ColumnCount; i++)
                dataGridViewOddLot.Columns[i].SortMode = DataGridViewColumnSortMode.Programmatic;
        }

        Dictionary<string, int> SortCounterDict = new Dictionary<string, int>(4);
        private int SortedColumnIndex = 0;
        private SortOrder SortedOrder = SortOrder.Descending;

        private void SortGrid(DataGridView dgv)
        {
            if (dgv == null || dgv.Columns.Count == 0)
                return;

            SortCounterDict[dgv.Name] = 1;
            if (SortedOrder == SortOrder.None)
                SortedOrder = SortOrder.Descending;

            if (dgv.Columns[SortedColumnIndex].SortMode == DataGridViewColumnSortMode.Programmatic)
            {
                if (SortedColumnIndex == iOrderPriceColIdx)
                    dgv.Sort(new Double32Comparer(SortedOrder, SortedColumnIndex));
                else
                    dgv.Sort(new Int32Comparer(SortedOrder, SortedColumnIndex));
            }
            else
            {
                dgv.Sort(dgv.Columns[SortedColumnIndex],
                    SortedOrder != SortOrder.Descending ? ListSortDirection.Ascending : ListSortDirection.Descending);
            }
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.ClearSelection();
            dgv.Refresh();

            // Set sort glyph direction
            int sortGlyphIndex = SortedColumnIndex;
            if (dgv.Columns[sortGlyphIndex].SortMode != DataGridViewColumnSortMode.NotSortable)
            {
                ClearSortGlyph(dgv, sortGlyphIndex);
                dgv.Columns[sortGlyphIndex].HeaderCell.SortGlyphDirection = SortedOrder;
            }
        }

        private void ClearSortGlyph(DataGridView DataGridView1, int columnIndex)
        {
            for (int i = 0; i < DataGridView1.Columns.Count; i++)
            {
                if (i != columnIndex)
                    DataGridView1.Columns[i].HeaderCell.SortGlyphDirection = SortOrder.None;
            }
        }

        //protected override void OnOddLotOrder(List<OddLotOrder> OddLotOrderList)
        protected override void OnOddLotOrderStatus(OddLotOrder ord)
        {
            //System.Diagnostics.Debug.Print("  OddLotOrderList.Count: " + OddLotOrderList.Count);
            //if (OddLotOrderList == null || OddLotOrderList.Count <= 0)
            if (ord == null)
                return;

            dataGridViewOddLot.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;    // performance reason
            //foreach (OddLotOrder ord in OddLotOrderList)
            {
                //System.Diagnostics.Debug.Print("  order no.: " + ord.OrderNo);
                DataGridViewRow dgvRow = null;
                object[] row1;
                string strOrderSide = null, strOrderNo = ord.OrderNo + "";
                if (ord.Side == 'B')
                {
                    strOrderSide = "B";
                    if (comboBoxShowSide.SelectedItem.ToString() != ShowAll && comboBoxShowSide.SelectedItem.ToString() != ShowBuyOnly)
                        return;
                }
                else
                {
                    strOrderSide = "S";
                    if (comboBoxShowSide.SelectedItem.ToString() != ShowAll && comboBoxShowSide.SelectedItem.ToString() != ShowSellOnly)
                        return;
                }

                row1 = new object[] { strOrderNo, strOrderSide, ord.StockCode, ord.Price, String.Format("{0:N0}", ord.Quantity), ord.SubmitBrokerNo }; // strExCode, ord.OrderType, 

                lock (OddLotGridDictMutex)
                {
                    if (OddLotGridDict.ContainsKey(strOrderNo))
                    {
                        if (ord.Deleted)
                        {
                            dgvRow = OddLotGridDict[strOrderNo] as DataGridViewRow;
                            dgvRow.Visible = false;
                        }
                        //continue;
                    }
                    else
                    {
                        //  Add new row
                        OddLotGridDict[strOrderNo] = dataGridViewOddLot.Rows[dataGridViewOddLot.Rows.Add(row1)];
                        dgvRow = OddLotGridDict[strOrderNo] as DataGridViewRow;
                        if (ord.Deleted)
                            dgvRow.Visible = false;
                        if (checkBoxSoundAlert.Checked && TimeSearchStart != DateTime.MinValue)
                        {
                            int searchElapsed = (int)(DateTime.Now - TimeSearchStart).TotalMilliseconds;
                            if (searchElapsed < 800)
                                TimeSearchStart = DateTime.Now;
                            else
                            { // play sound and highlight check box to red background
                                checkBoxSoundAlert.BackColor = Color.Red;
                                MemoryStream stream = (MemoryStream)GetResxObject("NewOddLot");
                                if (stream != null)
                                {
                                    byte[] buffer = null;
                                    buffer = new byte[stream.Length];
                                    if (stream.Read(buffer, 0, buffer.Length) > 0)
                                        WSound.PlayBuffer(buffer);
                                }
                            }
                        }
                    }
                }

                if (strOrderSide == "B") // GetResxString("Buy")
                    dgvRow.DefaultCellStyle.BackColor = Color.FromArgb(206, 235, 244);
                else if (strOrderSide == "S") // GetResxString("Sell")
                    dgvRow.DefaultCellStyle.BackColor = Color.LightPink;
                dgvRow.DefaultCellStyle.SelectionBackColor = Color.DarkBlue;
                dgvRow.DefaultCellStyle.SelectionForeColor = Color.White;
                dgvRow.Height = 19;
            }
            dataGridViewOddLot.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            SortGrid(dataGridViewOddLot);
            dataGridViewOddLot.Refresh();
        }

        private void comboBoxShowSide_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (labelStkCodeTarget.Text.Trim().Length <= 0)
                return;

            PerformSearch();
        }

        private void textBoxStkCode_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar >= 97 && e.KeyChar <= 122)
                e.KeyChar = (char)(e.KeyChar & 223);
            else if (e.KeyChar != 8 && (e.KeyChar < 48 || (e.KeyChar > 57 && e.KeyChar < 65) || e.KeyChar > 90))
                e.Handled = true;
        }

        private void textBoxStkCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;

            int iStkCode = 0;
            string strStkCode = textBoxStkCode.Text.Trim();
            if (int.TryParse(strStkCode, out iStkCode) && strStkCode != "")
                labelStkCodeTarget.Text = textBoxStkCode.Text;
            else
                labelStkCodeTarget.Text = "";
            PerformSearch();
        }

        private void dataGridViewOddLot_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            lock (GridClickMutex)
            {
                try
                {
                    if (e.RowIndex == -1 && dataGridViewOddLot.Columns[e.ColumnIndex].SortMode != DataGridViewColumnSortMode.NotSortable)  // && e.ColumnIndex == 0
                    {
                        if (SortedColumnIndex == e.ColumnIndex)
                        {
                            if (SortedOrder == SortOrder.Ascending)
                                SortedOrder = SortOrder.Descending;
                            else
                                SortedOrder = SortOrder.Ascending;
                        }
                        else
                        {
                            SortedOrder = SortOrder.Ascending;
                            SortedColumnIndex = e.ColumnIndex;
                        }

                        int i, n = dataGridViewOddLot.Columns.Count;
                        for (i = 0; i < n; i++)
                        {
                            if (i != e.ColumnIndex)
                            {
                                dataGridViewOddLot.Columns[i].HeaderCell.SortGlyphDirection = SortOrder.None;
                            }
                            else if (dataGridViewOddLot.Columns[i].SortMode == DataGridViewColumnSortMode.Programmatic)
                            {
                                if (e.ColumnIndex == iOrderPriceColIdx)
                                    dataGridViewOddLot.Sort(new Double32Comparer(SortedOrder, e.ColumnIndex));
                                else
                                    dataGridViewOddLot.Sort(new Int32Comparer(SortedOrder, e.ColumnIndex));
                                dataGridViewOddLot.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection = SortedOrder;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "dataGridViewOrder_CellClick");
                }
            }
        }

        #region "IComparer"

        private class Int32Comparer : System.Collections.IComparer
        {
            private static int sortOrderModifier = 1;
            private static int sortColumnIndex = 13;

            public Int32Comparer(SortOrder sortOrder, int sortColumnIndex)
            {
                if (sortOrder == SortOrder.Descending)
                {
                    sortOrderModifier = -1;
                }
                else if (sortOrder == SortOrder.Ascending)
                {
                    sortOrderModifier = 1;
                }
                Int32Comparer.sortColumnIndex = sortColumnIndex;
            }

            public int Compare(object x, object y)
            {
                DataGridViewRow DataGridViewRow1 = (DataGridViewRow)x;
                DataGridViewRow DataGridViewRow2 = (DataGridViewRow)y;

                // Try to sort based on the Last Name column.
                string sx = DataGridViewRow1.Cells[sortColumnIndex].Value != null ? DataGridViewRow1.Cells[sortColumnIndex].Value.ToString().Replace(",", "") : "";
                string sy = DataGridViewRow2.Cells[sortColumnIndex].Value != null ? DataGridViewRow2.Cells[sortColumnIndex].Value.ToString().Replace(",", "") : "";
                int vx, vy;

                int CompareResult = 0;

                if (int.TryParse(sx, out vx))
                {
                    if (int.TryParse(sy, out vy))
                    {
                        if (vx < vy) CompareResult = -1;
                        else if (vx > vy) CompareResult = 1;
                    }
                    else
                    {
                        if (sy.Length <= 0) CompareResult = 1;
                        else CompareResult = -1;
                    }
                }
                else if (int.TryParse(sy, out vy))
                {
                    if (sx.Length <= 0) CompareResult = -1;
                    else CompareResult = 1;
                }
                else
                {
                    CompareResult = System.String.Compare(sx, sy);
                }

                if (CompareResult == 0)
                    CompareResult = String.Compare(DataGridViewRow1.Cells[0].Value.ToString(), DataGridViewRow2.Cells[0].Value.ToString());

                return CompareResult * sortOrderModifier;
            }
        }


        private class Double32Comparer : System.Collections.IComparer
        {
            private static int sortOrderModifier = 1;
            private static int sortColumnIndex = 13;

            public Double32Comparer(SortOrder sortOrder, int sortColumnIndex)
            {
                if (sortOrder == SortOrder.Descending)
                {
                    sortOrderModifier = -1;
                }
                else if (sortOrder == SortOrder.Ascending)
                {
                    sortOrderModifier = 1;
                }
                Double32Comparer.sortColumnIndex = sortColumnIndex;
            }

            public int Compare(object x, object y)
            {
                DataGridViewRow DataGridViewRow1 = (DataGridViewRow)x;
                DataGridViewRow DataGridViewRow2 = (DataGridViewRow)y;

                // Try to sort based on the Last Name column.
                string sx = DataGridViewRow1.Cells[sortColumnIndex].Value != null ? DataGridViewRow1.Cells[sortColumnIndex].Value.ToString().Replace(",", "") : "";
                string sy = DataGridViewRow2.Cells[sortColumnIndex].Value != null ? DataGridViewRow2.Cells[sortColumnIndex].Value.ToString().Replace(",", "") : "";
                double vx, vy;

                int CompareResult = 0;

                if (double.TryParse(sx, out vx))
                {
                    if (double.TryParse(sy, out vy))
                    {
                        if (vx < vy) CompareResult = -1;
                        else if (vx > vy) CompareResult = 1;
                    }
                    else
                    {
                        if (sy.Length <= 0) CompareResult = 1;
                        else CompareResult = -1;
                    }
                }
                else if (double.TryParse(sy, out vy))
                {
                    if (sx.Length <= 0) CompareResult = -1;
                    else CompareResult = 1;
                }
                else
                {
                    CompareResult = System.String.Compare(sx, sy);
                }

                if (CompareResult == 0)
                    CompareResult = String.Compare(DataGridViewRow1.Cells[0].Value.ToString(), DataGridViewRow2.Cells[0].Value.ToString());

                return CompareResult * sortOrderModifier;
            }
        }

        #endregion

        private void checkBoxSoundAlert_CheckedChanged(object sender, EventArgs e)
        {
            checkBoxSoundAlert.BackColor = Color.FromKnownColor(KnownColor.Control);
        }

    }
}
