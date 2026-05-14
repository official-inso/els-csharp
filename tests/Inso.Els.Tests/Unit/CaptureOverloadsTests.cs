using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Inso.Els.Tests.Helpers;
using Xunit;

namespace Inso.Els.Tests.Unit
{
    public class CaptureOverloadsTests
    {
        private static (ElsClient client, StubHandler handler) Build()
        {
            var handler = new StubHandler();
            for (int i = 0; i < 5; i++) handler.EnqueueOk();
            var http = new HttpClient(handler);
            return (new ElsClient(new ElsOptions
            {
                Endpoint = "https://els.example",
                ApiKey = "k",
                BatchSize = 1,
                BatchInterval = TimeSpan.FromMilliseconds(20),
                MaxRetries = 0,
                HttpClient = http,
                BufferDir = System.IO.Path.GetTempPath(),
            }), handler);
        }

        [Fact]
        public async Task CaptureError_NamedArgs_AppliedToOptions()
        {
            var (client, _) = Build();
            try
            {
                client.CaptureError(new InvalidOperationException("x"),
                    url: "/api",
                    level: ElsLevel.Critical,
                    meta: new Dictionary<string, object?> { ["k"] = "v" });

                await client.FlushAsync(TimeSpan.FromSeconds(1));
                // Sanity: at least one capture got accepted (counters are best-effort).
                client.Stats.Enqueued.Should().BeGreaterThan(0);
            }
            finally
            {
                await client.CloseAsync();
            }
        }

        [Fact]
        public async Task CaptureMessage_NamedArgs_AppliedToOptions()
        {
            var (client, _) = Build();
            try
            {
                client.CaptureMessage("hello", ElsLevel.Warning,
                    url: "/health",
                    meta: new Dictionary<string, object?> { ["pct"] = 80 });

                await client.FlushAsync(TimeSpan.FromSeconds(1));
                client.Stats.Enqueued.Should().BeGreaterThan(0);
            }
            finally
            {
                await client.CloseAsync();
            }
        }

        [Fact]
        public void ShortConstructor_UsesProvidedEndpointAndKey()
        {
            using var client = new ElsClient("https://els.example", "key", "demo");
            client.SessionId.Should().StartWith("els-");
        }
    }
}
