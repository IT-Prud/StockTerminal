using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using TradeDB;
using System.Diagnostics;

namespace StockTerminal.Forms
{
    public partial class AmendForm : BaseForm 
    {
        private string orderNumber = "";
        public string OrderNumber
        {
            get { return orderNumber; }

            set
            {
                orderNumber = value;
                if (orderNumber != null && orderNumber.Trim().Length > 0)
                {
                    List<Order> Orders = null;
                    int OrdNo;
                    if (int.TryParse(OrderNumber, out OrdNo) && OrdNo > 0)
                    {
                        Orders = TradeDB.GetOrderByOrderNo(new List<int> { OrdNo });
                        if (Orders != null && Orders[0] != null)
                            ExchangeType = Orders[0].ExType;
                    }
                }
            }
        }
        private ExchangeTypeEnum ExchangeType = ExchangeTypeEnum.HKG;
        public string BuySell = "", StockCode = "", OrderPrice = "", OrginalOrderQty = "", OrderQty = "";
        public string StockName = "", FilledQty = "", LastAction = "", Accno = "", OTP = "";
        public Order.OrderStatusEnum OrderStatus;
        protected decimal Spread = 1;
        protected Int64 lotSize = 100;
        private bool nonNumberEntered = false;
        private Color defaulfBtnCol;
        public bool IsBuyOrder;
        public bool IsAuctionOrder;
        private Stock CurrStock = null;
        private Account CurrAccount = null;
        public List<string> AccountList1 = new List<string>(20);
        private object formLoadMutex = new object();
        public ExchangeTypeEnum FormExType = ExchangeTypeEnum.HKG;

        private string pListenedAccount = null;
        public string ListenedAccount
        {
            get { return pListenedAccount; }

            set
            {
                string pValue = value;
                if (pValue != null) pValue = pValue.Trim().ToUpper();

                if (pListenedAccount != pValue)
                {
                    if (pListenedAccount != null) UnListenAccount(new List<string> { pListenedAccount });
                    pListenedAccount = value;
                    CurrAccount = null;

                    if (pListenedAccount != null && pListenedAccount.Length > 0)
                    {
                        ListenAccount(new List<string> { pListenedAccount });
                    }
                }
            }
        }

        public AmendForm() : this(null, null)
        {
            //InitializeComponent();
        }

        public AmendForm(CultureInfo Culture, string PersistString) : base(Culture, PersistString)
        {
            lock (formLoadMutex)
            {
                InitializeComponent();
                LayoutLockable = false;
                defaulfBtnCol = buttonGo.BackColor;
            }
        }

        private void AmendForm_Load(object sender, EventArgs e)
        {
            lock (formLoadMutex)
            {
                Redraw();
                //ListenAccountList();
                try
                {
                    ListenOrderStatus(new List<string> { OrderNumber });
                }
                catch (Exception ex)
                {
                    Debug.Print(DateTime.Now.ToString() + ": " + this.GetType().ToString() + ": " + Utils.Utils.GetFunctionName() + ": " + ex.Message);
                }
            }
        }

        protected override void OnCultureChange(CultureInfo ci)
        {
            Redraw();
        }

        public void Redraw()
        {
            SetCurrentStockCode(StockCode);
            SetCurrentOrderNo(OrderNumber);
            ListenedAccount = Accno;
            labelAccountNo.Text = Accno;
            labelOrderNum.Text = OrderNumber;
            if (IsBuyOrder)
                labelBuySell.Text = GetResxString("Buy");
            else
                labelBuySell.Text = GetResxString("Sell");
            labelExchange.Text = Stock.GetExchangeCode(FormExType);
            try { labelStockCode.Text = Convert.ToInt32(StockCode).ToString(); }
            catch { }
            labelStockName.Text = StockName;
            labelFilledQty.Text = FilledQty;
            textBoxNewStkPrice.Text = OrderPrice;
            labelStockPrice.Text = OrderPrice;
            textBoxNewStkQty.Text = OrderQty;
            labelStockQty.Text = OrderQty;
            calAmount();
            int iFilledQty, iOrderQty;
            int.TryParse(FilledQty, out iFilledQty);
            int.TryParse(OrderQty, out iOrderQty);
            if (OrderStatus != Order.OrderStatusEnum.Pending && OrderStatus != Order.OrderStatusEnum.Queue || LastAction.Trim().Length != 0 ||
                ((ExchangeType == ExchangeTypeEnum.SHG || ExchangeType == ExchangeTypeEnum.SZE) && iFilledQty > 0 && IsBuyOrder))
            {
                if ((ExchangeType == ExchangeTypeEnum.SHG || ExchangeType == ExchangeTypeEnum.SZE) && iFilledQty > 0 && iFilledQty < iOrderQty && IsBuyOrder)
                {
                    buttonGo.Visible = false;
                    labelPartialFilled.Visible = true;
                }
                else
                {
                    buttonGo.Visible = true;
                    labelPartialFilled.Visible = false;
                }
                textBoxNewStkPrice.Enabled = false;
                textBoxNewStkQty.Enabled = false;
                buttonGo.Enabled = false;
                buttonMax.Enabled = false;
                vScrollBarPrice.Enabled = false;
                vScrollBarQty.Enabled = false;
                buttonGo.BackColor = defaulfBtnCol;
                buttonMax.BackColor = defaulfBtnCol;
            }
            else
            {
                buttonGo.Visible = true;
                labelPartialFilled.Visible = false;
                textBoxNewStkPrice.Enabled = true;
                textBoxNewStkQty.Enabled = true;
                buttonGo.Enabled = true;
                buttonMax.Enabled = true;
                vScrollBarPrice.Enabled = true;
                vScrollBarQty.Enabled = true;
                if (IsBuyOrder)
                {
                    buttonGo.BackColor = Color.SkyBlue;
                    buttonMax.BackColor = Color.SkyBlue;
                }
                else
                {
                    buttonGo.BackColor = Color.LightPink;
                    buttonMax.BackColor = Color.LightPink;
                }
            }
            if (IsAuctionOrder)
            {
                textBoxNewStkPrice.Enabled = false;
                vScrollBarPrice.Enabled = false;
            }
            if (IsBuyOrder)
            {
                labelBuySell.BackColor = Color.SkyBlue;
                labelStockCode.BackColor = Color.SkyBlue;
                labelExchange.BackColor = Color.SkyBlue;
                labelStockName.BackColor = Color.SkyBlue;
                textBoxNewStkPrice.BackColor = Color.SkyBlue;
                textBoxNewStkQty.BackColor = Color.SkyBlue;
            }
            else
            {
                labelBuySell.BackColor = Color.LightPink;
                labelStockCode.BackColor = Color.LightPink;
                labelStockName.BackColor = Color.LightPink;
                textBoxNewStkPrice.BackColor = Color.LightPink;
                textBoxNewStkQty.BackColor = Color.LightPink;
            }
        }

