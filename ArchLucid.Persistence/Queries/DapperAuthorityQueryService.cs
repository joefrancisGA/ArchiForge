using ArchLucid.ArtifactSynthesis.Interfaces;
using ArchLucid.ArtifactSynthesis.Models;
using ArchLucid.ContextIngestion.Interfaces;
using ArchLucid.ContextIngestion.Models;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Models;
using ArchLucid.KnowledgeGraph.Interfaces;
using ArchLucid.KnowledgeGraph.Models;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

namespace ArchLucid.Persistence.Queries;

/// <summary>
///     <see cref="IAuthorityQueryService" /> implementation that composes existing repositories (same graph as in-memory;
///     storage is repository-dependent).
/// </summary>
/// <remarks>Registered scoped in DI when SQL-backed persistence is enabled.</remarks>
public sealed class DapperAuthorityQueryService(
    IRunRepository runRepository,
    IContextSnapshotRepository contextSnapshotRepository,
    IGraphSnapshotRepository graphSnapshotRepository,
    IFindingsSnapshotRepository findingsSnapshotRepository,
    IDecisionTraceRepository decisionTraceRepository,
    IGoldenManifestRepository goldenManifestRepository,
    IArtifactBundleRepository artifactBundleRepository)
    : IAuthorityQueryService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<RunSummaryDto>> ListRunsByProjectAsync(
        ScopeContext scope,
        string projectId,
        int take,
        CancellationToken ct)
    {
        IReadOnlyList<RunRecord> runs = await runRepository.ListByProjectAsync(scope, projectId, take, ct);
        return runs.Select(MapSummary).ToList();
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<RunSummaryDto> Items, bool HasMore)> ListRunsByProjectKeysetAsync(
        ScopeContext scope,
        string projectId,
        DateTime? cursorCreatedUtc,
        Guid? cursorRunId,
        int take,
        CancellationToken ct)
    {
        RunListPage page = await runRepository.ListByProjectKeysetAsync(
            scope,
            projectId,
            cursorCreatedUtc,
            cursorRunId,
            take,
            ct);

        return (page.Items.Select(MapSummary).ToList(), page.HasMore);
    }

    /// <inheritdoc />
    public async Task<RunSummaryDto?> GetRunSummaryAsync(ScopeContext scope, Guid runId, CancellationToken ct)
    {
        RunRecord? run = await runRepository.GetByIdAsync(scope, runId, ct);
        return run is null ? null : MapSummary(run);
    }

    public async Task<RunDetailDto?> GetRunDetailAsync(ScopeContext scope, Guid runId, CancellationToken ct)
    {
        RunRecord? run = await runRepository.GetByIdAsync(scope, runId, ct);
        if (run is null)
            return null;

        Task<ContextSnapshot?> contextTask = run.ContextSnapshotId.HasValue
            ? contextSnapshotRepository.GetByIdAsync(run.ContextSnapshotId.Value, ct)
            : Task.FromResult<ContextSnapshot?>(null);
        Task<GraphSnapshot?> graphTask = run.GraphSnapshotId.HasValue
            ? graphSnapshotRepository.GetByIdAsync(run.GraphSnapshotId.Value, ct)
            : Task.FromResult<GraphSnapshot?>(null);
        Task<FindingsSnapshot?> findingsTask = run.FindingsSnapshotId.HasValue
            ? findingsSnapshotRepository.GetByIdAsync(run.FindingsSnapshotId.Value, ct)
            : Task.FromResult<FindingsSnapshot?>(null);
        Task<DecisionTrace?> traceTask = run.DecisionTraceId.HasValue
            ? decisionTraceRepository.GetByIdAsync(scope, run.DecisionTraceId.Value, ct)
            : Task.FromResult<DecisionTrace?>(null);
        Task<ManifestDocument?> manifestTask = run.GoldenManifestId.HasValue
            ? goldenManifestRepository.GetByIdAsync(scope, run.GoldenManifestId.Value, ct)
            : Task.FromResult<ManifestDocument?>(null);
        Task<ArtifactBundle?> bundleTask = run.GoldenManifestId.HasValue
            ? artifactBundleRepository.GetByManifestIdAsync(scope, run.GoldenManifestId.Value, loadArtifactBodies: true, ct)
            : Task.FromResult<ArtifactBundle?>(null);

        await Task.WhenAll(contextTask, graphTask, findingsTask, traceTask, manifestTask, bundleTask);

        return new RunDetailDto
        {
            Run = run,
            ContextSnapshot = await contextTask,
            GraphSnapshot = await graphTask,
            FindingsSnapshot = await findingsTask,
            AuthorityTrace = await traceTask,
            GoldenManifest = await manifestTask,
            ArtifactBundle = await bundleTask
        };
    }

    /// <inheritdoc />
    public async Task<ManifestSummaryDto?> GetManifestSummaryAsync(ScopeContext scope, Guid manifestId,
        CancellationToken ct)
    {
        ManifestDocument? manifest = await goldenManifestRepository.GetByIdAsync(scope, manifestId, ct);
        return manifest is null ? null : AuthorityRunMapper.MapManifestSummary(manifest);
    }

    private static RunSummaryDto MapSummary(RunRecord run)
    {
        return AuthorityRunMapper.MapSummary(run);
    }
}
