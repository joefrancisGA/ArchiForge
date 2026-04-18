using ArchLucid.Core.Tenancy;

namespace ArchLucid.Persistence.Tenancy;

/// <summary>In-memory / non-SQL hosts: hard purge is a no-op (Worker lifecycle still advances statuses until Deleted).</summary>
public sealed class NoOpTenantHardPurgeService : ITenantHardPurgeService
{
    /// <inheritdoc />
    public Task<TenantHardPurgeResult> PurgeTenantAsync(
        Guid tenantId,
        TenantHardPurgeOptions options,
        CancellationToken cancellationToken)
    {
        _ = tenantId;
        _ = options;
        _ = cancellationToken;

        return Task.FromResult(new TenantHardPurgeResult());
    }
}
