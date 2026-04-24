using ArchLucid.Core.Scoping;

using FluentAssertions;

namespace ArchLucid.Core.Tests.Scoping;

[Trait("Category", "Unit")]
public sealed class AmbientScopeContextTests
{
    [Fact]
    public void CurrentOverride_ByDefault_IsNull()
    {
        ScopeContext? current = AmbientScopeContext.CurrentOverride;

        current.Should().BeNull();
    }

    [Fact]
    public void Push_ThenDispose_RestoresNullOverride()
    {
        Guid tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid workspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid projectId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        ScopeContext pushed = new() { TenantId = tenantId, WorkspaceId = workspaceId, ProjectId = projectId };

        IDisposable handle = AmbientScopeContext.Push(pushed);

        ScopeContext? current = AmbientScopeContext.CurrentOverride;
        current.Should().NotBeNull();
        current.TenantId.Should().Be(tenantId);
        current.WorkspaceId.Should().Be(workspaceId);
        current.ProjectId.Should().Be(projectId);

        handle.Dispose();

        AmbientScopeContext.CurrentOverride.Should().BeNull();
    }

    [Fact]
    public void NestedPush_DisposeInnerThenOuter_RestoresCorrectly()
    {
        ScopeContext outerScope = new()
        {
            TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            WorkspaceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            ProjectId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")
        };
        ScopeContext innerScope = new()
        {
            TenantId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            WorkspaceId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            ProjectId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff")
        };

        IDisposable outer = AmbientScopeContext.Push(outerScope);
        IDisposable inner = AmbientScopeContext.Push(innerScope);

        AmbientScopeContext.CurrentOverride.Should().BeSameAs(innerScope);

        inner.Dispose();

        ScopeContext afterInner = AmbientScopeContext.CurrentOverride;
        afterInner.Should().NotBeNull();
        afterInner.TenantId.Should().Be(outerScope.TenantId);

        outer.Dispose();

        AmbientScopeContext.CurrentOverride.Should().BeNull();
    }

    [Fact]
    public void Dispose_TwiceOnSameHandle_IsSafe()
    {
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid()
        };

        IDisposable handle = AmbientScopeContext.Push(scope);

        AmbientScopeContext.CurrentOverride.Should().NotBeNull();

        handle.Dispose();
        handle.Dispose();

        AmbientScopeContext.CurrentOverride.Should().BeNull();
    }
}
