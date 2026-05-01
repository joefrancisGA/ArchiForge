namespace ArchLucid.Core.Scim.Models;

public sealed class ScimUserRecord
{
    public Guid Id
    {
        get;
        init;
    }

    public Guid TenantId
    {
        get;
        init;
    }

    public string ExternalId
    {
        get;
        init;
    } = string.Empty;

    public string UserName
    {
        get;
        init;
    } = string.Empty;

    public string? DisplayName
    {
        get;
        init;
    }

    public bool Active
    {
        get;
        init;
    }

    public string? ResolvedRole
    {
        get;
        init;
    }

    public ScimResolvedRoleOrigin ResolvedRoleOrigin
    {
        get;
        init;
    } = ScimResolvedRoleOrigin.Unknown;

    public DateTimeOffset CreatedUtc
    {
        get;
        init;
    }

    public DateTimeOffset UpdatedUtc
    {
        get;
        init;
    }
}
