using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using StockTerminal.Utils;
using TradeDB;

namespace StockTerminal.Utils
{
    public partial class AccountComboBox : ComboBox
    {
        private bool _isDroppedDown = false;
        private int _dropDownHeight = 200;
        private int _dropDownWidth = 0;
        //private int _maxDropDownItems = 8;
        private ToolStripDropDown _popupControl;
        private ToolStripControlHost _controlHost;
        public ListBox MyListBox;
        public ITradeDB TradeDB;
        //private readonly List<string> suggestList = new List<string>(2000);
        //private AccountTreeNode accountTree = new AccountTreeNode();
        private bool lostFocusToListBox = false;

        //public delegate void MyDroppedDownEventHandler(object sender, EventArgs e);
        //public delegate void MyDrawItemEventHandler(object sender, DrawItemEventArgs e);
        //public delegate void MyMeasureItemEventHandler(object sender, MeasureItemEventArgs e);


        #region Delegates

        //[Category("Behavior"), Description("Occurs when ShowSuggestList changed to True.")]
        //public event MyDroppedDownEventHandler DroppedDownSuggest;

        //[Category("Behavior"), Description("Occurs when the SelectedIndex property changes.")]
        //public event EventHandler SelectedIndexChanged;

        //[Category("Behavior"), Description("Occurs whenever a particular item/area needs to be painted.")]
        //public event MyDrawItemEventHandler DrawItem;

        //[Category("Behavior"), Description("Occurs whenever a particular item's height needs to be calculated.")]
        //public event MyMeasureItemEventHandler MeasureItem;

        #endregion

        public AccountComboBox()
        {
            //SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            //SetStyle(ControlStyles.ContainerControl, true);
            //SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            //SetStyle(ControlStyles.ResizeRedraw, true);
            //SetStyle(ControlStyles.Selectable, true);
            //SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            //SetStyle(ControlStyles.UserMouse, true);
            //SetStyle(ControlStyles.UserPaint, true);
            //SetStyle(ControlStyles.Selectable, true);

            //base.BackColor = Color.Transparent;

            //this.Height = 21;
            //this.Width = 95;

            //this.SuspendLayout();
            //this.ResumeLayout(false);

            MyListBox = new ListBox();
            MyListBox.IntegralHeight = true;
            MyListBox.BorderStyle = BorderStyle.FixedSingle;
            MyListBox.SelectionMode = SelectionMode.One;
            MyListBox.BindingContext = new BindingContext();
            MyListBox.Font = this.Font;
            MyListBox.Dock = DockStyle.Fill;

            _controlHost = new ToolStripControlHost(MyListBox);
            _controlHost.Padding = new Padding(0);
            _controlHost.Margin = new Padding(0);
            _controlHost.AutoSize = false;

            _popupControl = new ToolStripDropDown();
            _popupControl.Padding = new Padding(0);
            _popupControl.Margin = new Padding(0);
            _popupControl.AutoSize = true;
            _popupControl.DropShadowEnabled = false;
            _popupControl.Items.Add(_controlHost);

            _dropDownWidth = this.Width;

            this.DropDown += new EventHandler(MyComboBoxDropDown);
            this.GotFocus += new EventHandler(MyComboBoxGotFocus);
            this.KeyPress += new KeyPressEventHandler(MyComboBoxKeyPressed);
            this.KeyDown += new KeyEventHandler(MyComboBoxKeyDown);
            this.KeyUp += new KeyEventHandler(MyComboBoxKeyUp);
            this.Leave += new EventHandler(MyComboBoxLeave);

            //MyListBox.MeasureItem += new MeasureItemEventHandler(_listBox_MeasureItem);
            //MyListBox.DrawItem += new DrawItemEventHandler(_listBox_DrawItem);
            MyListBox.MouseClick += new MouseEventHandler(_listBox_MouseClick);
            MyListBox.MouseMove += new MouseEventHandler(_listBox_MouseMove);
            MyListBox.KeyDown += new KeyEventHandler(MyListBoxKeyDown);

            _popupControl.Closed += new ToolStripDropDownClosedEventHandler(_popupControl_Closed);
        }

        #region Properties

