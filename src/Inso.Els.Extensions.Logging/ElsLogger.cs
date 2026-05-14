using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Inso.Els.Extensions.Logging
{
    internal sealed class ElsLogger : ILogger
    {
        private readonly string _category;
        private readonly IElsClient _client;
        private readonly ElsLoggerOptions _options;
        private readonly IExternalScopeProvider? _scopeProvider;

        public ElsLogger(string category, IElsClient client, ElsLoggerOptions options, IExternalScopeProvider? scopeProvider)
        {
            _category = category;
            _client = client;
            _options = options;
            _scopeProvider = scopeProvider;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => _scopeProvider?.Push(state) ?? NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel)
        {
            if (logLevel == LogLevel.None) return false;
            return MapLevel(logLevel) >= _options.MinLevel;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var message = formatter?.Invoke(state, exception) ?? state?.ToString() ?? string.Empty;
            var elsLevel = MapLevel(logLevel);

            var meta = new Dictionary<string, object?>
            {
                ["log.category"] = _category,
            };
            if (eventId.Id != 0) meta["log.eventId"] = eventId.Id;
            if (!string.IsNullOrEmpty(eventId.Name)) meta["log.eventName"] = eventId.Name;

            if (state is IReadOnlyList<KeyValuePair<string, object?>> kvps)
            {
                for (int i = 0; i < kvps.Count; i++)
                {
                    var kv = kvps[i];
                    if (kv.Key == "{OriginalFormat}") continue;
                    meta[kv.Key] = kv.Value;
                }
            }

            if (_options.IncludeScopes && _scopeProvider is not null)
            {
                _scopeProvider.ForEachScope((scope, accumulator) =>
                {
                    AppendScope(scope, accumulator);
                }, meta);
            }

            var options = new CaptureOptions
            {
                Url = _options.DefaultUrl,
                Level = elsLevel,
                Meta = meta,
            };

            if (exception is not null)
            {
                _client.CaptureError(exception, options.WithMetaItem("log.message", message));
            }
            else
            {
                _client.CaptureMessage(message, elsLevel, options);
            }
        }

        private static void AppendScope(object? scope, Dictionary<string, object?> sink)
        {
            switch (scope)
            {
                case null:
                    return;
                case IEnumerable<KeyValuePair<string, object?>> kvps:
                    foreach (var kv in kvps)
                    {
                        if (kv.Key == "{OriginalFormat}") continue;
                        sink["scope." + kv.Key] = kv.Value;
                    }
                    return;
                case IDictionary dict:
                    foreach (DictionaryEntry entry in dict)
                    {
                        sink["scope." + (entry.Key?.ToString() ?? "?")] = entry.Value;
                    }
                    return;
                default:
                    sink["scope"] = scope.ToString();
                    return;
            }
        }

        private static ElsLevel MapLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace:
                case LogLevel.Debug: return ElsLevel.Debug;
                case LogLevel.Information: return ElsLevel.Info;
                case LogLevel.Warning: return ElsLevel.Warning;
                case LogLevel.Error: return ElsLevel.Error;
                case LogLevel.Critical: return ElsLevel.Critical;
                default: return ElsLevel.Info;
            }
        }

        private sealed class NullScope : IDisposable
        {
            internal static readonly NullScope Instance = new NullScope();
            public void Dispose() { }
        }
    }
}
