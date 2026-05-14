using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace Inso.Els.Internal
{
    /// <summary>
    /// Applies SDK defaults and per-call overrides to entries before they are
    /// enqueued (or sent synchronously). Handles user context, exception
    /// causes, and per-entry metadata enrichment.
    /// </summary>
    internal sealed class EntryEnricher
    {
        private const int MaxCauseDepth = 8;

        private readonly ElsOptions _options;
        private readonly Func<string> _sessionIdProvider;
        private readonly Func<UserContext?> _userProvider;

        public EntryEnricher(
            ElsOptions options,
            Func<string> sessionIdProvider,
            Func<UserContext?> userProvider)
        {
            _options = options;
            _sessionIdProvider = sessionIdProvider;
            _userProvider = userProvider;
        }

        /// <summary>
        /// Applies <paramref name="options"/> (per-call overrides), client defaults,
        /// and user context to <paramref name="entry"/>. Returns the resulting entry.
        /// </summary>
        public ErrorEntry Apply(ErrorEntry entry, CaptureOptions? options)
        {
            if (entry is null) throw new ArgumentNullException(nameof(entry));

            var meta = entry.Meta is null
                ? null
                : new Dictionary<string, object?>(entry.Meta as IDictionary<string, object?> ?? CopyReadonly(entry.Meta));

            ErrorEntry next = entry;

            if (options is not null)
            {
                next = next with
                {
                    Url = options.Url ?? next.Url,
                    Level = options.Level ?? next.Level,
                    Source = options.Source ?? next.Source,
                    Stack = options.Stack ?? next.Stack,
                    ComponentStack = options.ComponentStack ?? next.ComponentStack,
                    UserAgent = options.UserAgent ?? next.UserAgent,
                    Language = options.Language ?? next.Language,
                    Referrer = options.Referrer ?? next.Referrer,
                    SessionId = options.SessionId ?? next.SessionId,
                    ServiceName = options.ServiceName ?? next.ServiceName,
                    AppVersion = options.AppVersion ?? next.AppVersion,
                    HttpStatus = options.HttpStatus ?? next.HttpStatus,
                    DurationMs = options.DurationMs ?? next.DurationMs,
                };

                if (options.Meta is not null)
                {
                    meta ??= new Dictionary<string, object?>();
                    foreach (var kv in options.Meta) meta[kv.Key] = kv.Value;
                }

                if (options.Cause is not null)
                {
                    var causes = WalkCauses(options.Cause);
                    if (causes.Count > 0)
                    {
                        meta ??= new Dictionary<string, object?>();
                        meta["error.causes"] = causes;
                    }
                }
            }

            // Apply defaults
            if (next.Level is null) next = next with { Level = _options.DefaultLevel };
            if (next.Source is null) next = next with { Source = _options.DefaultSource };
            if (string.IsNullOrEmpty(next.Timestamp)) next = next with { Timestamp = NowIso() };
            if (string.IsNullOrEmpty(next.SessionId)) next = next with { SessionId = _sessionIdProvider() };
            if (string.IsNullOrEmpty(next.AppSlug)) next = next with { AppSlug = _options.AppSlug };
            if (string.IsNullOrEmpty(next.DeploymentEnv)) next = next with { DeploymentEnv = _options.DeploymentEnv };
            if (string.IsNullOrEmpty(next.ServiceName)) next = next with { ServiceName = _options.ServiceName };
            if (string.IsNullOrEmpty(next.AppVersion)) next = next with { AppVersion = _options.AppVersion };

            // User context
            var user = _userProvider();
            if (user is not null)
            {
                meta ??= new Dictionary<string, object?>();
                if (!string.IsNullOrEmpty(user.Id)) meta["user.id"] = user.Id;
                if (!string.IsNullOrEmpty(user.Email)) meta["user.email"] = user.Email;
                if (!string.IsNullOrEmpty(user.Name)) meta["user.name"] = user.Name;
                if (user.Extra is not null)
                {
                    foreach (var kv in user.Extra) meta["user." + kv.Key] = kv.Value;
                }
            }

            if (meta is not null && meta.Count > 0)
            {
                next = next with { Meta = meta };
            }

            return next;
        }

        /// <summary>Builds an <see cref="ErrorEntry"/> from an exception.</summary>
        public ErrorEntry FromException(Exception exception)
        {
            return new ErrorEntry
            {
                Message = exception.Message ?? exception.GetType().Name,
                Stack = StackTraceCapture.FromException(exception),
            };
        }

        /// <summary>Builds an <see cref="ErrorEntry"/> from a message.</summary>
        public ErrorEntry FromMessage(string message, ElsLevel level)
        {
            return new ErrorEntry
            {
                Message = message ?? string.Empty,
                Level = level,
            };
        }

        internal static string NowIso()
        {
            return DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffK", CultureInfo.InvariantCulture);
        }

        private static List<string> WalkCauses(Exception root)
        {
            var causes = new List<string>();
            if (root is AggregateException agg)
            {
                foreach (var ie in agg.Flatten().InnerExceptions)
                {
                    causes.Add(ie.Message ?? ie.GetType().Name);
                    if (causes.Count >= MaxCauseDepth) return causes;
                }
                return causes;
            }
            var current = root.InnerException;
            int depth = 0;
            while (current is not null && depth < MaxCauseDepth)
            {
                causes.Add(current.Message ?? current.GetType().Name);
                current = current.InnerException;
                depth++;
            }
            return causes;
        }

        private static IDictionary<string, object?> CopyReadonly(IReadOnlyDictionary<string, object?> src)
        {
            var copy = new Dictionary<string, object?>(src.Count);
            foreach (var kv in src) copy[kv.Key] = kv.Value;
            return copy;
        }
    }
}
