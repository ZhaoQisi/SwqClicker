using System;
using System.IO;
using System.Media;

namespace AshClicker
{
    /// <summary>
    /// 播放程序目录 Sounds 文件夹里的外部 WAV；文件不存在、读取失败或格式不对时，
    /// 回退到编译时嵌入的默认音效。
    ///
    /// 每次播放前都会检查文件是否被替换，因此手动替换 WAV 后不需要重新编译，
    /// 也不需要重启程序，下一次播放就会生效。
    /// </summary>
    public sealed class SoundEffectPlayer : IDisposable
    {
        private readonly string _filePath;
        private readonly byte[] _fallbackData;
        private readonly object _sync = new object();

        private SoundPlayer _player;
        private bool _loaded;
        private DateTime _lastWriteUtc;
        private long _lastLength;

        public SoundEffectPlayer(string filePath, byte[] fallbackData)
        {
            if (filePath == null) throw new ArgumentNullException("filePath");
            if (fallbackData == null) throw new ArgumentNullException("fallbackData");

            _filePath = filePath;
            _fallbackData = fallbackData;
            Preload();
        }

        public string FilePath
        {
            get { return _filePath; }
        }

        /// <summary>提前加载一次，避免第一次播放时才开始读文件。</summary>
        public void Preload()
        {
            lock (_sync)
            {
                EnsureLoaded();
            }
        }

        public void Play()
        {
            lock (_sync)
            {
                EnsureLoaded();

                if (_player == null) return;

                try
                {
                    _player.Play();
                }
                catch (Exception)
                {
                    // 没有音频设备或 WAV 无法播放时不应当影响连发功能
                }
            }
        }

        private void EnsureLoaded()
        {
            var exists = File.Exists(_filePath);
            var lastWriteUtc = DateTime.MinValue;
            long length = 0;

            if (exists)
            {
                try
                {
                    var info = new FileInfo(_filePath);
                    lastWriteUtc = info.LastWriteTimeUtc;
                    length = info.Length;
                }
                catch (Exception)
                {
                    exists = false;
                }
            }

            if (_loaded &&
                lastWriteUtc == _lastWriteUtc &&
                length == _lastLength)
            {
                return;
            }

            SoundPlayer player = null;

            if (exists)
            {
                try
                {
                    // 先读成内存流再 Load，避免播放器长期占用文件句柄，
                    // 这样程序运行期间也可以直接替换 WAV。
                    var data = File.ReadAllBytes(_filePath);
                    using (var stream = new MemoryStream(data))
                    {
                        var filePlayer = new SoundPlayer(stream);
                        filePlayer.Load();
                        player = filePlayer;
                    }
                }
                catch (Exception)
                {
                    // 文件损坏或不是有效 WAV，继续使用内置音效
                }
            }

            if (player == null)
            {
                using (var stream = new MemoryStream(_fallbackData))
                {
                    player = new SoundPlayer(stream);
                    player.Load();
                }
            }

            var oldPlayer = _player;
            _player = player;
            _lastWriteUtc = lastWriteUtc;
            _lastLength = length;
            _loaded = true;

            if (oldPlayer != null)
            {
                oldPlayer.Dispose();
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_player != null)
                {
                    _player.Dispose();
                    _player = null;
                }

                _loaded = false;
            }
        }
    }
}
