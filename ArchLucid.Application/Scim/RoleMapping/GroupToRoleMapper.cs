using ArchLucid.Core.Authorization;
using ArchLucid.Core.Configuration;

using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Scim.RoleMapping;

public sealed class GroupToRoleMapper(IOptions<ScimOptions> options) : IGroupToRoleMapper
{
    private readonly ScimOptions _options = options.Value;

    /// <inheritdoc />
    public string? TryMapGroupToRole(string displayName, string externalId)
    {
        string[] keys = [externalId.Trim(), displayName.Trim()];

        foreach (string key in keys)
        {
            if (string.IsNullOrEmpty(key))
                continue;

            if (_options.GroupRoleMappingOverrides.TryGetValue(key, out string? mapped) &&
                !string.IsNullOrWhiteSpace(mapped))
                return mapped.Trim();
        }

        if (string.Equals(externalId, "archlucid:admins", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(displayName, "archlucid:admins", StringComparison.OrdinalIgnoreCase))
            return ArchLucidRoles.Admin;

        if (string.Equals(externalId, "archlucid:operators", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(displayName, "archlucid:operators", StringComparison.OrdinalIgnoreCase))
            return ArchLucidRoles.Operator;

        if (string.Equals(externalId, "archlucid:auditors", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(displayName, "archlucid:auditors", StringComparison.OrdinalIgnoreCase))
            return ArchLucidRoles.Auditor;

        if (string.Equals(externalId, "archlucid:readers", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(displayName, "archlucid:readers", StringComparison.OrdinalIgnoreCase))
            return ArchLucidRoles.Reader;

        return null;
    }
}
