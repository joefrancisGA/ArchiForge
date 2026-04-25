using ArchLucid.Core.Scoping;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

namespace ArchLucid.AgentRuntime.Tests;

[Trait("Category", "Unit")]
public sealed class CachingAgentCompletionClientTests
{
    [Fact]
    public async Task CompleteJsonAsync_when_disabled_invokes_inner_each_time()
    {
        CountingCompletionClient inner = new();
        FixedScopeProvider scope = new(
            new ScopeContext { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() });

        using MemoryLlmCompletionResponseStore store = new(8);
        CachingAgentCompletionClient sut = new(
            inner,
            store,
            "dep",
            false,
            true,
            TimeSpan.FromMinutes(5),
            scope,
            NullLogger<CachingAgentCompletionClient>.Instance);

        _ = await sut.CompleteJsonAsync("s", "u");
        _ = await sut.CompleteJsonAsync("s", "u");

        inner.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task CompleteJsonAsync_when_enabled_reuses_inner_for_identical_prompts()
    {
        CountingCompletionClient inner = new();
        FixedScopeProvider scope = new(
            new ScopeContext { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() });

        using MemoryLlmCompletionResponseStore store = new(8);
        CachingAgentCompletionClient sut = new(
            inner,
            store,
            "dep",
            true,
            true,
            TimeSpan.FromMinutes(5),
            scope,
            NullLogger<CachingAgentCompletionClient>.Instance);

        string first = await sut.CompleteJsonAsync("s", "u");
        string second = await sut.CompleteJsonAsync("s", "u");

        inner.CallCount.Should().Be(1);
        second.Should().Be(first);
    }

    [Fact]
    public async Task CompleteJsonAsync_when_partitionByScope_and_scope_changes_invokes_inner_again()
    {
        CountingCompletionClient inner = new();
        MutableScopeProvider scope = new(
            new ScopeContext
            {
                TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                WorkspaceId = Guid.NewGuid(),
                ProjectId = Guid.NewGuid()
            });

        using MemoryLlmCompletionResponseStore store = new(8);
        CachingAgentCompletionClient sut = new(
            inner,
            store,
            "dep",
            true,
            true,
            TimeSpan.FromMinutes(5),
            scope,
            NullLogger<CachingAgentCompletionClient>.Instance);

        _ = await sut.CompleteJsonAsync("s", "u");

        scope.Current = new ScopeContext
        {
            TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            WorkspaceId = scope.Current.WorkspaceId,
            ProjectId = scope.Current.ProjectId
        };

        _ = await sut.CompleteJsonAsync("s", "u");

        inner.CallCount.Should().Be(2);
    }

    private sealed class CountingCompletionClient : IAgentCompletionClient
    {
        public int CallCount
        {
            get;
            private set;
        }

        public LlmProviderDescriptor Descriptor => LlmProviderDescriptor.ForOffline("test", "counting");

        public Task<string> CompleteJsonAsync(
            string systemPrompt,
            string userPrompt,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            return Task.FromResult("{\"n\":" + CallCount + "}");
        }
    }

    private sealed class FixedScopeProvider(ScopeContext scope) : IScopeContextProvider
    {
        private readonly ScopeContext _scope = scope ?? throw new ArgumentNullException(nameof(scope));

        public ScopeContext GetCurrentScope()
        {
            return _scope;
        }
    }

    private sealed class MutableScopeProvider(ScopeContext initial) : IScopeContextProvider
    {
        public ScopeContext Current
        {
            get;
            set;
        } = initial ?? throw new ArgumentNullException(nameof(initial));

        public ScopeContext GetCurrentScope()
        {
            return Current;
        }
    }
}
