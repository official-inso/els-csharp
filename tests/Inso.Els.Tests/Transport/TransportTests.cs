using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Inso.Els.Tests.Helpers;
using Xunit;

namespace Inso.Els.Tests.Transport
{
    public class TransportTests
    {
        private static (ElsClient client, StubHandler handler) BuildClient(
            int maxRetries = 3,
            ElsAuthScheme scheme = ElsAuthScheme.Bearer,
            int batchSize = 1)
        {
            var handler = new StubHandler();
            var http = new HttpClient(handler);
            var opts = new ElsOptions
            {
                Endpoint = "https://els.example",
                ApiKey = "k_secret",
                AppSlug = "svc",
                MaxRetries = maxRetries,
                RetryBaseDelay = TimeSpan.FromMilliseconds(1),
                BatchSize = batchSize,
                BatchInterval = TimeSpan.FromMilliseconds(5),
                HttpClient = http,
                AuthScheme = scheme,
            };
            return (new ElsClient(opts), handler);
        }

        [Fact]
        public async Task SendAsync_PostsToErrorsEndpoint_WithBearer()
        {
            var (client, handler) = BuildClient();
            handler.EnqueueOk();

            await client.SendAsync(new InvalidOperationException("boom"),
                new CaptureOptions { Url = "/api/x" });

            handler.Requests.Should().ContainSingle();
            var req = handler.Requests[0];
            req.RequestUri!.AbsolutePath.Should().Be("/errors");
            req.Headers.Authorization!.Scheme.Should().Be("Bearer");
            req.Headers.Authorization.Parameter.Should().Be("k_secret");
            req.Headers.UserAgent.ToString().Should().Contain("inso-els-csharp");

            await client.CloseAsync();
        }

        [Fact]
        public async Task SendAsync_AppliesApiKeyHeader_WhenConfigured()
        {
            var (client, handler) = BuildClient(scheme: ElsAuthScheme.ApiKeyHeader);
            handler.EnqueueOk();

            await client.SendAsync(new Exception("x"), new CaptureOptions { Url = "/u" });

            var req = handler.Requests[0];
            req.Headers.Authorization.Should().BeNull();
            req.Headers.TryGetValues("X-API-Key", out var values).Should().BeTrue();
            values!.First().Should().Be("k_secret");

            await client.CloseAsync();
        }

        [Fact]
        public async Task SendAsync_RetriesOn429_RespectingRetryAfter()
        {
            var (client, handler) = BuildClient(maxRetries: 2);
            handler.EnqueueStatus(
                (HttpStatusCode)429,
                configure: r => r.Headers.TryAddWithoutValidation("Retry-After", "0"));
            handler.EnqueueOk();

            await client.SendAsync(new Exception("x"), new CaptureOptions { Url = "/u" });

            handler.Requests.Should().HaveCount(2);
            await client.CloseAsync();
        }

        [Fact]
        public async Task SendAsync_RetriesOn5xx_ThenSucceeds()
        {
            var (client, handler) = BuildClient(maxRetries: 2);
            handler.EnqueueStatus(HttpStatusCode.InternalServerError, "boom");
            handler.EnqueueOk();

            await client.SendAsync(new Exception("x"), new CaptureOptions { Url = "/u" });

            handler.Requests.Should().HaveCount(2);
            await client.CloseAsync();
        }

        [Fact]
        public async Task SendAsync_PermanentOn4xx_ThrowsAndNoRetry()
        {
            var (client, handler) = BuildClient(maxRetries: 3);
            handler.EnqueueStatus(HttpStatusCode.BadRequest, "{\"error\":\"VALIDATION_ERROR\"}");

            Func<Task> act = () => client.SendAsync(new Exception("x"), new CaptureOptions { Url = "/u" });
            var ex = await act.Should().ThrowAsync<ElsSendException>();
            ex.Which.StatusCode.Should().Be(400);
            ex.Which.IsRetryable.Should().BeFalse();
            handler.Requests.Should().HaveCount(1);

            await client.CloseAsync();
        }

        [Fact]
        public async Task SendAsync_ThrowsRetryableAfterMaxRetries_OnPersistent5xx()
        {
            var (client, handler) = BuildClient(maxRetries: 2);
            handler.EnqueueStatus(HttpStatusCode.InternalServerError);
            handler.EnqueueStatus(HttpStatusCode.InternalServerError);
            handler.EnqueueStatus(HttpStatusCode.InternalServerError);

            Func<Task> act = () => client.SendAsync(new Exception("x"), new CaptureOptions { Url = "/u" });
            var ex = await act.Should().ThrowAsync<ElsSendException>();
            ex.Which.IsRetryable.Should().BeTrue();
            ex.Which.StatusCode.Should().Be(500);
            handler.Requests.Should().HaveCount(3); // initial + 2 retries

            await client.CloseAsync();
        }

        [Fact]
        public async Task HealthAsync_Success_DoesNotThrow()
        {
            var (client, handler) = BuildClient();
            handler.EnqueueOk("{\"status\":\"ok\"}");

            await client.HealthAsync();
            handler.Requests[0].RequestUri!.AbsolutePath.Should().Be("/health");
            handler.Requests[0].Method.Should().Be(HttpMethod.Get);

            await client.CloseAsync();
        }

        [Fact]
        public async Task HealthAsync_503_Throws()
        {
            var (client, handler) = BuildClient();
            handler.EnqueueStatus(HttpStatusCode.ServiceUnavailable, "{\"status\":\"down\"}");

            Func<Task> act = () => client.HealthAsync();
            await act.Should().ThrowAsync<ElsSendException>();

            await client.CloseAsync();
        }
    }
}
