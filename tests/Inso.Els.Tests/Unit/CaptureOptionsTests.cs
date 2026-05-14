using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Inso.Els.Tests.Unit
{
    public class CaptureOptionsTests
    {
        [Fact]
        public void WithUrl_SetsUrl()
        {
            var opts = new CaptureOptions().WithUrl("/api");
            opts.Url.Should().Be("/api");
        }

        [Fact]
        public void WithLevel_And_WithSource()
        {
            var opts = new CaptureOptions().WithLevel(ElsLevel.Critical).WithSource(ElsSource.Client);
            opts.Level.Should().Be(ElsLevel.Critical);
            opts.Source.Should().Be(ElsSource.Client);
        }

        [Fact]
        public void WithMetaItem_AddsKeyWithoutMutatingOriginal()
        {
            var first = new CaptureOptions().WithMetaItem("a", 1);
            var second = first.WithMetaItem("b", 2);

            first.Meta.Should().ContainKey("a");
            first.Meta.Should().NotContainKey("b");
            second.Meta.Should().ContainKey("a").And.ContainKey("b");
        }

        [Fact]
        public void WithMeta_ReplacesMeta()
        {
            var opts = new CaptureOptions().WithMetaItem("a", 1).WithMeta(new Dictionary<string, object?> { ["x"] = "y" });
            opts.Meta.Should().NotContainKey("a").And.ContainKey("x");
        }

        [Fact]
        public void WithCause_Stored()
        {
            var inner = new Exception("inner");
            var opts = new CaptureOptions().WithCause(inner);
            opts.Cause.Should().BeSameAs(inner);
        }
    }
}
