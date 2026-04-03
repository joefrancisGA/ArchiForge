using System.Security.Claims;

using ArchiForge.Core.Scoping;

using Microsoft.Extensions.Primitives;

namespace ArchiForge.Host.Core.Auth.Services;

/// <summary>
/// Resolves <see cref="ScopeContext"/> from optional ambient override, then JWT claims, then <c>x-*-id</c> headers (headers only when the claim is absent or not a valid GUID), with dev fallbacks.
/// </summary>
/// <param name="httpContextAccessor">Current HTTP context when the call is on the request thread.</param>
/// <remarks>
/// Register as <strong>singleton</strong>: each <see cref="GetCurrentScope"/> call reads <see cref="IHttpContextAccessor.HttpContext"/> (or <see cref="AmbientScopeContext"/>),
/// so there is no per-request instance state. Singleton registration allows singleton <c>IAgentCompletionClient</c> decorators (LLM response cache) to resolve this interface from the root provider.
/// </remarks>
public sealed class HttpScopeContextProvider(IHttpContextAccessor httpContextAccessor) : IScopeContextProvider
{
    /// <inheritdoc />
    /// <remarks>
    /// Order per dimension: <see cref="AmbientScopeContext.CurrentOverride"/> (background jobs), then JWT claims
    /// <c>tenant_id</c> / <c>workspace_id</c> / <c>project_id</c> when they parse as GUIDs, then <c>x-*-id</c> headers,
    /// else <see cref="ScopeIds"/> defaults. Claims win over headers so callers cannot override token-bound scope via headers (IDOR mitigation).
    /// </remarks>
    public ScopeContext GetCurrentScope()
    {
        ScopeContext? ambient = AmbientScopeContext.CurrentOverride;
        if (ambient is not null)
            return ambient;

        HttpContext? http = httpContextAccessor.HttpContext;
        ClaimsPrincipal? user = http?.User;
        IHeaderDictionary? headers = http?.Request.Headers;

        return new ScopeContext
        {
            TenantId = ResolveScopeId(user, headers, "tenant_id", "x-tenant-id", ScopeIds.DefaultTenant),
            WorkspaceId = ResolveScopeId(user, headers, "workspace_id", "x-workspace-id", ScopeIds.DefaultWorkspace),
            ProjectId = ResolveScopeId(user, headers, "project_id", "x-project-id", ScopeIds.DefaultProject)
        };
    }

    /// <summary>
    /// Prefers a well-formed JWT claim over the matching header so scope stays bound to the token when both are present.
    /// </summary>
    private static Guid ResolveScopeId(
        ClaimsPrincipal? user,
        IHeaderDictionary? headers,
        string claimType,
        string headerName,
        Guid defaultId)
    {
        string? claimValue = user?.FindFirst(claimType)?.Value;

        if (!string.IsNullOrWhiteSpace(claimValue) && Guid.TryParse(claimValue, out Guid fromClaim))
            return fromClaim;

        if (headers is null || !headers.TryGetValue(headerName, out StringValues headerRaw))
            return defaultId;

        string? headerText = headerRaw.ToString();

        if (string.IsNullOrWhiteSpace(headerText) || !Guid.TryParse(headerText, out Guid fromHeader))
            return defaultId;

        return fromHeader;
    }
}
