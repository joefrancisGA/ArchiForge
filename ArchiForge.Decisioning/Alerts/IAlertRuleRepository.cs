namespace ArchiForge.Decisioning.Alerts;

public interface IAlertRuleRepository
{
    Task CreateAsync(AlertRule rule, CancellationToken ct);
    Task UpdateAsync(AlertRule rule, CancellationToken ct);
    Task<AlertRule?> GetByIdAsync(Guid ruleId, CancellationToken ct);

    Task<IReadOnlyList<AlertRule>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct);

    Task<IReadOnlyList<AlertRule>> ListEnabledByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct);
}
