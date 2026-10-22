using System;
using System.Drawing;
using System.IO;

namespace AshClicker
{
    /// <summary>
    /// 外置背景皮肤：读取程序目录 Images 文件夹中的图片，并检测文件变化。
    /// 图片不存在或损坏时保持当前背景（启动时则为无背景），不会影响程序运行。
    /// </summary>
    public sealed class BackgroundSkin : IDisposable
    {
        private readonly string _filePath;
        private readonly object _sync = new object();

        private Image _image;
        private bool _loaded;
        private bool _lastExists;
        private DateTime _lastWriteUtc;
        private long _lastLength;

        public BackgroundSkin(string filePath)
        {
            if (filePath == null) throw new ArgumentNullException("filePath");
            _filePath = filePath;
        }

        public string FilePath
        {
            get { return _filePath; }
        }

        public Image Image
        {
            get
            {
                lock (_sync)
                {
                    return _image;
                }
            }
        }

        /// <summary>
        /// 检查背景文件是否变化。返回 true 表示需要重新应用背景。
        /// 文件损坏时保留旧背景，等下次文件变化再重试。
        /// </summary>
        public bool Refresh()
        {
            lock (_sync)
            {
                var exists = File.Exists(_filePath);
                var writeUtc = DateTime.MinValue;
                long length = 0;

                if (exists)
                {
                    try
                    {
                        var info = new FileInfo(_filePath);
                        writeUtc = info.LastWriteTimeUtc;
                        length = info.Length;
                    }
                    catch (Exception)
                    {
                        // 文件暂时不可读，保留当前背景
                        return false;
                    }
                }

                if (_loaded &&
                    exists == _lastExists &&
                    writeUtc == _lastWriteUtc &&
                    length == _lastLength)
                {
                    return false;
                }

                if (!exists)
                {
                    ReplaceImage(null);
                    _loaded = true;
                    _lastExists = false;
                    _lastWriteUtc = DateTime.MinValue;
                    _lastLength = 0;
                    return true;
                }

                Image loaded = null;

                try
                {
                    // 用 FileShare.ReadWrite 并复制到 Bitmap，避免图片文件被锁住。
                    using (var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var source = Image.FromStream(stream, false, true))
                    {
                        loaded = new Bitmap(source);
                    }
                }
                catch (Exception)
                {
                    // 文件损坏或不是有效图片：保留旧背景，记录当前时间戳避免每 1 秒重试
                    _loaded = true;
                    _lastExists = true;
                    _lastWriteUtc = writeUtc;
                    _lastLength = length;
                    return false;
                }

                ReplaceImage(loaded);
                _loaded = true;
                _lastExists = true;
                _lastWriteUtc = writeUtc;
                _lastLength = length;
                return true;
            }
        }

        private void ReplaceImage(Image image)
        {
            var old = _image;
            _image = image;

            if (old != null && !ReferenceEquals(old, image))
            {
                old.Dispose();
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                ReplaceImage(null);
                _loaded = false;
            }
        }
    }
}
