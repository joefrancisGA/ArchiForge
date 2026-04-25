namespace ArchLucid.Core.Scim.Models;

public sealed class ScimTokenRow
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

    public string PublicLookupKey
    {
        get;
        init;
    } = string.Empty;

    public byte[] SecretHash
    {
        get;
        init;
    } = [];

    public DateTimeOffset CreatedUtc
    {
        get;
        init;
    }

    public DateTimeOffset? RevokedUtc
    {
        get;
        init;
    }
}
