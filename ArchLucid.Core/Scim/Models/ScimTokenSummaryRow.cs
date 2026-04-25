namespace ArchLucid.Core.Scim.Models;

public sealed class ScimTokenSummaryRow
{
    public Guid Id
    {
        get;
        init;
    }

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

    public string PublicLookupKey
    {
        get;
        init;
    } = string.Empty;
}
