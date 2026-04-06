using System.Data;

namespace ArchiForge.Persistence.Integration;

/// <summary>Transactional outbox for integration events (same pattern as <see cref="Retrieval.IRetrievalIndexingOutboxRepository"/>).</summary>
public interface IIntegrationEventOutboxRepository
{
    Task EnqueueAsync(
        Guid runId,
        string eventType,
        string? messageId,
        ReadOnlyMemory<byte> payloadUtf8,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct);

    Task EnqueueAsync(
        Guid runId,
        string eventType,
        string? messageId,
        ReadOnlyMemory<byte> payloadUtf8,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        IDbConnection connection,
        IDbTransaction transaction,
        CancellationToken ct);

    Task<IReadOnlyList<IntegrationEventOutboxEntry>> DequeuePendingAsync(int maxBatch, CancellationToken ct);

    Task MarkProcessedAsync(Guid outboxId, CancellationToken ct);

    /// <summary>Updates row after a failed publish (backoff or dead-letter).</summary>
    Task RecordPublishFailureAsync(
        Guid outboxId,
        int newRetryCount,
        DateTime? nextRetryUtc,
        DateTime? deadLetteredUtc,
        string? lastErrorMessage,
        CancellationToken ct);

    Task<long> CountIntegrationOutboxPublishPendingAsync(CancellationToken ct);

    Task<long> CountIntegrationOutboxDeadLetterAsync(CancellationToken ct);

    Task<IReadOnlyList<IntegrationEventOutboxDeadLetterRow>> ListDeadLettersAsync(int maxRows, CancellationToken ct);

    /// <summary>Clears dead-letter state so the row is eligible for publish retries again.</summary>
    Task<bool> ResetDeadLetterForRetryAsync(Guid outboxId, CancellationToken ct);
}
