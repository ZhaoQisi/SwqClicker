using System;
using System.Collections.Generic;

namespace AshClicker
{
    /// <summary>
    /// 发送按键的抽象，便于在驱动模式和轮询模式之间切换，也便于单独测试状态机。
    /// </summary>
    public interface IKeySender
    {
        /// <summary>按下并抬起一个按键，按住时长由 holdMilliseconds 决定。</summary>
        void Tap(int vKey, int holdMilliseconds);
    }

    /// <summary>
    /// 连发状态机。
    ///
    /// 所有物理按键事件（来自 Interception 驱动拦截，或 DD 模式的轮询）都先交给这里，
    /// 由它决定这个事件是否要「吞掉」，并在连发循环里发送映射后的按键。
    ///
    /// 状态约定：
    ///   - 长按连发（mode 0）：物理键按住期间持续发送映射键，松开即停；
    ///   - 按键切换（mode 1）：按一下开始持续发送，再按一下停止；
    ///   - 暂停：按住暂停键期间停止一切发送，期间按键原样放行给系统；
    ///           松开暂停键立刻恢复，之前处于连发/切换状态的键会继续。
    ///           暂停键本身是修饰键（Alt/Ctrl/Shift/Win）时直接放行，避免 Alt+Tab 之类的组合失效。
    /// </summary>
    public class ClickerEngine
    {
        private readonly object _sync = new object();
        private readonly IKeySender _sender;

        private readonly Dictionary<int, int> _targets = new Dictionary<int, int>(); // 触发键 -> 连发的键
        private readonly Dictionary<int, int> _modes = new Dictionary<int, int>();   // 触发键 -> 模式

        private readonly HashSet<int> _held = new HashSet<int>();      // 长按模式：正在重复的键
        private readonly HashSet<int> _latched = new HashSet<int>();   // 切换模式：已开启的键
        private readonly HashSet<int> _down = new HashSet<int>();      // 物理上按住的键，用于过滤系统自动重复
        private readonly HashSet<int> _swallowed = new HashSet<int>(); // 按下已被吞掉的键，抬起也要吞掉

        private volatile bool _enabled;
        private volatile bool _paused;

        private int _pauseVKey;
        private int _releaseDelay = 5;
        private bool _pauseKeyDown;
        private bool _pauseKeyPassThrough;

        public ClickerEngine(IKeySender sender)
        {
            if (sender == null) throw new ArgumentNullException("sender");
            _sender = sender;
        }

        public bool Enabled
        {
            get { return _enabled; }
        }

        public bool Paused
        {
            get { return _paused; }
        }

        /// <summary>暂停状态变化（可能来自后台拦截线程），订阅方需要自行切回 UI 线程。</summary>
        public event Action<bool> PauseChanged;

        /// <summary>某个方案的按键开始/停止连发时触发（参数：按下的键、是否激活）。</summary>
        public event Action<int, bool> SchemeStateChanged;

        public void Start(IDictionary<int, int> targets, IDictionary<int, int> modes, int pauseVKey,
            int releaseDelayMilliseconds)
        {
            if (targets == null) throw new ArgumentNullException("targets");

            lock (_sync)
            {
                _targets.Clear();
                _modes.Clear();

                foreach (var pair in targets)
                {
                    _targets[pair.Key] = pair.Value;
                }

                if (modes != null)
                {
                    foreach (var pair in modes)
                    {
                        _modes[pair.Key] = pair.Value;
                    }
                }

                _pauseVKey = pauseVKey;
                _pauseKeyPassThrough = IsModifierVKey(pauseVKey);
                _releaseDelay = Math.Max(releaseDelayMilliseconds, 1);

                ResetStateLocked();

                _enabled = true;
            }
        }

        public void Stop()
        {
            bool wasPaused;

            lock (_sync)
            {
                wasPaused = _paused;
                _enabled = false;
                ResetStateLocked();
                _paused = false;
            }

            if (wasPaused && PauseChanged != null)
            {
                PauseChanged(false);
            }
        }

        /// <summary>运行中调整「抬起间隔」时用。</summary>
        public void SetReleaseDelay(int milliseconds)
        {
            lock (_sync)
            {
                _releaseDelay = Math.Max(milliseconds, 1);
            }
        }

