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
/// <see cref="IAuthorityQueryService"/> backed by the same repository abstractions as <see cref="DapperAuthorityQueryService"/> (in-memory stores in test / storage-off mode).
/// </summary>
public sealed class InMemoryAuthorityQueryService(
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

    public async Task<RunSummaryDto?> GetRunSummaryAsync(ScopeContext scope, Guid runId, CancellationToken ct)
    {
        RunRecord? run = await runRepository.GetByIdAsync(scope, runId, ct);
        return run is null ? null : MapSummary(run);
    }

    /// <inheritdoc />
    public async Task<RunDetailDto?> GetRunDetailAsync(ScopeContext scope, Guid runId, CancellationToken ct)
    {
        RunRecord? run = await runRepository.GetByIdAsync(scope, runId, ct);
        if (run is null)
            return null;

        return new RunDetailDto
        {
            Run = run,
            ContextSnapshot = run.ContextSnapshotId.HasValue
                ? await contextSnapshotRepository.GetByIdAsync(run.ContextSnapshotId.Value, ct)
                : null,
            GraphSnapshot = run.GraphSnapshotId.HasValue
                ? await graphSnapshotRepository.GetByIdAsync(run.GraphSnapshotId.Value, ct)
                : null,
            FindingsSnapshot = run.FindingsSnapshotId.HasValue
                ? await findingsSnapshotRepository.GetByIdAsync(run.FindingsSnapshotId.Value, ct)
                : null,
            DecisionTrace = run.DecisionTraceId.HasValue
                ? await decisionTraceRepository.GetByIdAsync(scope, run.DecisionTraceId.Value, ct)
                : null,
            GoldenManifest = run.GoldenManifestId.HasValue
                ? await goldenManifestRepository.GetByIdAsync(scope, run.GoldenManifestId.Value, ct)
                : null,
            ArtifactBundle = run is { ArtifactBundleId: not null, GoldenManifestId: not null }
                ? await artifactBundleRepository.GetByManifestIdAsync(scope, run.GoldenManifestId.Value, ct)
                : null
        };
    }

    /// <inheritdoc />
    public async Task<ManifestSummaryDto?> GetManifestSummaryAsync(ScopeContext scope, Guid manifestId, CancellationToken ct)
    {
        GoldenManifest? manifest = await goldenManifestRepository.GetByIdAsync(scope, manifestId, ct);
        return manifest is null ? null : AuthorityRunMapper.MapManifestSummary(manifest);
    }

    private static RunSummaryDto MapSummary(RunRecord run) => AuthorityRunMapper.MapSummary(run);
}
