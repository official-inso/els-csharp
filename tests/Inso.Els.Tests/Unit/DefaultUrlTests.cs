using System;
using FluentAssertions;
using Inso.Els.Internal;
using Xunit;

namespace Inso.Els.Tests.Unit
{
    public class DefaultUrlTests
    {
        [Fact]
        public void Apply_EmptyUrl_FallsBackToAppSlug()
        {
            var opts = new ElsOptions { Endpoint = "https://x", ApiKey = "k", AppSlug = "my-app" }.Normalize();
            var enricher = new EntryEnricher(opts, () => "s", () => null);

            var result = enricher.Apply(new ErrorEntry { Message = "m" }, null);

            result.Url.Should().Be("my-app");
        }

        [Fact]
        public void Apply_EmptyUrl_NoAppSlug_FallsBackToUnknown()
        {
            var opts = new ElsOptions { Endpoint = "https://x", ApiKey = "k" }.Normalize();
            var enricher = new EntryEnricher(opts, () => "s", () => null);

            var result = enricher.Apply(new ErrorEntry { Message = "m" }, null);

            result.Url.Should().Be("unknown");
        }
    }
}
