using ArchLucid.Core.Pagination;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Caching;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Repositories;

using FluentAssertions;

using Moq;

namespace ArchLucid.Persistence.Tests;

/// <summary>
///     <see cref="CachingRunRepository" /> defends against a misbehaving <see cref="IHotPathReadCache" /> that returns
///     null for reference-typed pages (should not happen for a correct cache implementation).
/// </summary>
[Trait("Category", "Unit")]
public sealed class CachingRunRepositoryHotPathNullDefenseTests
{
    [Fact]
    public async Task ListByProjectKeysetAsync_throws_when_first_page_cache_returns_null_RunListPage()
    {
        Mock<IHotPathReadCache> cache = new();
        cache.Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<RunListPage?>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string?>(),
                It.IsAny<int?>()))
            .ReturnsAsync((RunListPage?)null);

        CachingRunRepository sut = new(Mock.Of<IRunRepository>(), cache.Object);
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid()
        };

        Func<Task> act = async () =>
            await sut.ListByProjectKeysetAsync(scope, "p", null, null, 20, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Run list cache returned null*");
    }

    [Fact]
    public async Task ListRecentInScopeKeysetAsync_throws_when_first_page_cache_returns_null_RunListPage()
    {
        Mock<IHotPathReadCache> cache = new();
        cache.Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<RunListPage?>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string?>(),
                It.IsAny<int?>()))
            .ReturnsAsync((RunListPage?)null);

        CachingRunRepository sut = new(Mock.Of<IRunRepository>(), cache.Object);
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid()
        };

        Func<Task> act = async () =>
            await sut.ListRecentInScopeKeysetAsync(scope, null, null, 20, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Run list cache returned null*");
    }

    [Fact]
    public async Task ListByProjectKeysetAsync_bypasses_cache_when_cursor_supplied()
    {
        Mock<IRunRepository> inner = new();
        Mock<IHotPathReadCache> cache = new();
        RunListPage expected = new([], false);

        inner.Setup(i => i.ListByProjectKeysetAsync(
                It.IsAny<ScopeContext>(),
                It.IsAny<string>(),
                It.IsAny<DateTime?>(),
                It.IsAny<Guid?>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        CachingRunRepository sut = new(inner.Object, cache.Object);
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid()
        };

        DateTime cursorUtc = DateTime.UtcNow;
        Guid cursorRun = Guid.NewGuid();

        RunListPage page = await sut.ListByProjectKeysetAsync(scope, "p", cursorUtc, cursorRun, 10, CancellationToken.None);

        page.Should().BeSameAs(expected);

        cache.Verify(
            c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<RunListPage?>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string?>(),
                It.IsAny<int?>()),
            Times.Never);
    }

    [Fact]
    public async Task ListRecentInScopeKeysetAsync_bypasses_cache_when_cursor_supplied()
    {
        Mock<IRunRepository> inner = new();
        Mock<IHotPathReadCache> cache = new();
        RunListPage expected = new([], false);

        inner.Setup(i => i.ListRecentInScopeKeysetAsync(
                It.IsAny<ScopeContext>(),
                It.IsAny<DateTime?>(),
                It.IsAny<Guid?>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        CachingRunRepository sut = new(inner.Object, cache.Object);
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid()
        };

        DateTime cursorUtc = DateTime.UtcNow;
        Guid cursorRun = Guid.NewGuid();

        RunListPage page =
            await sut.ListRecentInScopeKeysetAsync(scope, cursorUtc, cursorRun, 10, CancellationToken.None);

        page.Should().BeSameAs(expected);

        cache.Verify(
            c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<RunListPage?>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string?>(),
                It.IsAny<int?>()),
            Times.Never);
    }
}
