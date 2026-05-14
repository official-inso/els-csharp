using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Inso.Els.Tests.Helpers;
using Xunit;

namespace Inso.Els.Tests.Integration
{
    public class EndToEndTests
    {
        [Fact]
        public async Task EndToEnd_BatchDeliveryViaLoopbackServer()
        {
            using var server = new LoopbackServer();
            server.StubBatchOk(created: 3);
            server.StubSingleOk();

            await using var client = new ElsClient(new ElsOptions
            {
                Endpoint = server.BaseUrl,
                ApiKey = "k",
                AppSlug = "svc",
                BatchSize = 3,
                BatchInterval = TimeSpan.FromMilliseconds(50),
                MaxRetries = 0,
                RetryBaseDelay = TimeSpan.FromMilliseconds(5),
                BufferDir = System.IO.Path.GetTempPath(),
            });

            for (int i = 0; i < 3; i++)
            {
                client.CaptureMessage($"m-{i}", ElsLevel.Error, new CaptureOptions { Url = "/api" });
            }

            await client.FlushAsync(TimeSpan.FromSeconds(2));
            await Task.Delay(300);

            server.Log.Should().NotBeEmpty();
            var entry = server.Log.First(e => e.RequestMessage.Path == "/errors/batch");
            entry.Should().NotBeNull();

            var doc = JsonDocument.Parse(entry!.RequestMessage.Body ?? "{}");
            doc.RootElement.GetProperty("errors").GetArrayLength().Should().Be(3);
        }
    }
}
