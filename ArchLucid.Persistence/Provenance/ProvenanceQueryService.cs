using ArchiForge.Core.Scoping;
using ArchiForge.Provenance;
using ArchiForge.Provenance.Services;

namespace ArchiForge.Persistence.Provenance;

/// <summary>
/// <see cref="IProvenanceQueryService"/> implementation using <see cref="IProvenanceSnapshotRepository"/> and in-memory graph algorithms.
/// </summary>
public sealed class ProvenanceQueryService(IProvenanceSnapshotRepository repo) : IProvenanceQueryService
{
    /// <inheritdoc />
    public async Task<GraphViewModel?> GetFullGraphAsync(ScopeContext scope, Guid runId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(scope);
        if (runId == Guid.Empty)
            throw new ArgumentException("runId must be a non-empty GUID.", nameof(runId));

        DecisionProvenanceGraph? graph = await LoadGraphAsync(scope, runId, ct);
        return graph is null ? null : ProvenanceGraphViewMapper.ToViewModel(graph);
    }

    /// <inheritdoc />
    public async Task<GraphViewModel?> GetDecisionSubgraphAsync(
        ScopeContext scope,
        Guid runId,
        string decisionKey,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(decisionKey);

        DecisionProvenanceGraph? full = await LoadGraphAsync(scope, runId, ct);
        if (full is null)
            return null;

        if (!ProvenanceGraphAlgorithms.TryResolveDecisionNodeId(full, decisionKey, out Guid decisionNodeId))
            return null;

        DecisionProvenanceGraph sub = ProvenanceGraphAlgorithms.ExtractDecisionSubgraph(full, decisionNodeId);
        return ProvenanceGraphViewMapper.ToViewModel(sub);
    }

    /// <inheritdoc />
    public async Task<GraphViewModel?> GetNodeNeighborhoodAsync(
        ScopeContext scope,
        Guid runId,
        Guid nodeId,
        int depth,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(scope);
        if (runId == Guid.Empty)
            throw new ArgumentException("runId must be a non-empty GUID.", nameof(runId));
        int safeDepth = Math.Clamp(depth, 1, 10);

        DecisionProvenanceGraph? full = await LoadGraphAsync(scope, runId, ct);
        if (full is null)
            return null;

        if (full.Nodes.All(n => n.Id != nodeId))
            return null;

        DecisionProvenanceGraph sub = ProvenanceGraphAlgorithms.ExtractNeighborhood(full, nodeId, safeDepth);
        return ProvenanceGraphViewMapper.ToViewModel(sub);
    }

    private async Task<DecisionProvenanceGraph?> LoadGraphAsync(ScopeContext scope, Guid runId, CancellationToken ct)
    {
        DecisionProvenanceSnapshot? snapshot = await repo.GetByRunIdAsync(scope, runId, ct);
        if (snapshot is null)
            return null;

        try
        {
            return ProvenanceGraphSerializer.Deserialize(snapshot.GraphJson);
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize provenance graph for run '{runId}'. " +
                "The stored JSON may be corrupt or from an incompatible schema version.", ex);
        }
    }
}
