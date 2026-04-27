using ArchLucid.Application.Explanation;
using ArchLucid.Application.Audit;
using ArchLucid.ArtifactSynthesis.Packaging;
using ArchLucid.Core.Explanation;
using ArchLucid.Core.Scoping;
using ArchLucid.Host.Core.Demo;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Queries;

namespace ArchLucid.Host.Core.Marketing;

/// <summary>Default <see cref="IPublicShowcaseCommitPageClient"/> — resolves runs in the pinned demo scope only (single-catalog Contoso hero).</summary>
public sealed class PublicShowcaseCommitPageClient(
    IRunRepository runRepository,
    IAuthorityQueryService authorityQuery,
    IArtifactQueryService artifactQuery,
    IRunPipelineAuditTimelineService pipelineTimeline,
    IRunExplanationSummaryService runExplanationSummary,
    TimeProvider timeProvider,
    ILogger<PublicShowcaseCommitPageClient> logger) : IPublicShowcaseCommitPageClient
{
    private readonly IRunRepository _runRepository =
        runRepository ?? throw new ArgumentNullException(nameof(runRepository));

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

    private readonly ILogger<PublicShowcaseCommitPageClient> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<DemoCommitPagePreviewResponse?> GetShowcaseCommitPageAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        ScopeContext scope = DemoScopes.BuildDemoScope();
        RunRecord? run = await _runRepository.GetByIdAsync(scope, runId, cancellationToken);

        if (run is null || !run.IsPublicShowcase)
            return null;

        if (run.GoldenManifestId is not { } manifestId || manifestId == Guid.Empty)
            return null;

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
            demoStatusMessage: "public showcase — demo tenant; replace before publishing",
            detail,
            manifestDto,
            descriptors,
            timeline,
            explanation,
            _logger,
            runId);
    }
}
