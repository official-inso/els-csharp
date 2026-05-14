using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Inso.Els
{
    /// <summary>
    /// A single error entry sent to the ELS API. Field names map 1:1 to the
    /// wire JSON via <see cref="JsonPropertyNameAttribute"/> so the schema is
    /// stable across SDK versions.
    /// </summary>
    public sealed record ErrorEntry
    {
        /// <summary>Error text (required).</summary>
        [JsonPropertyName("message")]
        public string Message { get; init; } = string.Empty;

        /// <summary>URL or route where the error occurred (required).</summary>
        [JsonPropertyName("url")]
        public string Url { get; init; } = string.Empty;

        /// <summary>RFC3339 / ISO 8601 UTC timestamp. Auto-filled by the SDK when empty.</summary>
        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; init; }

        /// <summary>Stack trace. Auto-captured by <c>CaptureError</c>.</summary>
        [JsonPropertyName("stack")]
        public string? Stack { get; init; }

        /// <summary>Framework-specific component trace (e.g. React).</summary>
        [JsonPropertyName("componentStack")]
        public string? ComponentStack { get; init; }

        /// <summary>Severity.</summary>
        [JsonPropertyName("level")]
        [JsonConverter(typeof(Internal.ElsLevelJsonConverter))]
        public ElsLevel? Level { get; init; }

        /// <summary>Origin (client / server).</summary>
        [JsonPropertyName("source")]
        [JsonConverter(typeof(Internal.ElsSourceJsonConverter))]
        public ElsSource? Source { get; init; }

        /// <summary>User agent of the client.</summary>
        [JsonPropertyName("userAgent")]
        public string? UserAgent { get; init; }

        /// <summary>Browser language / locale (e.g. <c>en-US</c>).</summary>
        [JsonPropertyName("language")]
        public string? Language { get; init; }

        /// <summary>Screen size (<c>WxH</c>).</summary>
        [JsonPropertyName("screenSize")]
        public string? ScreenSize { get; init; }

        /// <summary>Viewport size (<c>WxH</c>).</summary>
        [JsonPropertyName("viewportSize")]
        public string? ViewportSize { get; init; }

        /// <summary>HTTP Referer header.</summary>
        [JsonPropertyName("referrer")]
        public string? Referrer { get; init; }

        /// <summary>Microservice name.</summary>
        [JsonPropertyName("serviceName")]
        public string? ServiceName { get; init; }

        /// <summary>Deployment environment (e.g. <c>DEV</c>, <c>PRODUCTION</c>).</summary>
        [JsonPropertyName("deploymentEnv")]
        public string? DeploymentEnv { get; init; }

        /// <summary>Application identifier.</summary>
        [JsonPropertyName("appSlug")]
        public string? AppSlug { get; init; }

        /// <summary>Application version (opaque, up to 128 chars).</summary>
        [JsonPropertyName("appVersion")]
        public string? AppVersion { get; init; }

        /// <summary>Session identifier (auto-generated per client when not set).</summary>
        [JsonPropertyName("sessionId")]
        public string? SessionId { get; init; }

        /// <summary>HTTP status code of the failed response.</summary>
        [JsonPropertyName("httpStatus")]
        public int? HttpStatus { get; init; }

        /// <summary>Duration of the failed operation in milliseconds.</summary>
        [JsonPropertyName("durationMs")]
        public long? DurationMs { get; init; }

        /// <summary>Arbitrary key/value metadata.</summary>
        [JsonPropertyName("meta")]
        public IReadOnlyDictionary<string, object?>? Meta { get; init; }
    }
}
