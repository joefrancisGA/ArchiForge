using ArchiForge.Core.Audit;

namespace ArchiForge.Persistence.Audit;

public sealed class InMemoryAuditRepository : IAuditRepository
{
    private readonly object _gate = new();
    private readonly List<AuditEvent> _events = [];

    public Task AppendAsync(AuditEvent auditEvent, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
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
        _ = ct;
        var n = take <= 0 ? 100 : take;
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
