namespace ArchLucid.Core.Tenancy;

/// <summary>Input for <see cref="ITenantProvisioningService.ProvisionAsync" />.</summary>
public sealed class TenantProvisioningRequest
{
    public string Name
    {
        get;
        init;
    } = string.Empty;

    public string AdminEmail
    {
        get;
        init;
    } = string.Empty;

    public TenantTier Tier
    {
        get;
        init;
    } = TenantTier.Standard;

    /// <summary>Optional Entra <c>tid</c> to store on <c>dbo.Tenants.EntraTenantId</c> (multi-org directory link).</summary>
    public Guid? EntraTenantId
    {
        get;
        init;
    }

    /// <summary>When set, used as audit actor instead of HTTP actor context (e.g. self-service registration email).</summary>
    public string? AuditActorOverride
    {
        get;
        init;
    }
}
