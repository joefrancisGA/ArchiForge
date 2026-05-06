namespace ArchLucid.Application.Scim.Patching;
public sealed class ScimPatchException : Exception
{
    public ScimPatchException(string scimType, string detail) : base(detail)
    {
        ArgumentNullException.ThrowIfNull(scimType);
        ArgumentNullException.ThrowIfNull(detail);
        ScimType = scimType;
    }

    public string ScimType { get; }
}