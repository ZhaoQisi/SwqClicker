using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using AshClicker.Driver;
using AshClicker.Properties;
using Microsoft.Win32;

namespace AshClicker
{
    public partial class AshForm : Form
    {
        private const int HOTKEY_TOGGLE_ID = 12490;

        private DdControl dd;
        private bool started;
        private bool DDLoaded;
        private InterceptionControl ic;

        private readonly ClickerEngine engine;
        private readonly KeySender keySender;
        private InterceptionListener listener;

        private Dictionary<int, bool> _polledDown = new Dictionary<int, bool>();
        private int[] _pollKeys = new int[0];
        private int _registeredToggleKey;
        private readonly List<int> _activeSchemes = new List<int>();
        private MiniStatusForm mini;
        private Panel listHeaderDivider;
        private ToolTip infoToolTip;
        private CircleExclamation pauseInfoIcon;
        private Panel profileTabsViewport;
        private FlowLayoutPanel profileTabs;
        private Button profileTabsLeft;
        private Button profileTabsRight;
        private int profileTabsScrollX;
        private ToolTip profileTabToolTip;
        private Button buttonDeleteProfile;

        public AshForm()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Console.WriteLine($"Version: {version}");

            var infoVersion = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

            Console.WriteLine($"Informational Version: {infoVersion}");
            InitializeComponent();

            listHeaderDivider = new Panel
            {
                Height = 2,
                BackColor = Color.FromArgb(140, 140, 140),
                Enabled = false,
                TabStop = false,
                Visible = false
            };
            Controls.Add(listHeaderDivider);

            infoToolTip = new ToolTip
            {
                AutoPopDelay = 15000,
                InitialDelay = 300,
                ReshowDelay = 100,
                ShowAlways = true
            };

            infoToolTip.SetToolTip(label7, "按住暂停键暂停按键功能，松开按键后恢复按键。");
            infoToolTip.SetToolTip(buttonPauseHotkey, "按住暂停键暂停按键功能，松开按键后恢复按键。");

            pauseInfoIcon = new CircleExclamation { Visible = false };
            Controls.Add(pauseInfoIcon);

            profileTabsViewport = new Panel
            {
                Location = new Point(17, 170),
                Size = new Size(372, 30),
                BackColor = Color.Transparent
            };

            profileTabs = new FlowLayoutPanel
            {
                Location = new Point(22, 0),
                Size = new Size(328, 30),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            profileTabsLeft = new Button
            {
                Text = "‹",
                Location = new Point(0, 0),
                Size = new Size(22, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(246, 238, 238),
                ForeColor = Color.FromArgb(90, 70, 70),
                Font = new Font(Font.FontFamily, 12F, FontStyle.Bold),
                TabStop = false,
                Visible = false
            };
            profileTabsLeft.FlatAppearance.BorderSize = 0;

            profileTabsRight = new Button
            {
                Text = "›",
                Location = new Point(350, 0),
                Size = new Size(22, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(246, 238, 238),
                ForeColor = Color.FromArgb(90, 70, 70),
                Font = new Font(Font.FontFamily, 12F, FontStyle.Bold),
                TabStop = false,
                Visible = false
            };
            profileTabsRight.FlatAppearance.BorderSize = 0;

            profileTabsViewport.Controls.Add(profileTabs);
            profileTabsViewport.Controls.Add(profileTabsLeft);
            profileTabsViewport.Controls.Add(profileTabsRight);
            profileTabsLeft.BringToFront();
            profileTabsRight.BringToFront();

            Controls.Add(profileTabsViewport);
            profileTabsViewport.BringToFront();

            profileTabsLeft.Click += (s, e) => ScrollProfileTabs(-120);
            profileTabsRight.Click += (s, e) => ScrollProfileTabs(120);
            profileTabs.MouseWheel += ProfileTabs_MouseWheel;
            profileTabsViewport.MouseWheel += ProfileTabs_MouseWheel;

            profileTabToolTip = new ToolTip
            {
                AutoPopDelay = 5000,
                InitialDelay = 300,
                ReshowDelay = 100,
                ShowAlways = true
            };

            profileTabToolTip.SetToolTip(profileTabsLeft, "向左滚动配置");
            profileTabToolTip.SetToolTip(profileTabsRight, "向右滚动配置");

            buttonDeleteProfile = new Button
            {
                Text = "删除配置",
                Location = new Point(265, 364),
                Size = new Size(124, 34),
                UseVisualStyleBackColor = true
            };
            buttonDeleteProfile.Click += ButtonDeleteProfile_Click;
            Controls.Add(buttonDeleteProfile);
            buttonDeleteProfile.BringToFront();

            backgroundSkin = new BackgroundSkin(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "background.png"));
            backgroundTimer = new Timer { Interval = 1000 };
            backgroundTimer.Tick += (s, e) => RefreshBackground();
            DoubleBuffered = true;

            keySender = new KeySender();
            engine = new ClickerEngine(keySender);
            engine.PauseChanged += Engine_PauseChanged;
            engine.SchemeStateChanged += Engine_SchemeStateChanged;
            Resize += AshForm_Resize;

            try
            {
                ic = new InterceptionControl();
                keySender.Interception = ic;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                // 定义日志文件路径
                var logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");

                // 写入日志文件
                File.AppendAllText(logFilePath,
                    $"[{DateTime.Now}] Exception: {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}");
                try
                {
                    string mouseStatus = RunScCommand("sc query mouse.sys");
                    string keyboardStatus = RunScCommand("sc query keyboard.sys");

                    File.AppendAllText(logFilePath, $"[{DateTime.Now}] SC Query Results:{Environment.NewLine}");
                    File.AppendAllText(logFilePath,
                        $"Mouse.sys Status:{Environment.NewLine}{mouseStatus}{Environment.NewLine}");
                    File.AppendAllText(logFilePath,
                        $"Keyboard.sys Status:{Environment.NewLine}{keyboardStatus}{Environment.NewLine}");
                }
                catch (Exception scEx)
                {
                    File.AppendAllText(logFilePath,
                        $"[{DateTime.Now}] Error querying SC status: {scEx.Message}{Environment.NewLine}");
                }

                buttonSwitchEnable.Enabled = false;
                MessageBox.Show("还没有安装驱动喔！请记得先点击安装驱动！");
            }
        }

        string RunScCommand(string command)
        {
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $"/c {command}";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new Exception($"Error running command: {error}");
            }

            return output;
        }

