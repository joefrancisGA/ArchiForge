namespace ArchLucid.Core.Tenancy;

/// <summary>Input for <see cref="ITenantProvisioningService.ProvisionAsync"/>.</summary>
public sealed class TenantProvisioningRequest
{
    public string Name { get; init; } = string.Empty;

    public string AdminEmail { get; init; } = string.Empty;

    public TenantTier Tier { get; init; } = TenantTier.Standard;
}
