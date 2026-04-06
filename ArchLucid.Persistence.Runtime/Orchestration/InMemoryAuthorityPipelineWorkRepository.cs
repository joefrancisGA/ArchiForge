namespace ArchiForge.Persistence.Orchestration;

/// <summary>In-memory outbox for tests and <c>StorageProvider=InMemory</c>.</summary>
public sealed class InMemoryAuthorityPipelineWorkRepository : IAuthorityPipelineWorkRepository
{
    private readonly List<AuthorityPipelineWorkOutboxEntry> _pending = [];

    private readonly Lock _sync = new();

    /// <inheritdoc />
    public Task EnqueueAsync(
        Guid runId,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string payloadJson,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);

        AuthorityPipelineWorkOutboxEntry entry = new()
        {
            OutboxId = Guid.NewGuid(),
            RunId = runId,
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            PayloadJson = payloadJson,
            CreatedUtc = DateTime.UtcNow,
        };

        lock (_sync)
        {
            _pending.Add(entry);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AuthorityPipelineWorkOutboxEntry>> DequeuePendingAsync(int maxBatch, CancellationToken cancellationToken)
    {
        int take = Math.Clamp(maxBatch, 1, 100);

        lock (_sync)
        {
            List<AuthorityPipelineWorkOutboxEntry> batch = _pending
                .OrderBy(x => x.CreatedUtc)
                .Take(take)
                .ToList();

            return Task.FromResult<IReadOnlyList<AuthorityPipelineWorkOutboxEntry>>(batch);
        }
    }

    /// <inheritdoc />
    public Task MarkProcessedAsync(Guid outboxId, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            _pending.RemoveAll(x => x.OutboxId == outboxId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<long> CountPendingAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult((long)_pending.Count);
        }
    }
}
