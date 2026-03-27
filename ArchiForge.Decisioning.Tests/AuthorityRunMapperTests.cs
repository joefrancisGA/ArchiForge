using ArchiForge.Decisioning.Manifest.Sections;
using ArchiForge.Decisioning.Models;
using ArchiForge.Persistence.Models;
using ArchiForge.Persistence.Queries;

using FluentAssertions;

namespace ArchiForge.Decisioning.Tests;

/// <summary>
/// <see cref="AuthorityRunMapper"/> keeps <see cref="DapperAuthorityQueryService"/> and <see cref="InMemoryAuthorityQueryService"/> DTO projections aligned.
/// </summary>
[Trait("Category", "Unit")]
public sealed class AuthorityRunMapperTests
{
    [Fact]
    public void MapSummary_Projects_RunRecord_To_RunSummaryDto()
    {
        DateTime created = new(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);
        Guid runId = Guid.NewGuid();
        Guid ctx = Guid.NewGuid();
        Guid graph = Guid.NewGuid();
        Guid findings = Guid.NewGuid();
        Guid manifest = Guid.NewGuid();
        Guid trace = Guid.NewGuid();
        Guid bundle = Guid.NewGuid();

        RunRecord run = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ScopeProjectId = Guid.NewGuid(),
            RunId = runId,
            ProjectId = "my-project",
            Description = "desc",
            CreatedUtc = created,
            ContextSnapshotId = ctx,
            GraphSnapshotId = graph,
            FindingsSnapshotId = findings,
            GoldenManifestId = manifest,
            DecisionTraceId = trace,
            ArtifactBundleId = bundle
        };

        RunSummaryDto dto = AuthorityRunMapper.MapSummary(run);

        dto.RunId.Should().Be(runId);
        dto.ProjectId.Should().Be("my-project");
        dto.Description.Should().Be("desc");
        dto.CreatedUtc.Should().Be(created);
        dto.ContextSnapshotId.Should().Be(ctx);
        dto.GraphSnapshotId.Should().Be(graph);
        dto.FindingsSnapshotId.Should().Be(findings);
        dto.GoldenManifestId.Should().Be(manifest);
        dto.DecisionTraceId.Should().Be(trace);
        dto.ArtifactBundleId.Should().Be(bundle);
        dto.HasContextSnapshot.Should().BeTrue();
        dto.HasGoldenManifest.Should().BeTrue();
    }

    [Fact]
    public void MapManifestSummary_Projects_GoldenManifest_To_ManifestSummaryDto()
    {
        Guid manifestId = Guid.NewGuid();
        Guid runId = Guid.NewGuid();
        DateTime created = new(2026, 4, 2, 8, 30, 0, DateTimeKind.Utc);

        GoldenManifest manifest = new()
        {
            ManifestId = manifestId,
            RunId = runId,
            CreatedUtc = created,
            ManifestHash = "abc123",
            RuleSetId = "rules-v2",
            RuleSetVersion = "2.1.0",
            RuleSetHash = "rh",
            Metadata = new ManifestMetadata { Status = "Committed" },
            Decisions = [new ResolvedArchitectureDecision(), new ResolvedArchitectureDecision()],
            Warnings = ["w1", "w2", "w3"],
            UnresolvedIssues = new UnresolvedIssuesSection
            {
                Items =
                [
                    new ManifestIssue(),
                    new ManifestIssue()
                ]
            }
        };

        ManifestSummaryDto dto = AuthorityRunMapper.MapManifestSummary(manifest);

        dto.ManifestId.Should().Be(manifestId);
        dto.RunId.Should().Be(runId);
        dto.CreatedUtc.Should().Be(created);
        dto.ManifestHash.Should().Be("abc123");
        dto.RuleSetId.Should().Be("rules-v2");
        dto.RuleSetVersion.Should().Be("2.1.0");
        dto.DecisionCount.Should().Be(2);
        dto.WarningCount.Should().Be(3);
        dto.UnresolvedIssueCount.Should().Be(2);
        dto.Status.Should().Be("Committed");
    }
}
