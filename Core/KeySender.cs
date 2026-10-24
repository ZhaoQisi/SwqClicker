using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace AshClicker
{
    /// <summary>
    /// 真正把按键送进系统的实现：Interception 驱动模式走驱动注入，DD 模式走 DD 驱动。
    ///
    /// 所有按下的输出键都会被记录，并在停止、退出或异常时补发抬起，
    /// 避免出现 Q 一直按着、只能重启电脑的情况。
    /// </summary>
    public class KeySender : IKeySender
    {
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const uint KeyEventKeyUp = 0x0002;

        private readonly object _sendSync = new object();
        private readonly ConcurrentDictionary<int, byte> _pressed = new ConcurrentDictionary<int, byte>();

        // 供进程退出/未处理异常时做最后兜底，不依赖具体 KeySender 实例。
        private static readonly ConcurrentDictionary<int, byte> GlobalPressed =
            new ConcurrentDictionary<int, byte>();

        public InterceptionControl Interception { get; set; }

        internal DdControl Dd { get; set; }

        public bool UseInterception { get; set; } = true;

        public void Tap(int vKey, int holdMilliseconds)
        {
            var key = AshKeyMap.Get(vKey);
            if (key == null) return;

            var hold = Math.Max(holdMilliseconds, 1);
            var downSent = false;

            lock (_sendSync)
            {
                try
                {
                    downSent = Send(vKey, key, true);
                    if (!downSent)
                    {
                        Log("Send down failed: " + vKey);
                        return;
                    }

                    _pressed[vKey] = 0;
                    GlobalPressed[vKey] = 0;

                    AshStopwatch.PreciseDelay(hold);
                }
                catch (Exception ex)
                {
                    Log("Tap failed: " + vKey + " " + ex.Message);
                }
                finally
                {
                    if (downSent)
                    {
                        if (TrySendUp(vKey, key))
                        {
                            _pressed.TryRemove(vKey, out _);
                            GlobalPressed.TryRemove(vKey, out _);
                        }
                        else
                        {
                            // 驱动发送抬起失败时，用系统 keybd_event 再补一次；
                            // 同时保留记录，停止/退出时还会再次尝试。
                            EmergencyKeyUp(vKey);
                            Log("Key left pressed after failed up: " + vKey);
                        }
                    }
                }
            }
        }

        /// <summary>停止/退出时补发所有已按下但未抬起的输出键。</summary>
        public void ReleaseAll()
        {
            // 发送线程可能卡在驱动调用里，不能让界面在关闭时无限等待。
            if (!Monitor.TryEnter(_sendSync, 1000))
            {
                EmergencyReleaseAll();
                return;
            }

            try
            {
                foreach (var vKey in _pressed.Keys)
                {
                    var key = AshKeyMap.Get(vKey);

                    if (key != null && TrySendUp(vKey, key))
                    {
                        _pressed.TryRemove(vKey, out _);
                        GlobalPressed.TryRemove(vKey, out _);
                    }
                    else
                    {
                        EmergencyKeyUp(vKey);
                        Log("Key still pressed after release: " + vKey);
                    }
                }
            }
            finally
            {
                Monitor.Exit(_sendSync);
            }
        }

        /// <summary>
        /// 不等待发送锁的紧急抬起，用于界面卡住或发送线程挂起时。
        /// </summary>
        public void EmergencyReleaseAll()
        {
            foreach (var vKey in _pressed.Keys)
            {
                EmergencyKeyUp(vKey);
            }
        }

        /// <summary>进程退出/未处理异常时的全局兜底。</summary>
        public static void EmergencyReleaseAllStatic()
        {
            foreach (var vKey in GlobalPressed.Keys)
            {
                EmergencyKeyUp(vKey);
            }

            GlobalPressed.Clear();
        }

        private bool Send(int vKey, AshKey key, bool down)
        {
            if (UseInterception && Interception != null)
            {
                var extended = AshKeyMap.IsExtendedKey(vKey);
                var state = (ushort)(extended ? 0x02 : 0x00); // bit1 = E0 前缀
                if (!down)
                {
                    state |= 0x01; // bit0 = 抬起
                }

                var stroke = new InterceptionControl.Stroke
                {
                    Key = new InterceptionControl.KeyStroke
                    {
                        Code = (ushort)key.ScanCode,
                        State = state
                    }
                };

                return Interception.Send(1, ref stroke) > 0;
            }

            if (Dd != null)
            {
                Dd.key(key.DdCode, down ? 1 : 2);
                return true;
            }

            return false;
        }

        private bool TrySendUp(int vKey, AshKey key)
        {
            for (var i = 0; i < 3; i++)
            {
                try
                {
                    if (Send(vKey, key, false))
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Log("Send up failed: " + vKey + " " + ex.Message);
                }

                Thread.Sleep(2);
            }

            return false;
        }

        private static void EmergencyKeyUp(int vKey)
        {
            try
            {
                keybd_event((byte)vKey, 0, KeyEventKeyUp, UIntPtr.Zero);
            }
            catch (Exception ex)
            {
                Log("Emergency key up failed: " + vKey + " " + ex.Message);
            }
        }

        private static void Log(string message)
        {
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
                File.AppendAllText(path, "[" + DateTime.Now + "] " + message + Environment.NewLine);
            }
            catch (Exception)
            {
                // 日志失败不能影响按键释放
            }
        }
    }
}