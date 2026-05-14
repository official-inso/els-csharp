using System;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Inso.Els.Tests.Helpers;
using Xunit;

namespace Inso.Els.Tests.Unit
{
    public class StatsEventTests
    {
        [Fact]
        public async Task StatsChanged_FiresAfterSend()
        {
            var handler = new StubHandler();
            handler.EnqueueOk();
            var http = new HttpClient(handler);
            await using var client = new ElsClient(new ElsOptions
            {
                Endpoint = "https://els.example",
                ApiKey = "k",
                BatchSize = 1,
                BatchInterval = TimeSpan.FromMilliseconds(20),
                MaxRetries = 0,
                HttpClient = http,
                BufferDir = System.IO.Path.GetTempPath(),
            });
            int fired = 0;
            client.StatsChanged += (_, _) => System.Threading.Interlocked.Increment(ref fired);

            client.CaptureMessage("x", ElsLevel.Error, url: "/u");
            await client.FlushAsync(TimeSpan.FromSeconds(1));
            await Task.Delay(100);

            fired.Should().BeGreaterThan(0);
        }
    }
}
