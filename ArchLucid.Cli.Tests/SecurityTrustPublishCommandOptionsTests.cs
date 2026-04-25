using ArchLucid.Cli.Commands;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class SecurityTrustPublishCommandOptionsTests
{
    [Fact]
    public void Parse_requires_date_and_summary_url()
    {
        SecurityTrustPublishCommandOptions? opts = SecurityTrustPublishCommandOptions.Parse(
            ["--kind", "pen-test", "--summary-url", "https://example.com/s.md"],
            out string? error);

        opts.Should().BeNull();
        error.Should().Contain("--date");
    }

    [Fact]
    public void Parse_defaults_assessor_and_assessment_code_for_pen_test()
    {
        SecurityTrustPublishCommandOptions? opts = SecurityTrustPublishCommandOptions.Parse(
            [
                "--kind",
                "pen-test",
                "--date",
                "2026-07-29",
                "--summary-url",
                "https://example.com/summary.md"
            ],
            out string? error);

        error.Should().BeNull();
        opts!.AssessmentCode.Should().Be("2026-Q2");
        opts.AssessorDisplayName.Should().Be("Aeronova Red Team LLC");
        opts.PublishedOn.Should().Be("2026-07-29");
        opts.SummaryUrl.Should().Be("https://example.com/summary.md");
    }

    [Fact]
    public void Parse_rejects_unknown_kind()
    {
        SecurityTrustPublishCommandOptions? opts = SecurityTrustPublishCommandOptions.Parse(
            ["--kind", "soc2", "--date", "2026-07-29", "--summary-url", "https://x/y"],
            out string? error);

        opts.Should().BeNull();
        error.Should().Contain("pen-test");
    }
}
