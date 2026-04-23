namespace ArchLucid.Core.Tenancy;

/// <summary>
///     Hard-deletes tenant-scoped rows in <c>dbo</c> (append-only <c>dbo.AuditEvents</c> retained per retention policy).
///     Invoked when the trial lifecycle reaches the purge phase.
/// </summary>
public interface ITenantHardPurgeService
{
    Task<TenantHardPurgeResult> PurgeTenantAsync(
        Guid tenantId,
        TenantHardPurgeOptions options,
        CancellationToken cancellationToken);
}
