using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace AshClicker
{

    public partial class KeySelectionFrame : Form
    {
        private readonly ListView mainListView;
        private readonly bool sameKeyRepeatAvailable;
        private readonly BackgroundSkin backgroundSkin;
        private readonly Timer backgroundTimer;
        private bool backgroundTransparent;

        public KeySelectionFrame(ListView mainListView, bool sameKeyRepeatAvailable)
        {
            this.mainListView = mainListView;
            this.sameKeyRepeatAvailable = sameKeyRepeatAvailable;
            InitializeComponent();

            backgroundSkin = new BackgroundSkin(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "background.png"));
            backgroundTimer = new Timer { Interval = 1000 };
            backgroundTimer.Tick += (s, e) => RefreshBackground();
            DoubleBuffered = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!(textBox1.Tag is AshKey userPress) || !(textBox2.Tag is AshKey willPress))
            {
                MessageBox.Show("请先选择按键喔 :(");
                return;
            }

            if (userPress.VKey == willPress.VKey && !sameKeyRepeatAvailable)
            {
                MessageBox.Show("同键连发需要 Interception 驱动，请先安装驱动并重启后再试。");
                return;
            }

            var name = textBoxName.Text.Trim();
            if (name.Length > 6)
            {
                name = name.Substring(0, 6);
            }

            if (name.Length == 0)
            {
                name = AshConfigPool.Instance.NextDefaultSchemeName();
            }

            mainListView.Items.Add(AshUtil.CreateKeyMappingItem(name, userPress, willPress, comboBox1.SelectedIndex));
            AshConfigPool.Instance.UpdateFromList(mainListView);
            Close();
        }

        private void textBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                var key = AshKeyMap.Get(e.KeyValue);
                textBox.Tag = key;
                textBox.Text = key.ShortName;
                e.SuppressKeyPress = true;
                UpdateAddButton();
            }
        }

        private void KeySelectionFrame_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 0;
            textBox1.Tag = null;
            textBox2.Tag = null;
            textBoxName.Text = AshConfigPool.Instance.NextDefaultSchemeName();
            UpdateAddButton();
            RefreshBackground();
            backgroundTimer.Start();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var image = backgroundSkin == null ? null : backgroundSkin.Image;

            if (image == null)
            {
                base.OnPaintBackground(e);
                return;
            }

            e.Graphics.Clear(Color.White);
            BackgroundSkin.DrawCover(e.Graphics, image, ClientRectangle);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            backgroundTimer.Stop();
            backgroundTimer.Dispose();
            backgroundSkin.Dispose();
            base.OnFormClosed(e);
        }

        private void RefreshBackground()
        {
            if (IsDisposed || Disposing) return;
            if (!backgroundSkin.Refresh()) return;

            var hasImage = backgroundSkin.Image != null;
            if (hasImage != backgroundTransparent)
            {
                backgroundTransparent = hasImage;
                if (hasImage)
                {
                    BackgroundSkin.SetLabelsTransparent(this);
                }
            }

            Invalidate(true);
        }

        /// <summary>
        /// 没有 Interception 驱动时，同键连发会在轮询模式下自己触发自己，
        /// 因此直接禁用添加按钮，避免用户配置出无法使用的键位。
        /// </summary>
        private void UpdateAddButton()
        {
            var userPress = textBox1.Tag as AshKey;
            var willPress = textBox2.Tag as AshKey;
            var sameKeyBlocked = !sameKeyRepeatAvailable &&
                                 userPress != null &&
                                 willPress != null &&
                                 userPress.VKey == willPress.VKey;

            button1.Enabled = !sameKeyBlocked;
            button1.Text = sameKeyBlocked ? "同键连发需驱动" : "添加按键";
        }
    }
}
