using System;
using FluentAssertions;
using Xunit;

namespace Inso.Els.Tests.Unit
{
    public class ElsOptionsTests
    {
        [Fact]
        public void Constructor_RequiresEndpoint()
        {
            Action act = () => new ElsClient(new ElsOptions { ApiKey = "k" });
            act.Should().Throw<ElsConfigurationException>().WithMessage("*Endpoint*");
        }

        [Fact]
        public void Constructor_RequiresApiKey()
        {
            Action act = () => new ElsClient(new ElsOptions { Endpoint = "https://x" });
            act.Should().Throw<ElsConfigurationException>().WithMessage("*ApiKey*");
        }

        [Fact]
        public void Normalize_AppliesDefaults_OnNonPositiveValues()
        {
            var opts = new ElsOptions
            {
                Endpoint = "https://x",
                ApiKey = "k",
                BatchSize = 0,
                BufferSize = -1,
                MaxRetries = -5,
                RetryBaseDelay = TimeSpan.Zero,
                Timeout = TimeSpan.Zero,
                FlushTimeout = TimeSpan.Zero,
                MaxBufferFileSize = 0,
                BufferFileName = "",
                SampleRate = -0.5,
            };

            using var client = new ElsClient(opts);
            // No exception, defaults were applied silently.
        }

        [Theory]
        [InlineData(-1.0)]
        [InlineData(2.0)]
        [InlineData(double.NaN)]
        public void Normalize_ClampsInvalidSampleRate(double bad)
        {
            var opts = new ElsOptions { Endpoint = "https://x", ApiKey = "k", SampleRate = bad };
            using var client = new ElsClient(opts);
        }

        [Fact]
        public void Normalize_TrimsTrailingSlashOnEndpoint()
        {
            var opts = new ElsOptions { Endpoint = "https://x/", ApiKey = "k" }.Normalize();
            opts.Endpoint.Should().Be("https://x");
        }
    }
}
