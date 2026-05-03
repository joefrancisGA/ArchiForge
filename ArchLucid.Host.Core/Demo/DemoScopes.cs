using ArchLucid.Core.Scoping;

namespace ArchLucid.Host.Core.Demo;

/// <summary>Shared hard-pinned demo scope for anonymous demo read models.</summary>
public static class DemoScopes
{
    /// <summary>
    /// Hard-pins the demo scope to the well-known development defaults — the same scope the
    /// demo seed service writes into. Production hosts cannot reach this code path
    /// because routes are gated on <c>Demo:Enabled=true</c>.
    /// </summary>
    public static ScopeContext BuildDemoScope() => new()
    {
        TenantId = ScopeIds.DefaultTenant, WorkspaceId = ScopeIds.DefaultWorkspace, ProjectId = ScopeIds.DefaultProject,
    };
}
