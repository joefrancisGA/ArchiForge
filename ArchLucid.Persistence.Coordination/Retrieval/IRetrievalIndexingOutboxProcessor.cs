namespace ArchLucid.Persistence.Coordination.Retrieval;

/// <summary>
///     Drains <see cref="IRetrievalIndexingOutboxRepository" /> and invokes retrieval indexing for each pending run.
/// </summary>
public interface IRetrievalIndexingOutboxProcessor
{
    /// <summary>Processes one batch of pending outbox rows (best-effort; failures are logged per row).</summary>
    Task ProcessPendingBatchAsync(CancellationToken ct);
}
