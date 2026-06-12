using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Globalization;

namespace StockTerminal.Forms
{
    public partial class PresetAcc : StockTerminal.Forms.BaseForm
    {
        public PresetAcc() : this(null,null)
        {
            InitializeComponent();
        }

        public PresetAcc(CultureInfo Culture, string PersistString)
            : base(Culture, PersistString)
        {
            InitializeComponent();

        }

        private void dataGridViewPresetAcc_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
