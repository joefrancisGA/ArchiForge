using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Options;
using ArchLucid.Persistence.Repositories;

using FluentAssertions;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ArchLucid.Persistence.Tests;

/// <summary>
///     Ensures bulk archival evicts hot-path run cache entries so run lookups do not return archived rows until TTL
///     expiry.
/// </summary>
[Trait("Suite", "Core")]
public sealed class CachingRunRepositoryArchiveInvalidationTests
{
    [Fact]
    public async Task ArchiveRunsCreatedBeforeAsync_removes_cached_GetById_row()
    {
        HotPathCacheOptions options = new() { AbsoluteExpirationSeconds = 3600 };
        IOptionsMonitor<HotPathCacheOptions> monitor = new FixedOptionsMonitor<HotPathCacheOptions>(options);
        MemoryHotPathReadCache hotPath = new(new MemoryCache(new MemoryCacheOptions()), monitor);
        InMemoryRunRepository inner = new();
        CachingRunRepository repo = new(inner, hotPath);

        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid()
        };

        RunRecord run = new()
        {
            RunId = Guid.NewGuid(),
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ScopeProjectId = scope.ProjectId,
            ProjectId = "default",
            CreatedUtc = DateTime.UtcNow.AddDays(-10)
        };

        await inner.SaveAsync(run, CancellationToken.None);

        RunRecord? beforeArchive = await repo.GetByIdAsync(scope, run.RunId, CancellationToken.None);

        beforeArchive.Should().NotBeNull();

        RunArchiveBatchResult batch =
            await repo.ArchiveRunsCreatedBeforeAsync(DateTimeOffset.UtcNow.AddDays(-1), CancellationToken.None);

        batch.UpdatedCount.Should().Be(1);

        RunRecord? afterArchive = await repo.GetByIdAsync(scope, run.RunId, CancellationToken.None);

        afterArchive.Should().BeNull();
    }
}
