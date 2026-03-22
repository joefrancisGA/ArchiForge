using ArchiForge.Decisioning.Alerts;

namespace ArchiForge.Persistence.Alerts;

public sealed class InMemoryAlertRuleRepository : IAlertRuleRepository
{
    private readonly List<AlertRule> _items = [];
    private readonly Lock _gate = new();

    public Task CreateAsync(AlertRule rule, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
            _items.Add(rule);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(AlertRule rule, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var i = _items.FindIndex(x => x.RuleId == rule.RuleId);
            if (i >= 0)
                _items[i] = rule;
        }

        return Task.CompletedTask;
    }

    public Task<AlertRule?> GetByIdAsync(Guid ruleId, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
            return Task.FromResult(_items.FirstOrDefault(x => x.RuleId == ruleId));
    }

    public Task<IReadOnlyList<AlertRule>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var result = _items
                .Where(x => x.TenantId == tenantId && x.WorkspaceId == workspaceId && x.ProjectId == projectId)
                .OrderByDescending(x => x.CreatedUtc)
                .ToList();
            return Task.FromResult<IReadOnlyList<AlertRule>>(result);
        }
    }

    public Task<IReadOnlyList<AlertRule>> ListEnabledByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var result = _items
                .Where(x => x.TenantId == tenantId && x.WorkspaceId == workspaceId && x.ProjectId == projectId && x.IsEnabled)
                .OrderByDescending(x => x.CreatedUtc)
                .ToList();
            return Task.FromResult<IReadOnlyList<AlertRule>>(result);
        }
    }
}