        private SoundEffectPlayer _start;
        private SoundEffectPlayer _stop;

        private readonly BackgroundSkin backgroundSkin;
        private readonly Timer backgroundTimer;
        private bool backgroundTransparent;

        private static readonly string SoundsDirectory =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds");

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        private const int LVM_GETHEADER = 0x101F;
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 2;

        /// <summary>无边框窗口用标题栏模拟系统拖动。</summary>
        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
        }

        private void buttonMinimize_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW：无边框窗口也带阴影
                return cp;
            }
        }

        /// <summary>
        /// 背景只绘制在标题栏下方的内容区，等比覆盖并底部对齐，
        /// 顶部超出部分裁掉，不拉伸变形。
        /// </summary>
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var image = backgroundSkin == null ? null : backgroundSkin.Image;

            if (image == null)
            {
                base.OnPaintBackground(e);
                return;
            }

            // 半透明背景叠加在白底上，保留图片本身的透明度，
            // 避免直接用窗体灰色背景导致颜色发暗或发灰。
            e.Graphics.Clear(Color.White);

            var target = new Rectangle(
                0,
                titleBar.Height,
                ClientSize.Width,
                ClientSize.Height - titleBar.Height);

            BackgroundSkin.DrawCover(e.Graphics, image, target);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            using (var pen = new Pen(Color.FromArgb(200, 200, 200)))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }

        private void AshForm_Load(object sender, EventArgs e)
        {
            // 代码添加的控件不参与设计器字体缩放，按实际字体/尺寸放到“暂停键”文字后面。
            var iconSize = Math.Max(16, label7.Height - 2);
            pauseInfoIcon.Size = new Size(iconSize, iconSize);
            pauseInfoIcon.Font = new Font(label7.Font.FontFamily, label7.Font.Size, FontStyle.Bold);
            var textWidth = TextRenderer.MeasureText(label7.Text, label7.Font).Width;
            pauseInfoIcon.Location = new Point(
                label7.Left + textWidth + 4,
                label7.Top + (label7.Height - iconSize) / 2);
            pauseInfoIcon.Visible = true;
            pauseInfoIcon.BringToFront();

            // 等图标显示、句柄创建后再绑定，避免沿用旧 Tooltip 文案。
            infoToolTip.SetToolTip(pauseInfoIcon, "按住暂停键暂停按键功能，松开按键后恢复按键。");

            RefreshBackground();
            backgroundTimer.Start();

            if (Icon != null)
            {
                titleIcon.Image = Icon.ToBitmap();
            }

            dd = new DdControl();
            keySender.Dd = dd;

            _start = new SoundEffectPlayer(
                Path.Combine(SoundsDirectory, "按键开启.wav"),
                Resources.start);
            _stop = new SoundEffectPlayer(
                Path.Combine(SoundsDirectory, "按键关闭.wav"),
                Resources.stop);

            using (var stream = new MemoryStream(Resources.silence))
            {
                new SoundPlayer(stream).PlaySync();
            }


            AshConfigPool.Instance.LoadConfig();
            LoadActiveProfileToList();
            LayoutProfileTabs();
            UpdateProfileTabs();

            listView1.ClientSizeChanged += (cs, ce) =>
            {
                FitListColumns();
                UpdateListHeaderDivider();
            };

            var pressDelay = AshConfigPool.Instance.PressDelay;
            var releaseDelay = AshConfigPool.Instance.ReleaseDelay;
            textBox1.Text = (pressDelay > 0) ? pressDelay.ToString() : "20";
            AshConfigPool.Instance.PressDelay = pressDelay > 0 ? pressDelay : 20;
            textBox2.Text = (releaseDelay > 0) ? releaseDelay.ToString() : "5";
            AshConfigPool.Instance.ReleaseDelay = releaseDelay > 0 ? releaseDelay : 5;

            UpdateHotkeyButtons();
            ApplyToggleHotkey();
        }

        /// <summary>
        /// 读取或刷新 Images\background.png。文件变化后无需重启程序。
        /// </summary>
        private void RefreshBackground()
        {
            if (IsDisposed || Disposing) return;
            if (!backgroundSkin.Refresh()) return;

            var image = backgroundSkin.Image;
            ApplyBackgroundTransparency(image != null);
            Invalidate(true);
        }

        /// <summary>
        /// 有背景图时让标题栏、Label 和 CheckBox 透出背景。
        /// Button、TextBox、ListView 保持实色，避免影响可读性和交互。
        /// </summary>
        private void ApplyBackgroundTransparency(bool hasImage)
        {
            if (hasImage == backgroundTransparent) return;
            backgroundTransparent = hasImage;

            // 标题栏保持实色，背景只从标题栏下方开始。
            titleBar.BackColor = Color.FromArgb(250, 250, 250);

            BackgroundSkin.SetLabelsTransparent(this);
        }

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("winmm.dll")]
        public static extern uint timeBeginPeriod(uint uMilliseconds);

        [DllImport("winmm.dll")]
        public static extern uint timeEndPeriod(uint uMilliseconds);

        /// <summary>
        /// 让「绑定关系」列自动填满剩余宽度（-2），不出现左右滚动条；
        /// 出现竖向滚动条时 ListView 会触发 ClientSizeChanged，这里会跟着重算。
        /// </summary>
        private void FitListColumns()
        {
            if (listView1.Columns.Count < 2) return;

            listView1.Columns[1].Width = -2;
        }

        /// <summary>
        /// ListView 原生表头和内容之间没有明显的分隔线，
        /// 这里按实际表头底部位置叠一条 1px 的线。
        /// </summary>
        private void UpdateListHeaderDivider()
        {
            if (listHeaderDivider == null || listView1 == null ||
                listView1.IsDisposed || !listView1.IsHandleCreated)
            {
                return;
            }

            var header = SendMessage(listView1.Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
            RECT headerRect;

            if (header == IntPtr.Zero || !GetWindowRect(header, out headerRect))
            {
                listHeaderDivider.Visible = false;
                return;
            }

            var headerBottom = PointToClient(new Point(headerRect.Left, headerRect.Bottom)).Y;

            if (headerBottom <= listView1.Top || headerBottom >= listView1.Bottom)
            {
                listHeaderDivider.Visible = false;
                return;
            }

            listHeaderDivider.Bounds = new Rectangle(
                listView1.Left,
                headerBottom,
                listView1.Width,
                2);
            listHeaderDivider.Visible = true;
            listHeaderDivider.BringToFront();
        }

        /// <summary>
        /// 配置标签是代码创建的控件，不会参与设计器字体缩放，
        /// 这里按实际按钮底部和列表顶部重新定位，避免盖住表头。
        /// </summary>
        private void LayoutProfileTabs()
        {
            if (profileTabsViewport == null || profileTabs == null || listView1 == null) return;

            // 标签栏至少 30px 高，保证文字不被边框裁掉。
            var top = buttonSwitchEnable.Bottom;
            profileTabsViewport.Bounds = new Rectangle(listView1.Left, top, listView1.Width, 30);
            profileTabsRight.Left = profileTabsViewport.Width - profileTabsRight.Width;

            // 列表下移到标签栏下方，底部仍贴着添加/删除按钮，保持整体布局。
            var listTop = profileTabsViewport.Bottom + 2;
            var listBottom = buttonAddKey.Top - 6;
            var listHeight = Math.Max(60, listBottom - listTop);
            listView1.Bounds = new Rectangle(listView1.Left, listTop, listView1.Width, listHeight);

            // 删除配置是代码创建的按钮，不参与设计器缩放，按实际按钮宽度定位。
            var deleteProfileLeft = buttonDeleteKey.Right + 12;
            buttonDeleteProfile.Location = new Point(deleteProfileLeft, buttonAddKey.Top);
            buttonDeleteProfile.Size = new Size(
                Math.Max(60, listView1.Right - deleteProfileLeft),
                buttonAddKey.Height);
            buttonDeleteProfile.BringToFront();

            profileTabsViewport.BringToFront();
            UpdateProfileTabScroll(false);
            UpdateListHeaderDivider();
        }

        private void ScrollProfileTabs(int delta)
        {
            profileTabsScrollX += delta;
            UpdateProfileTabScroll(false);
        }

        private void ProfileTabs_MouseWheel(object sender, MouseEventArgs e)
        {
            ScrollProfileTabs(-e.Delta);
        }

        /// <summary>
        /// 标签超出宽度时用左右箭头滚动，不再使用会盖住文字的横向滚动条。
        /// </summary>
        private void UpdateProfileTabScroll(bool ensureActive)
        {
            if (profileTabsViewport == null || profileTabs == null) return;

            var viewWidth = Math.Max(0, profileTabsViewport.ClientSize.Width - 44);
            var totalWidth = 0;
            foreach (Control control in profileTabs.Controls)
            {
                totalWidth += control.Width + control.Margin.Horizontal;
            }

            var maxScroll = Math.Max(0, totalWidth - viewWidth);

            if (ensureActive)
            {
                profileTabs.PerformLayout();
                var active = AshConfigPool.Instance.ActiveProfileIndex;
                foreach (Control control in profileTabs.Controls)
                {
                    var tab = control as Button;
                    if (tab == null || !(tab.Tag is int index) || index != active) continue;

                    var left = tab.Left;
                    var right = left + tab.Width;

                    if (left < profileTabsScrollX)
                    {
                        profileTabsScrollX = left;
                    }
                    else if (right > profileTabsScrollX + viewWidth)
                    {
                        profileTabsScrollX = right - viewWidth;
                    }

                    break;
                }
            }

            profileTabsScrollX = Math.Max(0, Math.Min(maxScroll, profileTabsScrollX));

            profileTabs.Width = Math.Max(viewWidth, totalWidth);
            profileTabs.Height = profileTabsViewport.ClientSize.Height;
            profileTabs.Left = 22 - profileTabsScrollX;

            var overflow = totalWidth > viewWidth;
            profileTabsLeft.Visible = overflow;
            profileTabsRight.Visible = overflow;
            profileTabsLeft.Enabled = overflow && profileTabsScrollX > 0;
            profileTabsRight.Enabled = overflow && profileTabsScrollX < maxScroll;
        }

        private void LoadActiveProfileToList()
        {
            listView1.BeginUpdate();
            try
            {
                listView1.Items.Clear();
                AshConfigPool.Instance.LoadToList(listView1);
            }
            finally
            {
                listView1.EndUpdate();
            }

            FitListColumns();
            UpdateListHeaderDivider();
        }

        /// <summary>重建配置标签栏，末尾附带一个“＋”新增按钮。</summary>
        private void UpdateProfileTabs()
        {
            if (profileTabs == null) return;

            profileTabs.SuspendLayout();

            var oldControls = new List<Control>();
            foreach (Control control in profileTabs.Controls)
            {
                oldControls.Add(control);
            }

            profileTabs.Controls.Clear();
            foreach (var control in oldControls)
            {
                control.Dispose();
            }

            var profiles = AshConfigPool.Instance.Profiles;
            // 至少留出文字高度 + 上下边距，避免标题被裁成一半。
            var tabHeight = Math.Max(24, profileTabsViewport.Height - 4);
            for (var i = 0; i < profiles.Count; i++)
            {
                var index = i;
                var name = profiles[i].Name;
                var width = Math.Min(180, Math.Max(64, TextRenderer.MeasureText(name, profileTabs.Font).Width + 24));

                var tab = new Button
                {
                    Text = name,
                    Tag = index,
                    Width = width,
                    Height = tabHeight,
                    Margin = new Padding(0, 2, 4, 0),
                    FlatStyle = FlatStyle.Flat,
                    UseVisualStyleBackColor = false,
                    AutoEllipsis = true,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Cursor = Cursors.Hand,
                    TabStop = true
                };
                tab.FlatAppearance.BorderSize = 0;
                tab.Click += ProfileTab_Click;
                tab.MouseUp += ProfileTab_MouseUp;
                profileTabToolTip.SetToolTip(tab, name + "\r\n右键重命名配置");
                profileTabs.Controls.Add(tab);
            }

            var addButton = new Button
            {
                Text = "＋",
                Tag = -1,
                Width = 30,
                Height = tabHeight,
                Margin = new Padding(0, 2, 0, 0),
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                BackColor = Color.FromArgb(246, 238, 238),
                ForeColor = Color.FromArgb(90, 70, 70),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                TabStop = true
            };
            addButton.FlatAppearance.BorderSize = 1;
            addButton.FlatAppearance.BorderColor = Color.FromArgb(205, 160, 160);
            addButton.Click += AddProfile_Click;
            profileTabToolTip.SetToolTip(addButton, "新增一套配置");
            profileTabs.Controls.Add(addButton);

            profileTabs.ResumeLayout();
            profileTabs.Enabled = !started;
            UpdateProfileTabScroll(true);
            UpdateProfileTabSelection();
            UpdateDeleteProfileButton();
        }

        private void UpdateDeleteProfileButton()
        {
            if (buttonDeleteProfile == null) return;
            buttonDeleteProfile.Enabled = !started && AshConfigPool.Instance.Profiles.Count > 1;
        }

        private void ButtonDeleteProfile_Click(object sender, EventArgs e)
        {
            if (started) return;

            var profiles = AshConfigPool.Instance.Profiles;
            if (profiles.Count <= 1) return;

            var profile = AshConfigPool.Instance.ActiveProfile;
            var result = MessageBox.Show(
                "确定删除配置「" + profile.Name + "」吗？\r\n该配置下的所有键位都会一起删除。",
                "删除配置",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (result != DialogResult.Yes) return;

            if (AshConfigPool.Instance.DeleteActiveProfile())
            {
                LoadActiveProfileToList();
                UpdateProfileTabs();
            }
        }

        private void UpdateProfileTabSelection()
        {
            if (profileTabs == null) return;

            var active = AshConfigPool.Instance.ActiveProfileIndex;
            foreach (Control control in profileTabs.Controls)
            {
                var tab = control as Button;
                if (tab == null || !(tab.Tag is int index) || index < 0) continue;

                var selected = index == active;
                // 选中态用背景色区分，不用边框挤压文字。
                tab.BackColor = selected
                    ? Color.FromArgb(255, 248, 248)
                    : Color.FromArgb(240, 230, 230);
                tab.ForeColor = selected
                    ? Color.FromArgb(120, 45, 45)
                    : Color.FromArgb(70, 60, 60);
                tab.FlatAppearance.BorderSize = selected ? 1 : 0;
                tab.FlatAppearance.BorderColor = Color.FromArgb(225, 185, 185);
            }

            UpdateDeleteProfileButton();
        }

        private void ProfileTab_Click(object sender, EventArgs e)
        {
            if (started) return;

            var tab = sender as Button;
            if (tab == null || !(tab.Tag is int index)) return;
            if (index == AshConfigPool.Instance.ActiveProfileIndex) return;

            AshConfigPool.Instance.UpdateFromList(listView1);
            AshConfigPool.Instance.SetActiveProfile(index);
            LoadActiveProfileToList();
            UpdateProfileTabSelection();
        }

        private void ProfileTab_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || started) return;

            var tab = sender as Button;
            if (tab == null || !(tab.Tag is int index)) return;

            ShowRenameProfileDialog(index);
        }

        private void ShowRenameProfileDialog(int index)
        {
            var profiles = AshConfigPool.Instance.Profiles;
            if (index < 0 || index >= profiles.Count) return;

            using (var form = new ProfileRenameForm(profiles[index].Name))
            {
                if (form.ShowDialog(this) == DialogResult.OK &&
                    AshConfigPool.Instance.RenameProfile(index, form.ProfileName))
                {
                    UpdateProfileTabs();
                }
            }
        }

        private void AddProfile_Click(object sender, EventArgs e)
        {
            if (started) return;

            AshConfigPool.Instance.UpdateFromList(listView1);
            AshConfigPool.Instance.AddProfile();
            LoadActiveProfileToList();
            UpdateProfileTabs();
        }

        /// <summary>连发循环的每一拍：驱动模式下由拦截线程更新状态，这里只负责发送；DD 模式需要自己轮询。</summary>
        private void TimerTick()
        {
            if (!usingInterception)
            {
                PollKeys();
            }

            engine.Tick();
        }

        /// <summary>DD / 无驱动模式拿不到拦截事件，只能轮询按键状态。</summary>
        private void PollKeys()
        {
            foreach (var vKey in _pollKeys)
            {
                var isDown = (GetAsyncKeyState(vKey) & 0x8000) != 0;

                bool previous;
                if (_polledDown.TryGetValue(vKey, out previous) && previous == isDown) continue;

                _polledDown[vKey] = isDown;

                if (isDown)
                {
                    engine.HandleKeyDown(vKey);
                }
                else
                {
                    engine.HandleKeyUp(vKey);
                }
            }
        }

        private void BuildPollKeys(Dictionary<int, int> targets, int pauseKey)
        {
            var keys = new List<int>(targets.Keys);
            if (pauseKey != 0 && !keys.Contains(pauseKey))
            {
                keys.Add(pauseKey);
            }

            _pollKeys = keys.ToArray();
            _polledDown = new Dictionary<int, bool>();
            foreach (var key in _pollKeys)
            {
                _polledDown[key] = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "DD|*.DLL" };

            if (ofd.ShowDialog() != DialogResult.OK) return;

            LoadDllFile(ofd.FileName);
            usingInterception = false;
        }

        private void LoadDllFile(string dllfile)
        {
            int ret = dd.Load(dllfile);
            if (ret != 1)
            {
                MessageBox.Show("Load Error");
                return;
            }

            ret = dd.btn(0);
            if (ret != 1)
            {
                MessageBox.Show("Initialize Error");
                return;
            }

            buttonLoadDriver.Text = "DD 驱动已加载";
            buttonSwitchEnable.Enabled = true;
            DDLoaded = true;
        }

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, KeyModifiers modkey, Keys vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private void button2_Click(object sender, EventArgs e)
        {
            ToggleClicker();
        }

        private void ToggleClicker()
        {
            if (started)
            {
                StopClicker();
            }
            else
            {
                StartClicker();
            }
        }

        private void StartClicker()
        {
            if (started) return;

            if (!ValidateHotkeys()) return;

            Dictionary<int, int> targets;
            Dictionary<int, int> modes;
            AshConfigPool.Instance.GetMaps(out targets, out modes);

            if (targets.Count == 0)
            {
                MessageBox.Show("还没有添加任何键位喔！");
                return;
            }

            if (usingInterception && ic == null)
            {
                MessageBox.Show("驱动还没有就绪，请先安装驱动并重启电脑。");
                return;
            }

            if (!SameKeyRepeatAllowed)
            {
                foreach (var pair in targets)
                {
                    if (pair.Key == pair.Value)
                    {
                        MessageBox.Show("同键连发需要 Interception 驱动，当前模式不支持，请安装驱动并重启后再试，或修改键位。");
                        return;
                    }
                }
            }

            AshStopwatch.UsePrecise = preciseDelay.Checked;

            keySender.UseInterception = usingInterception;

            engine.Start(targets, modes, AshConfigPool.Instance.PauseKey, AshConfigPool.Instance.ReleaseDelay);

            if (usingInterception)
            {
                // 驱动模式：接管键盘，被映射的按键不会再传给游戏
                listener = new InterceptionListener(ic, engine);
                listener.Start();
            }
            else
            {
                BuildPollKeys(targets, AshConfigPool.Instance.PauseKey);
            }

            int pressDelay;
            if (!int.TryParse(textBox1.Text, out pressDelay) || pressDelay < 1) pressDelay = 15;
            AshConfigPool.Instance.PressDelay = pressDelay;
            AshTimer.Instance.Delay = pressDelay;
            AshTimer.Instance.Func = TimerTick;
            AshTimer.Instance.Start();

            started = true;
            buttonSwitchEnable.Text = "按键关闭";
            label3.Text = "已开启";
            preciseDelay.Enabled = false;
            buttonAddKey.Enabled = false;
            buttonDeleteKey.Enabled = false;
            buttonToggleHotkey.Enabled = false;
            buttonPauseHotkey.Enabled = false;
            profileTabs.Enabled = false;
            buttonDeleteProfile.Enabled = false;

            _activeSchemes.Clear();
            UpdateMiniStatus();

            // 音效只响应总开关的开启
            if (soundEffect.Checked && _start != null) _start.Play();
        }

        private void StopClicker()
        {
            if (!started)
            {
                keySender.EmergencyReleaseAll();
                return;
            }

            started = false;

            // 先做一次不等锁的紧急抬起，防止界面线程随后等待发送线程。
            keySender.EmergencyReleaseAll();

            // 先停用引擎：拦截线程随后收到的按键会原样放行，不会卡住键盘。
            // engine.Stop() 内部还会补发一次正常抬起。
            engine.Stop();

            AshTimer.Instance.Stop();
            keySender.ReleaseAll();

            if (listener != null)
            {
                listener.Stop();
                listener = null;
            }

            buttonSwitchEnable.Text = "按键开启";
            label3.Text = "关闭";
            preciseDelay.Enabled = true;
            buttonAddKey.Enabled = true;
            buttonDeleteKey.Enabled = true;
            buttonToggleHotkey.Enabled = true;
            buttonPauseHotkey.Enabled = true;
            profileTabs.Enabled = true;
            UpdateDeleteProfileButton();

            _activeSchemes.Clear();
            UpdateMiniStatus();

            // 音效只响应总开关的关闭
            if (soundEffect.Checked && _stop != null) _stop.Play();
        }

        private void Engine_PauseChanged(bool paused)
        {
            if (IsDisposed) return;

            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action<bool>(OnPauseChanged), paused);
                }
                else
                {
                    OnPauseChanged(paused);
                }
            }
            catch (InvalidOperationException)
            {
                // 窗口正在关闭，忽略
            }
        }

        private void OnPauseChanged(bool paused)
        {
            if (!started) return;

            label3.Text = paused ? "已暂停" : "已开启";
            UpdateMiniStatus();
        }

        private void Engine_SchemeStateChanged(int vKey, bool active)
        {
            if (IsDisposed) return;

            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action<int, bool>(OnSchemeStateChanged), vKey, active);
                }
                else
                {
                    OnSchemeStateChanged(vKey, active);
                }
            }
            catch (InvalidOperationException)
            {
                // 窗口正在关闭，忽略
            }
        }

        private void OnSchemeStateChanged(int vKey, bool active)
        {
            _activeSchemes.Remove(vKey);
            if (active)
            {
                _activeSchemes.Add(vKey);
            }

            UpdateMiniStatus();
        }

        private void AshForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                ShowMiniStatus();
            }
            else
            {
                HideMiniStatus();
            }

            UpdateListHeaderDivider();
            LayoutProfileTabs();
        }

        private void ShowMiniStatus()
        {
            if (mini == null || mini.IsDisposed)
            {
                mini = new MiniStatusForm();
                mini.RestoreRequested += (s, e) => RestoreFromMini();
                mini.ExitRequested += (s, e) => Close();

                // 只在第一次显示时放到默认位置，之后记住用户拖动的位置
                mini.MoveToDefaultPosition();
            }

            UpdateMiniStatus();

            if (!mini.Visible)
            {
                if (!IsOnAnyScreen(mini.Bounds))
                {
                    mini.MoveToDefaultPosition();
                }

                mini.Show();
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

        private void HideMiniStatus()
        {
            if (mini != null && !mini.IsDisposed && mini.Visible)
            {
                mini.Hide();
            }
        }

        private void RestoreFromMini()
        {
            WindowState = FormWindowState.Normal;
            Show();
            Activate();
            HideMiniStatus();
        }

        private void UpdateMiniStatus()
        {
            if (mini == null || mini.IsDisposed) return;

            if (!started)
            {
                mini.SetStatus("已关闭", false);
                return;
            }

            var name = CurrentSchemeName();

            if (engine.Paused)
            {
                mini.SetStatus(name ?? "已暂停", false);
                return;
            }

            mini.SetStatus(name ?? "已开启", true);
        }

        private string CurrentSchemeName()
        {
            if (_activeSchemes.Count == 0) return null;

            var vKey = _activeSchemes[_activeSchemes.Count - 1];
            return AshConfigPool.Instance.GetSchemeName(vKey);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Text.Length == 0) textBox1.Text = "1";

            textBox1.Text = new string(textBox1.Text.Where(char.IsDigit).ToArray());
            textBox1.SelectionStart = textBox1.Text.Length;
            AshConfigPool.Instance.PressDelay = Math.Max(int.Parse(textBox1.Text), 1);
            AshTimer.Instance.Delay = AshConfigPool.Instance.PressDelay;
            AshConfigPool.Instance.SaveConfig();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (textBox2.Text.Length == 0) textBox2.Text = "1";

            textBox2.Text = new string(textBox2.Text.Where(char.IsDigit).ToArray());
            textBox2.SelectionStart = textBox2.Text.Length;
            AshConfigPool.Instance.ReleaseDelay = Math.Max(int.Parse(textBox2.Text), 1);
            engine.SetReleaseDelay(AshConfigPool.Instance.ReleaseDelay);
            AshConfigPool.Instance.SaveConfig();
        }

        private void buttonAddKey_Click(object sender, EventArgs e)
        {
            new KeySelectionFrame(listView1, SameKeyRepeatAllowed).Show();
        }

        private void buttonDeleteKey_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem selectedItem in listView1.SelectedItems)
            {
                listView1.Items.Remove(selectedItem);
            }

            AshConfigPool.Instance.UpdateFromList(listView1);
        }

        private bool ValidateHotkeys()
        {
            var toggleKey = AshConfigPool.Instance.ToggleKey;
            var pauseKey = AshConfigPool.Instance.PauseKey;

            if (toggleKey != 0 && toggleKey == pauseKey)
            {
                MessageBox.Show("开关快捷键和暂停键不能是同一个按键。");
                return false;
            }

            if (pauseKey != 0 && AshConfigPool.Instance.IsMappedKey(pauseKey))
            {
                MessageBox.Show("暂停键不能是映射里的「按下的键」，请换一个。");
                return false;
            }

            if (toggleKey != 0 && AshConfigPool.Instance.IsMappedKey(toggleKey))
            {
                MessageBox.Show("开关快捷键不能是映射里的「按下的键」，请换一个。");
                return false;
            }

            return true;
        }

        private void buttonToggleHotkey_Click(object sender, EventArgs e)
        {
            var picked = PickKey(AshConfigPool.Instance.ToggleKey, false);
            if (!picked.HasValue) return;

            var key = picked.Value;

            if (key != 0)
            {
                if (key == AshConfigPool.Instance.PauseKey)
                {
                    MessageBox.Show("开关快捷键不能和暂停键相同。");
                    return;
                }

                if (AshConfigPool.Instance.IsMappedKey(key))
                {
                    MessageBox.Show("这个按键已经是映射里的「按下的键」，会和拦截逻辑冲突，请换一个。");
                    return;
                }
            }

            AshConfigPool.Instance.ToggleKey = key;
            AshConfigPool.Instance.SaveConfig();
            ApplyToggleHotkey();
        }

        private void buttonPauseHotkey_Click(object sender, EventArgs e)
        {
            var picked = PickKey(AshConfigPool.Instance.PauseKey, true);
            if (!picked.HasValue) return;

            var key = picked.Value;

            if (key != 0)
            {
                if (key == AshConfigPool.Instance.ToggleKey)
                {
                    MessageBox.Show("暂停键不能和开关快捷键相同。");
                    return;
                }

                if (AshConfigPool.Instance.IsMappedKey(key))
                {
                    MessageBox.Show("这个按键已经是映射里的「按下的键」，会和拦截逻辑冲突，请换一个。");
                    return;
                }
            }

            AshConfigPool.Instance.PauseKey = key;
            AshConfigPool.Instance.SaveConfig();
            UpdateHotkeyButtons();
        }

        private int? PickKey(int currentKey, bool allowModifiers)
        {
            using (var form = new HotkeyCaptureForm(currentKey, allowModifiers))
            {
                return form.ShowDialog(this) == DialogResult.OK ? form.SelectedKey : (int?)null;
            }
        }

        private void UpdateHotkeyButtons()
        {
            buttonToggleHotkey.Text = DescribeHotkey(AshConfigPool.Instance.ToggleKey);
            buttonPauseHotkey.Text = DescribeHotkey(AshConfigPool.Instance.PauseKey);
        }

        private static string DescribeHotkey(int vKey)
        {
            if (vKey <= 0) return "未设置（点击设置）";

            var key = AshKeyMap.Get(vKey);
            if (key == null) return "未设置（点击设置）";

            // 修饰键单独作为暂停键时，显示成更直观的名字
            switch (vKey)
            {
                case 0xA0:
                case 0xA1:
                    return "Shift（点击修改）";
                case 0xA2:
                case 0xA3:
                    return "Ctrl（点击修改）";
                case 0xA4:
                case 0xA5:
                    return "Alt（点击修改）";
                case 0x5B:
                    return "左 Win（点击修改）";
                case 0x5C:
                    return "右 Win（点击修改）";
                default:
                    return key.ShortName + "（点击修改）";
            }
        }

        /// <summary>注册/重新注册「一键开关」的全局快捷键，这样关闭状态下也能用快捷键开启。</summary>
        private void ApplyToggleHotkey()
        {
            if (!IsHandleCreated) return;

            if (_registeredToggleKey != 0)
            {
                UnregisterHotKey(Handle, HOTKEY_TOGGLE_ID);
                _registeredToggleKey = 0;
            }

            var toggleKey = AshConfigPool.Instance.ToggleKey;
            if (toggleKey > 0)
            {
                if (RegisterHotKey(Handle, HOTKEY_TOGGLE_ID, 0, (Keys)toggleKey))
                {
                    _registeredToggleKey = toggleKey;
                }
                else
                {
                    MessageBox.Show("快捷键「" + AshKeyMap.Get(toggleKey).Description +
                                    "」注册失败，可能已被系统或其他程序占用，请换一个。");
                    AshConfigPool.Instance.ToggleKey = 0;
                    AshConfigPool.Instance.SaveConfig();
                }
            }

            UpdateHotkeyButtons();
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;

            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_TOGGLE_ID)
            {
                ToggleClicker();
                return;
            }

            base.WndProc(ref m);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_registeredToggleKey != 0)
            {
                UnregisterHotKey(Handle, HOTKEY_TOGGLE_ID);
                _registeredToggleKey = 0;
            }

            StopClicker();

            // 即使没有处于“开启”状态，也把可能残留的按下键释放掉。
            keySender.EmergencyReleaseAll();
            keySender.ReleaseAll();

            backgroundTimer.Stop();
            backgroundTimer.Dispose();
            backgroundSkin.Dispose();
            infoToolTip.Dispose();
            profileTabToolTip.Dispose();

            if (_start != null)
            {
                _start.Dispose();
                _start = null;
            }

            if (_stop != null)
            {
                _stop.Dispose();
                _stop = null;
            }

            if (mini != null && !mini.IsDisposed)
            {
                mini.Close();
                mini = null;
            }

            base.OnFormClosed(e);
        }

        private volatile bool usingInterception = true;

        /// <summary>
        /// 同键连发只有在 Interception 驱动模式下才可用。
        /// 无驱动/DD 轮询模式会看到自己注入的键，导致自触发。
        /// </summary>
        private bool SameKeyRepeatAllowed
        {
            get { return usingInterception && ic != null; }
        }

        private void buttonDriverless_Click(object sender, EventArgs e)
        {
            buttonLoadDriver.Text = "无驱动模式";
            buttonLoadDriver.Enabled = false;
            buttonSwitchEnable.Enabled = true;
            usingInterception = true;
        }

        private void buttonInstallInterception_Click(object sender, EventArgs e)
        {
            var dialogResult = MessageBox.Show(
                "即将安装 Interception 驱动。\n\n" +
                "该驱动基于 LGPL 协议发布。\n" +
                "项目地址: https://github.com/oblitum/Interception/\n\n" +
                "此驱动用于模拟键盘和鼠标输入，可能影响输入设备的行为。\n\n" +
                "是否继续？",
                "确认安装",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (dialogResult == DialogResult.No)
            {
                return;
            }

            var result = RunInstaller("/install");
            if (result.Contains("successfully installed"))
            {
                RunScCommand(
                    "sc create keyboard type= kernel start= auto binPath= \"C:\\Windows\\System32\\drivers\\keyboard.sys\"");
                RunScCommand(
                    "sc create mouse type= kernel start= auto binPath= \"C:\\Windows\\System32\\drivers\\mouse.sys\"");
                RunScCommand(
                    "sc start keyboard");
                RunScCommand(
                    "sc start mouse");
                MessageBox.Show(
                    "安装成功，但是你应该需要重启一下电脑才能使用喔！\n\n" +
                    "提示：如果长时间不使用该驱动，建议及时卸载以减少安全隐患。",
                    "安装完成",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else if (result.Contains("Could not write"))
            {
                MessageBox.Show(
                    "文件写入失败，可能是驱动已经安装过。\n\n" +
                    "请尝试重启电脑后重新运行程序，并检查按键功能是否正常开启。",
                    "安装失败",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            else
            {
                MessageBox.Show(
                    "操作失败，详细信息如下：\n\n" + result,
                    "错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        static string RunInstaller(string arguments)
        {
            // 创建临时文件来存储 Properties.Resources.installer
            string tempInstallerPath = Path.Combine(Path.GetTempPath(), "installer.exe");
            File.WriteAllBytes(tempInstallerPath, Properties.Resources.installer);

            // 配置进程启动信息
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = tempInstallerPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Verb = "runas" // 以管理员权限运行
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                // 捕获输出
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // 删除临时文件
                if (File.Exists(tempInstallerPath))
                {
                    File.Delete(tempInstallerPath);
                }

                // 返回最后一行的输出
                if (!string.IsNullOrEmpty(output))
                {
                    string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    return lines[lines.Length - 1]; // 返回最后一行
                }
                else
                {
                    throw new Exception("No output received from installer.");
                }
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            CreateRegistry("keyboard");
            CreateRegistry("mouse");
            var result = RunInstaller("/uninstall");
            if (result.Contains("uninstalled"))
            {
                MessageBox.Show(
                    "卸载成功！请重启电脑以完成卸载过程。\n\n" +
                    "提示：如果需要重新安装驱动，请重启后再运行。",
                    "卸载完成",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(
                    "操作失败，详细信息如下：\n\n" + result,
                    "错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        static void CreateRegistry(string serviceName)
        {
            try
            {
                using (RegistryKey servicesKey =
                       Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Services", true))
                {
                    if (servicesKey == null)
                    {
                        throw new Exception("Failed to open Services registry key.");
                    }

                    // 创建主项
                    using (RegistryKey serviceKey = servicesKey.CreateSubKey(serviceName))
                    {
                        if (serviceKey == null)
                        {
                            throw new Exception($"Failed to create {serviceName} registry key.");
                        }

                        // 可选：创建 Security 和 Enum 子项
                        serviceKey.CreateSubKey("Security")?.Close();
                        serviceKey.CreateSubKey("Enum")?.Close();

                        Console.WriteLine($"{serviceName} registry recreated successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error recreating {serviceName} registry: {ex.Message}");
            }
        }
    }
}
