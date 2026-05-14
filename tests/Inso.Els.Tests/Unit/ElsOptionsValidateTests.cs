using FluentAssertions;
using Xunit;

namespace Inso.Els.Tests.Unit
{
    public class ElsOptionsValidateTests
    {
        [Fact]
        public void Validate_AcceptsValidOptions()
        {
            var opts = new ElsOptions { Endpoint = "https://els.example", ApiKey = "k" };
            opts.Validate().Should().BeEmpty();
        }

        [Fact]
        public void Validate_ReportsMissingRequired()
        {
            var opts = new ElsOptions();
            var issues = opts.Validate();
            issues.Should().Contain(s => s.Contains("Endpoint"));
            issues.Should().Contain(s => s.Contains("ApiKey"));
        }

        [Fact]
        public void Validate_ReportsMalformedEndpoint()
        {
            var opts = new ElsOptions { Endpoint = "not-a-url", ApiKey = "k" };
            opts.Validate().Should().Contain(s => s.Contains("not a valid absolute URI"));
        }

        [Fact]
        public void Validate_ReportsBadSampleRate()
        {
            var opts = new ElsOptions { Endpoint = "https://els.example", ApiKey = "k", SampleRate = 2.5 };
            opts.Validate().Should().Contain(s => s.Contains("SampleRate"));
        }

        [Fact]
        public void Validate_ReportsAppVersionOver128Chars()
        {
            var opts = new ElsOptions
            {
                Endpoint = "https://els.example",
                ApiKey = "k",
                AppVersion = new string('v', 129),
            };
            opts.Validate().Should().Contain(s => s.Contains("AppVersion"));
        }
    }
}
