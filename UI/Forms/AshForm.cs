using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
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

        public AshForm()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Console.WriteLine($"Version: {version}");

            var infoVersion = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

            Console.WriteLine($"Informational Version: {infoVersion}");
            InitializeComponent();

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
        /// 背景按 cover 方式铺满窗口：等比放大到覆盖整个客户区，超出部分裁掉。
        /// 这样不会像 Zoom 那样在上下或左右留下窗体背景色。
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

            var target = ClientRectangle;
            if (target.Width <= 0 || target.Height <= 0) return;

            var scale = Math.Max(
                (float)target.Width / image.Width,
                (float)target.Height / image.Height);

            var width = image.Width * scale;
            var height = image.Height * scale;
            var left = (target.Width - width) / 2f;
            var top = (target.Height - height) / 2f;

            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.DrawImage(image, left, top, width, height);
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


            AshConfigPool.Instance.LoadToList(listView1);
            FitListColumns();
            listView1.ClientSizeChanged += (cs, ce) => FitListColumns();

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

            titleBar.BackColor = hasImage
                ? Color.Transparent
                : Color.FromArgb(250, 250, 250);

            SetTransparentLabels(this);
        }

        private static void SetTransparentLabels(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                if (child is Label || child is CheckBox)
                {
                    child.BackColor = Color.Transparent;
                }

                if (child.HasChildren)
                {
                    SetTransparentLabels(child);
                }
            }
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

            _activeSchemes.Clear();
            UpdateMiniStatus();

            // 音效只响应总开关的开启
            if (soundEffect.Checked && _start != null) _start.Play();
        }

        private void StopClicker()
        {
            if (!started) return;

            started = false;

            // 先停用引擎：拦截线程随后收到的按键会原样放行，不会卡住键盘
            engine.Stop();

            AshTimer.Instance.Stop();

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

            backgroundTimer.Stop();
            backgroundTimer.Dispose();
            backgroundSkin.Dispose();

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
