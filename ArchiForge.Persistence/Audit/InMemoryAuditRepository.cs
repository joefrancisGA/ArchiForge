using ArchiForge.Core.Audit;

namespace ArchiForge.Persistence.Audit;

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
        var n = Math.Clamp(take <= 0 ? 100 : take, 1, 500);
        List<AuditEvent> result;
        lock (_gate)
        {
            result = _events
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.WorkspaceId == workspaceId &&
                    x.ProjectId == projectId)
                .OrderByDescending(x => x.OccurredUtc)
                .Take(n)
                .ToList();
        }

        return Task.FromResult<IReadOnlyList<AuditEvent>>(result);
    }
}
