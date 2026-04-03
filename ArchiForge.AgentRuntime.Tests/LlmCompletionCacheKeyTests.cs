using ArchiForge.Core.Scoping;

using FluentAssertions;

namespace ArchiForge.AgentRuntime.Tests;

[Trait("Category", "Unit")]
public sealed class LlmCompletionCacheKeyTests
{
    [Fact]
    public void Compute_when_partitionByScope_differs_by_tenant_produces_different_keys()
    {
        ScopeContext scopeA = new()
        {
            TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333")
        };

        ScopeContext scopeB = new()
        {
            TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            WorkspaceId = scopeA.WorkspaceId,
            ProjectId = scopeA.ProjectId
        };

        string keyA = LlmCompletionCacheKey.Compute(true, "dep", "sys", "user", scopeA);
        string keyB = LlmCompletionCacheKey.Compute(true, "dep", "sys", "user", scopeB);

        keyA.Should().NotBe(keyB);
    }

    [Fact]
    public void Compute_when_partitionByScope_false_ignores_scope_ids()
    {
        ScopeContext scopeA = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid()
        };

        ScopeContext scopeB = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid()
        };

        string keyA = LlmCompletionCacheKey.Compute(false, "dep", "sys", "user", scopeA);
        string keyB = LlmCompletionCacheKey.Compute(false, "dep", "sys", "user", scopeB);

        keyA.Should().Be(keyB);
    }
}
