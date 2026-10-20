using System;
using System.Runtime.InteropServices;

namespace AshClicker
{
    public class AshKey
    {
        public string KeyName { get; }

        /// <summary>界面显示的短名称，例如 1、Q、Num1、F11。</summary>
        public string ShortName { get; }

        public int VKey { get; }
        public int DdCode { get; }
        public int ScanCode { get; }
        public string Description { get; }

        [DllImport("user32.dll")]
        static extern uint MapVirtualKey(uint uCode, uint uMapType);

        public AshKey(string keyName, int vKey, int ddCode, string description)
        {
            KeyName = keyName;
            VKey = vKey;
            DdCode = ddCode;
            // MapVirtualKey 对 PrintScreen 返回的扫描码（0x54）和真实键盘（E0 0x37）不一致
            ScanCode = vKey == 0x2C ? 0x37 : (int)MapVirtualKey((uint)vKey, 0);
            Description = description;
            ShortName = BuildShortName(keyName);
        }

        private static string BuildShortName(string keyName)
        {
            if (keyName == null) return string.Empty;

            // 小键盘统一成 Num1 / Num+ 这种写法
            if (keyName.StartsWith("NUM", StringComparison.Ordinal))
            {
                return "Num" + keyName.Substring(3);
            }

            switch (keyName)
            {
                case "ESC": return "Esc";
                case "TAB": return "Tab";
                case "BACKSPACE": return "BkSp";
                case "ENTER": return "Enter";
                case "SPACE": return "Space";
                case "CAPS": return "Caps";
                case "LSHIFT": return "LShift";
                case "RSHIFT": return "RShift";
                case "LCTRL": return "LCtrl";
                case "RCTRL": return "RCtrl";
                case "LALT": return "LAlt";
                case "RALT": return "RAlt";
                case "WIN": return "Win";
                case "RWIN": return "RWin";
                case "PRINT": return "PrtSc";
                case "SCROLL": return "ScrLk";
                case "PAUSE": return "Pause";
                case "INS": return "Ins";
                case "DEL": return "Del";
                case "HOME": return "Home";
                case "END": return "End";
                case "PGUP": return "PgUp";
                case "PGDN": return "PgDn";
                case "UP": return "Up";
                case "DOWN": return "Down";
                case "LEFT": return "Left";
                case "RIGHT": return "Right";
                case "FN": return "(添加)";
            }

            // 字母统一大写，数字和符号保持原样
            if (keyName.Length == 1 && char.IsLetter(keyName[0]))
            {
                return keyName.ToUpperInvariant();
            }

            return keyName;
        }
    }
}
