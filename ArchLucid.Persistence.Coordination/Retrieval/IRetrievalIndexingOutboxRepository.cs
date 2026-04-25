using System.Data;

namespace ArchLucid.Persistence.Coordination.Retrieval;

/// <summary>
///     Queue for deferred retrieval indexing after an authority run commits (transactional outbox–style durability on
///     SQL).
/// </summary>
public interface IRetrievalIndexingOutboxRepository
{
    /// <summary>Enqueues a run for background indexing using a dedicated connection (non-transactional with authority UOW).</summary>
    Task EnqueueAsync(
        Guid runId,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct);

    /// <summary>
    ///     Enqueues inside an existing SQL transaction so the outbox row commits with the authority pipeline UOW.
    /// </summary>
    Task EnqueueAsync(
        Guid runId,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        IDbConnection connection,
        IDbTransaction transaction,
        CancellationToken ct);

    /// <summary>Returns up to <paramref name="maxBatch" /> pending rows (unprocessed first).</summary>
    Task<IReadOnlyList<RetrievalIndexingOutboxEntry>> DequeuePendingAsync(int maxBatch, CancellationToken ct);

    /// <summary>Marks a row as processed so it is not returned again.</summary>
    Task MarkProcessedAsync(Guid outboxId, CancellationToken ct);

    /// <summary>Count of rows not yet processed (for observability / admin).</summary>
    Task<long> CountPendingAsync(CancellationToken ct);
}
