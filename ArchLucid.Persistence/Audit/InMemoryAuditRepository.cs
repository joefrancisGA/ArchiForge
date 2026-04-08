using ArchLucid.Core.Audit;

namespace ArchLucid.Persistence.Audit;

/// <summary>
/// In-memory implementation of <see cref="IAuditRepository"/> for testing and storage-off mode.
/// Bounded at <see cref="MaxEvents"/> entries; when full, the oldest <see cref="EvictCount"/> events are
/// removed in a single batch rather than one at a time to amortize eviction cost.
/// All operations are thread-safe via an exclusive lock.
/// </summary>
public sealed class InMemoryAuditRepository : IAuditRepository
{
    private const int MaxEvents = 5_000;
    private const int EvictCount = 1_000;

    private readonly Lock _gate = new();
    private readonly List<AuditEvent> _events = [];

    public Task AppendAsync(AuditEvent auditEvent, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(auditEvent);
        lock (_gate)
        {
            if (_events.Count >= MaxEvents)
                _events.RemoveRange(0, EvictCount);

            _events.Add(auditEvent);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AuditEvent>> GetByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        int take,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        int n = Math.Clamp(take <= 0 ? 100 : take, 1, 500);
        List<AuditEvent> result;
        lock (_gate)
        
            result = _events
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.WorkspaceId == workspaceId &&
                    x.ProjectId == projectId)
                .OrderByDescending(x => x.OccurredUtc)
                .Take(n)
                .ToList();
        

        return Task.FromResult<IReadOnlyList<AuditEvent>>(result);
    }

    public Task<IReadOnlyList<AuditEvent>> GetFilteredAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        AuditEventFilter filter,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(filter);

        int take = Math.Clamp(filter.Take <= 0 ? 100 : filter.Take, 1, 500);
        List<AuditEvent> snapshot;
        lock (_gate)
        {
            IEnumerable<AuditEvent> query = _events.Where(
                x =>
                    x.TenantId == tenantId
                    && x.WorkspaceId == workspaceId
                    && x.ProjectId == projectId);

            if (!string.IsNullOrWhiteSpace(filter.EventType))
            {
                query = query.Where(x => string.Equals(x.EventType, filter.EventType, StringComparison.Ordinal));
            }

            if (filter.FromUtc.HasValue)
            {
                query = query.Where(x => x.OccurredUtc >= filter.FromUtc.Value);
            }

            if (filter.ToUtc.HasValue)
            {
                query = query.Where(x => x.OccurredUtc <= filter.ToUtc.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.CorrelationId))
            {
                query = query.Where(
                    x => string.Equals(x.CorrelationId, filter.CorrelationId, StringComparison.Ordinal));
            }

            if (!string.IsNullOrWhiteSpace(filter.ActorUserId))
            {
                query = query.Where(
                    x => string.Equals(x.ActorUserId, filter.ActorUserId, StringComparison.Ordinal));
            }

            if (filter.RunId.HasValue)
            {
                query = query.Where(x => x.RunId == filter.RunId.Value);
            }

            snapshot = query
                .OrderByDescending(x => x.OccurredUtc)
                .Take(take)
                .ToList();
        }

        return Task.FromResult<IReadOnlyList<AuditEvent>>(snapshot);
    }
}
