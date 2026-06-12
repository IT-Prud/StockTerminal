using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace StockTerminal.Utils
{
    class DataGridViewDisableCheckBoxCell : DataGridViewCheckBoxCell
    {
        private bool enabledValue;
        private bool chkBoxVisibleValue;

        /// <summary>
        /// This property decides whether the checkbox should be shown 
        /// checked or unchecked.
        /// </summary>
        public bool Enabled
        {
            get
            {
                return enabledValue;
            }
            set
            {
                enabledValue = value;
            }
        }

        public bool ChkBoxVisible
        {
            get
            {
                return chkBoxVisibleValue;
            }
            set
            {
                chkBoxVisibleValue = value;
            }
        }

        public DataGridViewDisableCheckBoxCell()
        {
            this.enabledValue = true;
            this.chkBoxVisibleValue = true;
        }

        /// Override the Clone method so that the Enabled property is copied.
        public override object Clone()
        {
            DataGridViewDisableCheckBoxCell cell = (DataGridViewDisableCheckBoxCell)base.Clone();
            cell.Enabled = this.Enabled;
            return cell;
        }

        protected override void Paint (Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex, 
            DataGridViewElementStates elementState, object value, object formattedValue, string errorText, DataGridViewCellStyle cellStyle,
            DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts)
        {
            if (!this.enabledValue)
            {
                SolidBrush cellBackground = new SolidBrush(cellStyle.BackColor);
                graphics.FillRectangle(cellBackground, cellBounds);
                cellBackground.Dispose();
                PaintBorder(graphics, clipBounds, cellBounds,
                cellStyle, advancedBorderStyle);
                Rectangle checkBoxArea = cellBounds;
                Rectangle buttonAdjustment = this.BorderWidths(advancedBorderStyle);
                checkBoxArea.X += buttonAdjustment.X;
                checkBoxArea.Y += buttonAdjustment.Y;

                checkBoxArea.Height -= buttonAdjustment.Height;
                checkBoxArea.Width -= buttonAdjustment.Width;
                Point drawInPoint = new Point(cellBounds.X + cellBounds.Width / 2 - 7, cellBounds.Y + cellBounds.Height / 2 - 7);

                if (chkBoxVisibleValue)
                {
                    if ((bool)value)
                        CheckBoxRenderer.DrawCheckBox(graphics, drawInPoint, System.Windows.Forms.VisualStyles.CheckBoxState.CheckedDisabled);
                    else
                        CheckBoxRenderer.DrawCheckBox(graphics, drawInPoint, System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedDisabled);
                }
            }
            else
            {
                base.Paint(graphics, clipBounds, cellBounds, rowIndex, elementState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);
            }
        }
    }
}
