using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace AshClicker
{
    /// <summary>
    /// 单个按键的录入窗口：按下任意一个键即选中，Esc 取消，Backspace / Delete 清除。
    /// allowModifiers 为 true 时允许把 Shift / Ctrl / Alt / Win 单独作为按键（暂停键用）。
    /// </summary>
    public class HotkeyCaptureForm : Form
    {
        private readonly Label _currentLabel;
        private readonly Label _hintLabel;
        private readonly Label _tipLabel;
        private readonly bool _allowModifiers;
        private readonly BackgroundSkin _backgroundSkin;
        private readonly Timer _backgroundTimer;
        private bool _backgroundTransparent;

        public HotkeyCaptureForm(int currentKey, bool allowModifiers)
        {
            _allowModifiers = allowModifiers;

            Text = "设置快捷键";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            TopMost = true;
            KeyPreview = true;
            ClientSize = new Size(360, 150);
            DoubleBuffered = true;

            _backgroundSkin = new BackgroundSkin(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "background.png"));
            _backgroundTimer = new Timer { Interval = 1000 };
            _backgroundTimer.Tick += (s, e) => RefreshBackground();

            try
            {
                Icon = AshClicker.Properties.Resources.ashlogo;
            }
            catch (Exception)
            {
                // 图标加载失败不影响设置
            }

            _currentLabel = new Label
            {
                Location = new Point(16, 16),
                Size = new Size(328, 22),
                Text = "当前：" + DescribeKey(currentKey)
            };

            _hintLabel = new Label
            {
                Location = new Point(16, 50),
                Size = new Size(328, 26),
                Font = new Font(Font.FontFamily, 11F, FontStyle.Bold),
                Text = "请按下要设置的按键…"
            };

            _tipLabel = new Label
            {
                Location = new Point(16, 92),
                Size = new Size(328, 54),
                ForeColor = SystemColors.GrayText,
                Text = allowModifiers
                    ? "Esc 取消；Backspace 清除。\r\n可单独按 Shift/Ctrl/Alt/Win；\r\n按住暂停键暂停按键功能，松开按键后恢复按键。"
                    : "Esc 取消；Backspace 清除。\r\n建议避免与游戏内按键重复。"
            };

            Controls.Add(_currentLabel);
            Controls.Add(_hintLabel);
            Controls.Add(_tipLabel);

            KeyDown += HotkeyForm_KeyDown;
        }

        /// <summary>选中的虚拟键码，0 表示清除。</summary>
        public int SelectedKey { get; private set; }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            RefreshBackground();
            _backgroundTimer.Start();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Activate();
            Focus();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _backgroundTimer.Stop();
            _backgroundTimer.Dispose();
            _backgroundSkin.Dispose();
            base.OnFormClosed(e);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var image = _backgroundSkin == null ? null : _backgroundSkin.Image;

            if (image == null)
            {
                base.OnPaintBackground(e);
                return;
            }

            e.Graphics.Clear(Color.White);
            BackgroundSkin.DrawCover(e.Graphics, image, ClientRectangle);
        }

        private void RefreshBackground()
        {
            if (IsDisposed || Disposing) return;
            if (!_backgroundSkin.Refresh()) return;

            var hasImage = _backgroundSkin.Image != null;
            if (hasImage != _backgroundTransparent)
            {
                _backgroundTransparent = hasImage;
                if (hasImage)
                {
                    BackgroundSkin.SetLabelsTransparent(this);
                }
            }

            Invalidate(true);
        }

        private void HotkeyForm_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;

            var key = e.KeyCode;

            if (key == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            if (key == Keys.Back || key == Keys.Delete)
            {
                SelectedKey = 0;
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            if (key == Keys.ShiftKey || key == Keys.ControlKey || key == Keys.Menu ||
                key == Keys.LWin || key == Keys.RWin)
            {
                if (!_allowModifiers)
                {
                    _hintLabel.Text = "请按下一个普通按键（不能只按修饰键）";
                    return;
                }

                SelectedKey = ModifierVKey(key);
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            SelectedKey = (int)key;
            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>WinForms 只给通用修饰键，这里统一成左侧键，和驱动上报的扫描码对应。</summary>
        private static int ModifierVKey(Keys key)
        {
            switch (key)
            {
                case Keys.ShiftKey:
                    return 0xA0; // LSHIFT
                case Keys.ControlKey:
                    return 0xA2; // LCONTROL
                case Keys.Menu:
                    return 0xA4; // LMENU
                case Keys.LWin:
                    return 0x5B;
                case Keys.RWin:
                    return 0x5C;
                default:
                    return 0;
            }
        }

        private static string DescribeKey(int vKey)
        {
            if (vKey <= 0) return "未设置";

            var key = AshKeyMap.Get(vKey);
            return key == null ? vKey.ToString() : key.ShortName;
        }
    }
}
