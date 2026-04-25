using ArchLucid.Core.Authorization;
using ArchLucid.Host.Core.Authorization;

using Microsoft.AspNetCore.Authorization;

namespace ArchLucid.Host.Core.Startup;

/// <summary>Registers ArchLucid <see cref="AuthorizationOptions"/> policies (RBAC + permission claims).</summary>
public static class ArchLucidAuthorizationPoliciesExtensions
{
    /// <summary>
    /// Registers role- and claim-based policies. <see cref="AuthorizationOptions.FallbackPolicy"/> requires an authenticated user;
    /// use <c>[AllowAnonymous]</c> only for intentional public routes (health, version).
    /// </summary>
    public static IServiceCollection AddArchLucidAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(
                new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build())
            .AddPolicy(ArchLucidPolicies.ReadAuthority, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(
                    ArchLucidRoles.Reader,
                    ArchLucidRoles.Operator,
                    ArchLucidRoles.Admin,
                    ArchLucidRoles.Auditor);
            })
            .AddPolicy(ArchLucidPolicies.ExecuteAuthority, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(
                    ArchLucidRoles.Operator,
                    ArchLucidRoles.Admin);
                policy.Requirements.Add(new TrialActiveRequirement());
            })
            .AddPolicy(ArchLucidPolicies.AdminAuthority, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(ArchLucidRoles.Admin);
                policy.Requirements.Add(new TrialActiveRequirement());
            })
            .AddPolicy(ArchLucidPolicies.RequireAuditor, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(
                    ArchLucidRoles.Auditor,
                    ArchLucidRoles.Admin);
            })
            .AddPolicy(ArchLucidPolicies.CanCommitRuns, policy =>
                policy.RequireClaim("permission", "commit:run"))
            .AddPolicy("CanSeedResults", policy =>
                policy.RequireClaim("permission", "seed:results"))
            .AddPolicy(ArchLucidPolicies.CanExportConsultingDocx, policy =>
                policy.RequireClaim("permission", "export:consulting-docx"))
            .AddPolicy(ArchLucidPolicies.CanReplayComparisons, policy =>
                policy.RequireClaim("permission", "replay:comparisons"))
            .AddPolicy(ArchLucidPolicies.CanViewReplayDiagnostics, policy =>
                policy.RequireClaim("permission", "replay:diagnostics"))
            .AddPolicy(ArchLucidPolicies.ScimWrite, policy =>
            {
                policy.AddAuthenticationSchemes(ScimBearerDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
            });

        return services;
    }
}
