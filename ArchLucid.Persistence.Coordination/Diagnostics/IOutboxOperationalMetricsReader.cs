namespace ArchLucid.Persistence.Coordination.Diagnostics;

/// <summary>Reads SQL outbox depths for observability (no HTTP context; safe for background timers).</summary>
public interface IOutboxOperationalMetricsReader
{
    Task<OutboxOperationalMetricsSnapshot> ReadSnapshotAsync(CancellationToken cancellationToken = default);
}
