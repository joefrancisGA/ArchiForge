namespace ArchLucid.Application.Scim.RoleMapping;

public interface IGroupToRoleMapper
{
    /// <summary>Resolves ArchLucid role name from IdP group display name / externalId.</summary>
    string? TryMapGroupToRole(string displayName, string externalId);
}
