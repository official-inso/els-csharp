using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Inso.Els
{
    /// <summary>
    /// Public surface of the ELS client. Safe for concurrent use.
    /// </summary>
    public interface IElsClient : IDisposable
#if !NETSTANDARD2_0
        , IAsyncDisposable
#endif
    {
        /// <summary>Process-level session identifier attached to every captured entry.</summary>
        string SessionId { get; }

        /// <summary>
        /// Current user context. When set, the SDK enriches every captured
        /// entry with <c>user.id</c>, <c>user.email</c>, <c>user.name</c> and
        /// each <see cref="UserContext.Extra"/> key under <c>user.&lt;key&gt;</c>.
        /// </summary>
        UserContext? User { get; set; }

        /// <summary>Snapshot of runtime metrics.</summary>
        ElsStats Stats { get; }

        /// <summary>Number of entries currently waiting in the in-memory queue.</summary>
        int QueueSize { get; }

        /// <summary>
        /// Captures an exception with an automatic stack trace. Returns immediately;
        /// the entry is sent asynchronously by the background worker.
        /// </summary>
        void CaptureError(Exception exception, CaptureOptions? options = null);

        /// <summary>
        /// Convenience overload — covers the most common case (url, optional level, meta, cause)
        /// without forcing the caller to construct a <see cref="CaptureOptions"/>. When
        /// <paramref name="cause"/> is supplied, its <c>InnerException</c> chain (or
        /// <c>AggregateException.InnerExceptions</c>) is flattened into <c>meta["error.causes"]</c>.
        /// </summary>
        void CaptureError(Exception exception, string? url, ElsLevel? level = null, IDictionary<string, object?>? meta = null, Exception? cause = null);

        /// <summary>
        /// Captures a text message at the given level. Returns immediately;
        /// the entry is sent asynchronously by the background worker.
        /// </summary>
        void CaptureMessage(string message, ElsLevel level, CaptureOptions? options = null);

        /// <summary>
        /// Convenience overload for <see cref="CaptureMessage(string, ElsLevel, CaptureOptions?)"/>.
        /// </summary>
        void CaptureMessage(string message, ElsLevel level, string? url, IDictionary<string, object?>? meta = null);

        /// <summary>
        /// Captures a pre-built <see cref="ErrorEntry"/>. Missing fields are
        /// filled with client defaults. Returns immediately.
        /// </summary>
        void CaptureEntry(ErrorEntry entry, CaptureOptions? options = null);

        /// <summary>
        /// Sends a single error and waits for the server to confirm delivery.
        /// Throws <see cref="ElsSendException"/> on failure.
        /// </summary>
        Task SendAsync(Exception exception, CaptureOptions? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a pre-built <see cref="ErrorEntry"/> and waits for the server
        /// to confirm delivery. Throws <see cref="ElsSendException"/> on failure.
        /// </summary>
        Task SendEntryAsync(ErrorEntry entry, CaptureOptions? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks server connectivity. Throws <see cref="ElsSendException"/> when
        /// the server is unreachable or returns a non-success status.
        /// </summary>
        Task HealthAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Non-throwing variant of <see cref="HealthAsync(CancellationToken)"/>. Returns a
        /// structured result suitable for liveness / readiness probes.
        /// </summary>
        Task<ElsHealthResult> TryHealthAsync(CancellationToken cancellationToken = default);

        /// <summary>Statistics snapshot pushed after every batch send / disk flush / sample drop.</summary>
        event EventHandler<ElsStats>? StatsChanged;

        /// <summary>
        /// Drains the in-memory queue. Completes when the queue is empty,
        /// <paramref name="timeout"/> elapses, or the token is cancelled.
        /// </summary>
        Task FlushAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops the background worker, drains pending entries, attempts a
        /// final send, and persists anything that still didn't make it to the
        /// disk buffer. Idempotent.
        /// </summary>
        Task CloseAsync(CancellationToken cancellationToken = default);

        /// <summary>Overrides the auto-generated <see cref="SessionId"/>.</summary>
        void SetSessionId(string sessionId);
    }
}
