using ArchLucid.Application.Analysis;
using ArchLucid.Application.Diffs;

using FluentAssertions;

using Moq;

namespace ArchLucid.Application.Tests.Analysis;

/// <summary>
/// Branch coverage for <see cref="EndToEndReplayComparisonExportService"/>: executive profile, guards, and
/// manifest relationship subsections on the detailed path.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class EndToEndReplayComparisonExportServiceExecutiveAndRelationshipDiffTests
{
    [SkippableFact]
    public void GenerateMarkdown_executive_profile_emits_key_counts_not_full_run_metadata_section()
    {
        Mock<IEndToEndReplayComparisonSummaryFormatter> formatter = new();
        formatter.Setup(f => f.FormatMarkdown(It.IsAny<EndToEndReplayComparisonReport>()))
            .Returns("## Exec summary stub");

        EndToEndReplayComparisonExportService sut = new(formatter.Object);
        EndToEndReplayComparisonReport report = new()
        {
            LeftRunId = "L-exec", RightRunId = "R-exec", RunDiff = new RunMetadataDiffResult { ChangedFields = ["Alpha"], RequestIdsDiffer = true }
        };

        string md = sut.GenerateMarkdown(report, EndToEndComparisonExportProfile.Executive);

        md.Should().Contain("## Key counts");
        md.Should().Contain("Run metadata: 1 changed field(s); Request IDs differ: Yes");
        md.Should().NotContain("## Run Metadata Diff");
        md.Should().Contain("### Interpretation Notes");
    }

    [SkippableFact]
    public void GenerateMarkdown_null_report_throws_ArgumentNullException()
    {
        Mock<IEndToEndReplayComparisonSummaryFormatter> formatter = new();
        EndToEndReplayComparisonExportService sut = new(formatter.Object);

        Action act = () => sut.GenerateMarkdown(null!, EndToEndComparisonExportProfile.Short);

        act.Should().Throw<ArgumentNullException>().WithParameterName("report");
    }

    [SkippableFact]
    public void GenerateHtml_executive_profile_includes_key_counts_and_omits_agent_result_headings()
    {
        Mock<IEndToEndReplayComparisonSummaryFormatter> formatter = new();
        formatter.Setup(f => f.FormatMarkdown(It.IsAny<EndToEndReplayComparisonReport>()))
            .Returns("summary-line");

        EndToEndReplayComparisonExportService sut = new(formatter.Object);
        EndToEndReplayComparisonReport report = new()
        {
            LeftRunId = "L",
            RightRunId = "R",
            RunDiff = new RunMetadataDiffResult { ChangedFields = [] },
            AgentResultDiff = new AgentResultDiffResult { AgentDeltas = [] }
        };

        string html = sut.GenerateHtml(report, EndToEndComparisonExportProfile.Executive);

        html.Should().Contain("<h2>Key counts</h2>");
        html.Should().NotContain("Agent Result Diff");
    }

    [SkippableFact]
    public void GenerateMarkdown_detailed_includes_relationship_subsections_when_populated()
    {
        Mock<IEndToEndReplayComparisonSummaryFormatter> formatter = new();
        formatter.Setup(f => f.FormatMarkdown(It.IsAny<EndToEndReplayComparisonReport>()))
            .Returns("## Full summary");

        EndToEndReplayComparisonExportService sut = new(formatter.Object);
        EndToEndReplayComparisonReport report = new()
        {
            LeftRunId = "a",
            RightRunId = "b",
            ManifestDiff = new ManifestDiffResult
            {
                AddedRelationships =
                [
                    new RelationshipDiffItem { SourceId = "s1", TargetId = "t1", RelationshipType = "calls" }
                ],
                RemovedRelationships =
                [
                    new RelationshipDiffItem { SourceId = "s2", TargetId = "t2", RelationshipType = "reads" }
                ]
            }
        };

        string md = sut.GenerateMarkdown(report, EndToEndComparisonExportProfile.Detailed);

        md.Should().Contain("### Added Relationships");
        md.Should().Contain("s1 -> t1 (calls)");
        md.Should().Contain("### Removed Relationships");
        md.Should().Contain("s2 -> t2 (reads)");
    }

    [SkippableFact]
    public async Task GeneratePdfAsync_throws_when_cancellation_requested_before_render()
    {
        Mock<IEndToEndReplayComparisonSummaryFormatter> formatter = new();
        formatter.Setup(f => f.FormatMarkdown(It.IsAny<EndToEndReplayComparisonReport>()))
            .Returns("x");

        EndToEndReplayComparisonExportService sut = new(formatter.Object);
        EndToEndReplayComparisonReport report = new() { LeftRunId = "L", RightRunId = "R" };
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        Func<Task> act = async () => await sut.GeneratePdfAsync(report, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
