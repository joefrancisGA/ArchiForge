using System.Data;

using ArchLucid.Core.Integration;

namespace ArchLucid.Persistence;

/// <summary>In-memory outbox for tests and <c>StorageProvider=InMemory</c>.</summary>
public sealed class InMemoryIntegrationEventOutboxRepository : IIntegrationEventOutboxRepository
{
    private readonly List<IntegrationEventOutboxEntry> _rows = [];
    private readonly Lock _gate = new();

    /// <inheritdoc />
    public Task EnqueueAsync(
        Guid? runId,
        string eventType,
        string? messageId,
        ReadOnlyMemory<byte> payloadUtf8,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        return EnqueueCoreAsync(runId, eventType, messageId, payloadUtf8, tenantId, workspaceId, projectId);
    }

    /// <inheritdoc />
    public Task EnqueueAsync(
        Guid? runId,
        string eventType,
        string? messageId,
        ReadOnlyMemory<byte> payloadUtf8,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        IDbConnection connection,
        IDbTransaction transaction,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(transaction);

        return EnqueueCoreAsync(runId, eventType, messageId, payloadUtf8, tenantId, workspaceId, projectId);
    }

    private Task EnqueueCoreAsync(
        Guid? runId,
        string eventType,
        string? messageId,
        ReadOnlyMemory<byte> payloadUtf8,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId)
    {
        IntegrationEventOutboxEntry entry = new()
        {
            OutboxId = Guid.NewGuid(),
            RunId = runId,
            EventType = eventType,
            MessageId = messageId,
            PayloadUtf8 = payloadUtf8.ToArray(),
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            CreatedUtc = DateTime.UtcNow,
            Priority = IntegrationEventOutboxPriority.ForEventType(eventType),
            RetryCount = 0,
            NextRetryUtc = null,
            LastErrorMessage = null,
            DeadLetteredUtc = null
        };

        lock (_gate)

            _rows.Add(entry);


        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<IntegrationEventOutboxEntry>> DequeuePendingAsync(int maxBatch, CancellationToken ct)
    {
        int take = Math.Clamp(maxBatch, 1, 100);
        DateTime utcNow = DateTime.UtcNow;

        lock (_gate)
        {
            List<IntegrationEventOutboxEntry> batch = _rows
                .Where(e => e.DeadLetteredUtc is null && (e.NextRetryUtc is null || e.NextRetryUtc <= utcNow))
                .OrderBy(e => DrainSortPriority(e.Priority))
                .ThenBy(e => e.CreatedUtc)
                .Take(take)
                .ToList();

            return Task.FromResult<IReadOnlyList<IntegrationEventOutboxEntry>>(batch);
        }
    }

    /// <inheritdoc />
    public Task MarkProcessedAsync(Guid outboxId, CancellationToken ct)
    {
        lock (_gate)

            _rows.RemoveAll(e => e.OutboxId == outboxId);


        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RecordPublishFailureAsync(
        Guid outboxId,
        int newRetryCount,
        DateTime? nextRetryUtc,
        DateTime? deadLetteredUtc,
        string? lastErrorMessage,
        CancellationToken ct)
    {
        lock (_gate)
        {
            int idx = _rows.FindIndex(e => e.OutboxId == outboxId);

            if (idx < 0)
                return Task.CompletedTask;


            IntegrationEventOutboxEntry e = _rows[idx];

            _rows[idx] = new IntegrationEventOutboxEntry
            {
                OutboxId = e.OutboxId,
                RunId = e.RunId,
                EventType = e.EventType,
                MessageId = e.MessageId,
                PayloadUtf8 = e.PayloadUtf8,
                TenantId = e.TenantId,
                WorkspaceId = e.WorkspaceId,
                ProjectId = e.ProjectId,
                CreatedUtc = e.CreatedUtc,
                Priority = e.Priority,
                RetryCount = newRetryCount,
                NextRetryUtc = nextRetryUtc,
                DeadLetteredUtc = deadLetteredUtc,
                LastErrorMessage = lastErrorMessage
            };
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<long> CountIntegrationOutboxPublishPendingAsync(CancellationToken ct)
    {
        lock (_gate)
        {
            long n = _rows.LongCount(e => e.DeadLetteredUtc is null);

            return Task.FromResult(n);
        }
    }

    /// <inheritdoc />
    public Task<long> CountIntegrationOutboxDeadLetterAsync(CancellationToken ct)
    {
        lock (_gate)
        {
            long n = _rows.LongCount(e => e.DeadLetteredUtc is not null);

            return Task.FromResult(n);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<IntegrationEventOutboxDeadLetterRow>> ListDeadLettersAsync(int maxRows, CancellationToken ct)
    {
        int take = Math.Clamp(maxRows, 1, 500);

        lock (_gate)
        {
            List<IntegrationEventOutboxDeadLetterRow> list = _rows
                .Where(e => e.DeadLetteredUtc is not null)
                .OrderByDescending(e => e.DeadLetteredUtc)
                .Take(take)
                .Select(
                    e => new IntegrationEventOutboxDeadLetterRow
                    {
                        OutboxId = e.OutboxId,
                        RunId = e.RunId,
                        EventType = e.EventType,
                        DeadLetteredUtc = e.DeadLetteredUtc!.Value,
                        RetryCount = e.RetryCount,
                        LastErrorMessage = e.LastErrorMessage
                    })
                .ToList();

            return Task.FromResult<IReadOnlyList<IntegrationEventOutboxDeadLetterRow>>(list);
        }
    }

    /// <inheritdoc />
    public Task<bool> ResetDeadLetterForRetryAsync(Guid outboxId, CancellationToken ct)
    {
        lock (_gate)
        {
            int idx = _rows.FindIndex(e => e.OutboxId == outboxId && e.DeadLetteredUtc is not null);

            if (idx < 0)
                return Task.FromResult(false);


            IntegrationEventOutboxEntry e = _rows[idx];

            _rows[idx] = new IntegrationEventOutboxEntry
            {
                OutboxId = e.OutboxId,
                RunId = e.RunId,
                EventType = e.EventType,
                MessageId = e.MessageId,
                PayloadUtf8 = e.PayloadUtf8,
                TenantId = e.TenantId,
                WorkspaceId = e.WorkspaceId,
                ProjectId = e.ProjectId,
                CreatedUtc = e.CreatedUtc,
                Priority = e.Priority,
                RetryCount = 0,
                NextRetryUtc = null,
                DeadLetteredUtc = null,
                LastErrorMessage = null
            };

            return Task.FromResult(true);
        }
    }

    /// <summary>Matches <c>ORDER BY ISNULL(Priority, 1)</c> in SQL dequeue.</summary>
    private static int DrainSortPriority(int? priority) => priority ?? 1;
}
