namespace ArchLucid.Core.Tenancy;

/// <summary>Outcome of tenant provisioning.</summary>
public sealed class TenantProvisioningResult
{
    public Guid TenantId
    {
        get;
        init;
    }

    public Guid DefaultWorkspaceId
    {
        get;
        init;
    }

    public Guid DefaultProjectId
    {
        get;
        init;
    }

    public bool WasAlreadyProvisioned
    {
        get;
        init;
    }
}
