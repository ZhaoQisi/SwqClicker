using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace AshClicker
{
    /// <summary>删除配置的二次确认窗口，居中于主界面并沿用背景图。</summary>
    public class ConfirmDeleteProfileForm : Form
    {
        private readonly BackgroundSkin backgroundSkin;

        public ConfirmDeleteProfileForm(string profileName)
        {
            DoubleBuffered = true;
            Text = "删除配置";
            ClientSize = new Size(360, 170);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            var message = new Label
            {
                Text = "确定删除配置「" + profileName + "」吗？\r\n该配置下的所有键位都会一起删除。",
                Location = new Point(20, 20),
                Size = new Size(320, 70)
            };

            var yesButton = new Button
            {
                Text = "是",
                DialogResult = DialogResult.Yes,
                Location = new Point(168, 120),
                Size = new Size(80, 30)
            };

            var noButton = new Button
            {
                Text = "否",
                DialogResult = DialogResult.No,
                Location = new Point(258, 120),
                Size = new Size(80, 30)
            };

            Controls.Add(message);
            Controls.Add(yesButton);
            Controls.Add(noButton);

            // 默认按钮设为“否”，避免误触回车删除配置。
            AcceptButton = noButton;
            CancelButton = noButton;

            backgroundSkin = new BackgroundSkin(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "background.png"));
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            backgroundSkin.Refresh();
            if (backgroundSkin.Image != null)
            {
                BackgroundSkin.SetLabelsTransparent(this);
            }
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
            backgroundSkin.Dispose();
            base.OnFormClosed(e);
        }
    }
}