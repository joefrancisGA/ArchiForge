using ArchiForge.Decisioning.Alerts.Composite;

namespace ArchiForge.Persistence.Alerts;

/// <summary>In-memory <see cref="ICompositeAlertRuleRepository"/> for tests; clones rules on write to mimic isolated rows.</summary>
public sealed class InMemoryCompositeAlertRuleRepository : ICompositeAlertRuleRepository
{
    private readonly List<CompositeAlertRule> _items = [];
    private readonly Lock _gate = new();

    public Task CreateAsync(CompositeAlertRule rule, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
            _items.Add(CloneRule(rule));
        return Task.CompletedTask;
    }

    public Task UpdateAsync(CompositeAlertRule rule, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var i = _items.FindIndex(x => x.CompositeRuleId == rule.CompositeRuleId);
            if (i >= 0)
                _items[i] = CloneRule(rule);
        }

        return Task.CompletedTask;
    }

    public Task<CompositeAlertRule?> GetByIdAsync(Guid compositeRuleId, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var found = _items.FirstOrDefault(x => x.CompositeRuleId == compositeRuleId);
            return Task.FromResult(found is null ? null : CloneRule(found));
        }
    }

    public Task<IReadOnlyList<CompositeAlertRule>> ListByScopeAsync(
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
                .Select(CloneRule)
                .ToList();
            return Task.FromResult<IReadOnlyList<CompositeAlertRule>>(result);
        }
    }

    public Task<IReadOnlyList<CompositeAlertRule>> ListEnabledByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var result = _items
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.WorkspaceId == workspaceId &&
                    x.ProjectId == projectId &&
                    x.IsEnabled)
                .OrderByDescending(x => x.CreatedUtc)
                .Select(CloneRule)
                .ToList();
            return Task.FromResult<IReadOnlyList<CompositeAlertRule>>(result);
        }
    }

    private static CompositeAlertRule CloneRule(CompositeAlertRule r)
    {
        var copy = new CompositeAlertRule
        {
            CompositeRuleId = r.CompositeRuleId,
            TenantId = r.TenantId,
            WorkspaceId = r.WorkspaceId,
            ProjectId = r.ProjectId,
            Name = r.Name,
            Severity = r.Severity,
            Operator = r.Operator,
            IsEnabled = r.IsEnabled,
            SuppressionWindowMinutes = r.SuppressionWindowMinutes,
            CooldownMinutes = r.CooldownMinutes,
            ReopenDeltaThreshold = r.ReopenDeltaThreshold,
            DedupeScope = r.DedupeScope,
            TargetChannelType = r.TargetChannelType,
            CreatedUtc = r.CreatedUtc,
            Conditions = r.Conditions
                .Select(c => new AlertRuleCondition
                {
                    ConditionId = c.ConditionId,
                    MetricType = c.MetricType,
                    Operator = c.Operator,
                    ThresholdValue = c.ThresholdValue,
                })
                .ToList(),
        };
        return copy;
    }
}
