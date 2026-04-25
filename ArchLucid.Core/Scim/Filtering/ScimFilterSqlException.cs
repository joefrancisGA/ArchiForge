namespace ArchLucid.Core.Scim.Filtering;

public sealed class ScimFilterSqlException : Exception
{
    public ScimFilterSqlException(string message)
        : base(message)
    {
    }
}
