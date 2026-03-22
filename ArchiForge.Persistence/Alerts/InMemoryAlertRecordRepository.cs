using ArchiForge.Decisioning.Alerts;

namespace ArchiForge.Persistence.Alerts;

public sealed class InMemoryAlertRecordRepository : IAlertRecordRepository
{
    private readonly List<AlertRecord> _items = [];
    private readonly object _gate = new();

    public Task CreateAsync(AlertRecord alert, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
            _items.Add(alert);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(AlertRecord alert, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var i = _items.FindIndex(x => x.AlertId == alert.AlertId);
            if (i >= 0)
                _items[i] = alert;
        }

        return Task.CompletedTask;
    }

    public Task<AlertRecord?> GetByIdAsync(Guid alertId, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
            return Task.FromResult(_items.FirstOrDefault(x => x.AlertId == alertId));
    }

    public Task<AlertRecord?> GetOpenByDeduplicationKeyAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string deduplicationKey,
        CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var match = _items
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.WorkspaceId == workspaceId &&
                    x.ProjectId == projectId &&
                    string.Equals(x.DeduplicationKey, deduplicationKey, StringComparison.Ordinal) &&
                    (string.Equals(x.Status, AlertStatus.Open, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(x.Status, AlertStatus.Acknowledged, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(x => x.CreatedUtc)
                .FirstOrDefault();
            return Task.FromResult(match);
        }
    }

    public Task<IReadOnlyList<AlertRecord>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string? status,
        int take,
        CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var q = _items.Where(x => x.TenantId == tenantId && x.WorkspaceId == workspaceId && x.ProjectId == projectId);
            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(x => string.Equals(x.Status, status, StringComparison.OrdinalIgnoreCase));

            var result = q.OrderByDescending(x => x.CreatedUtc).Take(take).ToList();
            return Task.FromResult<IReadOnlyList<AlertRecord>>(result);
        }
    }
}
