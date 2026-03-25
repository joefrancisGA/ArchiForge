using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ContextIngestion.Interfaces;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;
using ArchiForge.KnowledgeGraph.Interfaces;
using ArchiForge.Persistence.Interfaces;
using ArchiForge.Persistence.Models;

namespace ArchiForge.Persistence.Queries;

/// <summary>
/// <see cref="IAuthorityQueryService"/> implementation that composes existing repositories (same graph as in-memory; storage is repository-dependent).
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

        RunDetailDto result = new() { Run = run };

        if (run.ContextSnapshotId.HasValue)
        {
            result.ContextSnapshot = await contextSnapshotRepository.GetByIdAsync(run.ContextSnapshotId.Value, ct);
        }

        if (run.GraphSnapshotId.HasValue)
        {
            result.GraphSnapshot = await graphSnapshotRepository.GetByIdAsync(run.GraphSnapshotId.Value, ct);
        }

        if (run.FindingsSnapshotId.HasValue)
        {
            result.FindingsSnapshot = await findingsSnapshotRepository.GetByIdAsync(run.FindingsSnapshotId.Value, ct);
        }

        if (run.DecisionTraceId.HasValue)
        {
            result.DecisionTrace = await decisionTraceRepository.GetByIdAsync(scope, run.DecisionTraceId.Value, ct);
        }

        if (run.GoldenManifestId.HasValue)
        {
            result.GoldenManifest = await goldenManifestRepository.GetByIdAsync(scope, run.GoldenManifestId.Value, ct);
        }

        if (run is { ArtifactBundleId: not null, GoldenManifestId: not null })
        {
            result.ArtifactBundle = await artifactBundleRepository.GetByManifestIdAsync(scope, run.GoldenManifestId.Value, ct);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<ManifestSummaryDto?> GetManifestSummaryAsync(ScopeContext scope, Guid manifestId, CancellationToken ct)
    {
        GoldenManifest? manifest = await goldenManifestRepository.GetByIdAsync(scope, manifestId, ct);
        return manifest is null ? null : AuthorityRunMapper.MapManifestSummary(manifest);
    }

    private static RunSummaryDto MapSummary(RunRecord run) => AuthorityRunMapper.MapSummary(run);
}
