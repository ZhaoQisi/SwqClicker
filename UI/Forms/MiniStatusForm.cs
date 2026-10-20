using System;
using System.Drawing;
using System.Windows.Forms;

namespace AshClicker
{
    /// <summary>
    /// 最小化后显示的小状态条：图标 + 状态文字，绿色表示已开启、红色表示已关闭/暂停。
    /// 双击恢复主窗口，右键菜单可以显示主界面或退出，按住可以拖动。
    /// </summary>
    public class MiniStatusForm : Form
    {
        private static readonly Color ActiveColor = Color.FromArgb(0, 140, 60);
        private static readonly Color InactiveColor = Color.FromArgb(200, 40, 40);

        private readonly PictureBox _icon;
        private readonly Label _label;
        private bool _dragging;
        private Point _dragOffset;

        public MiniStatusForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.FromArgb(246, 246, 246);
            DoubleBuffered = true;
            ClientSize = new Size(110, 28);

            _icon = new PictureBox
            {
                Location = new Point(6, 6),
                Size = new Size(16, 16),
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.Transparent
            };

            try
            {
                using (var small = new Icon(AshClicker.Properties.Resources.ashlogo, 16, 16))
                {
                    _icon.Image = small.ToBitmap();
                }
            }
            catch (Exception)
            {
                // 图标加载失败不影响状态显示
            }

            _label = new Label
            {
                AutoSize = false,
                Location = new Point(28, 6),
                Size = new Size(70, 16),
                Text = "已关闭",
                ForeColor = InactiveColor,
                BackColor = Color.Transparent,
                Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 9F)
            };

            Controls.Add(_icon);
            Controls.Add(_label);

            Paint += OnPaint;
            DoubleClick += (s, e) => RequestRestore();

            MouseDown += OnDragStart;
            MouseMove += OnDragMove;
            MouseUp += OnDragEnd;

            _icon.MouseDown += OnDragStart;
            _icon.MouseMove += OnDragMove;
            _icon.MouseUp += OnDragEnd;
            _icon.DoubleClick += (s, e) => RequestRestore();
            _label.MouseDown += OnDragStart;
            _label.MouseMove += OnDragMove;
            _label.MouseUp += OnDragEnd;
            _label.DoubleClick += (s, e) => RequestRestore();

            var menu = new ContextMenuStrip();
            menu.Items.Add("显示主界面", null, (s, e) => RequestRestore());
            menu.Items.Add("退出", null, (s, e) => RequestExit());
            ContextMenuStrip = menu;
        }

        public event EventHandler RestoreRequested;

        public event EventHandler ExitRequested;

        /// <summary>只允许拖动/双击，不抢焦点。</summary>
        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW：不出现在 Alt+Tab
                return cp;
            }
        }

        public void SetStatus(string text, bool active)
        {
            _label.Text = text;
            _label.ForeColor = active ? ActiveColor : InactiveColor;

            var size = TextRenderer.MeasureText(text, _label.Font);
            _label.Size = size;

            ClientSize = new Size(28 + size.Width + 8, 28);
            _label.Location = new Point(28, (28 - size.Height) / 2);
            Invalidate();
        }

        /// <summary>放在屏幕右下角（任务栏上方）。</summary>
        public void MoveToDefaultPosition()
        {
            var area = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(area.Right - Width - 16, area.Bottom - Height - 16);
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(Color.FromArgb(180, 180, 180)))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }

        private void RequestRestore()
        {
            var handler = RestoreRequested;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        private void RequestExit()
        {
            var handler = ExitRequested;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        private void OnDragStart(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            var control = sender as Control;
            var screenPoint = control == null ? PointToScreen(e.Location) : control.PointToScreen(e.Location);

            _dragOffset = new Point(screenPoint.X - Location.X, screenPoint.Y - Location.Y);
            _dragging = true;
        }

        private void OnDragMove(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;

            var control = sender as Control;
            var screenPoint = control == null ? PointToScreen(e.Location) : control.PointToScreen(e.Location);

            Location = new Point(screenPoint.X - _dragOffset.X, screenPoint.Y - _dragOffset.Y);
        }

        private void OnDragEnd(object sender, MouseEventArgs e)
        {
            _dragging = false;

            // 拖到屏幕外时拉回默认位置，避免找不到
            if (!IsOnAnyScreen(Bounds))
            {
                MoveToDefaultPosition();
            }
        }

        private static bool IsOnAnyScreen(Rectangle bounds)
        {
            foreach (var screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.IntersectsWith(bounds)) return true;
            }

            return false;
        }
    }
}
