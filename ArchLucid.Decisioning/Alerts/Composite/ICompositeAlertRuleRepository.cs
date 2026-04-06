namespace ArchiForge.Decisioning.Alerts.Composite;

/// <summary>
/// Persistence for <see cref="CompositeAlertRule"/> headers and their <see cref="AlertRuleCondition"/> rows.
/// </summary>
/// <remarks>
/// Implementations should load conditions with the rule graph. SQL: <c>DapperCompositeAlertRuleRepository</c> with
/// <c>dbo.CompositeAlertRules</c> and <c>dbo.CompositeAlertRuleConditions</c>.
/// </remarks>
public interface ICompositeAlertRuleRepository
{
    /// <summary>Atomically inserts the rule and all conditions (implementation-defined transaction boundaries).</summary>
    Task CreateAsync(CompositeAlertRule rule, CancellationToken ct);

    /// <summary>Replaces rule metadata and condition set (typical pattern: delete conditions then re-insert).</summary>
    Task UpdateAsync(CompositeAlertRule rule, CancellationToken ct);

    /// <summary>Loads one rule with conditions populated.</summary>
    Task<CompositeAlertRule?> GetByIdAsync(Guid compositeRuleId, CancellationToken ct);

    /// <summary>All composite rules in scope.</summary>
    Task<IReadOnlyList<CompositeAlertRule>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct);

    /// <summary>Enabled rules in scope; used by <see cref="ICompositeAlertService"/> after governance filtering.</summary>
    Task<IReadOnlyList<CompositeAlertRule>> ListEnabledByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct);
}
