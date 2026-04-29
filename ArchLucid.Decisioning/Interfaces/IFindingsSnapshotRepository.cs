using System.Data;

using ArchLucid.Decisioning.Models;

namespace ArchLucid.Decisioning.Interfaces;

/// <summary>
///     Persistence contract for <see cref="FindingsSnapshot" /> records that capture the
///     structured findings produced by the findings-orchestration pipeline for a run.
/// </summary>
public interface IFindingsSnapshotRepository
{
    /// <summary>
    ///     Persists a findings snapshot. Callers may pass an existing <paramref name="connection" />
    ///     and <paramref name="transaction" /> to participate in a multi-statement transaction.
    /// </summary>
    /// <param name="snapshot">The snapshot to persist.</param>
    /// <param name="ct">Propagates notification that the operation should be cancelled.</param>
    /// <param name="connection">Optional open connection to reuse.</param>
    /// <param name="transaction">Optional transaction to enlist in.</param>
    Task SaveAsync(
        FindingsSnapshot snapshot,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);

    /// <summary>
    ///     Returns the findings snapshot with the given <paramref name="findingsSnapshotId" />,
    ///     or <see langword="null" /> when not found.
    /// </summary>
    /// <param name="findingsSnapshotId">Primary key of the snapshot.</param>
    /// <param name="ct">Propagates notification that the operation should be cancelled.</param>
    Task<FindingsSnapshot?> GetByIdAsync(Guid findingsSnapshotId, CancellationToken ct);

    /// <summary>
    ///     Stable keyset page over relational <c>dbo.FindingRecords</c> (metadata projection only — no payloads). Pass
    ///     <paramref name="cursorSortOrder" /> and <paramref name="cursorFindingRecordId" /> together after the last item on
    ///     the previous page; both <see langword="null" /> requests the first page.
    /// </summary>
    Task<FindingRecordMetadataPage> ListFindingRecordsKeysetAsync(
        Guid findingsSnapshotId,
        int? cursorSortOrder,
        Guid? cursorFindingRecordId,
        string? severity,
        string? category,
        string? findingType,
        int take,
        CancellationToken ct);
}
