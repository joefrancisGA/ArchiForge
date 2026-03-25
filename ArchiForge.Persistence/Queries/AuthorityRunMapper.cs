using ArchiForge.Decisioning.Models;
using ArchiForge.Persistence.Models;

namespace ArchiForge.Persistence.Queries;

/// <summary>
/// Shared projection helpers used by both <see cref="DapperAuthorityQueryService"/> and
/// <see cref="InMemoryAuthorityQueryService"/> to map persistence models to query DTOs.
/// Centralised here to prevent the two implementations from diverging silently.
/// </summary>
internal static class AuthorityRunMapper
{
    /// <summary>Projects a <see cref="RunRecord"/> to a <see cref="RunSummaryDto"/>.</summary>
    internal static RunSummaryDto MapSummary(RunRecord run) => new()
    {
        RunId = run.RunId,
        ProjectId = run.ProjectId,
        Description = run.Description,
        CreatedUtc = run.CreatedUtc,
        ContextSnapshotId = run.ContextSnapshotId,
        GraphSnapshotId = run.GraphSnapshotId,
        FindingsSnapshotId = run.FindingsSnapshotId,
        GoldenManifestId = run.GoldenManifestId,
        DecisionTraceId = run.DecisionTraceId,
        ArtifactBundleId = run.ArtifactBundleId
    };

    /// <summary>Projects a <see cref="GoldenManifest"/> to a <see cref="ManifestSummaryDto"/>.</summary>
    internal static ManifestSummaryDto MapManifestSummary(GoldenManifest manifest) => new()
    {
        ManifestId = manifest.ManifestId,
        RunId = manifest.RunId,
        CreatedUtc = manifest.CreatedUtc,
        ManifestHash = manifest.ManifestHash,
        RuleSetId = manifest.RuleSetId,
        RuleSetVersion = manifest.RuleSetVersion,
        DecisionCount = manifest.Decisions.Count,
        WarningCount = manifest.Warnings.Count,
        UnresolvedIssueCount = manifest.UnresolvedIssues.Items.Count,
        Status = manifest.Metadata.Status
    };
}
