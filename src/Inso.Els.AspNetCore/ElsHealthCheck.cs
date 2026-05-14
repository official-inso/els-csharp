using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Inso.Els.AspNetCore
{
    /// <summary>
    /// <see cref="IHealthCheck"/> backed by <see cref="IElsClient.TryHealthAsync"/>.
    /// Register via <c>services.AddHealthChecks().AddEls()</c>.
    /// </summary>
    internal sealed class ElsHealthCheck : IHealthCheck
    {
        private readonly IElsClient _client;

        public ElsHealthCheck(IElsClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var result = await _client.TryHealthAsync(cancellationToken).ConfigureAwait(false);
            if (result.IsHealthy)
            {
                return HealthCheckResult.Healthy($"ELS reachable in {result.Latency.TotalMilliseconds:F0}ms");
            }
            var description = result.Error ?? "ELS unreachable";
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                description,
                exception: null,
                data: new System.Collections.Generic.Dictionary<string, object>
                {
                    ["statusCode"] = result.StatusCode ?? 0,
                    ["latencyMs"] = result.Latency.TotalMilliseconds,
                });
        }
    }

    /// <summary>Extension for registering <see cref="ElsHealthCheck"/> via the Health Checks API.</summary>
    public static class ElsHealthCheckExtensions
    {
        /// <summary>
        /// Adds an <see cref="IHealthCheck"/> probing the ELS server using
        /// <see cref="IElsClient.TryHealthAsync"/>. Requires <see cref="IElsClient"/>
        /// to be registered (e.g. via <c>services.AddEls(...)</c>).
        /// </summary>
        public static IHealthChecksBuilder AddEls(
            this IHealthChecksBuilder builder,
            string name = "els",
            HealthStatus failureStatus = HealthStatus.Unhealthy)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            return builder.AddCheck<ElsHealthCheck>(name, failureStatus);
        }
    }
}
