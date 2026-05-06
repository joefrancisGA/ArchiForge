namespace ArchLucid.Application.Scim.Filtering;
public sealed class ScimFilterParseException : Exception
{
    public ScimFilterParseException(string message) : base(message)
    {
        ArgumentNullException.ThrowIfNull(message);
    }
}