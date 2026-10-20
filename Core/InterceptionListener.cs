using System;
using System.IO;
using System.Threading;

namespace AshClicker
{
    /// <summary>
    /// 通过 Interception 驱动接管键盘输入：
    /// 单线程等待任意键盘设备的输入，收到按键后先问 <see cref="ClickerEngine"/> 要不要吞掉，
    /// 不吞的按键原样写回驱动，继续交给系统和游戏。
    ///
    /// 这样被映射的「按下的键」不会传给游戏，只有映射后的按键会发出去。
    /// 鼠标不参与拦截，避免影响指针。
    /// </summary>
    public class InterceptionListener
    {
        private const int IdleWaitMs = 100;
        private const int DrainWaitMs = 30;
        private const int DrainIdleWaits = 3;

        private readonly InterceptionControl _ic;
        private readonly ClickerEngine _engine;
        private Thread _thread;
        private volatile bool _running;

        public InterceptionListener(InterceptionControl ic, ClickerEngine engine)
        {
            if (ic == null) throw new ArgumentNullException("ic");
            if (engine == null) throw new ArgumentNullException("engine");

            _ic = ic;
            _engine = engine;
        }

        public bool IsRunning
        {
            get { return _running; }
        }

        public void Start()
        {
            if (_running) return;

            _running = true;

            _thread = new Thread(Loop)
            {
                IsBackground = true,
                Name = "interception-keyboard"
            };
            _thread.Start();

            // 先开线程再开过滤，避免开启瞬间的按键堵在驱动队列里
            _ic.SetKeyboardFilter(InterceptionControl.FilterKeyAll);
            _ic.SetMouseFilter(InterceptionControl.FilterMouseNone);
        }

        public void Stop()
        {
            _running = false;

            var thread = _thread;
            _thread = null;

            if (thread != null)
            {
                try
                {
                    thread.Join(3000);
                }
                catch (Exception)
                {
                    // 线程已经退出，忽略
                }
            }

            // 兜底：确保键盘不再被过滤，否则会表现为键盘无响应
            _ic.SetKeyboardFilter(InterceptionControl.FilterKeyNone);
        }

        private void Loop()
        {
            var stroke = new InterceptionControl.Stroke();
            var idle = 0;

            while (_running || idle < DrainIdleWaits)
            {
                int device;
                try
                {
                    device = _ic.WaitWithTimeout((uint)(_running ? IdleWaitMs : DrainWaitMs));
                }
                catch (Exception ex)
                {
                    Log("Wait failed: " + ex.Message);
                    break;
                }

                if (device <= 0)
                {
                    if (!_running) idle++;
                    continue;
                }

                idle = 0;

                int count;
                try
                {
                    count = _ic.Receive(device, ref stroke);
                }
                catch (Exception ex)
                {
                    Log("Receive failed: " + ex.Message);
                    break;
                }

                if (count > 0)
                {
                    Deliver(device, ref stroke);
                }
            }

            // 停止接收前先关过滤，之后到来的按键直接透传
            _ic.SetKeyboardFilter(InterceptionControl.FilterKeyNone);

            // 把队列里剩下的几笔也放掉，避免残留按键堵住键盘
            for (var i = 0; i < 5; i++)
            {
                int device;
                try
                {
                    device = _ic.WaitWithTimeout(10);
                }
                catch (Exception)
                {
                    break;
                }

                if (device <= 0) continue;

                int count;
                try
                {
                    count = _ic.Receive(device, ref stroke);
                }
                catch (Exception)
                {
                    break;
                }

                if (count > 0)
                {
                    Deliver(device, ref stroke);
                }
            }
        }

        private void Deliver(int device, ref InterceptionControl.Stroke stroke)
        {
            if (!_ic.IsKeyboard(device))
            {
                // 鼠标理论上不会被拦截，保险起见原样放行
                _ic.Send(device, ref stroke);
                return;
            }

            var state = stroke.Key.State;
            var isDown = (state & 0x01) == 0;

            var vKey = AshKeyMap.VKeyFromScanCode(stroke.Key.Code, state);

            var swallow = false;
            if (vKey > 0)
            {
                swallow = isDown ? _engine.HandleKeyDown(vKey) : _engine.HandleKeyUp(vKey);
            }

            if (!swallow)
            {
                _ic.Send(device, ref stroke);
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
                // 日志失败不影响按键功能
            }
        }
    }
}
