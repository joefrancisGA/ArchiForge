namespace ArchLucid.Core.Configuration;

/// <summary>SCIM 2.0 inbound service provider configuration (bound from <c>Scim</c> configuration section).</summary>
public sealed class ScimOptions
{
    public const string SectionName = "Scim";

    /// <summary>Optional overrides for IdP group key → <see cref="Authorization.ArchLucidRoles" /> name.</summary>
    public Dictionary<string, string> GroupRoleMappingOverrides
    {
        get;
        set;
    } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Daily reminder job threshold in days (<c>0</c> disables). Default six months.</summary>
    public int TokenRotationReminderDays
    {
        get;
        set;
    } = 180;
}
