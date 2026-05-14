using System;
using System.Collections.Generic;

namespace Inso.Els
{
    /// <summary>
    /// Per-call overrides applied to an <see cref="ErrorEntry"/> by the SDK
    /// when it is captured. All properties are optional — leave at <c>null</c>
    /// to inherit the value from the entry or the client defaults.
    /// </summary>
    public sealed record CaptureOptions
    {
        /// <summary>URL where the error happened.</summary>
        public string? Url { get; init; }

        /// <summary>Severity level.</summary>
        public ElsLevel? Level { get; init; }

        /// <summary>Origin (client / server).</summary>
        public ElsSource? Source { get; init; }

        /// <summary>Custom stack trace (overrides the auto-captured one).</summary>
        public string? Stack { get; init; }

        /// <summary>Framework-specific component trace.</summary>
        public string? ComponentStack { get; init; }

        /// <summary>User agent of the client.</summary>
        public string? UserAgent { get; init; }

        /// <summary>Locale string.</summary>
        public string? Language { get; init; }

        /// <summary>HTTP Referer.</summary>
        public string? Referrer { get; init; }

        /// <summary>Session identifier.</summary>
        public string? SessionId { get; init; }

        /// <summary>Microservice name.</summary>
        public string? ServiceName { get; init; }

        /// <summary>Override <c>AppVersion</c> for this entry.</summary>
        public string? AppVersion { get; init; }

        /// <summary>HTTP status code.</summary>
        public int? HttpStatus { get; init; }

        /// <summary>Duration of the failed operation in milliseconds.</summary>
        public long? DurationMs { get; init; }

        /// <summary>Arbitrary metadata. May be merged with existing entry meta.</summary>
        public IDictionary<string, object?>? Meta { get; init; }

        /// <summary>
        /// Exception whose <see cref="Exception.InnerException"/> chain is
        /// flattened into <c>meta["error.causes"]</c>.
        /// </summary>
        public Exception? Cause { get; init; }
    }
}