        private void SetCurrentStockCode(string NewStockCode)
        {
            NewStockCode = NewStockCode == null ? "" : NewStockCode.Trim();
            if (NewStockCode.Trim() == "") return;
            if (CurrStock != null) UnListenStock(new List<string> { CurrStock.StockSignature });
            CurrStock = null;                
                        
            try
            {
                //ListenStock(new List<string> { Stock.GetSignature(ExchangeType, NewStockCode) });
                GetStock(new List<string> { Stock.GetSignature(ExchangeType, NewStockCode) });
            }
            catch
            { }
        }

        private void SetCurrentOrderNo(string NewOrderNo)
        {
            NewOrderNo = NewOrderNo == null ? "" : NewOrderNo.Trim();
            if (NewOrderNo.Trim() == "" || labelOrderNum.Text.Trim() == NewOrderNo) return;
            UnListenOrderStatus(new List<string> { labelOrderNum.Text.Trim() });

            try
            {
                ListenOrderStatus(new List<string> { NewOrderNo });
            }
            catch
            { }
        }

        protected override void OnOrderStatus(Order TheOrder)
        {
            if (TheOrder != null && labelOrderNum.Text.Trim() == TheOrder.OrderNo.ToString())
            {
                OrderPrice = TheOrder.Price.ToString();
                OrginalOrderQty = OrderQty = TheOrder.Quantity.ToString();
                FilledQty = TheOrder.Filled.ToString();
                LastAction = TheOrder.LastAction;
                Accno = TheOrder.AccountNo.Trim();
                OrderStatus = (Order.OrderStatusEnum)TheOrder.Status;
                Redraw();
            }
        }

