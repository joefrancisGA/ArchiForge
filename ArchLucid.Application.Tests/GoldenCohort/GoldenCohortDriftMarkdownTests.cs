using ArchLucid.Application.GoldenCohort;

using FluentAssertions;

namespace ArchLucid.Application.Tests.GoldenCohort;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class GoldenCohortDriftMarkdownTests
{
    [Fact]
    public void BuildReport_includes_table_and_summary()
    {
        GoldenCohortDriftRow[] rows =
        [
            new GoldenCohortDriftRow("gc-001", "aa", "bb", false, "Cost", "Topology", false),
        ];

        string md = GoldenCohortDriftMarkdown.BuildReport(DateTimeOffset.Parse("2026-04-21T12:00:00Z"), rows, "Preamble line.");

        md.Should().Contain("# Golden cohort drift report");
        md.Should().Contain("gc-001");
        md.Should().Contain("Preamble line.");
        md.Should().Contain("1 / 1 items drifted.");
    }
}
