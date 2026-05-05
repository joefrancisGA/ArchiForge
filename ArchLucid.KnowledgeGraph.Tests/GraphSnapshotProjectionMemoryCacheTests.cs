using ArchLucid.Core.Scoping;
using ArchLucid.KnowledgeGraph.Caching;
using ArchLucid.KnowledgeGraph.Configuration;
using ArchLucid.KnowledgeGraph.Interfaces;
using ArchLucid.KnowledgeGraph.Models;

using FluentAssertions;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.KnowledgeGraph.Tests;

/// <summary><see cref="GraphSnapshotProjectionMemoryCache" /> exercises read-through + invalidation semantics.</summary>
public sealed class GraphSnapshotProjectionMemoryCacheTests
{
    [Fact]
    public async Task GetOrLoadAsync_hits_store_once_for_cached_projection()
    {
        IMemoryCache backing = new MemoryCache(new MemoryCacheOptions());
        KnowledgeGraphProjectionCacheOptions options = new()
        {
            Enabled = true,
            AbsoluteExpirationSeconds = 300,
        };

        Mock<IOptionsMonitor<KnowledgeGraphProjectionCacheOptions>> opts = new();
        opts.Setup(m => m.CurrentValue).Returns(options);

        IGraphSnapshotProjectionCache sut =
            new GraphSnapshotProjectionMemoryCache(backing, opts.Object);

        ScopeContext scope = CreateScope();
        Guid runId = Guid.NewGuid();
        Guid graphSnapshotId = Guid.NewGuid();
        GraphSnapshot materialized = new()
        {
            GraphSnapshotId = graphSnapshotId,
            RunId = runId,
            ContextSnapshotId = Guid.NewGuid(),
            CreatedUtc = DateTime.UtcNow
        };

        int loadCount = 0;

        Task<GraphSnapshot?> Loader(CancellationToken _)
        {
            loadCount++;
            return Task.FromResult<GraphSnapshot?>(materialized);
        }

        GraphSnapshot? first =
            await sut.GetOrLoadAsync(scope, runId, graphSnapshotId, Loader, CancellationToken.None);
        GraphSnapshot? second =
            await sut.GetOrLoadAsync(scope, runId, graphSnapshotId, Loader, CancellationToken.None);

        loadCount.Should().Be(1);
        first.Should().NotBeNull();
        second.Should().BeSameAs(first);
    }

    [Fact]
    public async Task GetOrLoadAsync_never_caches_null_projection()
    {
        IMemoryCache backing = new MemoryCache(new MemoryCacheOptions());

        Mock<IOptionsMonitor<KnowledgeGraphProjectionCacheOptions>> opts = new();
        opts.Setup(m => m.CurrentValue)
            .Returns(new KnowledgeGraphProjectionCacheOptions { Enabled = true });

        GraphSnapshotProjectionMemoryCache sut =
            new(backing, opts.Object);

        ScopeContext scope = CreateScope();
        Guid runId = Guid.NewGuid();
        Guid graphSnapshotId = Guid.NewGuid();
        int loadCount = 0;

        Task<GraphSnapshot?> Loader(CancellationToken _)
        {
            loadCount++;

            return Task.FromResult<GraphSnapshot?>(null);
        }


        GraphSnapshot? first = await sut.GetOrLoadAsync(scope, runId, graphSnapshotId, Loader, CancellationToken.None);
        GraphSnapshot? second = await sut.GetOrLoadAsync(scope, runId, graphSnapshotId, Loader, CancellationToken.None);

        first.Should().BeNull();
        second.Should().BeNull();
        loadCount.Should().Be(2);
    }

    [Fact]
    public void Invalidate_evicts_projection_entry()
    {
        IMemoryCache backing = new MemoryCache(new MemoryCacheOptions());

        Mock<IOptionsMonitor<KnowledgeGraphProjectionCacheOptions>> opts = new();
        opts.Setup(m => m.CurrentValue).Returns(new KnowledgeGraphProjectionCacheOptions { Enabled = true });

        ScopeContext scope = CreateScope();
        Guid runId = Guid.NewGuid();
        Guid graphSnapshotId = Guid.NewGuid();
        string key = GraphSnapshotProjectionCacheKeys.Projection(scope, runId, graphSnapshotId);

        using (ICacheEntry entry = backing.CreateEntry(key))
        {
            entry.Value = new object();
        }

        backing.TryGetValue(key, out _).Should().BeTrue();

        IGraphSnapshotProjectionCache sut =
            new GraphSnapshotProjectionMemoryCache(backing, opts.Object);

        sut.Invalidate(scope, runId, graphSnapshotId);

        backing.TryGetValue(key, out _).Should().BeFalse();
    }

    private static ScopeContext CreateScope()
    {
        return new ScopeContext
        {
            TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
        };
    }
}
