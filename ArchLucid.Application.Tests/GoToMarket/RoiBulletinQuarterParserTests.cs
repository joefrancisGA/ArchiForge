using ArchLucid.Core.GoToMarket;

using FluentAssertions;

namespace ArchLucid.Application.Tests.GoToMarket;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class RoiBulletinQuarterParserTests
{
    [Theory]
    [InlineData("Q1-2026", 2026, 1, 1, 2026, 4, 1)]
    [InlineData("Q2-2026", 2026, 4, 1, 2026, 7, 1)]
    [InlineData("Q3-2026", 2026, 7, 1, 2026, 10, 1)]
    [InlineData("Q4-2026", 2026, 10, 1, 2027, 1, 1)]
    public void TryParse_valid_quarters(string label, int yStart, int moStart, int dStart, int yEnd, int moEnd, int dEnd)
    {
        bool ok = RoiBulletinQuarterParser.TryParse(label, out RoiBulletinQuarterWindow window, out string? error);

        ok.Should().BeTrue();
        error.Should().BeNull();
        window.Label.Should().Be(label);
        window.StartUtcInclusive.Should().Be(new DateTimeOffset(yStart, moStart, dStart, 0, 0, 0, TimeSpan.Zero));
        window.EndUtcExclusive.Should().Be(new DateTimeOffset(yEnd, moEnd, dEnd, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void TryParse_rejects_invalid_format()
    {
        bool ok = RoiBulletinQuarterParser.TryParse("2026-Q1", out _, out string? error);

        ok.Should().BeFalse();
        error.Should().Contain("Invalid quarter");
    }

    [Fact]
    public void TryParse_rejects_empty()
    {
        bool ok = RoiBulletinQuarterParser.TryParse("   ", out _, out string? error);

        ok.Should().BeFalse();
        error.Should().Contain("Quarter is required");
    }
}
