using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace StockTerminal.Utils
{
    public class MyTextBox : TextBox
    {
        MouseEventHandler mouseClickHandler;
        EventHandler textEnterHandler;
        EventHandler textLeaveHandler;
        [System.ComponentModel.Browsable(false)]
        private bool hasFocus = false;


        public MyTextBox()
        {
            mouseClickHandler = new MouseEventHandler(OnMouseClick);
            this.MouseClick += mouseClickHandler;
            textEnterHandler = new EventHandler(OnEnter);
            this.Enter += textEnterHandler;
            textLeaveHandler = new EventHandler(OnTextLeave);
            this.Leave += textLeaveHandler;
        }

        private void OnMouseClick(object sender, MouseEventArgs e)
        {
            if (this.hasFocus == false)
            {
                (sender as TextBox).SelectAll();
                this.hasFocus = true;
            }
        }

        private void OnEnter(object sender, EventArgs e)
        {
            (sender as TextBox).SelectAll();
        }

        private void OnTextLeave(object sender, EventArgs e)
        {
            this.hasFocus = false;
        }
    }
}
