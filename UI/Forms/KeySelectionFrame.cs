using System;
using System.Windows.Forms;

namespace AshClicker
{

    public partial class KeySelectionFrame : Form
    {
        private readonly ListView mainListView;

        public KeySelectionFrame(ListView mainListView)
        {
            this.mainListView = mainListView;
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!(textBox1.Tag is AshKey userPress) || !(textBox2.Tag is AshKey willPress))
            {
                MessageBox.Show("请先选择按键喔 :(");
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
                textBox.Tag = AshKeyMap.Get(e.KeyValue);
                textBox.Text = AshKeyMap.Get(e.KeyValue).ShortName;
                e.SuppressKeyPress = true;
            }
        }

        private void KeySelectionFrame_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 0;
            textBox1.Tag = null;
            textBox2.Tag = null;
            textBoxName.Text = AshConfigPool.Instance.NextDefaultSchemeName();
        }
    }
}
