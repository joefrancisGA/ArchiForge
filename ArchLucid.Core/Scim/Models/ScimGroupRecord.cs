namespace ArchLucid.Core.Scim.Models;

public sealed class ScimGroupRecord
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

    public string DisplayName
    {
        get;
        init;
    } = string.Empty;

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
