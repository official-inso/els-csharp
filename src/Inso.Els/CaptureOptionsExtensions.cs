using System;
using System.Collections.Generic;

namespace Inso.Els
{
    /// <summary>
    /// Fluent helpers for building <see cref="CaptureOptions"/>. Each helper
    /// returns a new instance with the requested override applied.
    /// </summary>
    public static class CaptureOptionsExtensions
    {
        /// <summary>Returns a copy of <paramref name="options"/> with the URL set.</summary>
        public static CaptureOptions WithUrl(this CaptureOptions options, string url)
            => options with { Url = url };

        /// <summary>Returns a copy of <paramref name="options"/> with the level set.</summary>
        public static CaptureOptions WithLevel(this CaptureOptions options, ElsLevel level)
            => options with { Level = level };

        /// <summary>Returns a copy of <paramref name="options"/> with the source set.</summary>
        public static CaptureOptions WithSource(this CaptureOptions options, ElsSource source)
            => options with { Source = source };

        /// <summary>Returns a copy of <paramref name="options"/> with the stack trace set.</summary>
        public static CaptureOptions WithStack(this CaptureOptions options, string stack)
            => options with { Stack = stack };

        /// <summary>Returns a copy of <paramref name="options"/> with the component stack set.</summary>
        public static CaptureOptions WithComponentStack(this CaptureOptions options, string componentStack)
            => options with { ComponentStack = componentStack };

        /// <summary>Returns a copy of <paramref name="options"/> with the user agent set.</summary>
        public static CaptureOptions WithUserAgent(this CaptureOptions options, string userAgent)
            => options with { UserAgent = userAgent };

        /// <summary>Returns a copy of <paramref name="options"/> with the language set.</summary>
        public static CaptureOptions WithLanguage(this CaptureOptions options, string language)
            => options with { Language = language };

        /// <summary>Returns a copy of <paramref name="options"/> with the referrer set.</summary>
        public static CaptureOptions WithReferrer(this CaptureOptions options, string referrer)
            => options with { Referrer = referrer };

        /// <summary>Returns a copy of <paramref name="options"/> with the session id set.</summary>
        public static CaptureOptions WithSessionId(this CaptureOptions options, string sessionId)
            => options with { SessionId = sessionId };

        /// <summary>Returns a copy of <paramref name="options"/> with the service name set.</summary>
        public static CaptureOptions WithServiceName(this CaptureOptions options, string serviceName)
            => options with { ServiceName = serviceName };

        /// <summary>Returns a copy of <paramref name="options"/> with the app version set.</summary>
        public static CaptureOptions WithAppVersion(this CaptureOptions options, string appVersion)
            => options with { AppVersion = appVersion };

        /// <summary>Returns a copy of <paramref name="options"/> with the HTTP status set.</summary>
        public static CaptureOptions WithHttpStatus(this CaptureOptions options, int status)
            => options with { HttpStatus = status };

        /// <summary>Returns a copy of <paramref name="options"/> with the duration set.</summary>
        public static CaptureOptions WithDuration(this CaptureOptions options, long durationMs)
            => options with { DurationMs = durationMs };

        /// <summary>Returns a copy of <paramref name="options"/> with meta replaced wholesale.</summary>
        public static CaptureOptions WithMeta(this CaptureOptions options, IDictionary<string, object?> meta)
            => options with { Meta = meta };

        /// <summary>
        /// Returns a copy of <paramref name="options"/> with one meta entry added.
        /// Existing keys are overwritten. Allocates a new dictionary so the
        /// previous instance is not mutated.
        /// </summary>
        public static CaptureOptions WithMetaItem(this CaptureOptions options, string key, object? value)
        {
            if (key is null) throw new ArgumentNullException(nameof(key));
            var next = new Dictionary<string, object?>(options.Meta ?? new Dictionary<string, object?>())
            {
                [key] = value,
            };
            return options with { Meta = next };
        }

        /// <summary>Returns a copy of <paramref name="options"/> with the cause exception set.</summary>
        public static CaptureOptions WithCause(this CaptureOptions options, Exception cause)
            => options with { Cause = cause };
    }
}
