using System;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Inso.Els.Tests.Helpers;
using Xunit;

namespace Inso.Els.Tests.Unit
{
    public class AsyncBeforeSendTests
    {
        [Fact]
        public async Task BeforeSendAsync_ReturnsNull_DropsEntry()
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
                BeforeSendAsync = _ => new ValueTask<ErrorEntry?>((ErrorEntry?)null),
            });

            client.CaptureMessage("dropped", ElsLevel.Error, url: "/u");
            await client.FlushAsync(TimeSpan.FromSeconds(1));

            client.Stats.Enqueued.Should().Be(0);
        }

        [Fact]
        public async Task BeforeSendAsync_RunsBeforeBeforeSend()
        {
            var handler = new StubHandler();
            handler.EnqueueOk();
            var http = new HttpClient(handler);
            string sequence = "";
            await using var client = new ElsClient(new ElsOptions
            {
                Endpoint = "https://els.example",
                ApiKey = "k",
                BatchSize = 1,
                BatchInterval = TimeSpan.FromMilliseconds(20),
                MaxRetries = 0,
                HttpClient = http,
                BufferDir = System.IO.Path.GetTempPath(),
                BeforeSendAsync = async entry =>
                {
                    await Task.Yield();
                    sequence += "A";
                    return entry;
                },
                BeforeSend = entry =>
                {
                    sequence += "S";
                    return entry;
                },
            });

            client.CaptureMessage("x", ElsLevel.Error, url: "/u");

            // Fire-and-forget capture runs hooks on a background task; wait
            // for both to execute before asserting on their order.
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
            while (sequence.Length < 2 && DateTime.UtcNow < deadline)
            {
                await Task.Delay(20);
            }
            await client.FlushAsync(TimeSpan.FromSeconds(1));

            sequence.Should().Be("AS");
        }

        [Fact]
        public async Task BeforeSendAsync_Throw_ReportsToOnError_AndDrops()
        {
            var handler = new StubHandler();
            handler.EnqueueOk();
            var http = new HttpClient(handler);
            int onErrorCount = 0;
            await using var client = new ElsClient(new ElsOptions
            {
                Endpoint = "https://els.example",
                ApiKey = "k",
                BatchSize = 1,
                BatchInterval = TimeSpan.FromMilliseconds(20),
                MaxRetries = 0,
                HttpClient = http,
                BufferDir = System.IO.Path.GetTempPath(),
                BeforeSendAsync = _ => throw new InvalidOperationException("hook crash"),
                OnError = _ => System.Threading.Interlocked.Increment(ref onErrorCount),
            });

            client.CaptureMessage("x", ElsLevel.Error, url: "/u");
            await client.FlushAsync(TimeSpan.FromSeconds(1));

            onErrorCount.Should().BeGreaterThan(0);
            client.Stats.Enqueued.Should().Be(0);
        }
    }
}
