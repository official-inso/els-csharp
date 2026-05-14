using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Inso.Els.Tests.Helpers;
using Xunit;

namespace Inso.Els.Tests.DiskBuffer
{
    public class DiskBufferTests
    {
        [Fact]
        public async Task FailedSend_PersistsToDisk()
        {
            using var temp = new TempBufferDir();
            var handler = new StubHandler();
            handler.EnqueueStatus(HttpStatusCode.BadRequest, "{\"error\":\"VALIDATION_ERROR\"}");
            var http = new HttpClient(handler);

            await using (var client = new ElsClient(new ElsOptions
            {
                Endpoint = "https://els.example",
                ApiKey = "k",
                BatchSize = 1,
                BatchInterval = TimeSpan.FromMilliseconds(20),
                MaxRetries = 0,
                HttpClient = http,
                BufferDir = temp.Path,
            }))
            {
                client.CaptureMessage("boom", ElsLevel.Error, new CaptureOptions { Url = "/u" });
                await client.FlushAsync(TimeSpan.FromSeconds(2));
                await Task.Delay(200);
            }

            var bufferFile = Path.Combine(temp.Path, ".els-buffer.jsonl");
            File.Exists(bufferFile).Should().BeTrue();
            var content = File.ReadAllText(bufferFile);
            content.Should().Contain("\"message\":\"boom\"");
        }

        [Fact]
        public async Task FlushOnStartup_DeletesFileAfterSuccess()
        {
            using var temp = new TempBufferDir();
            var bufferFile = Path.Combine(temp.Path, ".els-buffer.jsonl");

            // Pre-seed the buffer (simulates a previous run that couldn't send).
            File.WriteAllText(bufferFile,
                "{\"message\":\"queued-1\",\"url\":\"/u\",\"timestamp\":\"2026-05-14T00:00:00.0000000Z\",\"level\":\"error\",\"source\":\"server\"}\n");

            var handler = new StubHandler();
            handler.EnqueueOk();
            var http = new HttpClient(handler);

            await using (var client = new ElsClient(new ElsOptions
            {
                Endpoint = "https://els.example",
                ApiKey = "k",
                BatchSize = 1,
                MaxRetries = 0,
                HttpClient = http,
                BufferDir = temp.Path,
                BatchInterval = TimeSpan.FromMilliseconds(50),
            }))
            {
                await Task.Delay(400); // allow startup flush to run
            }

            File.Exists(bufferFile).Should().BeFalse();
            handler.Requests.Should().NotBeEmpty();
        }
    }
}
