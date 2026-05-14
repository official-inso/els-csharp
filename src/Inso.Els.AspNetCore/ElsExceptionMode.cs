using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Inso.Els.AspNetCore
{
    /// <summary>How the ELS exception middleware should react to an unhandled exception.</summary>
    public enum ElsExceptionMode
    {
        /// <summary>
        /// Capture the exception, then re-throw it so subsequent middleware
        /// (e.g. ASP.NET Core's developer-exception or status-code pages)
        /// can produce the HTTP response. Default.
        /// </summary>
        CaptureAndRethrow = 0,

        /// <summary>
        /// Capture the exception, suppress it, and write a generic 500 response.
        /// Use when no other recovery middleware is installed.
        /// </summary>
        CaptureAndHandle = 1,
    }

    /// <summary>Options for <c>UseElsExceptionHandling</c>.</summary>
    public sealed class ElsExceptionHandlerOptions
    {
        /// <summary>Behavior after capture. Default <see cref="ElsExceptionMode.CaptureAndRethrow"/>.</summary>
        public ElsExceptionMode Mode { get; set; } = ElsExceptionMode.CaptureAndRethrow;

        /// <summary>Severity level recorded for captured exceptions. Default <see cref="ElsLevel.Critical"/>.</summary>
        public ElsLevel Level { get; set; } = ElsLevel.Critical;

        /// <summary>
        /// Optional callback invoked after a successful capture. May be used to log,
        /// emit metrics, or mutate the response when <see cref="Mode"/> is
        /// <see cref="ElsExceptionMode.CaptureAndHandle"/>. Must not throw.
        /// </summary>
        public Func<Exception, HttpContext, Task>? OnException { get; set; }
    }
}
