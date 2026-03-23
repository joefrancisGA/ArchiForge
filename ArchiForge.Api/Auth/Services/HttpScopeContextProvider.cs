using ArchiForge.Core.Scoping;

using Microsoft.AspNetCore.Http;

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
        var ambient = AmbientScopeContext.CurrentOverride;
        if (ambient is not null)
            return ambient;

        var http = httpContextAccessor.HttpContext;
        var user = http?.User;
        var headers = http?.Request.Headers;

        var tenant = TryHeader("x-tenant-id", TryGetClaim("tenant_id", ScopeIds.DefaultTenant));
        var workspace = TryHeader("x-workspace-id", TryGetClaim("workspace_id", ScopeIds.DefaultWorkspace));
        var project = TryHeader("x-project-id", TryGetClaim("project_id", ScopeIds.DefaultProject));

        return new ScopeContext
        {
            TenantId = tenant,
            WorkspaceId = workspace,
            ProjectId = project
        };

        Guid TryGetClaim(string claim, Guid fallback)
        {
            var value = user?.FindFirst(claim)?.Value;
            return Guid.TryParse(value, out var parsed) ? parsed : fallback;
        }

        Guid TryHeader(string key, Guid fallback)
        {
            if (headers is null || !headers.TryGetValue(key, out var value))
                return fallback;
            return Guid.TryParse(value.ToString(), out var parsed) ? parsed : fallback;
        }
    }
}
