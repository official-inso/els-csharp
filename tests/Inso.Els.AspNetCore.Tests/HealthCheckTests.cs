using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace Inso.Els.AspNetCore.Tests
{
    public class HealthCheckTests
    {
        [Fact]
        public async Task ReturnsHealthy_WhenClientReportsHealthy()
        {
            var client = new StubClient(new ElsHealthResult { IsHealthy = true, StatusCode = 200, Latency = TimeSpan.FromMilliseconds(12) });
            var check = new ElsHealthCheck(client);
            var ctx = new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("els", check, HealthStatus.Unhealthy, tags: Array.Empty<string>()),
            };

            var result = await check.CheckHealthAsync(ctx);
            result.Status.Should().Be(HealthStatus.Healthy);
        }

        [Fact]
        public async Task ReturnsFailureStatus_WhenClientReportsUnhealthy()
        {
            var client = new StubClient(new ElsHealthResult { IsHealthy = false, StatusCode = 503, Error = "down" });
            var check = new ElsHealthCheck(client);
            var ctx = new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("els", check, HealthStatus.Degraded, tags: Array.Empty<string>()),
            };

            var result = await check.CheckHealthAsync(ctx);
            result.Status.Should().Be(HealthStatus.Degraded);
            result.Data.Should().ContainKey("statusCode");
        }

        private sealed class StubClient : IElsClient
        {
            private readonly ElsHealthResult _result;
            public StubClient(ElsHealthResult result) { _result = result; }

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
            public Task<ElsHealthResult> TryHealthAsync(CancellationToken cancellationToken = default) => Task.FromResult(_result);
            public Task FlushAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task CloseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public void SetSessionId(string sessionId) { }
            public void Dispose() { _ = StatsChanged; }
            public ValueTask DisposeAsync() => default;
        }
    }
}
