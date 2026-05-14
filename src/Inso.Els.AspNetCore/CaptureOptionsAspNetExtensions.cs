using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Inso.Els.AspNetCore
{
    /// <summary>
    /// Extensions for enriching <see cref="CaptureOptions"/> with information
    /// taken from an ASP.NET Core <see cref="HttpContext"/> / <see cref="HttpRequest"/>.
    /// </summary>
    public static class CaptureOptionsAspNetExtensions
    {
        private const int MaxLanguageLength = 20;

        /// <summary>
        /// Extracts URL, method, user agent, language, referer, and a handful
        /// of headers from <paramref name="context"/> and returns a copy of
        /// <paramref name="options"/> with the values applied.
        /// </summary>
        public static CaptureOptions WithHttpContext(this CaptureOptions options, HttpContext? context)
        {
            if (context is null) return options;
            return options.WithHttpRequest(context.Request);
        }

        /// <summary>
        /// Extracts URL, method, user agent, language, referer, and a handful
        /// of headers from <paramref name="request"/> and returns a copy of
        /// <paramref name="options"/> with the values applied.
        /// </summary>
        public static CaptureOptions WithHttpRequest(this CaptureOptions options, HttpRequest? request)
        {
            if (request is null) return options;

            var url = $"{request.Method} {request.Path}{request.QueryString}";
            string? userAgent = request.Headers.TryGetValue("User-Agent", out var ua) ? ua.ToString() : null;
            string? referer = request.Headers.TryGetValue("Referer", out var r) ? r.ToString() : null;
            string? language = null;
            if (request.Headers.TryGetValue("Accept-Language", out var lang) && lang.Count > 0)
            {
                var first = lang[0] ?? string.Empty;
                language = first.Length > MaxLanguageLength ? first.Substring(0, MaxLanguageLength) : first;
            }

            var meta = new Dictionary<string, object?>(options.Meta ?? new Dictionary<string, object?>())
            {
                ["http.method"] = request.Method,
                ["http.host"] = request.Host.HasValue ? request.Host.Value : null,
                ["http.scheme"] = request.Scheme,
                ["http.path"] = request.Path.HasValue ? request.Path.Value : null,
                ["http.protocol"] = request.Protocol,
            };

            if (request.HttpContext?.Connection?.RemoteIpAddress is { } ip)
            {
                meta["http.remoteAddr"] = ip.ToString();
            }
            if (request.Headers.TryGetValue("X-Request-Id", out var rid) && !StringValues.IsNullOrEmpty(rid))
            {
                meta["http.requestId"] = rid.ToString();
            }
            if (request.Headers.TryGetValue("X-Forwarded-For", out var xff) && !StringValues.IsNullOrEmpty(xff))
            {
                meta["http.forwardedFor"] = xff.ToString();
            }

            return options with
            {
                Url = options.Url ?? url,
                UserAgent = options.UserAgent ?? userAgent,
                Language = options.Language ?? language,
                Referrer = options.Referrer ?? referer,
                Meta = meta,
            };
        }
    }
}
