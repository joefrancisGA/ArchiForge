using ArchLucid.Core.Tenancy;

namespace ArchLucid.Api.Controllers.Admin;

/// <summary>JSON body for <c>POST /v1/admin/tenants</c>.</summary>
public sealed class TenantProvisionAdminRequest
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
}
