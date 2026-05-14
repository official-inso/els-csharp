using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Inso.Els.Extensions.Logging
{
    /// <summary>
    /// <see cref="ILoggerProvider"/> that routes log records to an
    /// <see cref="IElsClient"/>. Supports scopes via
    /// <see cref="ISupportExternalScope"/>.
    /// </summary>
    [ProviderAlias("Els")]
    public sealed class ElsLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private readonly IElsClient _client;
        private readonly ElsLoggerOptions _options;
        private readonly ConcurrentDictionary<string, ElsLogger> _loggers = new ConcurrentDictionary<string, ElsLogger>();
        private IExternalScopeProvider? _scopeProvider;

        /// <summary>Creates a provider using the given client and options.</summary>
        public ElsLoggerProvider(IElsClient client, ElsLoggerOptions? options = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _options = options ?? new ElsLoggerOptions();
        }

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
            => _loggers.GetOrAdd(categoryName, name => new ElsLogger(name, _client, _options, _scopeProvider));

        /// <inheritdoc />
        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}
