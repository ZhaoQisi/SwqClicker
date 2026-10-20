using System;
using System.Windows.Forms;

namespace AshClicker
{
    public class AshUtil
    {
        public static ListViewItem CreateKeyMappingItem(string name, AshKey userPress, AshKey willPress, int mode)
        {
            if (userPress == null || willPress == null)
            {
                throw new ArgumentNullException("userPress 和 willPress 都不能为 null。");
            }

            var press = new AshKeypress(name, userPress, willPress, mode);

            // 第一列：方案名；第二列：紧凑的绑定关系，例如 1|Q|切换
            var item = new ListViewItem(press.Name);
            item.SubItems.Add(press.BindingText);

            // 绑定自定义 Tag
            item.Tag = press;

            return item;
        }
    }
}
