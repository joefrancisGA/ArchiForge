namespace ArchiForge.Decisioning.Alerts;

public interface IAlertRecordRepository
{
    Task CreateAsync(AlertRecord alert, CancellationToken ct);
    Task UpdateAsync(AlertRecord alert, CancellationToken ct);

    Task<AlertRecord?> GetByIdAsync(Guid alertId, CancellationToken ct);

    Task<AlertRecord?> GetOpenByDeduplicationKeyAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string deduplicationKey,
        CancellationToken ct);

    Task<IReadOnlyList<AlertRecord>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string? status,
        int take,
        CancellationToken ct);
}
