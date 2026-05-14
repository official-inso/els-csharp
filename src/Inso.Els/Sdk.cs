using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Inso.Els
{
    /// <summary>
    /// Static facade around a single, ambient <see cref="IElsClient"/>.
    /// Useful for console apps and quick scripts. ASP.NET Core users should
    /// prefer the dependency-injection registration from
    /// <c>Inso.Els.AspNetCore</c>.
    /// </summary>
    public static class Sdk
    {
        private static IElsClient? _current;
        private static bool _processExitRegistered;
        private static readonly object _gate = new object();

        /// <summary>The current ambient client, or <c>null</c> if <see cref="Init"/> was not called.</summary>
        public static IElsClient? Current
        {
            get { lock (_gate) return _current; }
        }

        /// <summary>
        /// Raised whenever <see cref="Init"/> replaces an existing ambient
        /// client. The argument is the *previous* client (already closed by
        /// the time the event fires). Useful for telemetry / debug logs in
        /// hot-reload scenarios. Handlers must not throw.
        /// </summary>
        public static event EventHandler<IElsClient>? ClientReplaced;

        /// <summary>
        /// Initializes the ambient client. If a previous client exists, it is
        /// closed first (best-effort) and <see cref="ClientReplaced"/> fires.
        /// When <see cref="ElsOptions.AutoFlushOnExit"/> is <c>true</c> (default),
        /// an <c>AppDomain.ProcessExit</c> hook is registered on the first
        /// call to drain pending entries on shutdown.
        ///
        /// Throws <see cref="ElsConfigurationException"/> when
        /// <paramref name="options"/> are invalid.
        /// </summary>
        public static void Init(ElsOptions options)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));
            var next = new ElsClient(options);
            IElsClient? previous;
            lock (_gate)
            {
                previous = _current;
                _current = next;
                if (options.AutoFlushOnExit && !_processExitRegistered)
                {
                    AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
                    _processExitRegistered = true;
                }
            }
            if (previous is not null)
            {
                try { previous.CloseAsync().GetAwaiter().GetResult(); } catch { /* swallow */ }
                try { ClientReplaced?.Invoke(null, previous); } catch { /* swallow */ }
            }
        }

        /// <summary>Closes the ambient client asynchronously.</summary>
        public static async Task CloseAsync(CancellationToken cancellationToken = default)
        {
            IElsClient? current;
            lock (_gate)
            {
                current = _current;
                _current = null;
            }
            if (current is not null)
            {
                await current.CloseAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Synchronously closes the ambient client. Blocks the calling thread
        /// until shutdown completes or <see cref="ElsOptions.FlushTimeout"/>
        /// elapses inside the worker. Safe to call from console / worker
        /// shutdown paths; in UI sync-contexts prefer <see cref="CloseAsync"/>.
        /// </summary>
        public static void Close()
        {
            // Detach from any captured SyncContext to keep the call safe in
            // scenarios with a non-default SynchronizationContext (e.g. WPF).
            Task.Run(() => CloseAsync()).GetAwaiter().GetResult();
        }

        /// <summary>Captures an exception via the ambient client. No-op when not initialized.</summary>
        public static void CaptureError(Exception exception, CaptureOptions? options = null)
            => Current?.CaptureError(exception, options);

        /// <summary>Convenience overload mirroring the matching method on <see cref="IElsClient"/>.</summary>
        public static void CaptureError(Exception exception, string? url, ElsLevel? level = null, IDictionary<string, object?>? meta = null, Exception? cause = null)
            => Current?.CaptureError(exception, url, level, meta, cause);

        /// <summary>Captures a message via the ambient client. No-op when not initialized.</summary>
        public static void CaptureMessage(string message, ElsLevel level, CaptureOptions? options = null)
            => Current?.CaptureMessage(message, level, options);

        /// <summary>Convenience overload mirroring <see cref="IElsClient.CaptureMessage(string, ElsLevel, string?, IDictionary{string, object?}?)"/>.</summary>
        public static void CaptureMessage(string message, ElsLevel level, string? url, IDictionary<string, object?>? meta = null)
            => Current?.CaptureMessage(message, level, url, meta);

        /// <summary>Captures a pre-built entry via the ambient client.</summary>
        public static void CaptureEntry(ErrorEntry entry, CaptureOptions? options = null)
            => Current?.CaptureEntry(entry, options);

        /// <summary>Sends an exception synchronously via the ambient client. Returns immediately if not initialized.</summary>
        public static Task SendAsync(Exception exception, CaptureOptions? options = null, CancellationToken cancellationToken = default)
            => Current is { } c ? c.SendAsync(exception, options, cancellationToken) : Task.CompletedTask;

        /// <summary>Flushes the ambient client.</summary>
        public static Task FlushAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
            => Current is { } c ? c.FlushAsync(timeout, cancellationToken) : Task.CompletedTask;

        /// <summary>
        /// Returns true if <paramref name="exception"/> represents a retryable
        /// send failure. Unwraps <see cref="AggregateException"/>.
        /// </summary>
        public static bool IsRetryable(Exception? exception)
        {
            if (exception is null) return false;
            if (exception is ElsSendException se) return se.IsRetryable;
            if (exception is AggregateException agg)
            {
                foreach (var inner in agg.Flatten().InnerExceptions)
                {
                    if (inner is ElsSendException ise) return ise.IsRetryable;
                }
            }
            if (exception.InnerException is not null) return IsRetryable(exception.InnerException);
            return false;
        }

        private static void OnProcessExit(object? sender, EventArgs e)
        {
            try { Close(); } catch { /* swallow — process is exiting */ }
        }
    }
}
