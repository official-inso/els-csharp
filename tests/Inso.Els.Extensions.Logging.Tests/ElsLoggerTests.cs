using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Inso.Els.Extensions.Logging.Tests
{
    public class ElsLoggerTests
    {
        [Fact]
        public void LogError_RoutesToCaptureError_WithMappedLevel()
        {
            var client = new RecordingClient();
            var provider = new ElsLoggerProvider(client, new ElsLoggerOptions { MinLevel = ElsLevel.Info });
            var logger = provider.CreateLogger("my.category");

            var ex = new InvalidOperationException("boom");
            logger.LogError(ex, "user {UserId} not found", 42);

            client.Errors.Should().HaveCount(1);
            var (capturedEx, opts) = client.Errors[0];
            capturedEx.Should().BeSameAs(ex);
            opts!.Level.Should().Be(ElsLevel.Error);
            opts.Meta!["UserId"].Should().Be(42);
            opts.Meta!["log.category"].Should().Be("my.category");
        }

        [Fact]
        public void LogInformation_RoutesToCaptureMessage()
        {
            var client = new RecordingClient();
            var provider = new ElsLoggerProvider(client, new ElsLoggerOptions { MinLevel = ElsLevel.Debug });
            var logger = provider.CreateLogger("svc");

            logger.LogInformation("started");

            client.Messages.Should().HaveCount(1);
            client.Messages[0].Level.Should().Be(ElsLevel.Info);
            client.Messages[0].Message.Should().Be("started");
        }

        [Fact]
        public void LogBelowMinLevel_IsSkipped()
        {
            var client = new RecordingClient();
            var provider = new ElsLoggerProvider(client, new ElsLoggerOptions { MinLevel = ElsLevel.Warning });
            var logger = provider.CreateLogger("svc");

            logger.LogDebug("noisy");
            logger.LogInformation("noisy");
            logger.LogWarning("important");

            client.Messages.Should().HaveCount(1);
            client.Messages[0].Level.Should().Be(ElsLevel.Warning);
        }

        private sealed class RecordingClient : IElsClient
        {
            public List<(Exception Exception, CaptureOptions? Options)> Errors { get; } = new();
            public List<(string Message, ElsLevel Level, CaptureOptions? Options)> Messages { get; } = new();
            public string SessionId => "test-session";
            public UserContext? User { get; set; }
            public ElsStats Stats => default;
            public int QueueSize => 0;

            public event EventHandler<ElsStats>? StatsChanged;

            public void CaptureError(Exception exception, CaptureOptions? options = null) => Errors.Add((exception, options));
            public void CaptureError(Exception exception, string? url, ElsLevel? level = null, IDictionary<string, object?>? meta = null, Exception? cause = null)
                => CaptureError(exception, new CaptureOptions { Url = url, Level = level, Meta = meta, Cause = cause });
            public void CaptureMessage(string message, ElsLevel level, CaptureOptions? options = null) => Messages.Add((message, level, options));
            public void CaptureMessage(string message, ElsLevel level, string? url, IDictionary<string, object?>? meta = null)
                => CaptureMessage(message, level, new CaptureOptions { Url = url, Meta = meta });
            public void CaptureEntry(ErrorEntry entry, CaptureOptions? options = null) { }
            public Task SendAsync(Exception exception, CaptureOptions? options = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task SendEntryAsync(ErrorEntry entry, CaptureOptions? options = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task HealthAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<ElsHealthResult> TryHealthAsync(CancellationToken cancellationToken = default)
                => Task.FromResult(new ElsHealthResult { IsHealthy = true });
            public Task FlushAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task CloseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public void SetSessionId(string sessionId) { }
            public void Dispose() { _ = StatsChanged; }
            public ValueTask DisposeAsync() => default;
        }
    }
}
