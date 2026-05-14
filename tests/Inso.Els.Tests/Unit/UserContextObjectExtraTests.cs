using System.Collections.Generic;
using FluentAssertions;
using Inso.Els.Internal;
using Xunit;

namespace Inso.Els.Tests.Unit
{
    public class UserContextObjectExtraTests
    {
        [Fact]
        public void Apply_NonStringExtra_PreservesType()
        {
            var opts = new ElsOptions { Endpoint = "https://x", ApiKey = "k" }.Normalize();
            var user = new UserContext
            {
                Id = "u-1",
                Extra = new Dictionary<string, object?>
                {
                    ["tenant"] = "acme",
                    ["userId"] = 42,
                    ["isStaff"] = true,
                    ["created"] = 1_700_000_000L,
                },
            };
            var enricher = new EntryEnricher(opts, () => "session", () => user);

            var result = enricher.Apply(new ErrorEntry { Message = "m", Url = "u" }, null);

            result.Meta!["user.id"].Should().Be("u-1");
            result.Meta["user.tenant"].Should().Be("acme");
            result.Meta["user.userId"].Should().Be(42);
            result.Meta["user.isStaff"].Should().Be(true);
            result.Meta["user.created"].Should().Be(1_700_000_000L);
        }
    }
}
