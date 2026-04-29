using System.Data;

using ArchLucid.Core.Pagination;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Caching;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

namespace ArchLucid.Persistence.Repositories;

/// <summary>
///     Decorates <see cref="IRunRepository" /> with hot-path read caching and evicts on single-row writes and after bulk
///     archival.
/// </summary>
public sealed class CachingRunRepository(IRunRepository inner, IHotPathReadCache hotPathReadCache) : IRunRepository
{
    /// <summary>Short TTL match for dashboard lists (does not invalidate on unrelated writes).</summary>
    private const int ListAbsoluteExpirationSeconds = 15;

    private readonly IHotPathReadCache _hotPathReadCache =
        hotPathReadCache ?? throw new ArgumentNullException(nameof(hotPathReadCache));

    private readonly IRunRepository _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    /// <inheritdoc />
    public Task<RunRecord?> GetByIdAsync(ScopeContext scope, Guid runId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return _hotPathReadCache.GetOrCreateAsync(
            HotPathCacheKeys.Run(scope, runId),
            innerCt => _inner.GetByIdAsync(scope, runId, innerCt),
            ct,
            HotPathCacheKeys.LegacyRun(scope, runId));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RunRecord>> ListByProjectAsync(
        ScopeContext scope,
        string projectId,
        int take,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(scope);

        int safeTake = Math.Clamp(take <= 0 ? 20 : take, 1, 200);
        string key = HotPathCacheKeys.RunListByProjectFirstPage(scope, projectId, safeTake);

        IReadOnlyList<RunRecord>? cached = await _hotPathReadCache.GetOrCreateAsync(
            key,
            async innerCt => await _inner.ListByProjectAsync(scope, projectId, safeTake, innerCt),
            ct,
            absoluteExpirationSecondsOverride: ListAbsoluteExpirationSeconds);

        return cached ?? [];
    }

    /// <inheritdoc />
    public async Task<RunListPage> ListByProjectKeysetAsync(
        ScopeContext scope,
        string projectId,
        DateTime? cursorCreatedUtc,
        Guid? cursorRunId,
        int take,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(scope);

        if (cursorCreatedUtc.HasValue || cursorRunId.HasValue)
            return await _inner.ListByProjectKeysetAsync(scope, projectId, cursorCreatedUtc, cursorRunId, take, ct);

        int clampedTake = RunPagination.ClampTake(take);
        string key = HotPathCacheKeys.RunListByProjectFirstPage(scope, projectId, clampedTake);

        RunListPage? cached = await _hotPathReadCache.GetOrCreateAsync(
            key,
            async innerCt =>
                await _inner.ListByProjectKeysetAsync(scope, projectId, null, null, clampedTake, innerCt),
            ct,
            absoluteExpirationSecondsOverride: ListAbsoluteExpirationSeconds);

        if (cached is null) throw new InvalidOperationException("Run list cache returned null unexpectedly.");

        return cached;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RunRecord>> ListRecentInScopeAsync(ScopeContext scope, int take, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(scope);

        int safeTake = Math.Clamp(take <= 0 ? 200 : take, 1, 200);
        string key = HotPathCacheKeys.RunListRecentInScopeFirstPage(scope, safeTake);

        IReadOnlyList<RunRecord>? cached = await _hotPathReadCache.GetOrCreateAsync(
            key,
            async innerCt => await _inner.ListRecentInScopeAsync(scope, safeTake, innerCt),
            ct,
            absoluteExpirationSecondsOverride: ListAbsoluteExpirationSeconds);

        return cached ?? [];
    }

    /// <inheritdoc />
    public async Task<RunListPage> ListRecentInScopeKeysetAsync(
        ScopeContext scope,
        DateTime? cursorCreatedUtc,
        Guid? cursorRunId,
        int take,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(scope);

        if (cursorCreatedUtc.HasValue || cursorRunId.HasValue)
            return await _inner.ListRecentInScopeKeysetAsync(scope, cursorCreatedUtc, cursorRunId, take, ct);

        int clampedTake = RunPagination.ClampTake(take);
        string key = HotPathCacheKeys.RunListRecentInScopeFirstPage(scope, clampedTake);

        RunListPage? cached = await _hotPathReadCache.GetOrCreateAsync(
            key,
            async innerCt =>
                await _inner.ListRecentInScopeKeysetAsync(scope, null, null, clampedTake, innerCt),
            ct,
            absoluteExpirationSecondsOverride: ListAbsoluteExpirationSeconds);

        if (cached is null) throw new InvalidOperationException("Run list cache returned null unexpectedly.");

        return cached;
    }

    /// <inheritdoc />
    public Task<int> CountActiveRunsForArchitectureRequestAsync(
        ScopeContext scope,
        string architectureRequestId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return _inner.CountActiveRunsForArchitectureRequestAsync(scope, architectureRequestId, ct);
    }

    /// <inheritdoc />
    public async Task SaveAsync(
        RunRecord run,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        await _inner.SaveAsync(run, ct, connection, transaction);

        await HotPathCacheEviction.RemoveRunAsync(_hotPathReadCache, ScopeForRun(run), run.RunId, ct);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(
        RunRecord run,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        await _inner.UpdateAsync(run, ct, connection, transaction);

        await HotPathCacheEviction.RemoveRunAsync(_hotPathReadCache, ScopeForRun(run), run.RunId, ct);
    }

    /// <inheritdoc />
    public async Task<RunArchiveBatchResult> ArchiveRunsCreatedBeforeAsync(DateTimeOffset cutoffUtc,
        CancellationToken ct)
    {
        RunArchiveBatchResult batch = await _inner.ArchiveRunsCreatedBeforeAsync(cutoffUtc, ct);

        foreach (ArchivedRunScopeRow row in batch.ArchivedRuns)
        {
            ScopeContext scope = new()
            {
                TenantId = row.TenantId, WorkspaceId = row.WorkspaceId, ProjectId = row.ScopeProjectId
            };

            await HotPathCacheEviction.RemoveRunAsync(_hotPathReadCache, scope, row.RunId, ct);
        }

        return batch;
    }

    /// <inheritdoc />
    public async Task<RunArchiveByIdsResult> ArchiveRunsByIdsAsync(IReadOnlyList<Guid> runIds, CancellationToken ct)
    {
        RunArchiveByIdsResult result = await _inner.ArchiveRunsByIdsAsync(runIds, ct);

        foreach (ArchivedRunScopeRow row in result.ArchivedRuns)
        {
            ScopeContext scope = new()
            {
                TenantId = row.TenantId, WorkspaceId = row.WorkspaceId, ProjectId = row.ScopeProjectId
            };

            await HotPathCacheEviction.RemoveRunAsync(_hotPathReadCache, scope, row.RunId, ct);
        }

        return result;
    }

    private static ScopeContext ScopeForRun(RunRecord run)
    {
        return new ScopeContext
        {
            TenantId = run.TenantId, WorkspaceId = run.WorkspaceId, ProjectId = run.ScopeProjectId
        };
    }
}
