namespace ArchiForge.Decisioning.Alerts;

/// <summary>
/// CRUD and scoped queries for simple (non-composite) <see cref="AlertRule"/> definitions stored per tenant/workspace/project.
/// </summary>
/// <remarks>
/// After loading, <c>AlertService</c> applies <c>PolicyPackGovernanceFilter.FilterAlertRules</c> before evaluation.
/// </remarks>
public interface IAlertRuleRepository
{
    /// <summary>Inserts a new rule row.</summary>
    Task CreateAsync(AlertRule rule, CancellationToken ct);

    /// <summary>Updates mutable rule fields.</summary>
    Task UpdateAsync(AlertRule rule, CancellationToken ct);

    /// <summary>Loads a single rule by id.</summary>
    Task<AlertRule?> GetByIdAsync(Guid ruleId, CancellationToken ct);

    /// <summary>All rules in scope (enabled and disabled).</summary>
    Task<IReadOnlyList<AlertRule>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct);

    /// <summary>Rules eligible for evaluation (<see cref="AlertRule.IsEnabled"/> and scope match).</summary>
    Task<IReadOnlyList<AlertRule>> ListEnabledByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct);
}
