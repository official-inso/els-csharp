using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Inso.Els.Tests.Helpers;
using Xunit;

namespace Inso.Els.Tests.Transport
{
    public class TryHealthAsyncTests
    {
        [Fact]
        public async Task Success_ReturnsHealthy()
        {
            var handler = new StubHandler();
            handler.EnqueueOk();
            var http = new HttpClient(handler);
            await using var client = new ElsClient(new ElsOptions
            {
                Endpoint = "https://els.example",
                ApiKey = "k",
                HttpClient = http,
                BufferDir = System.IO.Path.GetTempPath(),
            });

            var result = await client.TryHealthAsync();
            result.IsHealthy.Should().BeTrue();
            result.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task ServerError_ReturnsUnhealthyWithStatus()
        {
            var handler = new StubHandler();
            handler.EnqueueStatus(HttpStatusCode.ServiceUnavailable);
            var http = new HttpClient(handler);
            await using var client = new ElsClient(new ElsOptions
            {
                Endpoint = "https://els.example",
                ApiKey = "k",
                MaxRetries = 0,
                HttpClient = http,
                BufferDir = System.IO.Path.GetTempPath(),
            });

            var result = await client.TryHealthAsync();
            result.IsHealthy.Should().BeFalse();
            result.StatusCode.Should().Be(503);
            result.Error.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task DoesNotThrow_OnNetworkError()
        {
            var handler = new StubHandler();
            handler.EnqueueException(new HttpRequestException("boom"));
            var http = new HttpClient(handler);
            await using var client = new ElsClient(new ElsOptions
            {
                Endpoint = "https://els.example",
                ApiKey = "k",
                MaxRetries = 0,
                HttpClient = http,
                BufferDir = System.IO.Path.GetTempPath(),
            });

            var result = await client.TryHealthAsync();
            result.IsHealthy.Should().BeFalse();
        }
    }
}
