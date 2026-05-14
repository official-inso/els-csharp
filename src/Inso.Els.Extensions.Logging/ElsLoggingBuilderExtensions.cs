using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Inso.Els.Extensions.Logging
{
    /// <summary>Extensions for adding the ELS logger provider to <see cref="ILoggingBuilder"/>.</summary>
    public static class ElsLoggingBuilderExtensions
    {
        /// <summary>
        /// Adds the <see cref="ElsLoggerProvider"/> to the logging pipeline.
        /// Resolves <see cref="IElsClient"/> from DI — register it first via
        /// <c>services.AddEls(...)</c>.
        /// </summary>
        public static ILoggingBuilder AddEls(this ILoggingBuilder builder, Action<ElsLoggerOptions>? configure = null)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));

            var options = new ElsLoggerOptions();
            configure?.Invoke(options);

            builder.Services.TryAddSingleton(options);
            builder.Services.AddSingleton<ILoggerProvider>(sp =>
            {
                var client = sp.GetRequiredService<IElsClient>();
                return new ElsLoggerProvider(client, options);
            });

            return builder;
        }
    }
}
