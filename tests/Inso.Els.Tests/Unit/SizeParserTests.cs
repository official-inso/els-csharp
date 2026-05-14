using FluentAssertions;
using Inso.Els.Internal;
using Xunit;

namespace Inso.Els.Tests.Unit
{
    public class SizeParserTests
    {
        [Theory]
        [InlineData("100", 100L)]
        [InlineData("100B", 100L)]
        [InlineData("1KB", 1024L)]
        [InlineData("1MB", 1L * 1024 * 1024)]
        [InlineData("1GB", 1L * 1024 * 1024 * 1024)]
        [InlineData("1TB", 1L * 1024 * 1024 * 1024 * 1024)]
        [InlineData("100mb", 100L * 1024 * 1024)]
        [InlineData("100 MB", 100L * 1024 * 1024)]
        [InlineData("1.5MB", (long)(1.5 * 1024 * 1024))]
        public void Parse_ValidValues(string input, long expected)
        {
            SizeParser.TryParse(input, out var v).Should().BeTrue();
            v.Should().Be(expected);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("garbage")]
        [InlineData("-5MB")]
        [InlineData("MB")]
        public void Parse_Invalid(string? input)
        {
            SizeParser.TryParse(input, out _).Should().BeFalse();
        }
    }
}
