using System.Security.Claims;

using ArchLucid.Core.Scoping;

using FluentAssertions;

using Microsoft.AspNetCore.Http;

namespace ArchLucid.Api.Tests;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class HttpScopeContextProviderTests
{
    private static HttpScopeContextProvider CreateProvider(HttpContext http)
    {
        IHttpContextAccessor accessor = new HttpContextAccessor { HttpContext = http };

        return new HttpScopeContextProvider(accessor);
    }

    [Fact]
    public void GetCurrentScope_prefers_claim_tenant_over_conflicting_header()
    {
        Guid claimTenant = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid headerTenant = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        DefaultHttpContext http = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("tenant_id", claimTenant.ToString("D"))],
                "Bearer"))
        };
        http.Request.Headers["x-tenant-id"] = headerTenant.ToString("D");

        ScopeContext scope = CreateProvider(http).GetCurrentScope();

        scope.TenantId.Should().Be(claimTenant);
        scope.WorkspaceId.Should().Be(ScopeIds.DefaultWorkspace);
        scope.ProjectId.Should().Be(ScopeIds.DefaultProject);
    }

    [Fact]
    public void GetCurrentScope_uses_header_when_claim_absent()
    {
        Guid headerTenant = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        DefaultHttpContext http = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };
        http.Request.Headers["x-tenant-id"] = headerTenant.ToString("D");

        ScopeContext scope = CreateProvider(http).GetCurrentScope();

        scope.TenantId.Should().Be(headerTenant);
    }

    [Fact]
    public void GetCurrentScope_uses_default_when_claim_invalid_and_header_absent()
    {
        DefaultHttpContext http = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("tenant_id", "not-a-guid")],
                "Bearer"))
        };

        ScopeContext scope = CreateProvider(http).GetCurrentScope();

        scope.TenantId.Should().Be(ScopeIds.DefaultTenant);
    }

    [Fact]
    public void GetCurrentScope_falls_back_to_header_when_claim_not_parseable_as_guid()
    {
        Guid headerTenant = Guid.Parse("dededede-dede-dede-dede-dededededede");

        DefaultHttpContext http = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("tenant_id", "not-a-guid")],
                "Bearer"))
        };
        http.Request.Headers["x-tenant-id"] = headerTenant.ToString("D");

        ScopeContext scope = CreateProvider(http).GetCurrentScope();

        scope.TenantId.Should().Be(headerTenant);
    }

    [Fact]
    public void GetCurrentScope_ambient_override_wins_over_claims_and_headers()
    {
        Guid ambientTenant = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        Guid claimTenant = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

        DefaultHttpContext http = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("tenant_id", claimTenant.ToString("D"))],
                "Bearer"))
        };
        http.Request.Headers["x-tenant-id"] = claimTenant.ToString("D");

        using (AmbientScopeContext.Push(new ScopeContext
        {
            TenantId = ambientTenant,
            WorkspaceId = ScopeIds.DefaultWorkspace,
            ProjectId = ScopeIds.DefaultProject
        }))
        {
            ScopeContext scope = CreateProvider(http).GetCurrentScope();
            scope.TenantId.Should().Be(ambientTenant);
        }
    }

    [Fact]
    public void GetCurrentScope_claim_precedence_applies_per_workspace_and_project()
    {
        Guid claimWs = Guid.Parse("f0f0f0f0-f0f0-f0f0-f0f0-f0f0f0f0f0f0");
        Guid headerWs = Guid.Parse("17171717-1717-1717-1717-171717171717");
        Guid claimProj = Guid.Parse("18181818-1818-1818-1818-181818181818");
        Guid headerProj = Guid.Parse("19191919-1919-1919-1919-191919191919");

        DefaultHttpContext http = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim("workspace_id", claimWs.ToString("D")),
                    new Claim("project_id", claimProj.ToString("D"))
                ],
                "Bearer"))
        };
        http.Request.Headers["x-workspace-id"] = headerWs.ToString("D");
        http.Request.Headers["x-project-id"] = headerProj.ToString("D");

        ScopeContext scope = CreateProvider(http).GetCurrentScope();

        scope.WorkspaceId.Should().Be(claimWs);
        scope.ProjectId.Should().Be(claimProj);
    }
}
