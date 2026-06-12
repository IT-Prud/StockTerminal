using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace StockTerminal.Utils
{
    class DataGridViewDisableCheckBoxColumn : DataGridViewCheckBoxColumn
    {
        public DataGridViewDisableCheckBoxColumn()
        {
            this.CellTemplate = new DataGridViewDisableCheckBoxCell();
        }
    }
}
