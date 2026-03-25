using System.Data;

using ArchiForge.Core.Scoping;
using ArchiForge.Provenance;

namespace ArchiForge.Persistence.Provenance;

/// <summary>
/// Persistence contract for <see cref="DecisionProvenanceSnapshot"/> records that store
/// the serialized provenance graph for a run, enabling lineage queries and subgraph extraction.
/// </summary>
public interface IProvenanceSnapshotRepository
{
    /// <summary>
    /// Persists a provenance snapshot. Callers may pass an existing <paramref name="connection"/>
    /// and <paramref name="transaction"/> to participate in a multi-statement transaction.
    /// </summary>
    /// <param name="snapshot">The snapshot to persist.</param>
    /// <param name="ct">Propagates notification that the operation should be cancelled.</param>
    /// <param name="connection">Optional open connection to reuse.</param>
    /// <param name="transaction">Optional transaction to enlist in.</param>
    Task SaveAsync(
        DecisionProvenanceSnapshot snapshot,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);

    /// <summary>
    /// Returns the provenance snapshot for <paramref name="runId"/> within <paramref name="scope"/>,
    /// or <see langword="null"/> when none has been persisted for that run or the run is outside scope.
    /// </summary>
    /// <param name="scope">Tenant/workspace/project boundary enforced by the implementation.</param>
    /// <param name="runId">The run whose provenance snapshot is requested.</param>
    /// <param name="ct">Propagates notification that the operation should be cancelled.</param>
    Task<DecisionProvenanceSnapshot?> GetByRunIdAsync(ScopeContext scope, Guid runId, CancellationToken ct);
}
