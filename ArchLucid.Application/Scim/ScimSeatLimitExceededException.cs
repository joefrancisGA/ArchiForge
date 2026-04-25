namespace ArchLucid.Application.Scim;

public sealed class ScimSeatLimitExceededException : Exception
{
    public ScimSeatLimitExceededException()
        : base("Enterprise seat limit reached for this tenant.")
    {
    }
}
