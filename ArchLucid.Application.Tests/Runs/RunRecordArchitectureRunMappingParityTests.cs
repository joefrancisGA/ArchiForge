using ArchLucid.Application.Runs.Mapping;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Persistence.Models;

using FluentAssertions;

namespace ArchLucid.Application.Tests.Runs;

/// <summary>
/// Regression: authority <see cref="RunRecord"/> + task ids must map to the same <see cref="ArchitectureRun"/> API shape
/// as a hand-built reference row for identical logical data.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class RunRecordArchitectureRunMappingParityTests
{
    [SkippableFact]
    public void ToArchitectureRun_matches_reference_ArchitectureRun_for_same_logical_row()
    {
        Guid runGuid = Guid.Parse("77777777-7777-7777-7777-777777777777");
        string runN = runGuid.ToString("N");
        DateTime created = new(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc);
        Guid? graphId = Guid.Parse("88888888-8888-8888-8888-888888888888");

        ArchitectureRun legacy = new()
        {
            RunId = runN,
            RequestId = "REQ-PARITY",
            Status = ArchitectureRunStatus.TasksGenerated,
            CreatedUtc = created,
            CompletedUtc = null,
            CurrentManifestVersion = null,
            ContextSnapshotId = "aabbccddeeff00112233445566778899",
            GraphSnapshotId = graphId,
            ArtifactBundleId = null,
            TaskIds = ["t-a", "t-b"]
        };

        RunRecord authorityEquivalent = new()
        {
            RunId = runGuid,
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ScopeProjectId = Guid.NewGuid(),
            ProjectId = "ParityProject",
            ArchitectureRequestId = legacy.RequestId,
            LegacyRunStatus = legacy.Status.ToString(),
            CreatedUtc = legacy.CreatedUtc,
            CompletedUtc = legacy.CompletedUtc,
            CurrentManifestVersion = legacy.CurrentManifestVersion,
            ContextSnapshotId = Guid.ParseExact(legacy.ContextSnapshotId!, "N"),
            GraphSnapshotId = legacy.GraphSnapshotId,
            ArtifactBundleId = legacy.ArtifactBundleId,
        };

        ArchitectureRun mapped = RunRecordToArchitectureRunMapper.ToArchitectureRun(
            authorityEquivalent,
            legacy.TaskIds);

        mapped.RunId.Should().Be(legacy.RunId);
        mapped.RequestId.Should().Be(legacy.RequestId);
        mapped.Status.Should().Be(legacy.Status);
        mapped.CreatedUtc.Should().Be(legacy.CreatedUtc);
        mapped.CompletedUtc.Should().Be(legacy.CompletedUtc);
        mapped.CurrentManifestVersion.Should().Be(legacy.CurrentManifestVersion);
        mapped.ContextSnapshotId.Should().Be(legacy.ContextSnapshotId);
        mapped.GraphSnapshotId.Should().Be(legacy.GraphSnapshotId);
        mapped.ArtifactBundleId.Should().Be(legacy.ArtifactBundleId);
        mapped.TaskIds.Should().Equal(legacy.TaskIds);
    }
}
