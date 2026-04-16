namespace ArchLucid.Core.Tenancy;

/// <summary>Creates tenant registry rows and default workspace/project identifiers.</summary>
public interface ITenantProvisioningService
{
    Task<TenantProvisioningResult> ProvisionAsync(TenantProvisioningRequest request, CancellationToken ct);
}
