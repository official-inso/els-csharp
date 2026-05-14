using System;

namespace Inso.Els
{
    /// <summary>
    /// Outcome of <see cref="IElsClient.TryHealthAsync(System.Threading.CancellationToken)"/>.
    /// Never throws — failures are reported through <see cref="Error"/> and
    /// <see cref="IsHealthy"/>.
    /// </summary>
    public sealed record ElsHealthResult
    {
        /// <summary>True when the server returned a 2xx response.</summary>
        public bool IsHealthy { get; init; }

        /// <summary>HTTP status code if the server responded; null for network / cancellation failures.</summary>
        public int? StatusCode { get; init; }

        /// <summary>Round-trip latency of the probe.</summary>
        public TimeSpan Latency { get; init; }

        /// <summary>Error message when <see cref="IsHealthy"/> is false; null otherwise.</summary>
        public string? Error { get; init; }
    }
}
