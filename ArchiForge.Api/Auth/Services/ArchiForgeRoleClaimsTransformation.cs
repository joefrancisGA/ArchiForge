using System.Security.Claims;

using ArchiForge.Api.Auth.Models;

using Microsoft.AspNetCore.Authentication;

namespace ArchiForge.Api.Auth.Services;

/// <summary>
/// Maps ArchiForge roles to legacy <c>permission</c> claims so existing policies
/// (CanCommitRuns, CanReplayComparisons, etc.) keep working with JWT or dev bypass.
/// </summary>
public sealed class ArchiForgeRoleClaimsTransformation : IClaimsTransformation
{
    private static readonly string[] AdminPermissions =
    [
        "commit:run",
        "seed:results",
        "export:consulting-docx",
        "replay:comparisons",
        "replay:diagnostics",
        "metrics:read"
    ];

    private static readonly string[] OperatorPermissions =
    [
        "commit:run",
        "seed:results",
        "export:consulting-docx",
        "replay:comparisons",
        "replay:diagnostics"
    ];

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return Task.FromResult(principal);

        ClaimsPrincipal clone = principal.Clone();
        if (clone.Identity is not ClaimsIdentity id)
            return Task.FromResult(principal);

        HashSet<string> roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Claim c in clone.FindAll(ClaimTypes.Role))
            roles.Add(c.Value);
        foreach (Claim c in clone.FindAll("roles"))
            roles.Add(c.Value);

        if (roles.Contains(ArchiForgeRoles.Admin))
        {
            foreach (string p in AdminPermissions)
                AddPermission(p);
        }
        else if (roles.Contains(ArchiForgeRoles.Operator))
        {
            foreach (string p in OperatorPermissions)
                AddPermission(p);
        }
        else if (roles.Contains(ArchiForgeRoles.Reader))
        {
            AddPermission("metrics:read");
        }

        return Task.FromResult(clone);

        void AddPermission(string value)
        {
            if (!id.HasClaim("permission", value))
                id.AddClaim(new Claim("permission", value));
        }
    }
}
