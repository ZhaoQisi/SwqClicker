using System;

namespace AshClicker
{
    /// <summary>
    /// 真正把按键送进系统的实现：Interception 驱动模式走驱动注入，DD 模式走 DD 驱动。
    /// </summary>
    public class KeySender : IKeySender
    {
        public InterceptionControl Interception { get; set; }

        internal DdControl Dd { get; set; }

        public bool UseInterception { get; set; } = true;

        public void Tap(int vKey, int holdMilliseconds)
        {
            var key = AshKeyMap.Get(vKey);
            if (key == null) return;

            var hold = Math.Max(holdMilliseconds, 1);

            if (UseInterception && Interception != null)
            {
                var extended = AshKeyMap.IsExtendedKey(vKey);
                var downState = (ushort)(extended ? 0x02 : 0x00); // bit1 = E0 前缀

                var stroke = new InterceptionControl.Stroke
                {
                    Key = new InterceptionControl.KeyStroke
                    {
                        Code = (ushort)key.ScanCode,
                        State = downState
                    }
                };

                Interception.Send(1, ref stroke);
                AshStopwatch.PreciseDelay(hold);
                stroke.Key.State = (ushort)(downState | 0x01);
                Interception.Send(1, ref stroke);
            }
            else if (Dd != null)
            {
                Dd.key(key.DdCode, 1);
                AshStopwatch.PreciseDelay(hold);
                Dd.key(key.DdCode, 2);
            }
        }
    }
}
