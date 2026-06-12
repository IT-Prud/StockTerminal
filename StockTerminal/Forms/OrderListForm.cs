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
    public partial class OrderListForm : BaseForm 
    {
        public OrderListForm() : this (null,null)
        {
            //InitializeComponent();
        }

        public OrderListForm(CultureInfo Culture, string PersistString) : base(Culture, PersistString)
        {
            InitializeComponent();
        }

        private void OrderListForm_Load(object sender, EventArgs e)
        {

        }
    }

}
