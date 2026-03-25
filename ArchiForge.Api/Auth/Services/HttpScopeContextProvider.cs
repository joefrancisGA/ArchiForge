using System.Security.Claims;

using ArchiForge.Core.Scoping;

using Microsoft.Extensions.Primitives;

namespace ArchiForge.Api.Auth.Services;

/// <summary>
/// Resolves <see cref="ScopeContext"/> from optional ambient override, then JWT claims, then <c>x-*-id</c> headers, with dev fallbacks.
/// </summary>
/// <param name="httpContextAccessor">Current HTTP context when the call is on the request thread.</param>
/// <remarks>
/// Registered scoped in the API host. Consumers such as <c>PolicyFilteredComplianceRulePackProvider</c> and authority controllers
/// depend on consistent scope for governance and row-level filtering.
/// </remarks>
public sealed class HttpScopeContextProvider(IHttpContextAccessor httpContextAccessor) : IScopeContextProvider
{
    /// <inheritdoc />
    /// <remarks>
    /// Order: <see cref="AmbientScopeContext.CurrentOverride"/> (background jobs), then headers <c>x-tenant-id</c> / <c>x-workspace-id</c> / <c>x-project-id</c>,
    /// then claims <c>tenant_id</c>, <c>workspace_id</c>, <c>project_id</c>, else <see cref="ScopeIds"/> defaults.
    /// </remarks>
    public ScopeContext GetCurrentScope()
    {
        ScopeContext? ambient = AmbientScopeContext.CurrentOverride;
        if (ambient is not null)
            return ambient;

        HttpContext? http = httpContextAccessor.HttpContext;
        ClaimsPrincipal? user = http?.User;
        IHeaderDictionary? headers = http?.Request.Headers;

        Guid tenant = TryHeader("x-tenant-id", TryGetClaim("tenant_id", ScopeIds.DefaultTenant));
        Guid workspace = TryHeader("x-workspace-id", TryGetClaim("workspace_id", ScopeIds.DefaultWorkspace));
        Guid project = TryHeader("x-project-id", TryGetClaim("project_id", ScopeIds.DefaultProject));

        return new ScopeContext
        {
            TenantId = tenant,
            WorkspaceId = workspace,
            ProjectId = project
        };

        Guid TryGetClaim(string claim, Guid fallback)
        {
            string? value = user?.FindFirst(claim)?.Value;
            return Guid.TryParse(value, out Guid parsed) ? parsed : fallback;
        }

        Guid TryHeader(string key, Guid fallback)
        {
            if (headers is null || !headers.TryGetValue(key, out StringValues value))
                return fallback;
            return Guid.TryParse(value.ToString(), out Guid parsed) ? parsed : fallback;
        }
    }
}
