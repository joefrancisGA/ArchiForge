using ArchLucid.AgentRuntime.Explanation;
using ArchLucid.Application.Audit;
using ArchLucid.ArtifactSynthesis.Packaging;
using ArchLucid.Core.Explanation;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Queries;

namespace ArchLucid.Host.Core.Demo;

/// <summary>Default <see cref="IDemoCommitPagePreviewClient"/>.</summary>
public sealed class DemoCommitPagePreviewClient(
    IDemoSeedRunResolver demoSeedRunResolver,
    IAuthorityQueryService authorityQuery,
    IArtifactQueryService artifactQuery,
    IRunPipelineAuditTimelineService pipelineTimeline,
    IRunExplanationSummaryService runExplanationSummary,
    TimeProvider timeProvider,
    ILogger<DemoCommitPagePreviewClient> logger) : IDemoCommitPagePreviewClient
{
    private const int PipelineTimelinePreviewCap = 10;

    private readonly IDemoSeedRunResolver _demoSeedRunResolver =
        demoSeedRunResolver ?? throw new ArgumentNullException(nameof(demoSeedRunResolver));

    private readonly IAuthorityQueryService _authorityQuery =
        authorityQuery ?? throw new ArgumentNullException(nameof(authorityQuery));

    private readonly IArtifactQueryService _artifactQuery =
        artifactQuery ?? throw new ArgumentNullException(nameof(artifactQuery));

    private readonly IRunPipelineAuditTimelineService _pipelineTimeline =
        pipelineTimeline ?? throw new ArgumentNullException(nameof(pipelineTimeline));

    private readonly IRunExplanationSummaryService _runExplanationSummary =
        runExplanationSummary ?? throw new ArgumentNullException(nameof(runExplanationSummary));

    private readonly TimeProvider _timeProvider =
        timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    private readonly ILogger<DemoCommitPagePreviewClient> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<DemoCommitPagePreviewResponse?> GetLatestCommittedDemoCommitPageAsync(
        CancellationToken cancellationToken = default)
    {
        RunRecord? run = await _demoSeedRunResolver.ResolveLatestCommittedDemoRunAsync(cancellationToken);

        if (run is null)
            return null;

        ScopeContext scope = DemoScopes.BuildDemoScope();
        Guid runId = run.RunId;
        Guid manifestId = run.GoldenManifestId!.Value;

        Task<RunDetailDto?> runDetailTask = _authorityQuery.GetRunDetailAsync(scope, runId, cancellationToken);
        Task<ManifestSummaryDto?> manifestTask = _authorityQuery.GetManifestSummaryAsync(scope, manifestId, cancellationToken);
        Task<IReadOnlyList<ArtifactDescriptor>> artifactsTask =
            _artifactQuery.ListArtifactsByManifestIdAsync(scope, manifestId, cancellationToken);
        Task<IReadOnlyList<RunPipelineTimelineItemDto>?> timelineTask =
            _pipelineTimeline.GetTimelineAsync(scope, runId, cancellationToken);
        Task<RunExplanationSummary?> explainTask =
            _runExplanationSummary.GetSummaryAsync(scope, runId, cancellationToken);

        await Task.WhenAll(runDetailTask, manifestTask, artifactsTask, timelineTask, explainTask);

        RunDetailDto? detail = await runDetailTask;
        ManifestSummaryDto? manifestDto = await manifestTask;
        RunExplanationSummary? explanation = await explainTask;

        if (detail is null || manifestDto is null || explanation is null)
        {
            _logger.LogWarning(
                "Demo commit preview: missing detail/manifest/explanation for run {RunId} (detail null? {DetailNull}, manifest null? {ManifestNull}, explain null? {ExplainNull}).",
                runId,
                detail is null,
                manifestDto is null,
                explanation is null);

            return null;
        }

        IReadOnlyList<ArtifactDescriptor> descriptors = await artifactsTask;
        IReadOnlyList<RunPipelineTimelineItemDto>? timeline = await timelineTask;

        return new DemoCommitPagePreviewResponse
        {
            GeneratedUtc = _timeProvider.GetUtcNow(),
            IsDemoData = true,
            DemoStatusMessage = "demo tenant — replace before publishing",
            Run = MapRun(detail.Run),
            AuthorityChain = MapAuthorityChain(detail.Run),
            Manifest = MapManifest(manifestDto),
            Artifacts = MapArtifacts(descriptors),
            PipelineTimeline = MapTimeline(timeline),
            RunExplanation = explanation,
        };
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
