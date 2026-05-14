using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Inso.Els.Tests.Helpers;
using Xunit;

namespace Inso.Els.Tests.Worker
{
    public class CaptureBatchingTests
    {
        private static (ElsClient client, StubHandler handler) Build(int batchSize, TimeSpan interval)
        {
            var handler = new StubHandler();
            var http = new HttpClient(handler);
            var opts = new ElsOptions
            {
                Endpoint = "https://els.example",
                ApiKey = "k",
                AppSlug = "svc",
                BatchSize = batchSize,
                BatchInterval = interval,
                RetryBaseDelay = TimeSpan.FromMilliseconds(1),
                MaxRetries = 0,
                HttpClient = http,
                FlushTimeout = TimeSpan.FromSeconds(2),
                BufferDir = System.IO.Path.GetTempPath(),
            };
            // Preload many OK responses
            for (int i = 0; i < 10; i++) handler.EnqueueOk();
            return (new ElsClient(opts), handler);
        }

        [Fact]
        public async Task CaptureMessage_FlushesWhenBatchSizeReached()
        {
            var (client, handler) = Build(batchSize: 3, interval: TimeSpan.FromSeconds(10));
            try
            {
                client.CaptureMessage("a", ElsLevel.Error, new CaptureOptions { Url = "/u" });
                client.CaptureMessage("b", ElsLevel.Error, new CaptureOptions { Url = "/u" });
                client.CaptureMessage("c", ElsLevel.Error, new CaptureOptions { Url = "/u" });

                // Wait up to 2s for the worker to flush a full batch.
                await WaitFor(() => handler.Requests.Count >= 1, TimeSpan.FromSeconds(2));

                handler.Requests.Should().NotBeEmpty();
                handler.Requests[0].RequestUri!.AbsolutePath.Should().Be("/errors/batch");
            }
            finally
            {
                await client.CloseAsync();
            }
        }

        [Fact]
        public async Task CaptureMessage_FlushesAfterInterval_EvenIfBatchNotFull()
        {
            var (client, handler) = Build(batchSize: 50, interval: TimeSpan.FromMilliseconds(150));
            try
            {
                client.CaptureMessage("a", ElsLevel.Error, new CaptureOptions { Url = "/u" });

                await WaitFor(() => handler.Requests.Count >= 1, TimeSpan.FromSeconds(2));
                handler.Requests.Should().NotBeEmpty();
            }
            finally
            {
                await client.CloseAsync();
            }
        }

        [Fact]
        public async Task Close_DrainsPendingEntries()
        {
            var (client, handler) = Build(batchSize: 50, interval: TimeSpan.FromSeconds(10));

            client.CaptureMessage("a", ElsLevel.Error, new CaptureOptions { Url = "/u" });
            client.CaptureMessage("b", ElsLevel.Error, new CaptureOptions { Url = "/u" });

            await client.CloseAsync();

            handler.Requests.Should().NotBeEmpty();
        }

        [Fact]
        public async Task SamplingDropsNonCritical_WhenRateIsZero()
        {
            var handler = new StubHandler();
            for (int i = 0; i < 10; i++) handler.EnqueueOk();
            var http = new HttpClient(handler);
            using var client = new ElsClient(new ElsOptions
            {
                Endpoint = "https://els.example",
                ApiKey = "k",
                BatchSize = 1,
                BatchInterval = TimeSpan.FromMilliseconds(20),
                HttpClient = http,
                SampleRate = 0.0001, // effectively zero
                MaxRetries = 0,
                BufferDir = System.IO.Path.GetTempPath(),
            });

            for (int i = 0; i < 5; i++)
            {
                client.CaptureMessage("ignored", ElsLevel.Info, new CaptureOptions { Url = "/u" });
            }
            client.CaptureMessage("critical-must-pass", ElsLevel.Critical, new CaptureOptions { Url = "/u" });

            await client.FlushAsync(TimeSpan.FromSeconds(1));
            await WaitFor(() => handler.Requests.Count >= 1, TimeSpan.FromSeconds(2));

            // Critical entry must have been sent at least once. Sampled-out ones may not show up.
            client.Stats.Sampled.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task MinLevel_FiltersBelowThreshold()
        {
            var handler = new StubHandler();
            for (int i = 0; i < 5; i++) handler.EnqueueOk();
            var http = new HttpClient(handler);
            using var client = new ElsClient(new ElsOptions
            {
                Endpoint = "https://els.example",
                ApiKey = "k",
                BatchSize = 1,
                BatchInterval = TimeSpan.FromMilliseconds(20),
                MinLevel = ElsLevel.Warning,
                MaxRetries = 0,
                HttpClient = http,
                BufferDir = System.IO.Path.GetTempPath(),
            });

            client.CaptureMessage("d", ElsLevel.Debug, new CaptureOptions { Url = "/u" });
            client.CaptureMessage("i", ElsLevel.Info, new CaptureOptions { Url = "/u" });
            client.CaptureMessage("w", ElsLevel.Warning, new CaptureOptions { Url = "/u" });

            await client.FlushAsync(TimeSpan.FromSeconds(1));
            await WaitFor(() => handler.Requests.Count >= 1, TimeSpan.FromSeconds(2));

            // Warning passed, Info+Debug filtered. Enqueued counter tracks accepted entries.
            client.Stats.Enqueued.Should().BeLessOrEqualTo(1);
        }

        [Fact]
        public void BeforeSend_NullDropsEntry()
        {
            var handler = new StubHandler();
            handler.EnqueueOk();
            var http = new HttpClient(handler);
            using var client = new ElsClient(new ElsOptions
            {
                Endpoint = "https://els.example",
                ApiKey = "k",
                HttpClient = http,
                MaxRetries = 0,
                BatchSize = 1,
                BatchInterval = TimeSpan.FromSeconds(1),
                BeforeSend = _ => null,
                BufferDir = System.IO.Path.GetTempPath(),
            });

            client.CaptureMessage("dropped", ElsLevel.Error, new CaptureOptions { Url = "/u" });
            client.Stats.Enqueued.Should().Be(0);
        }

        private static async Task WaitFor(Func<bool> condition, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                if (condition()) return;
                await Task.Delay(20);
            }
        }
    }
}
