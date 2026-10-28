using System;
using System.Drawing;
using System.Windows.Forms;

namespace AshClicker
{
    /// <summary>双击配置标签后弹出的重命名窗口。</summary>
    public class ProfileRenameForm : Form
    {
        private readonly TextBox _nameBox;

        public ProfileRenameForm(string currentName)
        {
            Text = "重命名配置";
            ClientSize = new Size(320, 120);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            var label = new Label
            {
                Text = "配置名称",
                Location = new Point(16, 16),
                Size = new Size(280, 20)
            };

            _nameBox = new TextBox
            {
                Text = currentName ?? string.Empty,
                Location = new Point(16, 40),
                Size = new Size(288, 25),
                MaxLength = 24
            };

            var okButton = new Button
            {
                Text = "确定",
                Location = new Point(154, 80),
                Size = new Size(72, 28)
            };

            var cancelButton = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Location = new Point(232, 80),
                Size = new Size(72, 28)
            };

            okButton.Click += OkButton_Click;

            Controls.Add(label);
            Controls.Add(_nameBox);
            Controls.Add(okButton);
            Controls.Add(cancelButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;
        }

        public string ProfileName { get; private set; }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _nameBox.Focus();
            _nameBox.SelectAll();
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            var name = _nameBox.Text.Trim();
            if (name.Length == 0)
            {
                MessageBox.Show("配置名称不能为空。", "重命名配置");
                _nameBox.Focus();
                return;
            }

            ProfileName = name;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}