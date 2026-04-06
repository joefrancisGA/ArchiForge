namespace ArchiForge.Persistence.Diagnostics;

/// <summary>In-memory host: outbox metrics are zeros (no durable outbox backlog).</summary>
public sealed class InMemoryOutboxOperationalMetricsReader : IOutboxOperationalMetricsReader
{
    private static readonly OutboxOperationalMetricsSnapshot Empty = new();

    /// <inheritdoc />
    public Task<OutboxOperationalMetricsSnapshot> ReadSnapshotAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Empty);
}
