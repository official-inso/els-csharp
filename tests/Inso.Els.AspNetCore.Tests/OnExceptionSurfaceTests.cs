using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Inso.Els.AspNetCore.Tests
{
    public class OnExceptionSurfaceTests
    {
        [Fact]
        public async Task OnException_Throws_FailureIsCapturedAsSeparateEntry()
        {
            var captured = new RecordingClient();
            using var host = await BuildAsync(captured, throwingHook: true);
            var client = host.GetTestClient();

            await client.GetAsync("/boom");

            // 1) original exception, 2) hook failure reported separately
            captured.Errors.Should().HaveCount(2);
            captured.Errors[1].Options!.Url.Should().Contain("OnException");
            captured.Errors[1].Options!.Level.Should().Be(ElsLevel.Warning);
        }

        private static async Task<IHost> BuildAsync(IElsClient client, bool throwingHook)
        {
            var builder = new HostBuilder()
                .ConfigureWebHost(web =>
                {
                    web.UseTestServer();
                    web.ConfigureServices(services =>
                    {
                        services.AddSingleton(client);
                        services.AddSingleton<ElsExceptionMiddleware>();
                    });
                    web.Configure(app =>
                    {
                        app.UseElsExceptionHandling(o =>
                        {
                            o.Mode = ElsExceptionMode.CaptureAndHandle;
                            if (throwingHook)
                            {
                                o.OnException = (_, _) => throw new InvalidOperationException("hook crash");
                            }
                        });
                        app.Run(_ => throw new InvalidOperationException("primary failure"));
                    });
                });
            return await builder.StartAsync();
        }

        private sealed class RecordingClient : IElsClient
        {
            public List<(Exception Exception, CaptureOptions? Options)> Errors { get; } = new();
            public string SessionId => "stub";
            public UserContext? User { get; set; }
            public ElsStats Stats => default;
            public int QueueSize => 0;
            public event EventHandler<ElsStats>? StatsChanged;

            public void CaptureError(Exception exception, CaptureOptions? options = null) => Errors.Add((exception, options));
            public void CaptureError(Exception exception, string? url, ElsLevel? level = null, IDictionary<string, object?>? meta = null, Exception? cause = null)
                => CaptureError(exception, new CaptureOptions { Url = url, Level = level, Meta = meta, Cause = cause });
            public void CaptureMessage(string message, ElsLevel level, CaptureOptions? options = null) { }
            public void CaptureMessage(string message, ElsLevel level, string? url, IDictionary<string, object?>? meta = null) { }
            public void CaptureEntry(ErrorEntry entry, CaptureOptions? options = null) { }
            public Task SendAsync(Exception exception, CaptureOptions? options = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task SendEntryAsync(ErrorEntry entry, CaptureOptions? options = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task HealthAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<ElsHealthResult> TryHealthAsync(CancellationToken cancellationToken = default)
                => Task.FromResult(new ElsHealthResult { IsHealthy = true });
            public Task FlushAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task CloseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public void SetSessionId(string sessionId) { }
            public void Dispose() { _ = StatsChanged; }
            public ValueTask DisposeAsync() => default;
        }
    }
}
