using System;
using System.Threading;
using System.Threading.Tasks;
using Inso.Els.Internal;

namespace Inso.Els
{
    /// <summary>
    /// Main ELS client. Captures errors and messages, batches them in the
    /// background, and delivers them to the ELS API with retry and disk
    /// buffering. Safe for concurrent use.
    /// </summary>
    public sealed class ElsClient : IElsClient
    {
        private readonly ElsOptions _options;
        private readonly DebugLog _debug;
        private readonly HttpTransport _transport;
        private readonly DiskBuffer _diskBuffer;
        private readonly BackgroundWorker _worker;
        private readonly EntryEnricher _enricher;
        private readonly Random _random = new Random();

        private long _enqueued;
        private long _dropped;
        private long _sent;
        private long _failed;
        private long _sampled;

        private int _disposed;
        private string _sessionId;
        private readonly object _userLock = new object();
        private UserContext? _user;

        /// <summary>Creates a new client. Validates <paramref name="options"/> eagerly.</summary>
        public ElsClient(ElsOptions options)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));
            _options = options.Normalize();
            _debug = new DebugLog(_options);
            _transport = new HttpTransport(_options, _debug);
            _diskBuffer = new DiskBuffer(_options, _debug);
            _sessionId = SessionIdFactory.New();

            _enricher = new EntryEnricher(
                _options,
                () => SessionId,
                () => User);

            _worker = new BackgroundWorker(
                _options,
                _transport,
                _diskBuffer,
                _debug,
                onSent: count => Interlocked.Add(ref _sent, count),
                onFailed: count => Interlocked.Add(ref _failed, count),
                onDropped: () => Interlocked.Increment(ref _dropped));
        }

        /// <inheritdoc />
        public string SessionId
        {
            get { return Volatile.Read(ref _sessionId); }
        }

        /// <inheritdoc />
        public UserContext? User
        {
            get { lock (_userLock) return _user; }
            set { lock (_userLock) _user = value; }
        }

        /// <inheritdoc />
        public ElsStats Stats => new ElsStats(
            Interlocked.Read(ref _enqueued),
            Interlocked.Read(ref _dropped),
            Interlocked.Read(ref _sent),
            Interlocked.Read(ref _failed),
            Interlocked.Read(ref _sampled),
            _diskBuffer.CurrentSize);

        /// <inheritdoc />
        public int QueueSize => _worker.QueueSize;

        /// <inheritdoc />
        public void SetSessionId(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId)) throw new ArgumentException("sessionId must not be empty", nameof(sessionId));
            Volatile.Write(ref _sessionId, sessionId);
        }

        /// <inheritdoc />
        public void CaptureError(Exception exception, CaptureOptions? options = null)
        {
            if (exception is null) return;
            if (IsDisposed) return;

            var entry = _enricher.FromException(exception);
            Enqueue(entry, options);
        }

        /// <inheritdoc />
        public void CaptureMessage(string message, ElsLevel level, CaptureOptions? options = null)
        {
            if (IsDisposed) return;

            var entry = _enricher.FromMessage(message ?? string.Empty, level);
            Enqueue(entry, options);
        }

        /// <inheritdoc />
        public void CaptureEntry(ErrorEntry entry, CaptureOptions? options = null)
        {
            if (entry is null) return;
            if (IsDisposed) return;
            Enqueue(entry, options);
        }

        /// <inheritdoc />
        public Task SendAsync(Exception exception, CaptureOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (exception is null) return Task.CompletedTask;
            ThrowIfDisposed();

            var entry = _enricher.FromException(exception);
            return SendEntryAsync(entry, options, cancellationToken);
        }

        /// <inheritdoc />
        public async Task SendEntryAsync(ErrorEntry entry, CaptureOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (entry is null) throw new ArgumentNullException(nameof(entry));
            ThrowIfDisposed();

            var prepared = Prepare(entry, options, isSync: true);
            if (prepared is null) return; // dropped by hooks / filters
            await _transport.SendSingleAsync(prepared, cancellationToken).ConfigureAwait(false);
            Interlocked.Increment(ref _sent);
        }

        /// <inheritdoc />
        public Task HealthAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return _transport.HealthAsync(cancellationToken);
        }

        /// <inheritdoc />
        public Task FlushAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            if (IsDisposed) return Task.CompletedTask;
            return _worker.FlushAsync(timeout ?? _options.FlushTimeout, cancellationToken);
        }

        /// <inheritdoc />
        public async Task CloseAsync(CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
            try
            {
                await _worker.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _transport.DisposeOwned();
            }
        }

        /// <summary>Synchronous dispose. Blocks up to <see cref="ElsOptions.FlushTimeout"/>.</summary>
        public void Dispose()
        {
            try
            {
                CloseAsync().GetAwaiter().GetResult();
            }
            catch
            {
                // intentionally swallow — Dispose must not throw
            }
        }

#if !NETSTANDARD2_0
        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            return new ValueTask(CloseAsync());
        }
#endif

        // ---------- Internal helpers ----------

        private bool IsDisposed => Volatile.Read(ref _disposed) == 1;

        private void ThrowIfDisposed()
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(ElsClient));
        }

        private void Enqueue(ErrorEntry entry, CaptureOptions? options)
        {
            var prepared = Prepare(entry, options, isSync: false);
            if (prepared is null) return;
            if (_worker.TryEnqueue(prepared))
            {
                Interlocked.Increment(ref _enqueued);
            }
            else
            {
                Interlocked.Increment(ref _dropped);
            }
        }

        private ErrorEntry? Prepare(ErrorEntry entry, CaptureOptions? options, bool isSync)
        {
            ErrorEntry enriched;
            try
            {
                enriched = _enricher.Apply(entry, options);
            }
            catch (Exception ex)
            {
                SafeOnError(ex);
                return null;
            }

            var level = enriched.Level ?? _options.DefaultLevel;

            if (_options.MinLevel.HasValue && level < _options.MinLevel.Value)
            {
                return null;
            }

            if (!isSync && level != ElsLevel.Critical && _options.SampleRate < 1.0)
            {
                if (_random.NextDouble() >= _options.SampleRate)
                {
                    Interlocked.Increment(ref _sampled);
                    return null;
                }
            }

            if (_options.BeforeSend is not null)
            {
                try
                {
                    var result = _options.BeforeSend(enriched);
                    if (result is null) return null;
                    enriched = result;
                }
                catch (Exception ex)
                {
                    SafeOnError(ex);
                    return null;
                }
            }

            return enriched;
        }

        private void SafeOnError(Exception ex)
        {
            try { _options.OnError?.Invoke(ex); }
            catch (Exception cb) { _debug.Write("OnError threw: {0}", cb.Message); }
        }
    }
}
