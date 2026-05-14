using System;
using System.Collections.Generic;
using FluentAssertions;
using Inso.Els.Internal;
using Xunit;

namespace Inso.Els.Tests.Unit
{
    public class EntryEnricherTests
    {
        private static EntryEnricher CreateEnricher(UserContext? user = null, ElsOptions? options = null)
        {
            var opts = (options ?? new ElsOptions { Endpoint = "https://x", ApiKey = "k", AppSlug = "svc", DeploymentEnv = "DEV" }).Normalize();
            return new EntryEnricher(opts, () => "session-1", () => user);
        }

        [Fact]
        public void Apply_FillsDefaults()
        {
            var enricher = CreateEnricher();
            var result = enricher.Apply(new ErrorEntry { Message = "m", Url = "u" }, null);

            result.Level.Should().Be(ElsLevel.Error);
            result.Source.Should().Be(ElsSource.Server);
            result.AppSlug.Should().Be("svc");
            result.DeploymentEnv.Should().Be("DEV");
            result.SessionId.Should().Be("session-1");
            result.Timestamp.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Apply_UserContext_EnrichesMeta()
        {
            var user = new UserContext
            {
                Id = "u-1",
                Email = "a@b",
                Name = "Anna",
                Extra = new Dictionary<string, string> { ["tenant"] = "acme" },
            };
            var enricher = CreateEnricher(user: user);

            var result = enricher.Apply(new ErrorEntry { Message = "m", Url = "u" }, null);

            result.Meta.Should().NotBeNull();
            result.Meta!["user.id"].Should().Be("u-1");
            result.Meta["user.email"].Should().Be("a@b");
            result.Meta["user.name"].Should().Be("Anna");
            result.Meta["user.tenant"].Should().Be("acme");
        }

        [Fact]
        public void Apply_Cause_FlattensInnerExceptionChain()
        {
            var root = new Exception("outer", new Exception("middle", new Exception("inner")));
            var enricher = CreateEnricher();

            var result = enricher.Apply(
                new ErrorEntry { Message = "m", Url = "u" },
                new CaptureOptions { Cause = root });

            result.Meta.Should().NotBeNull();
            result.Meta!.Should().ContainKey("error.causes");
            var causes = (List<string>?)result.Meta!["error.causes"];
            causes.Should().NotBeNull();
            causes!.Should().HaveCount(2);
            causes![0].Should().Be("middle");
            causes![1].Should().Be("inner");
        }

        [Fact]
        public void Apply_AggregateException_FlattensInnerExceptions()
        {
            var agg = new AggregateException(new Exception("a"), new Exception("b"));
            var enricher = CreateEnricher();

            var result = enricher.Apply(
                new ErrorEntry { Message = "m", Url = "u" },
                new CaptureOptions { Cause = agg });

            var causes = (List<string>?)result.Meta!["error.causes"];
            causes.Should().NotBeNull();
            causes!.Should().Contain("a").And.Contain("b");
        }

        [Fact]
        public void Apply_PerCallOverridesTakePrecedence()
        {
            var enricher = CreateEnricher();
            var result = enricher.Apply(
                new ErrorEntry { Message = "m", Url = "u", Level = ElsLevel.Warning },
                new CaptureOptions { Level = ElsLevel.Critical, Url = "/override" });

            result.Level.Should().Be(ElsLevel.Critical);
            result.Url.Should().Be("/override");
        }
    }
}
