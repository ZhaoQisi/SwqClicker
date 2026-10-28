using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace AshClicker
{
    public class AshConfigPool
    {
        private static readonly Lazy<AshConfigPool> _instance = new Lazy<AshConfigPool>(() => new AshConfigPool());
        public static AshConfigPool Instance => _instance.Value;

        private AshConfigPool()
        {
        }

        private List<AshProfile> _profiles = new List<AshProfile>();
        private int _activeProfile;

        /// <summary>所有配置，默认至少有一套。</summary>
        public IReadOnlyList<AshProfile> Profiles
        {
            get
            {
                EnsureProfiles();
                return _profiles;
            }
        }

        public int ActiveProfileIndex
        {
            get
            {
                EnsureProfiles();
                return _activeProfile;
            }
        }

        public AshProfile ActiveProfile
        {
            get
            {
                EnsureProfiles();
                return _profiles[_activeProfile];
            }
        }

        private Dictionary<int, int> KeyMap => ActiveProfile.KeyMap;
        private Dictionary<int, int> ModeMap => ActiveProfile.ModeMap;
        private Dictionary<int, string> NameMap => ActiveProfile.NameMap;

        public int PressDelay { get; set; }
        public int ReleaseDelay { get; set; }

        /// <summary>一键开启/关闭连发功能的全局快捷键，0 表示未设置。</summary>
        public int ToggleKey { get; set; }

        /// <summary>暂停键，0 表示未设置。</summary>
        public int PauseKey { get; set; }

        /// <summary>获取当前配置「按下的键 -> 连发的键」与「按下的键 -> 模式」的副本。</summary>
        public void GetMaps(out Dictionary<int, int> outKeyMap, out Dictionary<int, int> outModeMap)
        {
            EnsureProfiles();
            outKeyMap = new Dictionary<int, int>(KeyMap);
            outModeMap = new Dictionary<int, int>(ModeMap);
        }

        /// <summary>某个按键是否已经被配置成当前配置里的「按下的键」。</summary>
        public bool IsMappedKey(int vKey)
        {
            EnsureProfiles();
            return KeyMap.ContainsKey(vKey);
        }

        public string GetSchemeName(int vKey)
        {
            EnsureProfiles();
            string name;
            return NameMap.TryGetValue(vKey, out name) && !string.IsNullOrEmpty(name) ? name : "自定义键位";
        }

        /// <summary>下一个默认方案名：自定义键位1、自定义键位2……跳过已占用的名字。</summary>
        public string NextDefaultSchemeName()
        {
            EnsureProfiles();
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

        /// <summary>把当前配置加载到列表。</summary>
        public void LoadToList(ListView listView)
        {
            EnsureProfiles();
            listView.Items.Clear();

            foreach (var key in KeyMap.Keys)
            {
                var userPress = AshKeyMap.Get(key);
                var willPress = AshKeyMap.Get(KeyMap[key]);
                if (userPress == null || willPress == null) continue;

                var mode = ModeMap.GetValueOrDefault(key, 0);
                var name = GetSchemeName(key);
                listView.Items.Add(AshUtil.CreateKeyMappingItem(name, userPress, willPress, mode));
            }
        }

        /// <summary>把列表内容保存到当前配置。</summary>
        public void UpdateFromList(ListView listView)
        {
            EnsureProfiles();
            var profile = ActiveProfile;
            profile.KeyMap.Clear();
            profile.ModeMap.Clear();
            profile.NameMap.Clear();
            var invalid = new HashSet<ListViewItem>();

            foreach (ListViewItem item in listView.Items)
            {
                if (item.Tag is AshKeypress press)
                {
                    profile.KeyMap[press.UserPress.VKey] = press.WillPress.VKey;
                    profile.ModeMap[press.UserPress.VKey] = press.Mode;
                    profile.NameMap[press.UserPress.VKey] = press.Name == null ? string.Empty : press.Name.Trim();
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

        /// <summary>切换当前配置。</summary>
        public void SetActiveProfile(int index)
        {
            EnsureProfiles();
            if (index < 0 || index >= _profiles.Count) return;

            _activeProfile = index;
            SaveConfig();
        }

        /// <summary>新增一套空配置并切换过去，返回新配置索引。</summary>
        public int AddProfile()
        {
            EnsureProfiles();
            var profile = new AshProfile { Name = NextProfileName() };
            _profiles.Add(profile);
            _activeProfile = _profiles.Count - 1;
            SaveConfig();
            return _activeProfile;
        }

        /// <summary>删除当前配置，至少保留一套。</summary>
        public bool DeleteActiveProfile()
        {
            EnsureProfiles();
            if (_profiles.Count <= 1) return false;

            _profiles.RemoveAt(_activeProfile);
            if (_activeProfile >= _profiles.Count)
            {
                _activeProfile = _profiles.Count - 1;
            }

            SaveConfig();
            return true;
        }

        /// <summary>重命名指定配置。</summary>
        public bool RenameProfile(int index, string name)
        {
            EnsureProfiles();
            if (index < 0 || index >= _profiles.Count) return false;

            name = (name ?? string.Empty).Trim();
            if (name.Length == 0) return false;

            _profiles[index].Name = name;
            SaveConfig();
            return true;
        }

        private string NextProfileName()
        {
            var used = new HashSet<string>();
            foreach (var profile in _profiles)
            {
                if (!string.IsNullOrWhiteSpace(profile.Name)) used.Add(profile.Name);
            }

            for (var i = 2; i < 10000; i++)
            {
                var candidate = "配置 " + i;
                if (!used.Contains(candidate)) return candidate;
            }

            return "新配置";
        }

        /// <summary>确保至少有一套配置，并修正空的字典/名称。</summary>
        private void EnsureProfiles()
        {
            if (_profiles == null)
            {
                _profiles = new List<AshProfile>();
            }

            _profiles.RemoveAll(p => p == null);

            if (_profiles.Count == 0)
            {
                _profiles.Add(new AshProfile { Name = "默认配置" });
            }

            foreach (var profile in _profiles)
            {
                if (string.IsNullOrWhiteSpace(profile.Name))
                {
                    profile.Name = "默认配置";
                }
                else
                {
                    profile.Name = profile.Name.Trim();
                }

                if (profile.KeyMap == null) profile.KeyMap = new Dictionary<int, int>();
                if (profile.ModeMap == null) profile.ModeMap = new Dictionary<int, int>();
                if (profile.NameMap == null) profile.NameMap = new Dictionary<int, string>();
            }

            if (_activeProfile < 0 || _activeProfile >= _profiles.Count)
            {
                _activeProfile = 0;
            }
        }

        private static readonly string ConfigFilePath = "AshConfig.json";

        public void SaveConfig()
        {
            try
            {
                EnsureProfiles();
                var configData = new ConfigData
                {
                    Profiles = _profiles,
                    ActiveProfile = _activeProfile,
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
                if (!File.Exists(ConfigFilePath))
                {
                    EnsureProfiles();
                    Console.WriteLine("Configuration file not found. Using default settings.");
                    return;
                }

                var json = File.ReadAllText(ConfigFilePath);
                var configData = JsonConvert.DeserializeObject<ConfigData>(json);

                if (configData == null)
                {
                    EnsureProfiles();
                    return;
                }

                PressDelay = configData.PressDelay;
                ReleaseDelay = configData.ReleaseDelay;
                ToggleKey = configData.ToggleKey;
                PauseKey = configData.PauseKey;

                _profiles = configData.Profiles ?? new List<AshProfile>();
                _activeProfile = configData.ActiveProfile;

                var migrated = false;

                // 老版本配置没有 Profiles，把原来的单套 KeyMap 迁移成「默认配置」。
                if (_profiles.Count == 0)
                {
                    _profiles.Add(new AshProfile
                    {
                        Name = "默认配置",
                        KeyMap = configData.KeyMap ?? new Dictionary<int, int>(),
                        ModeMap = configData.ModeMap ?? new Dictionary<int, int>(),
                        NameMap = configData.NameMap ?? new Dictionary<int, string>()
                    });
                    migrated = true;
                }

                EnsureProfiles();

                foreach (var profile in _profiles)
                {
                    EnsureNames(profile);
                }

                if (migrated)
                {
                    SaveConfig();
                }

                Console.WriteLine("Configuration loaded successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");
            }
        }

        /// <summary>老版本配置没有方案名，按顺序补上「自定义键位N」。</summary>
        private static void EnsureNames(AshProfile profile)
        {
            if (profile.NameMap == null) profile.NameMap = new Dictionary<int, string>();
            if (profile.KeyMap == null) profile.KeyMap = new Dictionary<int, int>();

            var used = new HashSet<string>();
            foreach (var name in profile.NameMap.Values)
            {
                if (!string.IsNullOrWhiteSpace(name)) used.Add(name);
            }

            var next = 1;
            foreach (var key in profile.KeyMap.Keys)
            {
                string name;
                if (profile.NameMap.TryGetValue(key, out name) && !string.IsNullOrWhiteSpace(name)) continue;

                while (used.Contains(DefaultName(next))) next++;
                name = DefaultName(next);
                profile.NameMap[key] = name;
                used.Add(name);
                next++;
            }
        }

        private class ConfigData
        {
            public List<AshProfile> Profiles { get; set; }
            public int ActiveProfile { get; set; }

            // 老版本字段，仅用于读取旧配置；新配置保存时不再输出。
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public Dictionary<int, int> KeyMap { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public Dictionary<int, int> ModeMap { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public Dictionary<int, string> NameMap { get; set; }

            public int PressDelay { get; set; }
            public int ReleaseDelay { get; set; }

            public int ToggleKey { get; set; }
            public int PauseKey { get; set; }
        }
    }
}