        private void numUpDownNewStkPrice_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar.ToString() == "-")
                numUpDownNewStkPrice.DownButton();
            if (e.KeyChar.ToString() == "+")
                numUpDownNewStkPrice.UpButton();
            e.Handled = true; 
        }

        private void numUpDownNewStockQty_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar.ToString() == "-")
                numUpDownNewStockQty.DownButton();
            if (e.KeyChar.ToString() == "+")
                numUpDownNewStockQty.UpButton();
            e.Handled = true;
        }

        private void textBoxNewStkPrice_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                focusNextControl(sender, e);
                return;
            }

            if ((e.KeyChar.ToString() != "-" && e.KeyChar.ToString() != "+" && nonNumberEntered && e.KeyChar.ToString() != ".") ||
                (textBoxNewStkPrice.Text.IndexOf(".") >= 0 && e.KeyChar.ToString() == "."))
            {
                e.Handled = true;
                return;
            }

            if (e.KeyChar.ToString() == "-" || e.KeyChar.ToString() == "+")
            {
                if (textBoxNewStkPrice.Text != "" && textBoxNewStkPrice.Text != ".")
                {
                    decimal stkPrice = Convert.ToDecimal(textBoxNewStkPrice.Text.Replace(",", ""));
                    bool CanBuy = false, CanSell = false;
                    char OrderSide = ' ';
                    if (IsBuyOrder) OrderSide = 'B';
                    else OrderSide = 'S';
                    //if (BuySell.Trim() == GetResxString("Buy")) OrderSide = 'B';
                    //else if (BuySell.Trim() == GetResxString("Sell")) OrderSide = 'S';
                    //if (BuySell.ToUpper().Trim() == "BUY") OrderSide = 'B';
                    //else if (BuySell.ToUpper().Trim() == "SELL") OrderSide = 'S';
                    if (FormExType == ExchangeTypeEnum.HKG)
                    {
                        if (e.KeyChar.ToString() == "-")
                            stkPrice = Utils.OrderTypeSpread.CheckSpread(stkPrice, 'L', OrderSide, -1, CurrStock, TradeDB, out CanBuy, out CanSell);
                        else if (e.KeyChar.ToString() == "+")
                            stkPrice = Utils.OrderTypeSpread.CheckSpread(stkPrice, 'L', OrderSide, 1, CurrStock, TradeDB, out CanBuy, out CanSell);
                    }
                    else if (FormExType == ExchangeTypeEnum.SHG || FormExType == ExchangeTypeEnum.SZE)
                    {
                        if (e.KeyChar.ToString() == "-")
                            stkPrice = Utils.OrderTypeSpread.CheckSpread_ASHR(stkPrice, 'X', OrderSide, -1, CurrStock, TradeDB, out CanBuy, out CanSell);
                        else if (e.KeyChar.ToString() == "+")
                            stkPrice = Utils.OrderTypeSpread.CheckSpread_ASHR(stkPrice, 'X', OrderSide, 1, CurrStock, TradeDB, out CanBuy, out CanSell);
                    }

                    //EnableDisableBuySellBtn(CanBuy, CanSell);
                    if (stkPrice > 0) textBoxNewStkPrice.Text = String.Format("{0:N3}", stkPrice);
                    textBoxNewStkPrice.SelectionStart = 0;
                    textBoxNewStkPrice.SelectionLength = textBoxNewStkPrice.Text.Length;
                }
                e.Handled = true;
                return;
            }
        }

        private void focusNextControl(object sender, KeyPressEventArgs e)
        {
            Control currCtl = (Control)sender; //current control
            e.Handled = true;
            Control c = GetNextControl(currCtl, true);
            if (c != null)
                c.Focus();
        }

        private void textBoxNewStkPriceQty_KeyDown(object sender, KeyEventArgs e)
        {
            nonNumberEntered = false;

            if (e.KeyCode < Keys.D0 || e.KeyCode > Keys.D9)
            {
                if (e.KeyCode < Keys.NumPad0 || e.KeyCode > Keys.NumPad9)
                {
                    if (e.KeyCode != Keys.Back)
                    {
                        nonNumberEntered = true;
                    }
                }
            }
            if (Control.ModifierKeys == Keys.Shift)
            {
                nonNumberEntered = true;
            }
        }

        private void textBoxNewStkQty_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter) // key press cannot capture arrow, page up/down
            {
                e.Handled = true;
                focusNextControl(sender, e);
                return;
            }

            if (e.KeyChar.ToString() != "-" && e.KeyChar.ToString() != "+" && nonNumberEntered)
            {
                e.Handled = true;
                return;
            }

            if (e.KeyChar.ToString() == "-" || e.KeyChar.ToString() == "+")
            {
                if (textBoxNewStkQty.Text != "")
                {
                    long stkQty = Convert.ToInt64(textBoxNewStkQty.Text.Replace(",", ""));
                    long stkLotSize = CurrStock.LotSize;                    
                    if (e.KeyChar.ToString() == "-" && stkQty - stkLotSize > 0)
                        stkQty -= stkLotSize;
                    else if (e.KeyChar.ToString() == "+")
                        stkQty += stkLotSize;
                    textBoxNewStkQty.Text = String.Format("{0:N0}", stkQty);
                }
                e.Handled = true;
                return;
            }
        }

        private void textBoxNewStockPriceQty_Leave(object sender, EventArgs e)
        {
            calAmount();
        }

        private string formatPrice(string textPrice)
        {
            try
            {
                if (textPrice != "" && textPrice != ".")
                    if (textPrice.IndexOf(".") >= 0)
                    {
                        string beforedec = textPrice.Substring(0, textPrice.IndexOf("."));
                        string afterdec = textPrice.Substring(textPrice.IndexOf("."));
                        if (afterdec.Length > 4)
                        {
                            textPrice = String.Format("{0:0}", beforedec) + afterdec.Substring(0, 4);
                        }
                        else
                        {
                            textPrice = String.Format("{0:0.000}", Convert.ToDecimal(textPrice.Replace(",", "")));
                        }
                    }
                return textPrice;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return "";
            }
        }



        private void calAmount()
        {
            try
            {
                string textPrice = textBoxNewStkPrice.Text.Trim();
                string textQty = textBoxNewStkQty.Text.Trim();


                if (textPrice != "" && textPrice != "." && textQty != "")
                    labelAmount.Text = String.Format("{0:N0}", Convert.ToDecimal(textPrice.Replace(",", "")) * Convert.ToInt64(textQty.Replace(",", "")));
                else
                    labelAmount.Text = "";

                textBoxNewStkPrice.Text = formatPrice(textPrice);
                if (textQty != "")
                    textBoxNewStkQty.Text = String.Format("{0:N0}", Convert.ToInt64(textQty.Replace(",", "")));
                //textBoxNewStkPrice.SelectAll();
                this.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        //private void calAmount()
        //{
        //    try
        //    {
        //        if (textBoxNewStkPrice.Text != "" && textBoxNewStkPrice.Text != ".")
        //            if (textBoxNewStkPrice.Text.IndexOf(".") >= 0)
        //                textBoxNewStkPrice.Text = String.Format("{0:0.000}", Convert.ToDecimal(textBoxNewStkPrice.Text.Replace(",", "")));
        //                //textBoxNewStkPrice.Text = String.Format("{0:N3}", Convert.ToDecimal(textBoxNewStkPrice.Text.Replace(",", "")));
        //            else
        //                textBoxNewStkPrice.Text = String.Format("{0:0.000}", Convert.ToDecimal(textBoxNewStkPrice.Text.Replace(",", "")));
        //                //textBoxNewStkPrice.Text = String.Format("{0:N0}", Convert.ToDecimal(textBoxNewStkPrice.Text.Replace(",", "")));
        //        if (textBoxNewStkQty.Text != "")
        //            textBoxNewStkQty.Text = String.Format("{0:N0}", Convert.ToInt64(textBoxNewStkQty.Text.Replace(",", "")));
        //        if (textBoxNewStkPrice.Text != "" && textBoxNewStkPrice.Text != "." && textBoxNewStkQty.Text != "")
        //            labelAmount.Text = String.Format("{0:N2}", Convert.ToDecimal(textBoxNewStkPrice.Text.Replace(",", "")) * Convert.ToInt64(textBoxNewStkQty.Text.Replace(",", "")));
        //        this.Refresh();
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString());
        //    }
        //}


        private void textBoxNewStkPrice_KeyUp(object sender, KeyEventArgs e)
        {
            int oldLen = textBoxNewStkPrice.SelectionStart;
            int oldSelStart = textBoxNewStkPrice.SelectionStart;
            bool hasCommaBefore = textBoxNewStkPrice.Text.IndexOf(",") >= 0 ? true : false;
            bool hasDotBefore = textBoxNewStkPrice.Text.IndexOf(".") == 0 ? true : false;
            calAmount();
            bool hasCommaAfter = textBoxNewStkPrice.Text.IndexOf(",") >= 0 ? true : false;
            bool hasDotAfter = textBoxNewStkPrice.Text.IndexOf("0.") == 0 ? true : false;
            if (!hasCommaBefore && hasCommaAfter)
                oldLen++;
            else if (hasCommaBefore && !hasCommaAfter)
                oldLen--;
            if (hasDotBefore && hasDotAfter)
                oldLen++;
            if (oldLen < 0)
                oldLen = 0;
            if (e.KeyValue == 110 || e.KeyValue == 190)  //.
            {
                string oldprice = textBoxNewStkPrice.Text.Trim();
                int dotpos = oldprice.IndexOf(".");
                if (oldSelStart == 0) // input "." at the pos 0
                {
                    textBoxNewStkPrice.Text = "0.000";
                    oldLen = 2;
                }
                else if (dotpos > 0 && dotpos == oldLen) // input "." same pos as old "."
                {
                    oldLen = dotpos + 1;
                    textBoxNewStkPrice.Text = String.Format("{0:0.000}", Convert.ToDecimal(textBoxNewStkPrice.Text.Replace(",", "")));
                }
                else if (dotpos > 0) // input "." not same pos as old "."
                {
                    string beforedec = textBoxNewStkPrice.Text.Substring(0, textBoxNewStkPrice.Text.IndexOf("."));
                    string afterdec = textBoxNewStkPrice.Text.Substring(textBoxNewStkPrice.Text.IndexOf("."));
                    textBoxNewStkPrice.Text = String.Format("{0:0}", beforedec) + ".000";
                    oldLen = textBoxNewStkPrice.Text.IndexOf(".") + 1;
                }
            }
            if (e.KeyValue == 46)  //Del
            {
                textBoxNewStkPrice.Text = "0.000";
                oldLen = 0;
                calAmount();
                textBoxNewStkPrice.SelectAll();
            }
            else if (e.KeyValue == 13)  //enter
            {
                textBoxNewStkPrice.SelectAll();
            }
            else
            {
                textBoxNewStkPrice.SelectionStart = oldLen;
            }
        }


        private void textBoxNewStkPrice_old2_KeyUp(object sender, KeyEventArgs e)
        {
            int oldLen = textBoxNewStkPrice.SelectionStart;
            bool hasCommaBefore = textBoxNewStkPrice.Text.IndexOf(",") >= 0 ? true : false;
            bool hasDotBefore = textBoxNewStkPrice.Text.IndexOf(".") == 0 ? true : false;
            calAmount();
            bool hasCommaAfter = textBoxNewStkPrice.Text.IndexOf(",") >= 0 ? true : false;
            bool hasDotAfter = textBoxNewStkPrice.Text.IndexOf("0.") == 0 ? true : false;
            if (!hasCommaBefore && hasCommaAfter)
                oldLen++;
            else if (hasCommaBefore && !hasCommaAfter)
                oldLen--;
            if (hasDotBefore && hasDotAfter)
                oldLen++;
            if (oldLen < 0)
                oldLen = 0;
            if (e.KeyValue == 110 || e.KeyValue == 190)  //.
            {
                string oldprice = textBoxNewStkPrice.Text.Trim();
                int dotpos = oldprice.IndexOf(".");
                if (dotpos > 0 && dotpos == oldLen) // input "." same pos as old "."
                    oldLen = dotpos + 1;
                else if (dotpos > 0 && dotpos > oldLen) // input "." not same pos as old "."
                {
                    oldprice = oldprice.Replace(".", "");
                    textBoxNewStkPrice.Text = oldprice.Insert(oldLen, ".");
                    oldLen = oldLen + 1;
                }
            }
            if (e.KeyValue == 46)  //Del
            {
                textBoxNewStkPrice.Text = "0.000";
                oldLen = 0;
                calAmount();
                textBoxNewStkPrice.SelectAll();
            }
            else
            {
                textBoxNewStkPrice.SelectionStart = oldLen;
            }
            //textBoxNewStkPrice.SelectionStart = oldLen;
        }



        private void textBoxNewStkPriceold_KeyUp(object sender, KeyEventArgs e)
        {
            int oldLen = textBoxNewStkPrice.SelectionStart;
            bool hasCommaBefore = textBoxNewStkPrice.Text.IndexOf(",") >= 0 ? true : false;
            bool hasDotBefore = textBoxNewStkPrice.Text.IndexOf(".") == 0 ? true : false;
            calAmount();
            bool hasCommaAfter = textBoxNewStkPrice.Text.IndexOf(",") >= 0 ? true : false;
            bool hasDotAfter = textBoxNewStkPrice.Text.IndexOf("0.") == 0 ? true : false;
            if (!hasCommaBefore && hasCommaAfter)
                oldLen++;
            else if (hasCommaBefore && !hasCommaAfter)
                oldLen--;
            if (hasDotBefore && hasDotAfter)
                oldLen++;
            if (oldLen < 0)
                oldLen = 0;
            if (e.KeyValue == 110 || e.KeyValue == 190)  //.
            {
                string oldprice = textBoxNewStkPrice.Text.Trim();
                int dotpos = oldprice.IndexOf(".");
                if (dotpos > 0 && dotpos == oldLen) // input "." same pos as old "."
                    oldLen = dotpos + 1;
                else if (dotpos > 0 && dotpos > oldLen) // input "." not same pos as old "."
                {
                    oldprice = oldprice.Replace(".", "");
                    textBoxNewStkPrice.Text = oldprice.Insert(oldLen, ".");
                    oldLen = oldLen + 1;
                }
            }
            if (e.KeyValue == 46)  //Del
            {
                textBoxNewStkPrice.Text = "0.000";
                oldLen = 0;
                calAmount();
                textBoxNewStkPrice.SelectAll();
            }
            else
            {
                textBoxNewStkPrice.SelectionStart = oldLen;
            }
            //textBoxNewStkPrice.SelectionStart = oldLen;
        }

        
        private void textBoxNewStkQty_KeyUp(object sender, KeyEventArgs e)
        {
            int oldLen = textBoxNewStkQty.SelectionStart;
            bool hasCommaBefore = textBoxNewStkQty.Text.IndexOf(",") >= 0 ? true : false;
            calAmount();
            bool hasCommaAfter = textBoxNewStkQty.Text.IndexOf(",") >= 0 ? true : false;
            if (!hasCommaBefore && hasCommaAfter)
                oldLen++;
            else if (hasCommaBefore && !hasCommaAfter)
                oldLen--;
            if (oldLen < 0)
                oldLen = 0;
            textBoxNewStkQty.SelectionStart = oldLen;
        }

        private void vScrollBarPrice_Scroll(object sender, ScrollEventArgs e)
        {
            if (e.Type == ScrollEventType.EndScroll) return;

            decimal stkPrice = Convert.ToDecimal(textBoxNewStkPrice.Text.Replace(",", ""));
            bool CanBuy = false, CanSell = false;
            char OrderSide = ' ';
            if (IsBuyOrder) OrderSide = 'B';
            else OrderSide = 'S';
            //if (BuySell.Trim() == GetResxString("Buy")) OrderSide = 'B';
            //else if (BuySell.Trim() == GetResxString("Sell")) OrderSide = 'S';
            if (FormExType == ExchangeTypeEnum.HKG)
            {
                if (e.Type == ScrollEventType.SmallDecrement)
                    stkPrice = Utils.OrderTypeSpread.CheckSpread(stkPrice, 'L', OrderSide, 1, CurrStock, TradeDB, out CanBuy, out CanSell);
                if (e.Type == ScrollEventType.SmallIncrement)
                    stkPrice = Utils.OrderTypeSpread.CheckSpread(stkPrice, 'L', OrderSide, -1, CurrStock, TradeDB, out CanBuy, out CanSell);
            }
            else if (FormExType == ExchangeTypeEnum.SHG || FormExType == ExchangeTypeEnum.SZE)
            {
                if (e.Type == ScrollEventType.SmallDecrement)
                    stkPrice = Utils.OrderTypeSpread.CheckSpread_ASHR(stkPrice, 'X', OrderSide, 1, CurrStock, TradeDB, out CanBuy, out CanSell);
                if (e.Type == ScrollEventType.SmallIncrement)
                    stkPrice = Utils.OrderTypeSpread.CheckSpread_ASHR(stkPrice, 'X', OrderSide, -1, CurrStock, TradeDB, out CanBuy, out CanSell);
            }
            //EnableDisableBuySellBtn(CanBuy, CanSell);
            if (stkPrice > 0) textBoxNewStkPrice.Text = String.Format("{0:N3}", stkPrice);
            textBoxNewStkPrice.SelectionStart = 0;
            textBoxNewStkPrice.SelectionLength = textBoxNewStkPrice.Text.Length;




            //decimal stkPrice = 0;
            //if (textBoxNewStkPrice.Text != "" && textBoxNewStkPrice.Text != ".")
            //    stkPrice = Convert.ToDecimal(textBoxNewStkPrice.Text.Replace(",", ""));
            //bool CanBuy = false, CanSell = false;
            //if (e.Type == ScrollEventType.SmallDecrement)
            //    stkPrice = Utils.OrderTypeSpread.CheckSpread(stkPrice, 'X', ' ', 1, CurrStock, TradeDB, out CanBuy, out CanSell);
            //if (e.Type == ScrollEventType.SmallIncrement)
            //    stkPrice = Utils.OrderTypeSpread.CheckSpread(stkPrice, 'X', ' ', -1, CurrStock, TradeDB, out CanBuy, out CanSell);
            //if (stkPrice >= 0) textBoxNewStkPrice.Text = String.Format("{0:N3}", stkPrice);
            //textBoxNewStkPrice.SelectionStart = 0;
            //textBoxNewStkPrice.SelectionLength = textBoxNewStkPrice.Text.Length;
            calAmount();

        }

        private void vScrollBarQty_Scroll(object sender, ScrollEventArgs e)
        {
            //decimal stkQty = 0;
            //if (textBoxNewStkQty.Text != "")
            //    stkQty = Convert.ToDecimal(textBoxNewStkQty.Text.Replace(",", ""));
            //if (e.Type == ScrollEventType.SmallDecrement)
            //    stkQty += lotSize;
            //if (e.Type == ScrollEventType.SmallIncrement && stkQty - lotSize >= 0)
            //    stkQty -= lotSize;
            //textBoxNewStkQty.Text = String.Format("{0:N0}", stkQty);
            //calAmount();

            if (e.Type == ScrollEventType.EndScroll) return;

            if (CurrStock == null) return;
            long stkQty = 0;
            //long stkLotSize = (labelLotSize.Text.Trim() != "") ? long.Parse(labelLotSize.Text) : 0;
            long stkLotSize = CurrStock.LotSize;
            if (textBoxNewStkQty.Text != "")
                stkQty = Convert.ToInt64(textBoxNewStkQty.Text.Replace(",", ""));

            if (e.Type == ScrollEventType.SmallDecrement)
                stkQty += stkLotSize;
            if (e.Type == ScrollEventType.SmallIncrement && stkQty - stkLotSize > 0)
                stkQty -= stkLotSize;
            if (stkQty >= 0) textBoxNewStkQty.Text = String.Format("{0:N0}", stkQty);
            textBoxNewStkQty.SelectionStart = 0;
            textBoxNewStkQty.SelectionLength = textBoxNewStkQty.Text.Length;
            calAmount();
        }

        private void buttonGo_Click(object sender, EventArgs e)
        {
            if (!ChkOrderValue()) return;
            ModifyOrder_ConfirmForm modifyOrderConfirmForm = new ModifyOrder_ConfirmForm(this.Culture,null);
            modifyOrderConfirmForm.FormName = "ModifyOrder";
            modifyOrderConfirmForm.IsBuyOrder = IsBuyOrder;
            modifyOrderConfirmForm.strStockCode = StockCode;
            modifyOrderConfirmForm.strOrderNo = OrderNumber;
            modifyOrderConfirmForm.strExchangeCode = Stock.GetExchangeCode(FormExType);
            modifyOrderConfirmForm.strStkPrice = "$" + textBoxNewStkPrice.Text;
            modifyOrderConfirmForm.strStkQty = textBoxNewStkQty.Text;
            modifyOrderConfirmForm.StartPosition = FormStartPosition.CenterScreen;
            modifyOrderConfirmForm.AccountNo = this.Accno;
            /*
            int orgQty, amendedQty;
            if (SettingsForms["SkipOrderOTP"] != "1" && TradeDB.RequireOTP &&
                int.TryParse(OrginalOrderQty.Replace(",", ""), out orgQty) && orgQty > 0 &&
                int.TryParse(textBoxNewStkQty.Text.Replace(",", ""), out amendedQty) && amendedQty > 0 && amendedQty > orgQty)
            {
                modifyOrderConfirmForm.RequireOTP = true;
                modifyOrderConfirmForm.SetOTPVisible();
            }
             */
            if ((SettingsUserPreference["ConfirmBeforeOrder"] != null && SettingsUserPreference["ConfirmBeforeOrder"] == "0") || modifyOrderConfirmForm.ShowDialog(this) == DialogResult.OK)
            {
                OTP = modifyOrderConfirmForm.OTP;
                PrepareAmend();
                this.Close();
            }
        }

        private void PrepareAmend()
        {
            string ErrorMsg;            
            decimal newPrice;
            int newQty;

            Order order1 = new Order();
            int.TryParse(labelOrderNum.Text, out order1.OrderNo);            
            decimal.TryParse(labelStockPrice.Text, out order1.Price);            
            int.TryParse(labelStockQty.Text.Replace(",", ""), out order1.Quantity);            
            decimal.TryParse(textBoxNewStkPrice.Text, out newPrice);
            int.TryParse(textBoxNewStkQty.Text.Replace(",", ""), out newQty);
            if (order1.Price == newPrice && order1.Quantity == newQty)
            {
                // no changed
            }
            else
            {
                bool success = TradeDB.OrderAmend(order1.OrderNo, newPrice, newQty, 0, OTP, out ErrorMsg);
                OTP = "";
                if (success == true)
                {
                    textBoxNewStkPrice.Enabled = false;
                    textBoxNewStkQty.Enabled = false;
                    buttonGo.Enabled = false;
                    buttonMax.Enabled = false;
                    vScrollBarPrice.Enabled = false;
                    vScrollBarQty.Enabled = false;
                }
            }
        }

        protected override void OnAccount(Account TheAccount)
        {
            if (TheAccount != null && TheAccount.AccountNo == ListenedAccount)
                CurrAccount = TheAccount;

            //try
            //{
            //    if (Account1.Count >= 1)
            //    {
            //        CurrAccount = (Account)Account1[0].Clone();
            //        //CurrAccount = Accno.Trim();
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.ToString());                
            //}
        }

        protected override void OnAccountList(List<string> AccountList)
        {
            /*
            AccountList1 = AccountList;
            if (AccountList1 != null && AccountList1.Count > 0 && AccountList1[0].Trim() != "" && Accno != "")
            {
                //ListenAccount(new List<string> { AccountList1[0].ToUpper().Trim() });
                ListenedAccount = Accno;
            }
             */
        }


        protected override void OnStock(Stock TheStock)
        {
            if (TheStock != null)
            {
                this.CurrStock = TheStock;
                labelStockName.Text = CurrStock.GetBestName(this.Culture);

                if (CurrStock != null)
                {
                    labelCurrency.Text = GetGeneralResxString("Currency_" + CurrStock.Currency);
                    labelCurrency.ForeColor = (CurrStock.Currency == null || CurrStock.Currency == "" || CurrStock.Currency == "HKD") ?
                        Color.FromKnownColor(KnownColor.ControlText) : Color.Red;
                    labelCurrency.Location = new Point (labelAmount.Location.X - labelCurrency.Width - 5, labelAmount.Location.Y);
                }
            }
        }

        private void buttonMax_Click(object sender, EventArgs e)
        {
            if (CurrStock == null) return;            
            if (CurrStock.LotSize <= 0) return;

            //if (labelBuySell.Text.Trim() == GetResxString("Buy"))
            if (IsBuyOrder) 
            {
                decimal stkPrice;
                decimal.TryParse(textBoxNewStkPrice.Text, out stkPrice);
                if (stkPrice <= 0) return;
                if (CurrAccount == null) return;
                if (CurrAccount.Balances.ContainsKey(CurrStock.Currency))
                {
                    if (CurrAccount.Balances[CurrStock.Currency].T2DayBal <= 0) return;
                }
                //if (CurrAccount.T2DayBal <= 0) return;

                int stkQty;                
                int.TryParse(textBoxNewStkQty.Text.Replace(",", ""), out stkQty);
                //decimal MaxBuyQty = Math.Truncate(CurrAccount.T2DayBal / (CurrStock.LotSize * stkPrice)) * CurrStock.LotSize;
                decimal MaxBuyQty = 0;
                if (CurrAccount.Balances.ContainsKey(CurrStock.Currency))
                    MaxBuyQty = Math.Truncate(CurrAccount.Balances[CurrStock.Currency].T2DayBal / (CurrStock.LotSize * stkPrice)) * CurrStock.LotSize;

                if (MaxBuyQty > stkQty) textBoxNewStkQty.Text = String.Format("{0:N0}", MaxBuyQty);
            }
            //else if (labelBuySell.Text.Trim() == GetResxString("Sell"))
            else
            {
                if (CurrAccount == null) return;                

                decimal QtyRemains = 0;
                decimal MaxSellQty = 0;
                int stkCode = 0, acStkCode;
                int.TryParse(labelStockCode.Text, out stkCode);
                if (stkCode == 0) return;

                foreach (KeyValuePair<string, AccountStock> kvp in CurrAccount.Stocks)
                {
                    if (kvp.Value != null && int.TryParse(kvp.Value.Code, out acStkCode))
                    {
                        if (stkCode == acStkCode)
                        {
                            QtyRemains = kvp.Value.QtyOnHand + kvp.Value.QtyInTransit;
                            break;
                        }
                    }
                }

                if (QtyRemains >= 0)
                {
                    MaxSellQty = Math.Truncate(((decimal)QtyRemains / CurrStock.LotSize)) * CurrStock.LotSize;
                }
                textBoxNewStkQty.Text = String.Format("{0:N0}", MaxSellQty);
            }
            return;
        }

        private bool ChkOrderValue()
        {
            bool Valid = true;
            // check acc
            if (CurrAccount == null && ListenedAccount.Trim() != "123") { return false; }
            //if (CurrAccount == null) return false;
            //if (CurrAccount.AccountNo.Trim() == "") return false;
            
            if (!CheckQtyValue()) Valid = false;
            if (textBoxNewStkPrice.Enabled)
            {
                if (!CheckPriceValue()) Valid = false;                
            }

            if (Valid == false) return false;

            //set valid            
            VisibleIcon(1, textBoxNewStkPrice);
            VisibleIcon(1, textBoxNewStkQty);

            return true;
        }


        private bool CheckQtyValue()
        {
            int stkQty;
            //qty
            int.TryParse(textBoxNewStkQty.Text.Replace(",", ""), out stkQty);
            if (stkQty <= 0) { VisibleIcon(2, textBoxNewStkQty); return false; }                

            // check stock
            if (CurrStock == null) return false;
            if (CurrStock.LotSize <= 0) return false;

            if (IsBuyOrder)
            {
                if ((stkQty % CurrStock.LotSize) > 0) { VisibleIcon(2, textBoxNewStkQty); return false; }
            }
            else
            {
                if (FormExType == ExchangeTypeEnum.SHG || FormExType == ExchangeTypeEnum.SZE) // SHG, SZE sell dont need to check qty boardlot
                    VisibleIcon(1, textBoxNewStkQty);                
                else
                    if ((stkQty % CurrStock.LotSize) > 0) { VisibleIcon(2, textBoxNewStkQty); return false; } //only check HK stock sell order
            }

            //////if (IsBuyOrder)
            //////{
            //////    if ((stkQty % CurrStock.LotSize) > 0) { VisibleIcon(2, textBoxNewStkQty); return false; }
            //////}
            //////else if (FormExType == ExchangeTypeEnum.SHG || FormExType == ExchangeTypeEnum.SZE)
            //////{
            //////    VisibleIcon(1, textBoxNewStkQty);
            //////}

            return true;
        }

        private bool CheckPriceValue()
        {
            //price
            decimal stkPrice;
            decimal.TryParse(textBoxNewStkPrice.Text, out stkPrice);
            if (stkPrice <= 0) { VisibleIcon(2, textBoxNewStkPrice); return false; }                

            // check stock
            if (CurrStock == null) return false;
            if (CurrStock.LotSize <= 0) return false;

            char OrderSide = ' ';
            if (IsBuyOrder) OrderSide = 'B';
            else OrderSide = 'S';
            //if (BuySell.Trim() == GetResxString("Buy")) OrderSide = 'B';
            //else if (BuySell.Trim() == GetResxString("Sell")) OrderSide = 'S';
            bool CanBuy = false, CanSell = false;

            decimal BidRange = 0;
            decimal AskRange = 0;
            char OrderTypeNow = ' ';
            if (Utils.OrderTypeSpread.GetSpreadRange(stkPrice, 'X', CurrStock, TradeDB, out OrderTypeNow, out BidRange, out AskRange) > 0)
            {

            }

            if (OrderTypeNow == 'I' || OrderTypeNow == 'N')
            {
                if (FormExType == ExchangeTypeEnum.HKG)
                    stkPrice = Utils.OrderTypeSpread.CheckSpread(stkPrice, 'X', OrderSide, 0, CurrStock, TradeDB, out CanBuy, out CanSell);
            }
            else
            {
                // Only if after 9:30pm
                if (OrderSide == 'B' && CurrStock.Ask > 0) { if (stkPrice > CurrStock.Ask) return false; }
                if (OrderSide == 'S' && CurrStock.Bid > 0) { if (stkPrice < CurrStock.Bid) return false; }

                if (FormExType == ExchangeTypeEnum.HKG)
                    stkPrice = Utils.OrderTypeSpread.CheckSpread(stkPrice, 'L', OrderSide, 0, CurrStock, TradeDB, out CanBuy, out CanSell);
            }


            //////if (FormExType == ExchangeTypeEnum.HKG)
            //////    stkPrice = Utils.OrderTypeSpread.CheckSpread(stkPrice, 'L', OrderSide, 0, CurrStock, TradeDB, out CanBuy, out CanSell);
            //////else if (FormExType == ExchangeTypeEnum.SHG || FormExType == ExchangeTypeEnum.SZE)
            //////    stkPrice = Utils.OrderTypeSpread.CheckSpread_ASHR(stkPrice, 'X', OrderSide, 0, CurrStock, TradeDB, out CanBuy, out CanSell);

            if (stkPrice < 0) { VisibleIcon(2, textBoxNewStkPrice); return false; }                

            return true;
        }

        // 0 = reset, 1 = true, 2 = false
        private void VisibleIcon(int isValid, TextBox t)
        {
            if (isValid == 1)
            {
                if (t.Name.ToUpper().Trim() == "TEXTBOXNEWSTKPRICE")
                {
                    pictureBoxStockPriceTick.Visible = true;
                    pictureBoxStockPriceCross.Visible = false;
                }
                else if (t.Name.ToUpper().Trim() == "TEXTBOXNEWSTKQTY")
                {
                    pictureBoxStockQtyTick.Visible = true;
                    pictureBoxStockQtyCross.Visible = false;
                }
            }
            else if (isValid == 2)
            {
                if (t.Name.ToUpper().Trim() == "TEXTBOXNEWSTKPRICE")
                {
                    pictureBoxStockPriceTick.Visible = false;
                    pictureBoxStockPriceCross.Visible = true;
                }
                else if (t.Name.ToUpper().Trim() == "TEXTBOXNEWSTKQTY")
                {
                    pictureBoxStockQtyTick.Visible = false;
                    pictureBoxStockQtyCross.Visible = true;
                }
            }
            else
            {
                if (t.Name.ToUpper().Trim() == "TEXTBOXNEWSTKPRICE")
                {
                    pictureBoxStockPriceTick.Visible = false;
                    pictureBoxStockPriceCross.Visible = false;
                }
                else if (t.Name.ToUpper().Trim() == "TEXTBOXNEWSTKQTY")
                {
                    pictureBoxStockQtyTick.Visible = false;
                    pictureBoxStockQtyCross.Visible = false;
                }
            }
        }

        private void pictureBoxStockPriceCross_Click(object sender, EventArgs e)
        {

        }

        private void textBoxNewStkPrice_TextChanged(object sender, EventArgs e)
        {
            VisibleIcon(0, textBoxNewStkPrice);
        }

        private void textBoxNewStkQty_TextChanged(object sender, EventArgs e)
        {
            VisibleIcon(0, textBoxNewStkQty);
        }

        private void myGroupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void AmendForm_Shown(object sender, EventArgs e)
        {
            textBoxNewStkPrice.Focus();
        }

        private void labelAccountNo_Click(object sender, EventArgs e)
        {

        }

        private void labelAccountNo_DoubleClick(object sender, EventArgs e)
        {
            if (SettingsForms["AllowOrderChangeAccount"] != null && SettingsForms["AllowOrderChangeAccount"] == "1")
            {
                myTextBoxAccountno.Visible = true;
                myTextBoxAccountno.Focus();
            }
        }

        private void myTextBoxAccountno_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                string ErrorMsg; int Ordno = 0; string newAccno = "";
                int.TryParse(OrderNumber, out Ordno);
                //if (Ordno <= 30000) return;

                newAccno = myTextBoxAccountno.Text.Trim();
                if (newAccno == Accno || newAccno == "") return;

                if (TradeDB.OrderChangeAccount(Ordno, newAccno, out ErrorMsg))
                {                    
                    myTextBoxAccountno.Visible = false;
                    myTextBoxAccountno.Text = "";
                }
            }
        }

        private void labelPartialFilled_Click(object sender, EventArgs e)
        {

        }
    }
}
