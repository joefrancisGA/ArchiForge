using ArchLucid.Cli.Commands;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class RoiBulletinMarkdownFormatterTests
{
    [Fact]
    public void FormatDraft_includes_stats_and_minimum_n()
    {
        RoiBulletinPreviewPayload payload = new()
        {
            Quarter = "Q1-2026",
            TenantCount = 12,
            MeanBaselineHours = 18.5m,
            MedianBaselineHours = 16m,
            P90BaselineHours = 40m
        };

        string md = RoiBulletinMarkdownFormatter.FormatDraft(payload, 5);

        md.Should().Contain("Q1-2026");
        md.Should().Contain("12");
        md.Should().Contain("18.5");
        md.Should().Contain("16");
        md.Should().Contain("40");
        md.Should().Contain("Minimum-N gate");
        md.Should().Contain("DRAFT");
    }

    [Fact]
    public void FormatSynthetic_stamps_rows_and_forbids_changelog()
    {
        RoiBulletinPreviewPayload payload = new()
        {
            Quarter = "Q1-2026",
            TenantCount = 5,
            MeanBaselineHours = 22.4m,
            MedianBaselineHours = 20m,
            P90BaselineHours = 46m
        };

        string md = RoiBulletinMarkdownFormatter.FormatSynthetic(payload, 5);

        md.Should().Contain(RoiBulletinMarkdownFormatter.SyntheticRowStamp);
        md.Should().Contain("CHANGELOG");
        md.Should().Contain("ROI bulletin signed");
        md.Should().Contain("22.4");
    }
}
