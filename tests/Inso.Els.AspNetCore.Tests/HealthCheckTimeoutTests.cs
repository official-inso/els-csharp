using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace Inso.Els.AspNetCore.Tests
{
    public class HealthCheckTimeoutTests
    {
        [Fact]
        public async Task ProbeTimeout_FiresFailure_WhenClientHangs()
        {
            var hanging = new HangingClient();
            var check = new ElsHealthCheck(hanging) { ProbeTimeout = TimeSpan.FromMilliseconds(50) };
            var ctx = new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("els", check, HealthStatus.Degraded, tags: Array.Empty<string>()),
            };

            var result = await check.CheckHealthAsync(ctx);
            result.Status.Should().Be(HealthStatus.Degraded);
            result.Data.Should().ContainKey("timedOut");
        }

        private sealed class HangingClient : IElsClient
        {
            public string SessionId => "stub";
            public UserContext? User { get; set; }
            public ElsStats Stats => default;
            public int QueueSize => 0;
            public event EventHandler<ElsStats>? StatsChanged;

            public void CaptureError(Exception exception, CaptureOptions? options = null) { }
            public void CaptureError(Exception exception, string? url, ElsLevel? level = null, IDictionary<string, object?>? meta = null, Exception? cause = null) { }
            public void CaptureMessage(string message, ElsLevel level, CaptureOptions? options = null) { }
            public void CaptureMessage(string message, ElsLevel level, string? url, IDictionary<string, object?>? meta = null) { }
            public void CaptureEntry(ErrorEntry entry, CaptureOptions? options = null) { }
            public Task SendAsync(Exception exception, CaptureOptions? options = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task SendEntryAsync(ErrorEntry entry, CaptureOptions? options = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task HealthAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public async Task<ElsHealthResult> TryHealthAsync(CancellationToken cancellationToken = default)
            {
                // Block until cancellation so the probe timeout has to kick in.
                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
                return new ElsHealthResult { IsHealthy = true };
            }
            public Task FlushAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task CloseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public void SetSessionId(string sessionId) { }
            public void Dispose() { _ = StatsChanged; }
            public ValueTask DisposeAsync() => default;
        }
    }
}
