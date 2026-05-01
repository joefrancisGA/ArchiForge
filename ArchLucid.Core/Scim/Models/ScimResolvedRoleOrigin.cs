namespace ArchLucid.Core.Scim.Models;

/// <summary>How <see cref="ScimUserRecord.ResolvedRole"/> was assigned (audit + SCIM precedence).</summary>
public enum ScimResolvedRoleOrigin : byte
{
    /// <summary>Legacy row or unspecified.</summary>
    Unknown = 0,

    /// <summary>Operator-set via SCIM PATCH path <c>manualResolvedRole</c> (non-group).</summary>
    Manual = 1,

    /// <summary>Derived from Enterprise IdP group → role mappings.</summary>
    ScimGroups = 2
}
