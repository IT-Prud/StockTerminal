using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using Utils;

namespace StockTerminal.Forms
{
    public partial class ReconnectForm : StockTerminal.Forms.BaseForm
    {
        private int TimeLastShown = -10000;

        public ReconnectForm() : this(null, null)
        {
        }

        public ReconnectForm(CultureInfo Culture, string PersistString) : base(Culture, PersistString)
        {
            LayoutLockable = false;

            InitializeComponent();

            ListenConnectionStatus();
        }

        protected override void OnConnectionStatus(int State)
        {
            ShowStatus(State);
        }

        private void ShowStatus(int State)
        {
            switch (State)
            {
                case (int)global::TradeDB.ProcessState.Stopped:
                    pictureBoxIcon.Image = GetResxObject("ImageError") as Image;
                    labelMessage.Text = GetResxString("Reconnect_Message_Relogin");
                    labelStatus.Text = GetResxString("Reconnect_Status_FailedToConnectServer");
                    AppendLog("Reconnect", "Failed to connect server.", "Reconnect", false);
                    RefreshButtonLogoutState(true);
                    SetProgressBarEnable(false);
                    break;

                case (int)global::TradeDB.ProcessState.Login:
                    pictureBoxIcon.Image = GetResxObject("ImageExclamation") as Image;
                    labelMessage.Text = GetResxString("Reconnect_Message_Reconnecting");
                    labelStatus.Text = GetResxString("Reconnect_Status_Login");
                    SetProgressBarEnable(true);
                    AppendLog("Reconnect", "Login in progress...", "Reconnect", false);
                    break;

                case (int)global::TradeDB.ProcessState.Connecting:
                    pictureBoxIcon.Image = GetResxObject("ImageExclamation") as Image;
                    labelMessage.Text = GetResxString("Reconnect_Message_Reconnecting");
                    labelStatus.Text = GetResxString("Reconnect_Status_Connecting");
                    SetProgressBarEnable(true);
                    AppendLog("Reconnect", "Connecting...", "Reconnect", false);
                    break;

                case (int)global::TradeDB.ProcessState.Ready:
                    pictureBoxIcon.Image = null;
                    labelMessage.Text = GetResxString("Reconnect_Message_Reconnected");
                    labelStatus.Text = GetResxString("Reconnect_Status_Ready");
                    SetProgressBarEnable(false);
                    AppendLog("Reconnect", "Login Success", "Reconnect", false);
                    break;

                default:
                    pictureBoxIcon.Image = null;
                    labelMessage.Text = "";
                    labelStatus.Text = "";
                    SetProgressBarEnable(false);
                    break;
            }
        }

        private void SetProgressBarEnable(bool Enabled)
        {
            if (Enabled)
            {
                if (!timerProgressReconnect.Enabled)
                {
                    progressBarReconnect.Value = progressBarReconnect.Minimum;
                    progressBarReconnect.Visible = true;
                    timerProgressReconnect.Enabled = true;
                }
            }
            else
            {
                timerProgressReconnect.Enabled = false;
                progressBarReconnect.Visible = false;
                progressBarReconnect.Value = progressBarReconnect.Minimum;
            }
        }

        private void RefreshButtonLogoutState(bool ForceShowButton)
        {
            buttonLogout.Visible = (ForceShowButton || 
                (ProcessTimer.TickCount - TimeLastShown) >= 15000) ? true : false;
        }

        private void buttonLogout_Click(object sender, EventArgs e)
        {
            Program.MainFormInstance.RequestToLogout();
        }

        private void ReconnectForm_Shown(object sender, EventArgs e)
        {
            TimeLastShown = ProcessTimer.TickCount;

            RefreshButtonLogoutState(false);

            if (TradeDB != null)
            {
                ShowStatus(TradeDB.TradeState);
            }
        }

        private void ReconnectForm_Leave(object sender, EventArgs e)
        {
            this.Focus();
        }

        private void ReconnectForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
            }
        }

        private void timerProgressReconnect_Tick(object sender, EventArgs e)
        {
            RefreshButtonLogoutState(false);

            timerProgressReconnect.Enabled = false;
            if (progressBarReconnect.Value >= progressBarReconnect.Maximum)
            {
                progressBarReconnect.Value = progressBarReconnect.Minimum;
            }
            else
            {
                int value = progressBarReconnect.Value;
                if (value < progressBarReconnect.Minimum) value = progressBarReconnect.Minimum;
                value += ((progressBarReconnect.Maximum - progressBarReconnect.Minimum) >> 5);
                if (value < progressBarReconnect.Minimum)
                {
                    value = progressBarReconnect.Minimum;
                }
                else if (value > progressBarReconnect.Maximum)
                {
                    value = progressBarReconnect.Maximum;
                }
                progressBarReconnect.Value = value;
            }
            timerProgressReconnect.Enabled = true;
        }
    }
}
