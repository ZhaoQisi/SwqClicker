using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AshClicker
{
    /// <summary>
    /// 带圆圈的小感叹号，用于暂停键旁边的提示入口。
    /// 背景透明，直接绘制在窗体背景/背景图上。
    /// </summary>
    public sealed class CircleExclamation : Control
    {
        public CircleExclamation()
        {
            SetStyle(
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);

            BackColor = Color.Transparent;
            Size = new Size(18, 18);
            Cursor = Cursors.Help;
            TabStop = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (Width <= 1 || Height <= 1) return;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (var fill = new SolidBrush(Color.FromArgb(198, 72, 72)))
            {
                e.Graphics.FillEllipse(fill, rect);
            }

            using (var border = new Pen(Color.FromArgb(150, 35, 35)))
            {
                e.Graphics.DrawEllipse(border, rect);
            }

            TextRenderer.DrawText(
                e.Graphics,
                "!",
                Font,
                rect,
                Color.White,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPadding);
        }
    }
}
