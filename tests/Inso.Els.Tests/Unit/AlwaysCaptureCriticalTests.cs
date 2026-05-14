using System;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Inso.Els.Tests.Helpers;
using Xunit;

namespace Inso.Els.Tests.Unit
{
    public class AlwaysCaptureCriticalTests
    {
        [Fact]
        public async Task Default_True_CriticalPassesEvenWithZeroSampleRate()
        {
            var handler = new StubHandler();
            for (int i = 0; i < 5; i++) handler.EnqueueOk();
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
                SampleRate = 0.0001, // effectively zero
            });

            client.CaptureMessage("critical-pass", ElsLevel.Critical, url: "/u");
            await client.FlushAsync(TimeSpan.FromSeconds(1));

            client.Stats.Enqueued.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task False_CriticalIsSubjectToSampling()
        {
            var handler = new StubHandler();
            for (int i = 0; i < 5; i++) handler.EnqueueOk();
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
                SampleRate = 0.0,
                AlwaysCaptureCritical = false,
            });

            for (int i = 0; i < 20; i++)
            {
                client.CaptureMessage($"c-{i}", ElsLevel.Critical, url: "/u");
            }
            await client.FlushAsync(TimeSpan.FromSeconds(1));

            client.Stats.Sampled.Should().BeGreaterThan(0);
        }
    }
}
