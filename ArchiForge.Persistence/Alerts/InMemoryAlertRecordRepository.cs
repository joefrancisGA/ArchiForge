using ArchiForge.Decisioning.Alerts;

namespace ArchiForge.Persistence.Alerts;

/// <summary>
/// Thread-safe in-memory store implementing <see cref="IAlertRecordRepository"/> for tests and local scenarios.
/// </summary>
/// <remarks>Semantics mirror <see cref="DapperAlertRecordRepository"/> for open dedup (Open + Acknowledged only).</remarks>
public sealed class InMemoryAlertRecordRepository : IAlertRecordRepository
{
    private const int MaxEntries = 500;
    private readonly List<AlertRecord> _items = [];
    private readonly Lock _gate = new();

    public Task CreateAsync(AlertRecord alert, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _items.Add(alert);
            if (_items.Count > MaxEntries)
                _items.RemoveRange(0, _items.Count - MaxEntries);
        }
        return Task.CompletedTask;
    }

    public Task UpdateAsync(AlertRecord alert, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
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
        ct.ThrowIfCancellationRequested();
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
        ct.ThrowIfCancellationRequested();
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
        ct.ThrowIfCancellationRequested();
        var n = Math.Clamp(take <= 0 ? 50 : take, 1, 500);
        lock (_gate)
        {
            var q = _items.Where(x => x.TenantId == tenantId && x.WorkspaceId == workspaceId && x.ProjectId == projectId);
            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(x => string.Equals(x.Status, status, StringComparison.OrdinalIgnoreCase));

            var result = q.OrderByDescending(x => x.CreatedUtc).Take(n).ToList();
            return Task.FromResult<IReadOnlyList<AlertRecord>>(result);
        }
    }
}
