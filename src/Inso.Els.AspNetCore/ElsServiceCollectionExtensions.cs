using System;
using System.Globalization;
using Inso.Els.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

            return RegisterShared(services);
        }

        /// <summary>
        /// Registers <see cref="IElsClient"/> with options bound from an
        /// <see cref="IConfiguration"/> section (e.g. <c>"Els"</c>). Strings
        /// like <c>"100MB"</c> are accepted for <c>MaxBufferFileSize</c>. Invalid
        /// values (e.g. unknown <c>MinLevel</c>) throw
        /// <see cref="ElsConfigurationException"/> at startup.
        /// </summary>
        public static IServiceCollection AddEls(this IServiceCollection services, IConfiguration section)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            if (section is null) throw new ArgumentNullException(nameof(section));

            return services.AddEls(builder => Bind(section, builder));
        }

        /// <summary>
        /// Registers <see cref="IElsClient"/> using a pre-built <see cref="ElsOptions"/>.
        /// Useful when options are produced by <see cref="IOptions{TOptions}"/> pattern
        /// elsewhere (e.g. <c>services.Configure&lt;ElsOptions&gt;(config.GetSection("Els"))</c>).
        /// </summary>
        public static IServiceCollection AddElsFromOptions(this IServiceCollection services)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));

            services.AddSingleton<IElsClient>(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<ElsOptions>>().Value;
                return new ElsClient(opts);
            });

            return RegisterShared(services);
        }

        private static IServiceCollection RegisterShared(IServiceCollection services)
        {
            services.AddSingleton<ElsExceptionMiddleware>();
            services.AddHostedService<ElsHostedService>();
            return services;
        }

        private static void Bind(IConfiguration section, ElsOptionsBuilder b)
        {
            string? Get(string key) => section[key];

            int RequireInt(string key, int fallback)
            {
                var s = Get(key);
                if (string.IsNullOrEmpty(s)) return fallback;
                if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) return v;
                throw new ElsConfigurationException($"Els:{key} must be an integer (got '{s}').");
            }

            double RequireDouble(string key, double fallback)
            {
                var s = Get(key);
                if (string.IsNullOrEmpty(s)) return fallback;
                if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) return v;
                throw new ElsConfigurationException($"Els:{key} must be a number (got '{s}').");
            }

            bool RequireBool(string key, bool fallback)
            {
                var s = Get(key);
                if (string.IsNullOrEmpty(s)) return fallback;
                if (bool.TryParse(s, out var v)) return v;
                throw new ElsConfigurationException($"Els:{key} must be true or false (got '{s}').");
            }

            TimeSpan RequireTimeSpan(string key, TimeSpan fallback)
            {
                var s = Get(key);
                if (string.IsNullOrEmpty(s)) return fallback;
                if (TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out var v)) return v;
                throw new ElsConfigurationException($"Els:{key} must be a TimeSpan (e.g. \"00:00:10\", got '{s}').");
            }

            long RequireSize(string key, long fallback)
            {
                var s = Get(key);
                if (string.IsNullOrEmpty(s)) return fallback;
                if (SizeParser.TryParse(s, out var v)) return v;
                throw new ElsConfigurationException($"Els:{key} must be a byte size (e.g. \"100MB\", got '{s}').");
            }

            ElsLevel? RequireLevel(string key, ElsLevel? fallback)
            {
                var s = Get(key);
                if (string.IsNullOrEmpty(s)) return fallback;
                var parsed = ElsLevelExtensions.Parse(s);
                if (parsed is null)
                    throw new ElsConfigurationException($"Els:{key} must be one of debug/info/warning/error/critical (got '{s}').");
                return parsed;
            }

            ElsAuthScheme RequireAuth(string key, ElsAuthScheme fallback)
            {
                var s = Get(key);
                if (string.IsNullOrEmpty(s)) return fallback;
                if (string.Equals(s, "Bearer", StringComparison.OrdinalIgnoreCase)) return ElsAuthScheme.Bearer;
                if (string.Equals(s, "ApiKeyHeader", StringComparison.OrdinalIgnoreCase)) return ElsAuthScheme.ApiKeyHeader;
                throw new ElsConfigurationException($"Els:{key} must be Bearer or ApiKeyHeader (got '{s}').");
            }

            b.Endpoint = Get("Endpoint") ?? b.Endpoint;
            b.ApiKey = Get("ApiKey") ?? b.ApiKey;
            b.AppSlug = Get("AppSlug") ?? b.AppSlug;
            b.DeploymentEnv = Get("DeploymentEnv") ?? b.DeploymentEnv;
            b.ServiceName = Get("ServiceName") ?? b.ServiceName;
            b.AppVersion = Get("AppVersion") ?? b.AppVersion;
            b.BatchSize = RequireInt("BatchSize", b.BatchSize);
            b.BatchInterval = RequireTimeSpan("BatchInterval", b.BatchInterval);
            b.BufferSize = RequireInt("BufferSize", b.BufferSize);
            b.MaxRetries = RequireInt("MaxRetries", b.MaxRetries);
            b.RetryBaseDelay = RequireTimeSpan("RetryBaseDelay", b.RetryBaseDelay);
            b.Timeout = RequireTimeSpan("Timeout", b.Timeout);
            b.FlushTimeout = RequireTimeSpan("FlushTimeout", b.FlushTimeout);
            b.BufferDir = Get("BufferDir") ?? b.BufferDir;
            b.MaxBufferFileSize = RequireSize("MaxBufferFileSize", b.MaxBufferFileSize);
            b.BufferFileName = Get("BufferFileName") ?? b.BufferFileName;
            b.MinLevel = RequireLevel("MinLevel", b.MinLevel);
            b.SampleRate = RequireDouble("SampleRate", b.SampleRate);
            b.Debug = RequireBool("Debug", b.Debug);
            b.AuthScheme = RequireAuth("AuthScheme", b.AuthScheme);
        }
    }
}
