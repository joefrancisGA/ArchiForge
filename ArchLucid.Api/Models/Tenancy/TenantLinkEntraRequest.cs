namespace ArchLucid.Api.Models.Tenancy;

/// <summary>Optional body for <c>POST /v1/tenant/link-entra</c> (commercial Entra directory handoff).</summary>
public sealed class TenantLinkEntraRequest
{
    /// <summary>Corporate Entra directory id from the token <c>tid</c> claim.</summary>
    public Guid EntraTenantId
    {
        get;
        init;
    }

    /// <summary>
    /// Optional: admin email matching <c>dbo.IdentityUsers</c> for trial local handoff; requires <see cref="EntraOid" />.
    /// </summary>
    public string? LocalEmail
    {
        get;
        init;
    }

    /// <summary>Optional: Entra user id (<c>oid</c>); requires <see cref="LocalEmail" />.</summary>
    public string? EntraOid
    {
        get;
        init;
    }
}
