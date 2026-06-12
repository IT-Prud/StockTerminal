using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace StockTerminal.Forms
{
    public partial class InstructionForm : StockTerminal.Forms.BaseForm
    {
        public InstructionForm() : this(null, null)
        {
        }

        public InstructionForm(CultureInfo Culture, string PersistString) : base(Culture, PersistString)
        {
            InitializeComponent();
        }

        private void InstructionForm_Load(object sender, EventArgs e)
        {

        }

    }
}
