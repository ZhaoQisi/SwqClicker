using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json; // 引入 Newtonsoft.Json

namespace AshClicker
{
    public class AshConfigPool
    {
        private static readonly Lazy<AshConfigPool> _instance = new Lazy<AshConfigPool>(() => new AshConfigPool());
        public static AshConfigPool Instance => _instance.Value;

        private AshConfigPool()
        {
        }

        Dictionary<int, int> KeyMap { get; set; } = new Dictionary<int, int>();
        Dictionary<int, int> ModeMap { get; set; } = new Dictionary<int, int>();

        /// <summary>按下的键 -> 方案名（自定义键位1…）。</summary>
        Dictionary<int, string> NameMap { get; set; } = new Dictionary<int, string>();

        public int PressDelay { get; set; }
        public int ReleaseDelay { get; set; }

        /// <summary>一键开启/关闭连发功能的全局快捷键，0 表示未设置。</summary>
        public int ToggleKey { get; set; }

        /// <summary>暂停键，0 表示未设置。</summary>
        public int PauseKey { get; set; }

        /// <summary>获取「按下的键 -> 连发的键」与「按下的键 -> 模式」的副本（都是 VKey）。</summary>
        public void GetMaps(out Dictionary<int, int> outKeyMap, out Dictionary<int, int> outModeMap)
        {
            outKeyMap = new Dictionary<int, int>(KeyMap);
            outModeMap = new Dictionary<int, int>(ModeMap);
        }

        /// <summary>某个按键是否已经被配置成映射里的「按下的键」。</summary>
        public bool IsMappedKey(int vKey)
        {
            return KeyMap.ContainsKey(vKey);
        }

        public string GetSchemeName(int vKey)
        {
            string name;
            return NameMap.TryGetValue(vKey, out name) && !string.IsNullOrEmpty(name) ? name : "自定义键位";
        }

        /// <summary>下一个默认方案名：自定义键位1、自定义键位2……跳过已占用的名字。</summary>
        public string NextDefaultSchemeName()
        {
            for (var i = 1; i < 10000; i++)
            {
                var candidate = DefaultName(i);
                var used = false;
                foreach (var name in NameMap.Values)
                {
                    if (name == candidate)
                    {
                        used = true;
                        break;
                    }
                }

                if (!used) return candidate;
            }

            return "自定义键位";
        }

        /// <summary>默认方案名，控制在 6 个字符以内（10 个以后换用更短的前缀）。</summary>
        private static string DefaultName(int index)
        {
            return index <= 9 ? "自定义键位" + index : "自定键位" + index;
        }

        public void LoadToList(ListView listView)
        {
            LoadConfig();

            foreach (var key in KeyMap.Keys)
            {
                var userPress = AshKeyMap.Get(key);
                var willPress = AshKeyMap.Get(KeyMap[key]);
                var mode = ModeMap.GetValueOrDefault(key, 0);
                var name = GetSchemeName(key);
                listView.Items.Add(AshUtil.CreateKeyMappingItem(name, userPress, willPress, mode));
            }
        }

        public void UpdateFromList(ListView listView)
        {
            KeyMap.Clear();
            ModeMap.Clear();
            NameMap.Clear();
            var invalid = new HashSet<ListViewItem>();

            foreach (ListViewItem item in listView.Items)
            {
                if (item.Tag is AshKeypress press)
                {
                    KeyMap[press.UserPress.VKey] = press.WillPress.VKey;
                    ModeMap[press.UserPress.VKey] = press.Mode;
                    NameMap[press.UserPress.VKey] = press.Name == null ? string.Empty : press.Name.Trim();
                }
                else
                {
                    invalid.Add(item);
                }
            }

            if (invalid.Count > 0)
            {
                MessageBox.Show("检测到错误配置，已删除错误的条目:(");
            }

            foreach (var listViewItem in invalid)
            {
                listView.Items.Remove(listViewItem);
            }

            SaveConfig();
        }

        private static readonly string ConfigFilePath = "AshConfig.json";

        public void SaveConfig()
        {
            try
            {
                var configData = new ConfigData
                {
                    KeyMap = KeyMap,
                    ModeMap = ModeMap,
                    NameMap = NameMap,
                    PressDelay = PressDelay,
                    ReleaseDelay = ReleaseDelay,
                    ToggleKey = ToggleKey,
                    PauseKey = PauseKey
                };
                var json = JsonConvert.SerializeObject(configData, Formatting.Indented);
                File.WriteAllText(ConfigFilePath, json);
                Console.WriteLine($"Configuration saved to {ConfigFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving configuration: {ex.Message}");
            }
        }

        public void LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    var configData = JsonConvert.DeserializeObject<ConfigData>(json);

                    if (configData != null)
                    {
                        KeyMap = configData.KeyMap ?? new Dictionary<int, int>();
                        ModeMap = configData.ModeMap ?? new Dictionary<int, int>();
                        NameMap = configData.NameMap ?? new Dictionary<int, string>();
                        PressDelay = configData.PressDelay;
                        ReleaseDelay = configData.ReleaseDelay;
                        ToggleKey = configData.ToggleKey;
                        PauseKey = configData.PauseKey;
                        EnsureNames();
                        Console.WriteLine("Configuration loaded successfully.");
                    }
                }
                else
                {
                    Console.WriteLine("Configuration file not found. Using default settings.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");
            }
        }

        /// <summary>老版本配置没有方案名，按顺序补上「自定义键位N」。</summary>
        private void EnsureNames()
        {
            var used = new HashSet<string>();
            foreach (var name in NameMap.Values)
            {
                if (!string.IsNullOrEmpty(name)) used.Add(name);
            }

            var next = 1;
            foreach (var key in KeyMap.Keys)
            {
                string name;
                if (NameMap.TryGetValue(key, out name) && !string.IsNullOrEmpty(name)) continue;

                while (used.Contains(DefaultName(next))) next++;
                name = DefaultName(next);
                NameMap[key] = name;
                used.Add(name);
                next++;
            }
        }

        private class ConfigData
        {
            public Dictionary<int, int> KeyMap { get; set; }
            public Dictionary<int, int> ModeMap { get; set; }
            public Dictionary<int, string> NameMap { get; set; }

            public int PressDelay { get; set; }
            public int ReleaseDelay { get; set; }

            public int ToggleKey { get; set; }
            public int PauseKey { get; set; }
        }
    }
}
