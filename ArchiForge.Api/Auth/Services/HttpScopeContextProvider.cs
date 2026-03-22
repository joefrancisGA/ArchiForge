using ArchiForge.Core.Scoping;

namespace ArchiForge.Api.Auth.Services;

public sealed class HttpScopeContextProvider(IHttpContextAccessor httpContextAccessor) : IScopeContextProvider
{
    public ScopeContext GetCurrentScope()
    {
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
            if (headers is null || !headers.TryGetValue(key, out var value)) return fallback;
            return Guid.TryParse(value.ToString(), out var parsed) ? parsed : fallback;
        }
    }
}
