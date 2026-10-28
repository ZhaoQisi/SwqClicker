using System.Collections.Generic;

namespace AshClicker
{
    /// <summary>
    /// 一套独立的键位配置。连发间隔、开关快捷键和暂停键仍然是全局设置。
    /// </summary>
    public class AshProfile
    {
        public string Name { get; set; } = "默认配置";

        public Dictionary<int, int> KeyMap { get; set; } = new Dictionary<int, int>();

        public Dictionary<int, int> ModeMap { get; set; } = new Dictionary<int, int>();

        /// <summary>按下的键 -> 方案名（自定义键位1…）。</summary>
        public Dictionary<int, string> NameMap { get; set; } = new Dictionary<int, string>();
    }
}