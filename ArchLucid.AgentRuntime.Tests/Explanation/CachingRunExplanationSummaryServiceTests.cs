using ArchLucid.AgentRuntime.Explanation;
using ArchLucid.Core.Explanation;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Models;
using ArchLucid.Persistence.Caching;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Queries;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.AgentRuntime.Tests.Explanation;

/// <summary>Unit tests for <see cref="CachingRunExplanationSummaryService" /> hot-path caching.</summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class CachingRunExplanationSummaryServiceTests
{
    [Fact]
    public async Task GetSummaryAsync_cache_hit_second_call_does_not_invoke_inner()
    {
        Guid runId = Guid.NewGuid();
        ScopeContext scope = CreateScope();
        byte[] rowVersion = [0x01, 0x02];

        RunDetailDto detail = CreateDetail(runId, rowVersion);

        Mock<IAuthorityQueryService> authority = new();
        authority
            .Setup(a => a.GetRunDetailAsync(scope, runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);

        RunExplanationSummary summary = CreateSummary();

        Mock<IRunExplanationSummaryService> inner = new();
        inner
            .Setup(i => i.GetSummaryAsync(scope, runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        DictionaryHotPathReadCache cache = new();
        CachingRunExplanationSummaryService sut = new(
            inner.Object,
            cache,
            authority.Object,
            NullLogger<CachingRunExplanationSummaryService>.Instance);

        RunExplanationSummary? first = await sut.GetSummaryAsync(scope, runId, CancellationToken.None);
        RunExplanationSummary? second = await sut.GetSummaryAsync(scope, runId, CancellationToken.None);

        first.Should().BeSameAs(summary);
        second.Should().BeSameAs(summary);

        inner.Verify(
            i => i.GetSummaryAsync(scope, runId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSummaryAsync_first_call_invokes_inner_once()
    {
        Guid runId = Guid.NewGuid();
        ScopeContext scope = CreateScope();
        RunDetailDto detail = CreateDetail(runId, [0xAA]);

        Mock<IAuthorityQueryService> authority = new();
        authority
            .Setup(a => a.GetRunDetailAsync(scope, runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);

        RunExplanationSummary summary = CreateSummary();

        Mock<IRunExplanationSummaryService> inner = new();
        inner
            .Setup(i => i.GetSummaryAsync(scope, runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        CachingRunExplanationSummaryService sut = new(
            inner.Object,
            new DictionaryHotPathReadCache(),
            authority.Object,
            NullLogger<CachingRunExplanationSummaryService>.Instance);

        RunExplanationSummary? result = await sut.GetSummaryAsync(scope, runId, CancellationToken.None);

        result.Should().BeSameAs(summary);

        inner.Verify(
            i => i.GetSummaryAsync(scope, runId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSummaryAsync_when_run_detail_null_returns_null_without_inner()
    {
        Guid runId = Guid.NewGuid();
        ScopeContext scope = CreateScope();

        Mock<IAuthorityQueryService> authority = new();
        authority
            .Setup(a => a.GetRunDetailAsync(scope, runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RunDetailDto?)null);

        Mock<IRunExplanationSummaryService> inner = new();

        CachingRunExplanationSummaryService sut = new(
            inner.Object,
            new DictionaryHotPathReadCache(),
            authority.Object,
            NullLogger<CachingRunExplanationSummaryService>.Instance);

        RunExplanationSummary? result = await sut.GetSummaryAsync(scope, runId, CancellationToken.None);

        result.Should().BeNull();

        inner.Verify(
            i => i.GetSummaryAsync(It.IsAny<ScopeContext>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetSummaryAsync_different_RowVersion_invokes_inner_twice()
    {
        Guid runId = Guid.NewGuid();
        ScopeContext scope = CreateScope();

        byte[] version1 = [0x10];
        byte[] version2 = [0x20];

        int call = 0;

        Mock<IAuthorityQueryService> authority = new();
        authority
            .Setup(a => a.GetRunDetailAsync(scope, runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                call++;

                byte[] rv = call == 1 ? version1 : version2;

                return CreateDetail(runId, rv);
            });

        Mock<IRunExplanationSummaryService> inner = new();
        inner
            .Setup(i => i.GetSummaryAsync(scope, runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSummary);

        CachingRunExplanationSummaryService sut = new(
            inner.Object,
            new DictionaryHotPathReadCache(),
            authority.Object,
            NullLogger<CachingRunExplanationSummaryService>.Instance);

        await sut.GetSummaryAsync(scope, runId, CancellationToken.None);
        await sut.GetSummaryAsync(scope, runId, CancellationToken.None);

        inner.Verify(
            i => i.GetSummaryAsync(scope, runId, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    private static ScopeContext CreateScope()
    {
        return new ScopeContext { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() };
    }

    private static RunDetailDto CreateDetail(Guid runId, byte[] rowVersion)
    {
        return new RunDetailDto
        {
            Run = new RunRecord { RunId = runId, RowVersion = rowVersion },
            GoldenManifest = new GoldenManifest
            {
                ManifestHash = "h", RuleSetId = "r", RuleSetVersion = "v", RuleSetHash = "rh"
            }
        };
    }

    private static RunExplanationSummary CreateSummary()
    {
        return new RunExplanationSummary
        {
            Explanation = new ExplanationResult(),
            ThemeSummaries = [],
            OverallAssessment = "assessment",
            RiskPosture = "Low",
            Citations = []
        };
    }

    /// <summary>Mimics <see cref="IHotPathReadCache" /> no-null-cache semantics for tests.</summary>
    private sealed class DictionaryHotPathReadCache : IHotPathReadCache
    {
        private readonly Dictionary<string, object> _store = new(StringComparer.Ordinal);

        public Task<T?> GetOrCreateAsync<T>(
            string key,
            Func<CancellationToken, Task<T?>> factory,
            CancellationToken ct,
            string? legacyCacheKey = null,
            int? absoluteExpirationSecondsOverride = null)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(factory);

            if (_store.TryGetValue(key, out object? boxed) && boxed is T typed)
                return Task.FromResult<T?>(typed);

            if (legacyCacheKey is null
                || !_store.TryGetValue(legacyCacheKey, out object? leg) ||
                leg is not T legTyped)
                return MaterializeAsync(key, factory, ct);
            _store[key] = legTyped;
            _store.Remove(legacyCacheKey);

            return Task.FromResult<T?>(legTyped);
        }

        public Task RemoveAsync(string key, CancellationToken ct)
        {
            _store.Remove(key);

            return Task.CompletedTask;
        }

        private async Task<T?> MaterializeAsync<T>(
            string key,
            Func<CancellationToken, Task<T?>> factory,
            CancellationToken ct)
            where T : class
        {
            T? created = await factory(ct);

            if (created is not null)
            {
                _store[key] = created;
            }

            return created;
        }
    }
}
