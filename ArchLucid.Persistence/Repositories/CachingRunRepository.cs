using System.Data;

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
    private readonly IHotPathReadCache _hotPathReadCache =
        hotPathReadCache ?? throw new ArgumentNullException(nameof(hotPathReadCache));

    private readonly IRunRepository _inner = inner ?? throw new ArgumentNullException(nameof(inner));

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
    public Task<IReadOnlyList<RunRecord>> ListByProjectAsync(
        ScopeContext scope,
        string projectId,
        int take,
        CancellationToken ct)
    {
        return _inner.ListByProjectAsync(scope, projectId, take, ct);
    }

    /// <inheritdoc />
    public Task<(IReadOnlyList<RunRecord> Items, int TotalCount)> ListByProjectPagedAsync(
        ScopeContext scope,
        string projectId,
        int skip,
        int take,
        CancellationToken ct)
    {
        return _inner.ListByProjectPagedAsync(scope, projectId, skip, take, ct);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RunRecord>> ListRecentInScopeAsync(ScopeContext scope, int take, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return _inner.ListRecentInScopeAsync(scope, take, ct);
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
