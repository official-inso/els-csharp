using System;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inso.Els.AspNetCore
{
    /// <summary>DI extensions for registering the ELS client and middleware.</summary>
    public static class ElsServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="IElsClient"/> as a singleton along with the
        /// exception middleware and a hosted service that flushes the client
        /// on application shutdown.
        /// </summary>
        public static IServiceCollection AddEls(this IServiceCollection services, Action<ElsOptionsBuilder> configure)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            services.AddSingleton<IElsClient>(_ =>
            {
                var builder = new ElsOptionsBuilder();
                configure(builder);
                return new ElsClient(builder.Build());
            });

            services.AddSingleton<ElsExceptionMiddleware>();
            services.AddHostedService<ElsHostedService>();

            return services;
        }

        /// <summary>
        /// Registers <see cref="IElsClient"/> with options bound from an
        /// <see cref="IConfiguration"/> section (e.g. <c>"Els"</c>).
        /// </summary>
        public static IServiceCollection AddEls(this IServiceCollection services, IConfiguration section)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            if (section is null) throw new ArgumentNullException(nameof(section));

            return services.AddEls(builder => Bind(section, builder));
        }

        private static void Bind(IConfiguration section, ElsOptionsBuilder b)
        {
            string? Get(string key) => section[key];
            int GetInt(string key, int fallback) => int.TryParse(Get(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : fallback;
            long GetLong(string key, long fallback) => long.TryParse(Get(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : fallback;
            double GetDouble(string key, double fallback) => double.TryParse(Get(key), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : fallback;
            bool GetBool(string key, bool fallback) => bool.TryParse(Get(key), out var v) ? v : fallback;
            TimeSpan GetTs(string key, TimeSpan fallback) => TimeSpan.TryParse(Get(key), CultureInfo.InvariantCulture, out var v) ? v : fallback;

            b.Endpoint = Get("Endpoint") ?? b.Endpoint;
            b.ApiKey = Get("ApiKey") ?? b.ApiKey;
            b.AppSlug = Get("AppSlug") ?? b.AppSlug;
            b.DeploymentEnv = Get("DeploymentEnv") ?? b.DeploymentEnv;
            b.ServiceName = Get("ServiceName") ?? b.ServiceName;
            b.AppVersion = Get("AppVersion") ?? b.AppVersion;
            b.BatchSize = GetInt("BatchSize", b.BatchSize);
            b.BatchInterval = GetTs("BatchInterval", b.BatchInterval);
            b.BufferSize = GetInt("BufferSize", b.BufferSize);
            b.MaxRetries = GetInt("MaxRetries", b.MaxRetries);
            b.RetryBaseDelay = GetTs("RetryBaseDelay", b.RetryBaseDelay);
            b.Timeout = GetTs("Timeout", b.Timeout);
            b.FlushTimeout = GetTs("FlushTimeout", b.FlushTimeout);
            b.BufferDir = Get("BufferDir") ?? b.BufferDir;
            b.MaxBufferFileSize = GetLong("MaxBufferFileSize", b.MaxBufferFileSize);
            b.BufferFileName = Get("BufferFileName") ?? b.BufferFileName;
            b.MinLevel = ElsLevelExtensions.Parse(Get("MinLevel")) ?? b.MinLevel;
            b.SampleRate = GetDouble("SampleRate", b.SampleRate);
            b.Debug = GetBool("Debug", b.Debug);

            if (string.Equals(Get("AuthScheme"), "ApiKeyHeader", StringComparison.OrdinalIgnoreCase))
            {
                b.AuthScheme = ElsAuthScheme.ApiKeyHeader;
            }
        }
    }
}
