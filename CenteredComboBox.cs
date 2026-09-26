using System.Drawing;
using System.Windows.Forms;

namespace ArkaneSystems.MouseJiggle
{
    internal sealed class CenteredComboBox : ComboBox
    {
        internal CenteredComboBox()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            DropDownStyle = ComboBoxStyle.DropDownList;
        }

        protected override void OnDrawItem(DrawItemEventArgs eventArgs)
        {
            eventArgs.DrawBackground();
            if (eventArgs.Index >= 0 && eventArgs.Index < Items.Count)
            {
                string text = GetItemText(Items[eventArgs.Index]);
                Color color = (eventArgs.State & DrawItemState.Selected) == DrawItemState.Selected
                    ? SystemColors.HighlightText
                    : ForeColor;
                using (Brush brush = new SolidBrush(color))
                using (StringFormat format = new StringFormat())
                {
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;
                    format.Trimming = StringTrimming.EllipsisCharacter;
                    eventArgs.Graphics.DrawString(text, eventArgs.Font, brush, eventArgs.Bounds, format);
                }
            }
            eventArgs.DrawFocusRectangle();
            base.OnDrawItem(eventArgs);
        }
    }
}