        /// <summary>
        /// 处理物理按键按下，返回 true 表示这个事件被吞掉，不应该再传给系统。
        /// </summary>
        public bool HandleKeyDown(int vKey)
        {
            bool pauseChanged = false;
            bool paused = false;
            int activated = 0;
            int deactivated = 0;
            bool swallow;

            lock (_sync)
            {
                if (!_enabled) return false;

                if (IsPauseKey(vKey))
                {
                    // 修饰键做暂停键时放行，保证 Alt+Tab / Ctrl+C 这类组合还能用
                    swallow = !_pauseKeyPassThrough;

                    if (!_pauseKeyDown)
                    {
                        _pauseKeyDown = true;

                        if (!_paused)
                        {
                            _paused = true;
                            paused = true;
                            pauseChanged = true;
                        }
                    }
                }
                else if (_paused)
                {
                    // 暂停期间按键直接放行；但暂停前已经被吞掉的键（例如正按着的触发键）
                    // 继续保持吞掉，避免给系统发一堆没有按下事件的重复键
                    swallow = _swallowed.Contains(vKey);
                }
                else if (_targets.ContainsKey(vKey))
                {
                    swallow = true;
                    _swallowed.Add(vKey);

                    // 系统自动重复会产生连续的按下事件，这里只处理第一次
                    if (_down.Add(vKey))
                    {
                        if (_modes.GetValueOrDefault(vKey, 0) == 1)
                        {
                            if (_latched.Remove(vKey))
                            {
                                deactivated = vKey;
                            }
                            else
                            {
                                _latched.Add(vKey);
                                activated = vKey;
                            }
                        }
                        else
                        {
                            _held.Add(vKey);
                            activated = vKey;
                        }
                    }
                }
                else
                {
                    swallow = false;
                }
            }

            if (pauseChanged && PauseChanged != null)
            {
                PauseChanged(paused);
            }

            if (activated != 0 && SchemeStateChanged != null)
            {
                SchemeStateChanged(activated, true);
            }

            if (deactivated != 0 && SchemeStateChanged != null)
            {
                SchemeStateChanged(deactivated, false);
            }

            return swallow;
        }

        /// <summary>
        /// 处理物理按键抬起，返回 true 表示这个事件被吞掉。
        /// </summary>
        public bool HandleKeyUp(int vKey)
        {
            bool pauseChanged = false;
            bool swallow;
            int deactivated = 0;

            lock (_sync)
            {
                if (!_enabled) return false;

                if (IsPauseKey(vKey))
                {
                    swallow = !_pauseKeyPassThrough;
                    _pauseKeyDown = false;

                    if (_paused)
                    {
                        _paused = false;
                        pauseChanged = true;
                    }
                }
                else
                {
                    // 暂停期间松开触发键，也要把它从连发列表里摘掉，恢复后不会继续发
                    _down.Remove(vKey);
                    if (_held.Remove(vKey))
                    {
                        deactivated = vKey;
                    }

                    swallow = _swallowed.Remove(vKey);
                }
            }

            if (pauseChanged && PauseChanged != null)
            {
                PauseChanged(false);
            }

            if (deactivated != 0 && SchemeStateChanged != null)
            {
                SchemeStateChanged(deactivated, false);
            }

            return swallow;
        }

        /// <summary>
        /// 连发循环里调用：给所有处于活动状态的映射发送一次按键。
        /// </summary>
        public void Tick()
        {
            if (!_enabled || _paused) return;

            var targets = new List<int>();

            lock (_sync)
            {
                if (!_enabled || _paused) return;
                if (_held.Count == 0 && _latched.Count == 0) return;

                foreach (var vKey in _held)
                {
                    int target;
                    if (_targets.TryGetValue(vKey, out target) && !targets.Contains(target))
                    {
                        targets.Add(target);
                    }
                }

                foreach (var vKey in _latched)
                {
                    int target;
                    if (_targets.TryGetValue(vKey, out target) && !targets.Contains(target))
                    {
                        targets.Add(target);
                    }
                }
            }

            var releaseDelay = _releaseDelay;

            foreach (var target in targets)
            {
                if (!_enabled || _paused) return;

                _sender.Tap(target, releaseDelay);
            }
        }

        /// <summary>
        /// 判断某个物理键是不是暂停键。用左/右修饰键设置暂停键时，两个都认。
        /// </summary>
        private bool IsPauseKey(int vKey)
        {
            if (_pauseVKey == 0) return false;
            if (vKey == _pauseVKey) return true;

            switch (_pauseVKey)
            {
                case 0xA0:
                case 0xA1:
                    return vKey == 0xA0 || vKey == 0xA1; // Shift
                case 0xA2:
                case 0xA3:
                    return vKey == 0xA2 || vKey == 0xA3; // Ctrl
                case 0xA4:
                case 0xA5:
                    return vKey == 0xA4 || vKey == 0xA5; // Alt
                default:
                    return false;
            }
        }

        /// <summary>修饰键做暂停键时直接放行，避免 Alt+Tab 之类被吞掉。</summary>
        private static bool IsModifierVKey(int vKey)
        {
            switch (vKey)
            {
                case 0x10: // VK_SHIFT
                case 0x11: // VK_CONTROL
                case 0x12: // VK_MENU
                case 0xA0: // LSHIFT
                case 0xA1: // RSHIFT
                case 0xA2: // LCONTROL
                case 0xA3: // RCONTROL
                case 0xA4: // LMENU
                case 0xA5: // RMENU
                case 0x5B: // LWIN
                case 0x5C: // RWIN
                    return true;
                default:
                    return false;
            }
        }

        private void ResetStateLocked()
        {
            _held.Clear();
            _latched.Clear();
            _down.Clear();
            _swallowed.Clear();
            _pauseKeyDown = false;
        }
    }
}
