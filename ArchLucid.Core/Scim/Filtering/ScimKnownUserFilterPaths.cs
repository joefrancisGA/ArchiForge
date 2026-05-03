namespace ArchLucid.Core.Scim.Filtering;

/// <summary>
///     Canonical SCIM attribute paths used when translating IdP filters (Entra/Microsoft Graph provisioning shapes).
/// </summary>
public static class ScimKnownUserFilterPaths
{
    /// <summary>Maps to persisted primary identifier (<c>UserName</c>) — Entra frequently queries work email via this valued-attribute path.</summary>
    public const string EmailsWorkValue = @"emails[type eq ""work""].value";

    public static bool IsEmailsWorkValuePath(string attributePath)
    {
        if (attributePath is null)
            return false;

        return string.Equals(attributePath.Trim(), EmailsWorkValue, StringComparison.OrdinalIgnoreCase);
    }
}
