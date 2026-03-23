namespace ArchiForge.Decisioning.Alerts;

/// <summary>
/// Persistence for fired <see cref="AlertRecord"/> rows: insert/update, deduplication lookup, and scoped listing for the alerts API.
/// </summary>
/// <remarks>
/// SQL implementation: <c>ArchiForge.Persistence.Alerts.DapperAlertRecordRepository</c> (<c>dbo.AlertRecords</c>).
/// Used by <c>ArchiForge.Persistence.Alerts.AlertService</c>, composite suppression, and <c>AlertsController</c>.
/// </remarks>
public interface IAlertRecordRepository
{
    /// <summary>Persists a new alert row (typically after evaluation and dedup miss).</summary>
    Task CreateAsync(AlertRecord alert, CancellationToken ct);

    /// <summary>Updates lifecycle fields after acknowledge/resolve/suppress.</summary>
    Task UpdateAsync(AlertRecord alert, CancellationToken ct);

    /// <summary>Returns a row by primary key regardless of scope (callers often verify scope separately).</summary>
    Task<AlertRecord?> GetByIdAsync(Guid alertId, CancellationToken ct);

    /// <summary>
    /// Finds the newest open or acknowledged alert for the deduplication key within the scope (used to suppress duplicate fires).
    /// </summary>
    Task<AlertRecord?> GetOpenByDeduplicationKeyAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string deduplicationKey,
        CancellationToken ct);

    /// <summary>Lists recent alerts for the scope, optionally filtered by <paramref name="status"/>.</summary>
    Task<IReadOnlyList<AlertRecord>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string? status,
        int take,
        CancellationToken ct);
}