        public bool ShowSuggestList
        {
            get { return _isDroppedDown; }
            set
            {
                if (_isDroppedDown == true && value == false)
                {
                    if (_popupControl.IsDropDown)
                    {
                        _popupControl.Close();
                    }
                }

                _isDroppedDown = value;

                if (_isDroppedDown)
                {
                    //_controlHost.Control.Width = _dropDownWidth;

                    //MyListBox.Refresh();
                    MyListBox.SuspendLayout();
                    MyListBox.Items.Clear();

                    //if (suggestList != null && suggestList.Count > 0 && this.Text != null)
                    if (this.Text != null)
                    {
                        string[] foundStrArray = TradeDB != null ? TradeDB.GetAccountList(this.Text) : null;

                        if (foundStrArray != null)
                        {
                            MyListBox.Items.AddRange(foundStrArray);
                            MyListBox.Height = Math.Max(20, Math.Min(_dropDownHeight, MyListBox.GetItemHeight(0) * MyListBox.Items.Count + 3));
                        }
                        else
                        {
                            _popupControl.Close();
                            return;
                        }
                    }

                    /*
                    if (MyListBox.Items.Count > 0)
                    {
                        int h = 0;
                        int i = 0;
                        int maxItemHeight = 0;
                        int highestItemHeight = 0;

                        foreach (object item in MyListBox.Items)
                        {
                            int itHeight = MyListBox.GetItemHeight(i);
                            if (highestItemHeight < itHeight)
                            {
                                highestItemHeight = itHeight;
                            }
                            h = h + itHeight;
                            if (i <= (_maxDropDownItems - 1))
                            {
                                maxItemHeight = h;
                            }
                            i = i + 1;
                        }

                        if (maxItemHeight > _dropDownHeight)
                            MyListBox.Height = _dropDownHeight + 3;
                        else
                        {
                            if (maxItemHeight > highestItemHeight)
                                MyListBox.Height = maxItemHeight + 3;
                            else
                                MyListBox.Height = highestItemHeight + 3;
                        }
                    }
                     */

                    _popupControl.AutoClose = false;
                    _popupControl.Show(this, CalculateDropPosition(), ToolStripDropDownDirection.BelowRight);
                    _popupControl.AutoClose = true;

                    MyListBox.Width = DropDownWidth;

                    MyListBox.ResumeLayout();
                }

                Invalidate();
                //if (_isDroppedDown)
                //    OnDroppedDown(this, EventArgs.Empty);
            }
        }

        #endregion


        /*
        private string[] GetAccountFromTree()
        {
            List<string> resultList = accountTree.GetAccountNoList(this.Text);
            if (resultList != null && resultList.Count > 0)
            {
                resultList.Sort();
                return resultList.ToArray();
            }
            else
                return null;
        }

        public int AddSuggest(object item)
        {
            if (item == null)
                return -1;

            //if (suggestList == null)
            //    suggestList = new List<string>();
            BuildTree(item.ToString());
            suggestList.Add(item.ToString());
            //this.Items.Add(item);
            return 0;
        }
        */

        /*
        public void AddSuggest(List<string> SuggestList)
        {
            if (SuggestList != null)
            {
                suggestList.AddRange(SuggestList);

                for (int i = 0; i < SuggestList.Count; i++)
                    BuildTree(SuggestList[i]);
            }
        }

        private void BuildTree(string strAccount)
        {
            int idx;
            char c, cLast;

            accountTree.AddNode(strAccount, strAccount);

            cLast = strAccount[0];
            if (cLast < '1' || cLast > '9')
            {
                for (idx = 1; idx < strAccount.Length; idx++)
                {
                    c = strAccount[idx];
                    if (c >= '1' && c <= '9')
                    {
                        accountTree.AddNode(strAccount.Substring(idx), strAccount);
                        break;
                    }
                    else if (c == '0' && cLast != '0')
                    {
                        accountTree.AddNode(strAccount.Substring(idx), strAccount);
                        cLast = c;
                    }
                }
            }
        }
        */

        private Point CalculateDropPosition()
        {
            return new Point(0, Height - 1);

            /*
            Point point = new Point(0, this.Height);
            if ((this.PointToScreen(new Point(0, 0)).Y + this.Height + _controlHost.Height) > Screen.PrimaryScreen.WorkingArea.Height)
                point.Y = -this._controlHost.Height - 7;
            return point;
             */
        }

        //public virtual void OnDroppedDown(object sender, EventArgs e)
        //{
        //    if (DroppedDownSuggest != null)
        //        DroppedDownSuggest(this, e);
        //}

        //#region "overrides"

        //protected override void OnPaint(PaintEventArgs e)
        //{
        //    base.OnPaint(e);
        //}

        //protected override void OnSelectedIndexChanged(EventArgs e)
        //{
        //    if (SelectedIndexChanged != null)
        //        SelectedIndexChanged(this, e);

        //    base.OnSelectedIndexChanged(e);
        //}

        //#endregion

        #region ComboBox Controls Events

