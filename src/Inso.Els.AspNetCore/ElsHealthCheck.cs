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

        /// <summary>Optional per-probe timeout. Combined with the framework cancellation token.</summary>
        public TimeSpan? ProbeTimeout { get; set; }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (ProbeTimeout is { } timeout && timeout > TimeSpan.Zero)
            {
                cts.CancelAfter(timeout);
            }

            ElsHealthResult result;
            try
            {
                result = await _client.TryHealthAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return new HealthCheckResult(
                    context.Registration.FailureStatus,
                    description: $"ELS probe timed out after {ProbeTimeout?.TotalMilliseconds:F0}ms",
                    exception: null,
                    data: new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["timedOut"] = true,
                    });
            }

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
        /// <param name="builder">The Health Checks builder.</param>
        /// <param name="name">Name of the check. Default: <c>"els"</c>.</param>
        /// <param name="failureStatus">Status reported on failure. Default: <see cref="HealthStatus.Unhealthy"/>.</param>
        /// <param name="timeout">
        /// Optional per-probe timeout. When set, the check returns
        /// <paramref name="failureStatus"/> with <c>data["timedOut"] = true</c>
        /// instead of hanging on a stuck network call.
        /// </param>
        public static IHealthChecksBuilder AddEls(
            this IHealthChecksBuilder builder,
            string name = "els",
            HealthStatus failureStatus = HealthStatus.Unhealthy,
            TimeSpan? timeout = null)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));

            builder.Services.AddSingleton<ElsHealthCheck>(sp => new ElsHealthCheck(sp.GetRequiredService<IElsClient>())
            {
                ProbeTimeout = timeout,
            });
            return builder.AddCheck<ElsHealthCheck>(name, failureStatus);
        }
    }
}
