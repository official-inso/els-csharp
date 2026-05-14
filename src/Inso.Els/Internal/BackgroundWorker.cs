using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Inso.Els.Internal
{
    /// <summary>
    /// Background worker that batches captured entries and sends them via the
    /// HTTP transport. On failure, entries fall through to the disk buffer.
    /// Lifecycle: created in <see cref="ElsClient"/>, stopped during dispose.
    /// </summary>
    internal sealed class BackgroundWorker
    {
        private readonly ElsOptions _options;
        private readonly HttpTransport _transport;
        private readonly DiskBuffer _diskBuffer;
        private readonly DebugLog _debug;
        private readonly Channel<ErrorEntry> _channel;
        private readonly CancellationTokenSource _stopCts = new CancellationTokenSource();
        private readonly Task _loop;
        private readonly Action<long> _onSent;
        private readonly Action<long> _onFailed;
        private readonly Action _onDropped;

        public BackgroundWorker(
            ElsOptions options,
            HttpTransport transport,
            DiskBuffer diskBuffer,
            DebugLog debug,
            Action<long> onSent,
            Action<long> onFailed,
            Action onDropped)
        {
            _options = options;
            _transport = transport;
            _diskBuffer = diskBuffer;
            _debug = debug;
            _onSent = onSent;
            _onFailed = onFailed;
            _onDropped = onDropped;

            _channel = Channel.CreateBounded<ErrorEntry>(new BoundedChannelOptions(options.BufferSize)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false,
            });

            _loop = Task.Run(RunAsync);
        }

        public int QueueSize => _channel.Reader.Count;

        public bool TryEnqueue(ErrorEntry entry)
        {
            if (_stopCts.IsCancellationRequested) return false;
            return _channel.Writer.TryWrite(entry);
        }

        /// <summary>
        /// Waits up to <paramref name="timeout"/> for the channel to drain.
        /// Returns when the queue is empty or the timeout elapses.
        /// </summary>
        public async Task FlushAsync(TimeSpan timeout, CancellationToken ct)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
            {
                if (QueueSize == 0) return;
                await Task.Delay(50, ct).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Stops the worker, drains the queue, and waits for the loop to finish.
        /// </summary>
        public async Task StopAsync(CancellationToken ct)
        {
            _channel.Writer.TryComplete();
            _stopCts.Cancel();
            try
            {
                await _loop.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _debug.Write("worker loop ended with: {0}", ex.Message);
            }
        }

        private async Task RunAsync()
        {
            // Best-effort: replay anything left from a previous run.
            try
            {
                await _diskBuffer.FlushOnStartupAsync(_transport, _stopCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { /* shutdown */ }
            catch (Exception ex)
            {
                _debug.Write("startup flush failed: {0}", ex.Message);
            }

            var batch = new List<ErrorEntry>(_options.BatchSize);
            var reader = _channel.Reader;
            var ct = _stopCts.Token;

            while (true)
            {
                try
                {
                    var readTask = reader.WaitToReadAsync(ct).AsTask();
                    var delayTask = Task.Delay(_options.BatchInterval, ct);
                    var completed = await Task.WhenAny(readTask, delayTask).ConfigureAwait(false);

                    if (completed == readTask)
                    {
                        bool more;
                        try { more = await readTask.ConfigureAwait(false); }
                        catch (OperationCanceledException) { break; }
                        if (!more) break;

                        // Drain available items eagerly into the batch.
                        while (reader.TryRead(out var item))
                        {
                            batch.Add(item);
                            if (batch.Count >= _options.BatchSize)
                            {
                                await FlushBatchAsync(batch, ct).ConfigureAwait(false);
                                batch.Clear();
                            }
                        }
                    }
                    else
                    {
                        // Timer tick — flush partial batch if any.
                        if (batch.Count > 0)
                        {
                            await FlushBatchAsync(batch, ct).ConfigureAwait(false);
                            batch.Clear();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _debug.Write("worker tick failed: {0}", ex.Message);
                }
            }

            // Drain on shutdown.
            try
            {
                while (reader.TryRead(out var leftover))
                {
                    batch.Add(leftover);
                    if (batch.Count >= _options.BatchSize)
                    {
                        await FlushBatchAsync(batch, CancellationToken.None).ConfigureAwait(false);
                        batch.Clear();
                    }
                }
                if (batch.Count > 0)
                {
                    await FlushBatchAsync(batch, CancellationToken.None).ConfigureAwait(false);
                    batch.Clear();
                }
            }
            catch (Exception ex)
            {
                _debug.Write("worker shutdown drain failed: {0}", ex.Message);
            }
        }

        private async Task FlushBatchAsync(List<ErrorEntry> batch, CancellationToken ct)
        {
            int count = batch.Count;
            if (count == 0) return;
            try
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
                linked.CancelAfter(_options.Timeout + TimeSpan.FromMilliseconds(_options.MaxRetries * _options.RetryBaseDelay.TotalMilliseconds * 4));
                await _transport.SendBatchAsync(batch, linked.Token).ConfigureAwait(false);
                _onSent(count);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                _debug.Write("batch send timed out — buffering");
                _onFailed(count);
                _diskBuffer.Write(batch);
            }
            catch (Exception ex)
            {
                _debug.Write("batch send failed: {0}", ex.Message);
                _onFailed(count);
                try { _options.OnError?.Invoke(ex); } catch { /* swallow */ }
                _diskBuffer.Write(batch);
            }
        }
    }
}
