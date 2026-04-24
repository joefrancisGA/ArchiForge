using System.Security.Claims;

using ArchLucid.Core.Authorization;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     DevelopmentBypass with <see cref="ArchLucidRoles.Reader" /> — satisfies
///     <see cref="ArchLucidPolicies.ReadAuthority" /> but not consulting-docx permission.
/// </summary>
public sealed class ReaderRoleArchLucidApiFactory : ArchLucidApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?> { ["ArchLucidAuth:DevRole"] = ArchLucidRoles.Reader }));
    }
}

/// <summary>
///     Like <see cref="ArchLucidRoleClaimsTransformation" /> but drops one <c>permission</c> for the Operator role (tests
///     fine-grained policies).
/// </summary>
internal sealed class OmitOnePermissionForOperatorClaimsTransformation : IClaimsTransformation
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

    private readonly string _omitPermission;

    public OmitOnePermissionForOperatorClaimsTransformation(string omitPermission)
    {
        _omitPermission = omitPermission;
    }

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
        {
            foreach (string p in AdminPermissions)
                AddPermission(p);
        }
        else if (roles.Contains(ArchLucidRoles.Operator))
        {
            foreach (string p in OperatorPermissions)
            {
                if (!string.Equals(p, _omitPermission, StringComparison.Ordinal))
                    AddPermission(p);
            }
        }
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

public abstract class OperatorWithoutOnePermissionArchLucidApiFactory : ArchLucidApiFactory
{
    protected abstract string OmitPermission
    {
        get;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?> { ["ArchLucidAuth:DevRole"] = ArchLucidRoles.Operator }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IClaimsTransformation>();
            services.AddScoped<IClaimsTransformation>(_ =>
                new OmitOnePermissionForOperatorClaimsTransformation(OmitPermission));
        });
    }
}

public sealed class OperatorWithoutCommitRunPermissionApiFactory : OperatorWithoutOnePermissionArchLucidApiFactory
{
    protected override string OmitPermission => "commit:run";
}

public sealed class OperatorWithoutConsultingDocxPermissionApiFactory : OperatorWithoutOnePermissionArchLucidApiFactory
{
    protected override string OmitPermission => "export:consulting-docx";
}
