using ArchiForge.Core.Scoping;

namespace ArchiForge.Provenance;

public interface IProvenanceQueryService
{
    Task<GraphViewModel?> GetFullGraphAsync(ScopeContext scope, Guid runId, CancellationToken ct);

    /// <summary>
    /// Subgraph around one decision: incident edges + endpoints (findings, rules, manifest, artifacts).
    /// <paramref name="decisionKey"/> is the provenance decision node id (GUID) or the node's <see cref="ProvenanceNode.ReferenceId"/>.
    /// </summary>
    Task<GraphViewModel?> GetDecisionSubgraphAsync(ScopeContext scope, Guid runId, string decisionKey, CancellationToken ct);

    Task<GraphViewModel?> GetNodeNeighborhoodAsync(ScopeContext scope, Guid runId, Guid nodeId, int depth, CancellationToken ct);
}
