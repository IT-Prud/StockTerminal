using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Globalization;
using WeifenLuo.WinFormsUI.Docking;
using System.IO;

namespace StockTerminal.Forms
{
    public partial class GrayStockQuoteForm : StockTerminal.Forms.BaseForm
    {
        private string host = SettingsTradeDB["LoginHost01"], port = SettingsTradeDB["LoginPort01"], destWeb = null, destHost = null;

        public GrayStockQuoteForm()
        {
            InitializeComponent();
        }

        public GrayStockQuoteForm(CultureInfo Culture, string PersistString, DockPanel dockPanelMain)
            : base(Culture, PersistString)
        {
            InitializeComponent();

            //this.dockPanelMain = dockPanelMain;
        }

        private void GrayStockQuoteForm_Load(object sender, EventArgs e)
        {
            panelTop.BackColor = Color.FromArgb(255, 214, 124);
            labelStkCode.BackColor = Color.FromArgb(255, 214, 124);
            labelStkCodeTarget.BackColor = Color.FromArgb(255, 214, 124);
            this.BackColor = Color.FromArgb(255, 255, 200);

            int iPort;
            int.TryParse(port, out iPort);

            if (host == null || host.Trim() == "") // for the case if no ini file
                destHost = "www.pru.hk";
            else
                destHost = host;

            if (iPort == 80)
                destHost = "http://" + destHost;
            else // if (port == 443)
                destHost = "https://" + destHost;
            destWeb = destHost + "/InternetStock/Quote/Gray/List.asp";

            if (Culture != null)
            {
                switch (Culture.Name)
                {
                    case "zh-CHS":
                        destWeb += "?LangID=SC";
                        break;
                    case "zh-CHT":
                        destWeb += "?LangID=TC";
                        break;
                    default:
                        break;
                }
            }

            GetGrayFileList(true);
            ReloadImage();
            timerRefresh.Start();
        }

        private void GetGrayFileList(bool OnLoad)
        {
            WebRequest request = WebRequest.Create(destWeb);
            WebResponse response = null;
            Stream stream = null;
            byte[] buffer = new byte[1024];
            int bufferTail = 0;
            int readCount = 0;
            List<int> GrayStockList = new List<int>();

            try
            {
                using (response = request.GetResponse())
                using (stream = response.GetResponseStream())
                {
                    do
                    {
                        readCount = stream.Read(buffer, bufferTail, buffer.Length - bufferTail);
                        bufferTail += readCount;
                    } while (readCount > 0 && bufferTail < buffer.Length);
                }
            }
            catch
            {
            }
            finally
            {
                if (response != null) response.Close();
                if (stream != null) stream.Close();
            }
            StockCodeComboBox.Items.Clear();
            string result = Encoding.ASCII.GetString(buffer, 0, bufferTail);
            if (result == null || result.Trim().Length <= 0)
                return;
            result = result.Replace("\r\n", "[");
            string[] fields = result.Split('[');
            int i, n;
            for (i = 0, n = fields.Length; i < n; i++)
            {
                int fieldValue = -1;
                if (int.TryParse(fields[i], out fieldValue) && fieldValue > 0)
                {
                    GrayStockList.Add(fieldValue);
                    StockCodeComboBox.Items.Add(fieldValue);
                    if (OnLoad)
                        labelStkCodeTarget.Text = fieldValue + "";
                }
            }
        }

        private void ReloadImage()
        {
            string stockCodeText = labelStkCodeTarget.Text.Trim();
            int stockCode = 0;
            if (stockCodeText == null || stockCodeText.Length <= 0 || !int.TryParse(stockCodeText, out stockCode) || stockCode <= 0)
                return;

            WebRequest request = WebRequest.Create(destHost + "/InternetStock/Quote/Gray/" + stockCode + ".png");
            WebResponse response = null;
            Stream stream = null;
            try
            {
                using (response = request.GetResponse())
                using (stream = response.GetResponseStream())
                {
                    pictureBox1.Image = Bitmap.FromStream(stream);
                    //System.Drawing.Size size = new Size(pictureBox1.Image.Size.Width + 17, pictureBox1.Image.Size.Height + 39);
                    //this.FormBorderStyle = FormBorderStyle.Sizable;
                    //this.Size = size;
                    //DockState = DockState.Float;
                    //if (IsFloat)
                    //    FloatAt ( new Rectangle(100,100, pictureBox1.Image.Size.Width, pictureBox1.Image.Size.Height));
                    //this.Refresh();
                    pictureBox1.Show();
                }
            }
            catch
            {
                pictureBox1.Hide();
            }
            finally
            {
                if (response != null) response.Close();
                if (stream != null) stream.Close();
            }
        }

        private int TickCounter = 0;
        private void timerRefresh_Tick(object sender, EventArgs e)
        {
            TickCounter += timerRefresh.Interval;
            if (TickCounter >= 20000)
            {
                TickCounter = 0;
                GetGrayFileList(false);
            }
            ReloadImage();
        }

        private void StockCodeComboBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;

            string stockCodeText = StockCodeComboBox.Text.Trim();
            int stockCode = 0;
            StockCodeComboBox.Text = "";
            if (stockCodeText == null || stockCodeText.Length <= 0 || !int.TryParse(stockCodeText, out stockCode) || stockCode <= 0)
                return;

            labelStkCodeTarget.Text = stockCodeText;
            ReloadImage();
        }

        private void GrayStockQuoteForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            timerRefresh.Stop();
        }

        private void StockCodeComboBox_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (StockCodeComboBox.SelectedItem != null)
            {
                labelStkCodeTarget.Text = StockCodeComboBox.SelectedItem.ToString();
                StockCodeComboBox.Text = "";
                ReloadImage();
            }

        }
    }
}
