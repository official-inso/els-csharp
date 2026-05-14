using System;

namespace Inso.Els
{
    /// <summary>
    /// Failure raised when an ELS request cannot be delivered. Distinguishes
    /// transient failures (5xx, 429, network errors) from permanent ones (4xx).
    /// </summary>
    public sealed class ElsSendException : ElsException
    {
        /// <summary>HTTP status code returned by the server. Zero for network errors.</summary>
        public int StatusCode { get; }

        /// <summary>True for 5xx / 429 / network errors. False for 4xx (except 429).</summary>
        public bool IsRetryable { get; }

        /// <summary>Server response body if available; null otherwise.</summary>
        public string? ResponseBody { get; }

        /// <summary>Creates a send exception.</summary>
        public ElsSendException(int statusCode, bool isRetryable, string message, string? responseBody = null, Exception? inner = null)
            : base(message, inner ?? new Exception(message))
        {
            StatusCode = statusCode;
            IsRetryable = isRetryable;
            ResponseBody = responseBody;
        }
    }
}
