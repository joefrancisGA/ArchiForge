namespace ArchLucid.Core.Tenancy;

/// <summary>Persistence for <c>dbo.Tenants</c> / <c>dbo.TenantWorkspaces</c>.</summary>
public interface ITenantRepository
{
    Task<TenantRecord?> GetByIdAsync(Guid tenantId, CancellationToken ct);

    Task<TenantRecord?> GetBySlugAsync(string slug, CancellationToken ct);

    Task<IReadOnlyList<TenantRecord>> ListAsync(CancellationToken ct);

    Task InsertTenantAsync(
        Guid tenantId,
        string name,
        string slug,
        TenantTier tier,
        CancellationToken ct);

    Task InsertWorkspaceAsync(
        Guid workspaceId,
        Guid tenantId,
        string name,
        Guid defaultProjectId,
        CancellationToken ct);

    Task SuspendTenantAsync(Guid tenantId, CancellationToken ct);

    /// <summary>Oldest workspace for the tenant (default bootstrap workspace).</summary>
    Task<TenantWorkspaceLink?> GetFirstWorkspaceAsync(Guid tenantId, CancellationToken ct);
}
