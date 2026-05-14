using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Inso.Els.Internal
{
    /// <summary>
    /// Append-only JSONL disk buffer used when the server cannot be reached.
    /// On startup, <see cref="FlushOnStartupAsync"/> tries to replay the file
    /// before any new captures are processed. The file format is byte-compatible
    /// with the Go SDK so installations can be migrated without conversion.
    /// </summary>
    internal sealed class DiskBuffer
    {
        private readonly ElsOptions _options;
        private readonly DebugLog _debug;
        private readonly string _filePath;
        private readonly object _ioLock = new object();
        private long _currentSize;

        public DiskBuffer(ElsOptions options, DebugLog debug)
        {
            _options = options;
            _debug = debug;
            var dir = string.IsNullOrEmpty(options.BufferDir) ? Path.GetTempPath() : options.BufferDir!;
            _filePath = Path.Combine(dir, options.BufferFileName);
            try
            {
                Directory.CreateDirectory(dir);
                _currentSize = File.Exists(_filePath) ? new FileInfo(_filePath).Length : 0;
            }
            catch (Exception ex)
            {
                _debug.Write("disk buffer init failed: {0}", ex.Message);
                _currentSize = 0;
            }
        }

        public string FilePath => _filePath;

        public long CurrentSize
        {
            get
            {
                lock (_ioLock) return _currentSize;
            }
        }

        /// <summary>
        /// Appends the given entries to the buffer file. Each entry is
        /// serialized to a single JSON line. Entries beyond
        /// <see cref="ElsOptions.MaxBufferFileSize"/> are dropped and reported
        /// via <see cref="ElsOptions.OnError"/>.
        /// </summary>
        public void Write(IReadOnlyList<ErrorEntry> entries)
        {
            if (entries.Count == 0) return;
            lock (_ioLock)
            {
                if (_currentSize >= _options.MaxBufferFileSize)
                {
                    SafeOnError(new ElsException($"els: disk buffer full ({_currentSize} bytes), dropping {entries.Count} entries"));
                    return;
                }
                try
                {
                    using var fs = new FileStream(_filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
                    foreach (var entry in entries)
                    {
                        var bytes = JsonSerializer.SerializeToUtf8Bytes(entry, JsonSerialization.Default);
                        fs.Write(bytes, 0, bytes.Length);
                        fs.WriteByte((byte)'\n');
                        _currentSize += bytes.Length + 1;
                        if (_currentSize >= _options.MaxBufferFileSize)
                        {
                            SafeOnError(new ElsException($"els: disk buffer reached limit at {_currentSize} bytes"));
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _debug.Write("disk buffer write failed: {0}", ex.Message);
                    SafeOnError(new ElsException("els: failed to write disk buffer", ex));
                }
            }
        }

        /// <summary>
        /// Reads the buffer file (if any) and sends entries through
        /// <paramref name="transport"/>. On full success deletes the file.
        /// On failure, leaves the file intact so the next process run can try again.
        /// </summary>
        public async Task FlushOnStartupAsync(HttpTransport transport, CancellationToken ct)
        {
            List<ErrorEntry> entries;
            lock (_ioLock)
            {
                if (!File.Exists(_filePath)) return;
                entries = ReadAll();
                if (entries.Count == 0)
                {
                    TryDelete();
                    return;
                }
            }

            for (int i = 0; i < entries.Count; i += _options.BatchSize)
            {
                var slice = entries.GetRange(i, Math.Min(_options.BatchSize, entries.Count - i));
                try
                {
                    await transport.SendBatchAsync(slice, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _debug.Write("flush-on-startup failed at offset={0}: {1}", i, ex.Message);
                    SafeOnError(new ElsException("els: flush disk buffer failed", ex));
                    return;
                }
            }

            lock (_ioLock)
            {
                TryDelete();
            }
        }

        private List<ErrorEntry> ReadAll()
        {
            var result = new List<ErrorEntry>();
            try
            {
                using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(fs, Encoding.UTF8);
                string? line;
                while ((line = reader.ReadLine()) is not null)
                {
                    if (line.Length == 0) continue;
                    try
                    {
                        var entry = JsonSerializer.Deserialize<ErrorEntry>(line, JsonSerialization.Default);
                        if (entry is not null) result.Add(entry);
                    }
                    catch (Exception ex)
                    {
                        _debug.Write("malformed buffer line skipped: {0}", ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _debug.Write("disk buffer read failed: {0}", ex.Message);
            }
            return result;
        }

        private void TryDelete()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                    _currentSize = 0;
                }
            }
            catch (Exception ex)
            {
                _debug.Write("disk buffer delete failed: {0}", ex.Message);
            }
        }

        private void SafeOnError(Exception ex)
        {
            try { _options.OnError?.Invoke(ex); }
            catch (Exception cb) { _debug.Write("OnError threw: {0}", cb.Message); }
        }
    }
}
