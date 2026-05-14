using FluentAssertions;
using Xunit;

namespace Inso.Els.Tests.Unit
{
    public class ElsLevelTests
    {
        [Theory]
        [InlineData(ElsLevel.Debug, "debug")]
        [InlineData(ElsLevel.Info, "info")]
        [InlineData(ElsLevel.Warning, "warning")]
        [InlineData(ElsLevel.Error, "error")]
        [InlineData(ElsLevel.Critical, "critical")]
        public void ToWireValue_MatchesWireFormat(ElsLevel level, string expected)
        {
            level.ToWireValue().Should().Be(expected);
        }

        [Theory]
        [InlineData("debug", ElsLevel.Debug)]
        [InlineData("INFO", ElsLevel.Info)]
        [InlineData("Warning", ElsLevel.Warning)]
        [InlineData("warn", ElsLevel.Warning)]
        [InlineData("error", ElsLevel.Error)]
        [InlineData("critical", ElsLevel.Critical)]
        [InlineData("fatal", ElsLevel.Critical)]
        public void Parse_Accepts(string input, ElsLevel expected)
        {
            ElsLevelExtensions.Parse(input).Should().Be(expected);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("nope")]
        public void Parse_ReturnsNullOnInvalid(string? input)
        {
            ElsLevelExtensions.Parse(input).Should().BeNull();
        }

        [Fact]
        public void EnumOrder_DebugLessThanCritical()
        {
            (ElsLevel.Debug < ElsLevel.Critical).Should().BeTrue();
            (ElsLevel.Error > ElsLevel.Warning).Should().BeTrue();
        }
    }
}
