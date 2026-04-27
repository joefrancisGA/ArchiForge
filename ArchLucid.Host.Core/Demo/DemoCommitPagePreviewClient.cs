using ArchLucid.Application.Explanation;
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
        IReadOnlyList<ArtifactDescriptor> descriptors = await artifactsTask;
        IReadOnlyList<RunPipelineTimelineItemDto>? timeline = await timelineTask;

        return DemoCommitPagePreviewMapper.TryBuild(
            _timeProvider.GetUtcNow(),
            isDemoData: true,
            demoStatusMessage: "demo tenant — replace before publishing",
            detail,
            manifestDto,
            descriptors,
            timeline,
            explanation,
            _logger,
            runId);
    }
}
