namespace ArchLucid.Application.Scim.Patching;

public sealed class ScimPatchException : Exception
{
    public ScimPatchException(string scimType, string detail)
        : base(detail)
    {
        ScimType = scimType;
    }

    public string ScimType
    {
        get;
    }
}
