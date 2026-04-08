using System.Security.Claims;

using ArchLucid.Api.Auth.Models;

using Microsoft.AspNetCore.Authentication;

namespace ArchLucid.Api.Auth.Services;

/// <summary>
/// Maps ArchLucid roles to legacy <c>permission</c> claims so existing policies
/// (CanCommitRuns, CanReplayComparisons, etc.) keep working with JWT or dev bypass.
/// </summary>
public sealed class ArchLucidRoleClaimsTransformation : IClaimsTransformation
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

        HashSet<string> roles = new(StringComparer.OrdinalIgnoreCase);
        foreach (Claim c in clone.FindAll(ClaimTypes.Role))
            roles.Add(c.Value);
        foreach (Claim c in clone.FindAll("roles"))
            roles.Add(c.Value);

        if (roles.Contains(ArchLucidRoles.Admin))
        
            foreach (string p in AdminPermissions)
                AddPermission(p);
        
        else if (roles.Contains(ArchLucidRoles.Operator))
        
            foreach (string p in OperatorPermissions)
                AddPermission(p);
        
        else if (roles.Contains(ArchLucidRoles.Reader))
        
            AddPermission("metrics:read");
        

        return Task.FromResult(clone);

        void AddPermission(string value)
        {
            if (!id.HasClaim("permission", value))
                id.AddClaim(new Claim("permission", value));
        }
    }
}
