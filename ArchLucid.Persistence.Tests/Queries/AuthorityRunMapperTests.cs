using ArchLucid.Decisioning.Manifest.Sections;
using ArchLucid.Decisioning.Models;

using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Queries;

namespace ArchLucid.Persistence.Tests.Queries;

[Trait("Category", "Unit")]
public sealed class AuthorityRunMapperTests
{
    [SkippableFact]
    public void MapSummary_projects_run_record_fields()
    {
        Guid runId = Guid.NewGuid();
        RunRecord run = new()
        {
            RunId = runId,
            ProjectId = "my-proj",
            Description = "d",
            CreatedUtc = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            ContextSnapshotId = Guid.NewGuid(),
            GraphSnapshotId = Guid.NewGuid(),
            FindingsSnapshotId = Guid.NewGuid(),
            GoldenManifestId = Guid.NewGuid(),
            DecisionTraceId = Guid.NewGuid(),
            ArtifactBundleId = Guid.NewGuid()
        };

        RunSummaryDto dto = AuthorityRunMapper.MapSummary(run);

        dto.RunId.Should().Be(runId);
        dto.ProjectId.Should().Be("my-proj");
        dto.Description.Should().Be("d");
        dto.CreatedUtc.Should().Be(run.CreatedUtc);
        dto.ContextSnapshotId.Should().Be(run.ContextSnapshotId);
        dto.GraphSnapshotId.Should().Be(run.GraphSnapshotId);
        dto.FindingsSnapshotId.Should().Be(run.FindingsSnapshotId);
        dto.GoldenManifestId.Should().Be(run.GoldenManifestId);
        dto.DecisionTraceId.Should().Be(run.DecisionTraceId);
        dto.ArtifactBundleId.Should().Be(run.ArtifactBundleId);
    }

    [SkippableFact]
    public void MapManifestSummary_projects_manifest_counts_and_status()
    {
        Guid manifestId = Guid.NewGuid();
        Guid runId = Guid.NewGuid();
        ManifestDocument manifest = new()
        {
            ManifestId = manifestId,
            RunId = runId,
            CreatedUtc = DateTime.UtcNow,
            ManifestHash = "h",
            RuleSetId = "r",
            RuleSetVersion = "v",
            Metadata = new ManifestMetadata { Status = "Committed" },
            Decisions = [new ResolvedArchitectureDecision()],
            Warnings = ["w"],
            UnresolvedIssues = new UnresolvedIssuesSection
            {
                Items =
                [
                    new ManifestIssue
                    {
                        IssueType = "t",
                        Title = "title",
                        Description = "d",
                        Severity = "Medium"
                    }
                ]
            }
        };

        ManifestSummaryDto dto = AuthorityRunMapper.MapManifestSummary(manifest);

        dto.ManifestId.Should().Be(manifestId);
        dto.RunId.Should().Be(runId);
        dto.CreatedUtc.Should().Be(manifest.CreatedUtc);
        dto.ManifestHash.Should().Be("h");
        dto.RuleSetId.Should().Be("r");
        dto.RuleSetVersion.Should().Be("v");
        dto.DecisionCount.Should().Be(1);
        dto.WarningCount.Should().Be(1);
        dto.UnresolvedIssueCount.Should().Be(1);
        dto.Status.Should().Be("Committed");
    }
}
