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
    public partial class MessageBoxForm : BaseForm
    {
        MessageBoxButtons buttons = MessageBoxButtons.OK;
        MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1;
        MessageBoxIcon messageIcon = MessageBoxIcon.None;
        Button btn1 = null, btn2 = null, btn3 = null;

        public MessageBoxForm() : this(null,null)
        {
        }

        public MessageBoxForm(CultureInfo Culture, string PersistString) : base(Culture, PersistString)
        {
            LayoutLockable = false;

            InitializeComponent();
            ShowButtons(buttons);
        }

        public string Message
        {
            get
            {
                return labelMessage.Text;
            }

            set
            {
                labelMessage.Text = value;
            }
        }

        public MessageBoxButtons Buttons
        {
            get
            {
                return buttons;
            }

            set
            {
                if (buttons != value)
                {
                    buttons = value;
                    ShowButtons(buttons);
                }
                DefaultButton = defaultButton;
            }
        }

        public MessageBoxDefaultButton DefaultButton
        {
            get
            {
                return defaultButton;
            }

            set
            {
                defaultButton = value;
                switch (defaultButton)
                {
                    case MessageBoxDefaultButton.Button1:
                        if (btn1 != null) { btn1.Select(); }
                        break;

                    case MessageBoxDefaultButton.Button2:
                        if (btn2 != null) { btn2.Select(); }
                        else if (btn1 != null) { btn1.Select(); }
                        break;

                    case MessageBoxDefaultButton.Button3:
                        if (btn3 != null) { btn3.Select(); }
                        else if (btn2 != null) { btn2.Select(); }
                        else if (btn1 != null) { btn1.Select(); }
                        break;

                    default:
                        if (btn1 != null) { btn1.Select(); }
                        break;
                }
            }
        }

        public MessageBoxIcon MessageIcon
        {
            get
            {
                return messageIcon;
            }

            set
            {
                if (messageIcon != value)
                {
                    messageIcon = value;

                    switch (messageIcon)
                    {
                        case MessageBoxIcon.Error:
                            pictureBoxIcon.Image = GetResxObject("ImageError") as Image;
                            break;

                        case MessageBoxIcon.Exclamation:
                            pictureBoxIcon.Image = GetResxObject("ImageExclamation") as Image;
                            break;

                        case MessageBoxIcon.Information:
                            pictureBoxIcon.Image = GetResxObject("ImageInformation") as Image;
                            break;

                        case MessageBoxIcon.Question:
                            pictureBoxIcon.Image = GetResxObject("ImageQuestion") as Image;
                            break;

                        default:
                            pictureBoxIcon.Image = null;
                            break;
                    }
                }
            }
        }

        private void Btn_Click(object sender, EventArgs e)
        {
            this.DialogResult = (sender as Button).DialogResult;
            this.Close();
        }

        private void MessageBoxForm_Resize(object sender, EventArgs e)
        {
            OnResize();
        }

        private void MessageBoxForm_Shown(object sender, EventArgs e)
        {
            OnResize();
        }

        private void OnResize()
        {
            labelMessage.Width = this.ClientSize.Width - labelMessage.Left;
            labelMessage.Height = this.ClientSize.Height - panelButtons.Height;
            int left = 91;
            if (btn3 != null) { btn3.Left = panelButtons.ClientSize.Width - left; left += 91; }
            if (btn2 != null) { btn2.Left = panelButtons.ClientSize.Width - left; left += 91; }
            if (btn1 != null) { btn1.Left = panelButtons.ClientSize.Width - left; left += 91; }
        }

        private void ShowButtons(MessageBoxButtons buttons)
        {
            switch (buttons)
            {
                case MessageBoxButtons.AbortRetryIgnore:
                    if (btn1 == null) btn1 = new Button();
                    btn1.Name = "BtnAbort";
                    btn1.DialogResult = DialogResult.Abort;
                    btn1.Text = GetResxString(btn1.Name);
                    btn1.Location = new Point(this.panelButtons.ClientSize.Width - 273, 9);
                    btn1.Size = new Size(75, 23);
                    btn1.Click += new EventHandler(Btn_Click);
                    this.panelButtons.Controls.Add(btn1);
                    if (btn2 == null) btn2 = new Button();
                    btn2.Name = "BtnRetry";
                    btn2.DialogResult = DialogResult.Retry;
                    btn2.Text = GetResxString(btn2.Name);
                    btn2.Location = new Point(this.panelButtons.ClientSize.Width - 182, 9);
                    btn2.Size = new Size(75, 23);
                    btn2.Click += new EventHandler(Btn_Click);
                    this.panelButtons.Controls.Add(btn2);
                    if (btn3 == null) btn3 = new Button();
                    btn3.Name = "BtnIgnore";
                    btn3.DialogResult = DialogResult.Ignore;
                    btn3.Text = GetResxString(btn3.Name);
                    btn3.Location = new Point(this.panelButtons.ClientSize.Width - 91, 9);
                    btn3.Size = new Size(75, 23);
                    btn3.Click += new EventHandler(Btn_Click);
                    this.panelButtons.Controls.Add(btn3);
                    break;

                case MessageBoxButtons.OK:
                    if (btn1 == null) btn1 = new Button();
                    btn1.Name = "BtnOK";
                    btn1.DialogResult = DialogResult.OK;
                    btn1.Text = GetResxString(btn1.Name);
                    btn1.Location = new Point(this.panelButtons.ClientSize.Width - 91, 9);
                    btn1.Size = new Size(75, 23);
                    btn1.Click += new EventHandler(Btn_Click);
                    this.panelButtons.Controls.Add(btn1);
                    if (btn2 != null)
                    {
                        this.panelButtons.Controls.Remove(btn2);
                        btn2.Dispose();
                        btn2 = null;
                    }
                    if (btn3 != null)
                    {
                        this.panelButtons.Controls.Remove(btn3);
                        btn3.Dispose();
                        btn3 = null;
                    }
                    break;

                case MessageBoxButtons.OKCancel:
                    if (btn1 == null) btn1 = new Button();
                    btn1.Name = "BtnOK";
                    btn1.DialogResult = DialogResult.OK;
                    btn1.Text = GetResxString(btn1.Name);
                    btn1.Location = new Point(this.panelButtons.ClientSize.Width - 182, 9);
                    btn1.Size = new Size(75, 23);
                    btn1.Click += new EventHandler(Btn_Click);
                    this.panelButtons.Controls.Add(btn1);
                    if (btn2 == null) btn2 = new Button();
                    btn2.Name = "BtnCancel";
                    btn2.DialogResult = DialogResult.Cancel;
                    btn2.Text = GetResxString(btn2.Name);
                    btn2.Location = new Point(this.panelButtons.ClientSize.Width - 91, 9);
                    btn2.Size = new Size(75, 23);
                    btn2.Click += new EventHandler(Btn_Click);
                    this.panelButtons.Controls.Add(btn2);
                    if (btn3 != null)
                    {
                        this.panelButtons.Controls.Remove(btn3);
                        btn3.Dispose();
                        btn3 = null;
                    }
                    break;

                case MessageBoxButtons.RetryCancel:
                    if (btn1 == null) btn1 = new Button();
                    btn1.Name = "BtnRetry";
                    btn1.DialogResult = DialogResult.Retry;
                    btn1.Text = GetResxString(btn1.Name);
                    btn1.Location = new Point(this.panelButtons.ClientSize.Width - 182, 9);
                    btn1.Size = new Size(75, 23);
                    btn1.Click += new EventHandler(Btn_Click);
                    this.panelButtons.Controls.Add(btn1);
                    if (btn2 == null) btn2 = new Button();
                    btn2.Name = "BtnCancel";
                    btn2.DialogResult = DialogResult.Cancel;
                    btn2.Text = GetResxString(btn2.Name);
                    btn2.Location = new Point(this.panelButtons.ClientSize.Width - 91, 9);
                    btn2.Size = new Size(75, 23);
                    btn2.Click += new EventHandler(Btn_Click);
                    this.panelButtons.Controls.Add(btn2);
                    if (btn3 != null)
                    {
                        this.panelButtons.Controls.Remove(btn3);
                        btn3.Dispose();
                        btn3 = null;
                    }
                    break;

                case MessageBoxButtons.YesNo:
                    if (btn1 == null) btn1 = new Button();
                    btn1.Name = "BtnYes";
                    btn1.DialogResult = DialogResult.Yes;
                    btn1.Text = GetResxString(btn1.Name);
                    btn1.Location = new Point(this.panelButtons.ClientSize.Width - 182, 9);
                    btn1.Size = new Size(75, 23);
                    btn1.Click += new EventHandler(Btn_Click);
                    this.panelButtons.Controls.Add(btn1);
                    if (btn2 == null) btn2 = new Button();
                    btn2.Name = "BtnNo";
                    btn2.DialogResult = DialogResult.No;
                    btn2.Text = GetResxString(btn2.Name);
                    btn2.Location = new Point(this.panelButtons.ClientSize.Width - 91, 9);
                    btn2.Size = new Size(75, 23);
                    btn2.Click += new EventHandler(Btn_Click);
                    this.panelButtons.Controls.Add(btn2);
                    if (btn3 != null)
                    {
                        this.panelButtons.Controls.Remove(btn3);
                        btn3.Dispose();
                        btn3 = null;
                    }
                    break;

                case MessageBoxButtons.YesNoCancel:
                    if (btn1 == null) btn1 = new Button();
                    btn1.Name = "BtnYes";
                    btn1.DialogResult = DialogResult.Yes;
                    btn1.Text = GetResxString(btn1.Name);
                    btn1.Location = new Point(this.panelButtons.ClientSize.Width - 273, 9);
                    btn1.Size = new Size(75, 23);
                    btn1.Click += new EventHandler(Btn_Click);
                    this.panelButtons.Controls.Add(btn1);
                    if (btn2 == null) btn2 = new Button();
                    btn2.Name = "BtnNo";
                    btn2.DialogResult = DialogResult.No;
                    btn2.Text = GetResxString(btn2.Name);
                    btn2.Location = new Point(this.panelButtons.ClientSize.Width - 182, 9);
                    btn2.Size = new Size(75, 23);
                    btn2.Click += new EventHandler(Btn_Click);
                    this.panelButtons.Controls.Add(btn2);
                    if (btn3 == null) btn3 = new Button();
                    btn3.Name = "BtnCancel";
                    btn3.DialogResult = DialogResult.Cancel;
                    btn3.Text = GetResxString(btn3.Name);
                    btn3.Location = new Point(this.panelButtons.ClientSize.Width - 91, 9);
                    btn3.Size = new Size(75, 23);
                    btn3.Click += new EventHandler(Btn_Click);
                    this.panelButtons.Controls.Add(btn3);
                    break;
            }
        }
    }
}
