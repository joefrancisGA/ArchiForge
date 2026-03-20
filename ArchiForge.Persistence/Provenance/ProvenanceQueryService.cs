using ArchiForge.Core.Scoping;
using ArchiForge.Provenance;
using ArchiForge.Provenance.Services;

namespace ArchiForge.Persistence.Provenance;

public sealed class ProvenanceQueryService : IProvenanceQueryService
{
    private readonly IProvenanceSnapshotRepository _repo;

    public ProvenanceQueryService(IProvenanceSnapshotRepository repo) => _repo = repo;

    public async Task<GraphViewModel?> GetFullGraphAsync(ScopeContext scope, Guid runId, CancellationToken ct)
    {
        var graph = await LoadGraphAsync(scope, runId, ct).ConfigureAwait(false);
        return graph is null ? null : ProvenanceGraphViewMapper.ToViewModel(graph);
    }

    public async Task<GraphViewModel?> GetDecisionSubgraphAsync(
        ScopeContext scope,
        Guid runId,
        string decisionKey,
        CancellationToken ct)
    {
        var full = await LoadGraphAsync(scope, runId, ct).ConfigureAwait(false);
        if (full is null)
            return null;

        if (!ProvenanceGraphAlgorithms.TryResolveDecisionNodeId(full, decisionKey, out var decisionNodeId))
            return null;

        var sub = ProvenanceGraphAlgorithms.ExtractDecisionSubgraph(full, decisionNodeId);
        return ProvenanceGraphViewMapper.ToViewModel(sub);
    }

    public async Task<GraphViewModel?> GetNodeNeighborhoodAsync(
        ScopeContext scope,
        Guid runId,
        Guid nodeId,
        int depth,
        CancellationToken ct)
    {
        var full = await LoadGraphAsync(scope, runId, ct).ConfigureAwait(false);
        if (full is null)
            return null;

        if (full.Nodes.All(n => n.Id != nodeId))
            return null;

        var sub = ProvenanceGraphAlgorithms.ExtractNeighborhood(full, nodeId, depth);
        return ProvenanceGraphViewMapper.ToViewModel(sub);
    }

    private async Task<DecisionProvenanceGraph?> LoadGraphAsync(ScopeContext scope, Guid runId, CancellationToken ct)
    {
        var snapshot = await _repo.GetByRunIdAsync(scope, runId, ct).ConfigureAwait(false);
        if (snapshot is null)
            return null;

        return ProvenanceGraphSerializer.Deserialize(snapshot.GraphJson);
    }
}
