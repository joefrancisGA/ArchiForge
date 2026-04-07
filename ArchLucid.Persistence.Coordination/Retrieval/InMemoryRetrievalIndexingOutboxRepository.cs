using System.Data;

namespace ArchLucid.Persistence.Coordination.Retrieval;

/// <summary>In-memory <see cref="IRetrievalIndexingOutboxRepository"/> for tests and <c>StorageProvider=InMemory</c>.</summary>
public sealed class InMemoryRetrievalIndexingOutboxRepository : IRetrievalIndexingOutboxRepository
{
    private readonly List<RetrievalIndexingOutboxEntry> _pending = [];

    private readonly Lock _gate = new();

    /// <inheritdoc />
    public Task EnqueueAsync(
        Guid runId,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        return EnqueueCoreAsync(runId, tenantId, workspaceId, projectId, ct);
    }

    /// <inheritdoc />
    public Task EnqueueAsync(
        Guid runId,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        IDbConnection connection,
        IDbTransaction transaction,
        CancellationToken ct)
    {
        _ = connection;
        _ = transaction;

        return EnqueueCoreAsync(runId, tenantId, workspaceId, projectId, ct);
    }

    private Task EnqueueCoreAsync(
        Guid runId,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        RetrievalIndexingOutboxEntry entry = new()
        {
            OutboxId = Guid.NewGuid(),
            RunId = runId,
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            CreatedUtc = DateTime.UtcNow
        };

        lock (_gate)
            _pending.Add(entry);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RetrievalIndexingOutboxEntry>> DequeuePendingAsync(int maxBatch, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        int take = Math.Clamp(maxBatch, 1, 100);

        lock (_gate)
        {
            List<RetrievalIndexingOutboxEntry> batch = _pending
                .OrderBy(x => x.CreatedUtc)
                .Take(take)
                .ToList();
            return Task.FromResult<IReadOnlyList<RetrievalIndexingOutboxEntry>>(batch);
        }
    }

    /// <inheritdoc />
    public Task MarkProcessedAsync(Guid outboxId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
            _pending.RemoveAll(x => x.OutboxId == outboxId);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<long> CountPendingAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        lock (_gate)
        {
            return Task.FromResult((long)_pending.Count);
        }
    }
}
