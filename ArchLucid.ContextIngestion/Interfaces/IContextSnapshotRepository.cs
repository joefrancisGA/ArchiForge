using System.Data;

namespace ArchiForge.ContextIngestion.Interfaces;

using Models;

/// <summary>
/// Persistence contract for <see cref="ContextSnapshot"/> records that capture the
/// structured architecture-request context at the moment a run is initiated.
/// </summary>
public interface IContextSnapshotRepository
{
    /// <summary>
    /// Returns the most recent context snapshot for <paramref name="projectId"/>,
    /// or <see langword="null"/> when none exists.
    /// </summary>
    /// <param name="projectId">Project slug or identifier to query.</param>
    /// <param name="ct">Propagates notification that the operation should be cancelled.</param>
    Task<ContextSnapshot?> GetLatestAsync(string projectId, CancellationToken ct);

    /// <summary>
    /// Returns the context snapshot with the given <paramref name="snapshotId"/>,
    /// or <see langword="null"/> when not found.
    /// </summary>
    /// <param name="snapshotId">Primary key of the snapshot.</param>
    /// <param name="ct">Propagates notification that the operation should be cancelled.</param>
    Task<ContextSnapshot?> GetByIdAsync(Guid snapshotId, CancellationToken ct);

    /// <summary>
    /// Persists a context snapshot. Callers may pass an existing <paramref name="connection"/>
    /// and <paramref name="transaction"/> to participate in a multi-statement transaction.
    /// </summary>
    /// <param name="snapshot">The snapshot to persist.</param>
    /// <param name="ct">Propagates notification that the operation should be cancelled.</param>
    /// <param name="connection">Optional open connection to reuse.</param>
    /// <param name="transaction">Optional transaction to enlist in.</param>
    Task SaveAsync(
        ContextSnapshot snapshot,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);
}

