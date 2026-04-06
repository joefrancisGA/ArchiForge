using System.Data;

using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.KnowledgeGraph.Interfaces;

/// <summary>
/// Persistence contract for <see cref="GraphSnapshot"/> records that capture the
/// knowledge-graph state (nodes and edges) at the time of a run.
/// </summary>
public interface IGraphSnapshotRepository
{
    /// <summary>
    /// Persists a graph snapshot. Callers may pass an existing <paramref name="connection"/>
    /// and <paramref name="transaction"/> to participate in a multi-statement transaction.
    /// </summary>
    /// <param name="snapshot">The snapshot to persist.</param>
    /// <param name="ct">Propagates notification that the operation should be cancelled.</param>
    /// <param name="connection">Optional open connection to reuse.</param>
    /// <param name="transaction">Optional transaction to enlist in.</param>
    Task SaveAsync(
        GraphSnapshot snapshot,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);

    /// <summary>
    /// Returns the graph snapshot with the given <paramref name="graphSnapshotId"/>,
    /// or <see langword="null"/> when not found.
    /// </summary>
    /// <param name="graphSnapshotId">Primary key of the snapshot.</param>
    /// <param name="ct">Propagates notification that the operation should be cancelled.</param>
    Task<GraphSnapshot?> GetByIdAsync(Guid graphSnapshotId, CancellationToken ct);

    /// <summary>
    /// Returns the most recently persisted graph for the given context snapshot, or <see langword="null"/>.
    /// Used for incremental reuse when canonical objects are unchanged.
    /// </summary>
    Task<GraphSnapshot?> GetLatestByContextSnapshotIdAsync(Guid contextSnapshotId, CancellationToken ct);

    /// <summary>
    /// Returns denormalized edges from <c>GraphSnapshotEdges</c> when the index exists; otherwise empty.
    /// </summary>
    Task<IReadOnlyList<GraphSnapshotIndexedEdge>> ListIndexedEdgesAsync(Guid graphSnapshotId, CancellationToken ct);
}

