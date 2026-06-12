using System;
using System.Collections.Generic;
using System.Text;

namespace StockTerminal.Utils
{
    class ComboBoxItem
    {
        public string Name;
        public string Value;
        public ComboBoxItem(string Name, string Value)
        {
            this.Name = Name;
            this.Value = Value;
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
