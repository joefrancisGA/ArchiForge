namespace ArchiForge.Persistence.Orchestration;

/// <summary>Transactional-style queue for deferred authority pipeline continuation after the run header commits.</summary>
public interface IAuthorityPipelineWorkRepository
{
    Task EnqueueAsync(
        Guid runId,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string payloadJson,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuthorityPipelineWorkOutboxEntry>> DequeuePendingAsync(int maxBatch, CancellationToken cancellationToken);

    Task MarkProcessedAsync(Guid outboxId, CancellationToken cancellationToken);

    /// <summary>Rows with <c>ProcessedUtc</c> null.</summary>
    Task<long> CountPendingAsync(CancellationToken cancellationToken = default);
}
