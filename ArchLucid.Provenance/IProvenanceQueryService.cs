using ArchiForge.Core.Scoping;

namespace ArchiForge.Provenance;

/// <summary>
/// Loads stored provenance JSON for a run and projects it into UI-oriented <see cref="GraphViewModel"/> graphs.
/// </summary>
/// <remarks>
/// Implementation: <c>ArchiForge.Persistence.Provenance.ProvenanceQueryService</c>. Callers: <c>ArchiForge.Api.Services.Ask.AskService</c>, provenance HTTP controllers.
/// </remarks>
public interface IProvenanceQueryService
{
    /// <summary>
    /// Deserializes the snapshot for <paramref name="runId"/> in <paramref name="scope"/> and returns the full mapped graph.
    /// </summary>
    /// <returns>The view model, or <see langword="null"/> when no snapshot exists.</returns>
    /// <param name="scope">Caller scope for tenant/workspace/project isolation.</param>
    /// <param name="runId">Authority run id whose provenance to load.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<GraphViewModel?> GetFullGraphAsync(ScopeContext scope, Guid runId, CancellationToken ct);

    /// <summary>
    /// Subgraph around one decision: incident edges + endpoints (findings, rules, manifest, artifacts).
    /// </summary>
    /// <param name="scope">Caller scope for tenant/workspace/project isolation.</param>
    /// <param name="runId">Authority run id.</param>
    /// <param name="decisionKey">Provenance decision node id (GUID string) or the node's <see cref="ProvenanceNode.ReferenceId"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Filtered view model, or <see langword="null"/> when the run or decision cannot be resolved.</returns>
    Task<GraphViewModel?> GetDecisionSubgraphAsync(ScopeContext scope, Guid runId, string decisionKey, CancellationToken ct);

    /// <summary>
    /// BFS-style neighborhood of <paramref name="nodeId"/> up to <paramref name="depth"/> hops in the stored graph.
    /// </summary>
    /// <param name="scope">Caller scope for tenant/workspace/project isolation.</param>
    /// <param name="runId">Authority run id.</param>
    /// <param name="nodeId">Center node for the neighborhood query.</param>
    /// <param name="depth">Maximum hop distance from <paramref name="nodeId"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Subgraph view model, or <see langword="null"/> when the run or node id is missing.</returns>
    Task<GraphViewModel?> GetNodeNeighborhoodAsync(ScopeContext scope, Guid runId, Guid nodeId, int depth, CancellationToken ct);
}
