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
    public class MiddlewareTests
    {
        [Fact]
        public async Task Middleware_RethrowDefault_ReportsExceptionAndPropagates()
        {
            var captured = new RecordingClient();
            using var host = await BuildHostAsync(captured, rethrow: true);
            var client = host.GetTestClient();

            Func<Task> act = () => client.GetAsync("/boom");
            await act.Should().ThrowAsync<Exception>();
            captured.Errors.Should().HaveCount(1);
            captured.Errors[0].Exception.Message.Should().Be("kaboom");
            captured.Errors[0].Options!.Level.Should().Be(ElsLevel.Critical);
            captured.Errors[0].Options!.Url.Should().StartWith("GET /boom");
        }

        [Fact]
        public async Task Middleware_RethrowFalse_Returns500AndReports()
        {
            var captured = new RecordingClient();
            using var host = await BuildHostAsync(captured, rethrow: false);
            var client = host.GetTestClient();

            var response = await client.GetAsync("/boom");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.InternalServerError);
            captured.Errors.Should().HaveCount(1);
        }

        [Fact]
        public void WithHttpContext_PopulatesMetaFromRequest()
        {
            var ctx = new DefaultHttpContext();
            ctx.Request.Method = "POST";
            ctx.Request.Path = "/api/orders";
            ctx.Request.Scheme = "https";
            ctx.Request.Host = new HostString("example.test");
            ctx.Request.Headers["User-Agent"] = "tester/1.0";
            ctx.Request.Headers["Referer"] = "https://prev";
            ctx.Request.Headers["Accept-Language"] = "en-US";
            ctx.Request.Headers["X-Request-Id"] = "rid-1";

            var options = new CaptureOptions().WithHttpContext(ctx);

            options.Url.Should().Be("POST /api/orders");
            options.UserAgent.Should().Be("tester/1.0");
            options.Referrer.Should().Be("https://prev");
            options.Language.Should().Be("en-US");
            options.Meta.Should().NotBeNull();
            options.Meta!["http.method"].Should().Be("POST");
            options.Meta["http.requestId"].Should().Be("rid-1");
        }

        private static async Task<IHost> BuildHostAsync(IElsClient client, bool rethrow)
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
                        app.UseElsExceptionHandling(rethrow);
                        app.Run(_ => throw new InvalidOperationException("kaboom"));
                    });
                });
            var host = await builder.StartAsync();
            return host;
        }

        private sealed class RecordingClient : IElsClient
        {
            public List<(Exception Exception, CaptureOptions? Options)> Errors { get; } = new();
            public string SessionId => "test-session";
            public UserContext? User { get; set; }
            public ElsStats Stats => default;
            public int QueueSize => 0;

            public void CaptureError(Exception exception, CaptureOptions? options = null)
                => Errors.Add((exception, options));
            public void CaptureMessage(string message, ElsLevel level, CaptureOptions? options = null) { }
            public void CaptureEntry(ErrorEntry entry, CaptureOptions? options = null) { }
            public Task SendAsync(Exception exception, CaptureOptions? options = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task SendEntryAsync(ErrorEntry entry, CaptureOptions? options = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task HealthAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task FlushAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task CloseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public void SetSessionId(string sessionId) { }
            public void Dispose() { }
            public ValueTask DisposeAsync() => default;
        }
    }
}
