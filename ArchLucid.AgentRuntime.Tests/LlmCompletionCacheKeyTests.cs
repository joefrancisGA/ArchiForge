using ArchLucid.Core.Scoping;

using FluentAssertions;

namespace ArchLucid.AgentRuntime.Tests;

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
            TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid()
        };

        ScopeContext scopeB = new()
        {
            TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid()
        };

        string keyA = LlmCompletionCacheKey.Compute(false, "dep", "sys", "user", scopeA);
        string keyB = LlmCompletionCacheKey.Compute(false, "dep", "sys", "user", scopeB);

        keyA.Should().Be(keyB);
    }

    [Fact]
    public void Compute_throws_when_deployment_name_null_or_whitespace()
    {
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid()
        };

        Action empty = () => LlmCompletionCacheKey.Compute(false, "", "s", "u", scope);
        Action whitespace = () => LlmCompletionCacheKey.Compute(false, "   ", "s", "u", scope);

        empty.Should().Throw<ArgumentException>().WithParameterName("deploymentName");
        whitespace.Should().Throw<ArgumentException>().WithParameterName("deploymentName");
    }

    [Fact]
    public void Compute_throws_when_system_prompt_null()
    {
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid()
        };

        Action act = () => LlmCompletionCacheKey.Compute(false, "dep", null!, "u", scope);

        act.Should().Throw<ArgumentNullException>().WithParameterName("systemPrompt");
    }

    [Fact]
    public void Compute_throws_when_user_prompt_null()
    {
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid()
        };

        Action act = () => LlmCompletionCacheKey.Compute(false, "dep", "s", null!, scope);

        act.Should().Throw<ArgumentNullException>().WithParameterName("userPrompt");
    }

    [Fact]
    public void Compute_throws_when_scope_null()
    {
        Action act = () => LlmCompletionCacheKey.Compute(false, "dep", "s", "u", null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("scope");
    }
}
