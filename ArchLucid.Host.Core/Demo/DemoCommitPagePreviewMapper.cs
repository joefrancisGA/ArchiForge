using ArchLucid.Application.Audit;
using ArchLucid.ArtifactSynthesis.Packaging;
using ArchLucid.Core.Explanation;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Queries;

namespace ArchLucid.Host.Core.Demo;

/// <summary>Maps authority read-model DTOs into <see cref="DemoCommitPagePreviewResponse"/> for demo + public showcase.</summary>
public static class DemoCommitPagePreviewMapper
{
    private const int PipelineTimelinePreviewCap = 10;

    /// <summary>Builds a preview payload when detail, manifest, and explanation are all present.</summary>
    public static DemoCommitPagePreviewResponse? TryBuild(
        DateTimeOffset generatedUtc,
        bool isDemoData,
        string? demoStatusMessage,
        RunDetailDto? detail,
        ManifestSummaryDto? manifestDto,
        IReadOnlyList<ArtifactDescriptor> descriptors,
        IReadOnlyList<RunPipelineTimelineItemDto>? timeline,
        RunExplanationSummary? explanation,
        ILogger logger,
        Guid runIdForLog)
    {
        if (detail is not null && manifestDto is not null && explanation is not null)
            return new DemoCommitPagePreviewResponse
            {
                GeneratedUtc = generatedUtc,
                IsDemoData = isDemoData,
                DemoStatusMessage = demoStatusMessage ?? string.Empty,
                Run = MapRun(detail.Run),
                AuthorityChain = MapAuthorityChain(detail.Run),
                Manifest = MapManifest(manifestDto),
                Artifacts = MapArtifacts(descriptors),
                PipelineTimeline = MapTimeline(timeline),
                RunExplanation = explanation,
            };
        logger.LogWarning(
            "Commit page preview: missing detail/manifest/explanation for run {RunId} (detail null? {DetailNull}, manifest null? {ManifestNull}, explain null? {ExplainNull}).",
            runIdForLog,
            detail is null,
            manifestDto is null,
            explanation is null);

        return null;

    }

    private static DemoPreviewRun MapRun(RunRecord r) => new()
    {
        RunId = r.RunId.ToString("N"),
        ProjectId = r.ProjectId,
        Description = r.Description,
        CreatedUtc = r.CreatedUtc,
    };

    private static DemoPreviewAuthorityChain MapAuthorityChain(RunRecord r) => new()
    {
        ContextSnapshotId = FormatId(r.ContextSnapshotId),
        GraphSnapshotId = FormatId(r.GraphSnapshotId),
        FindingsSnapshotId = FormatId(r.FindingsSnapshotId),
        GoldenManifestId = FormatId(r.GoldenManifestId),
        DecisionTraceId = FormatId(r.DecisionTraceId),
        ArtifactBundleId = FormatId(r.ArtifactBundleId),
    };

    private static string? FormatId(Guid? id) =>
        id is { } g && g != Guid.Empty ? g.ToString("N") : null;

    private static DemoPreviewManifestSummary MapManifest(ManifestSummaryDto m) => new()
    {
        ManifestId = m.ManifestId.ToString("N"),
        RunId = m.RunId.ToString("N"),
        CreatedUtc = m.CreatedUtc,
        ManifestHash = m.ManifestHash,
        RuleSetId = m.RuleSetId,
        RuleSetVersion = m.RuleSetVersion,
        DecisionCount = m.DecisionCount,
        WarningCount = m.WarningCount,
        UnresolvedIssueCount = m.UnresolvedIssueCount,
        Status = m.Status,
        HasWarnings = m.WarningCount > 0,
        HasUnresolvedIssues = m.UnresolvedIssueCount > 0,
        OperatorSummary =
            $"{m.DecisionCount} decisions, {m.WarningCount} warnings, {m.UnresolvedIssueCount} unresolved issues, status {m.Status}",
    };

    private static IReadOnlyList<DemoPreviewArtifact> MapArtifacts(IReadOnlyList<ArtifactDescriptor> descriptors) =>
        descriptors
            .Select(a => new DemoPreviewArtifact
            {
                ArtifactId = a.ArtifactId.ToString("N"),
                ArtifactType = a.ArtifactType,
                Name = a.Name,
                Format = a.Format,
                CreatedUtc = a.CreatedUtc,
                ContentHash = a.ContentHash,
            })
            .ToList();

    private static IReadOnlyList<DemoPreviewTimelineItem> MapTimeline(IReadOnlyList<RunPipelineTimelineItemDto>? items)
    {
        if (items is null || items.Count == 0)
            return [];

        return items
            .Take(PipelineTimelinePreviewCap)
            .Select(e => new DemoPreviewTimelineItem
            {
                EventId = e.EventId.ToString("N"),
                OccurredUtc = e.OccurredUtc,
                EventType = e.EventType,
                ActorUserName = e.ActorUserName,
                CorrelationId = e.CorrelationId,
            })
            .ToList();
    }
}