        private void MyComboBoxKeyDown(object sender, KeyEventArgs e)
        {
            lostFocusToListBox = false;
            if (e.KeyCode == Keys.Enter)
            {
                if (this.DroppedDown && this.SelectedIndex >= 0)
                {
                    if (SelectedItem != null && this.Text.Trim() != "123")
                        this.Text = this.SelectedItem.ToString();
                    DroppedDown = false;
                }
                if (ShowSuggestList)
                {
					if (MyListBox.Items != null && MyListBox.Items.Count >= 1 && this.Text.Trim() != "123" && MyListBox.Items[0].ToString().IndexOf(this.Text.Trim()) >= 0)
						MyListBox.SelectedIndex = 0;
					else if (MyListBox.Items != null && MyListBox.Items.Count >= 1)
					{
						int tempInt = 0;
						foreach (String item in MyListBox.Items)
						{
							if (MyListBox.Items[tempInt].ToString().IndexOf(this.Text.Trim()) >= 0)
							{
								MyListBox.SelectedIndex = tempInt;
								break;
							}
							tempInt++;
						}
					}
					if (MyListBox.SelectedItem != null && this.Text.Trim() != "123")
                        this.Text = MyListBox.SelectedItem.ToString();
                    ShowSuggestList = false;
                    this.SelectAll();
                }
            }
            else if (e.KeyCode == Keys.Delete)
            {
                this.Text = "";
                this.DroppedDown = false;
                if (ShowSuggestList)
                    ShowSuggestList = false;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape && ShowSuggestList)
                ShowSuggestList = false;
            else if (MyListBox != null && ShowSuggestList && (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.PageUp || e.KeyCode == Keys.PageDown))
            {
                lostFocusToListBox = true;
                if (MyListBox.Items != null && MyListBox.Items.Count > 0)
                    if (MyListBox.SelectedIndex < 0)
                        MyListBox.SelectedIndex = 0;
                    else if (e.KeyCode == Keys.Down)
                        MyListBox.SelectedIndex++;
                    else if (e.KeyCode == Keys.Up)
                        MyListBox.SelectedIndex--;
                e.Handled = true;
                this.MyListBox.Focus();
            }
        }

        private void MyComboBoxKeyUp(object sender, KeyEventArgs e)
        {
            if (this.DroppedDown)
                return;
            if ((e.KeyCode == Keys.Up || e.KeyCode == Keys.Down) && MyListBox.SelectedItem != null)
                    this.Text = MyListBox.SelectedItem.ToString();

            // Only perform suggest search for letters and numeric
            if ((e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9) || (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9) || (e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z) || e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete)
            {
                if (this.Text.Trim() == "")
                {
                    _popupControl.Close();
                    return;
                }
                this.ShowSuggestList = true;
            }
        }

        private void MyComboBoxDropDown(object sender, EventArgs e)
        {
            // Close suggest list box if combo's list box is opened
            ShowSuggestList = false;
        }

        private void MyComboBoxGotFocus(object sender, EventArgs e)
        {
            Invalidate(true);
        }

        private void MyComboBoxKeyPressed(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar >= 97 && e.KeyChar <= 122)
                e.KeyChar = (char) (e.KeyChar & 223);        // convert to upper case
            else if (e.KeyChar != 8 && (e.KeyChar < 48 || (e.KeyChar > 57 && e.KeyChar < 65) || e.KeyChar > 90))
                e.Handled = true;
        }

        #endregion

        #region ListBox Controls Events

        private void MyListBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && ShowSuggestList && MyListBox.SelectedItem != null)
            {
                this.Text = MyListBox.SelectedItem.ToString();
                base.OnSelectionChangeCommitted(e);
                ShowSuggestList = false;
            }
        }

        private void _listBox_MouseMove(object sender, MouseEventArgs e)
        {
            int i;
            for (i = 0; i < (MyListBox.Items.Count); i++)
            {
                if (MyListBox.GetItemRectangle(i).Contains(MyListBox.PointToClient(MousePosition)))
                {
                    MyListBox.SelectedIndex = i;
                    return;
                }
            }
        }

        private void _listBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (MyListBox.Items.Count == 0 || MyListBox.SelectedItems.Count != 1)
                return;

            //this.SelectedIndex = MyListBox.SelectedIndex;
            if (MyListBox.SelectedItem != null)
            {
                this.Text = MyListBox.SelectedItem.ToString();
                base.OnSelectionChangeCommitted(e);
            }

            if (DropDownStyle == ComboBoxStyle.DropDownList)
                this.Invalidate(true);

            ShowSuggestList = false;
        }

        private void MyComboBoxLeave(object sender, EventArgs e)
        {
            if (ShowSuggestList && !lostFocusToListBox)
                ShowSuggestList = false;
        }

        //private void _listBox_DrawItem(object sender, DrawItemEventArgs e)
        //{
        //    if (e.Index >= 0)
        //        if (DrawItem != null)
        //            DrawItem(this, e);
        //}

        //private void _listBox_MeasureItem(object sender, MeasureItemEventArgs e)
        //{
        //    if (MeasureItem != null)
        //        MeasureItem(this, e);
        //}

        private void _popupControl_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            _isDroppedDown = false;
            Invalidate(true);
        }
        #endregion

    }
}
