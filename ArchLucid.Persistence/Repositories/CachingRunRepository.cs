using System.Data;

using ArchiForge.Core.Scoping;
using ArchiForge.Persistence.Caching;
using ArchiForge.Persistence.Interfaces;
using ArchiForge.Persistence.Models;

namespace ArchiForge.Persistence.Repositories;

/// <summary>Decorates <see cref="IRunRepository"/> with hot-path read caching and evicts on writes.</summary>
public sealed class CachingRunRepository(IRunRepository inner, IHotPathReadCache hotPathReadCache) : IRunRepository
{
    private readonly IRunRepository _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    private readonly IHotPathReadCache _hotPathReadCache =
        hotPathReadCache ?? throw new ArgumentNullException(nameof(hotPathReadCache));

    /// <inheritdoc />
    public async Task SaveAsync(
        RunRecord run,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        await _inner.SaveAsync(run, ct, connection, transaction);

        await _hotPathReadCache.RemoveAsync(HotPathCacheKeys.Run(ScopeForRun(run), run.RunId), ct);
    }

    /// <inheritdoc />
    public Task<RunRecord?> GetByIdAsync(ScopeContext scope, Guid runId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return _hotPathReadCache.GetOrCreateAsync(
            HotPathCacheKeys.Run(scope, runId),
            innerCt => _inner.GetByIdAsync(scope, runId, innerCt),
            ct);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RunRecord>> ListByProjectAsync(
        ScopeContext scope,
        string projectId,
        int take,
        CancellationToken ct) => _inner.ListByProjectAsync(scope, projectId, take, ct);

    /// <inheritdoc />
    public async Task UpdateAsync(
        RunRecord run,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        await _inner.UpdateAsync(run, ct, connection, transaction);

        await _hotPathReadCache.RemoveAsync(HotPathCacheKeys.Run(ScopeForRun(run), run.RunId), ct);
    }

    /// <inheritdoc />
    public Task<int> ArchiveRunsCreatedBeforeAsync(DateTimeOffset cutoffUtc, CancellationToken ct) =>
        _inner.ArchiveRunsCreatedBeforeAsync(cutoffUtc, ct);

    private static ScopeContext ScopeForRun(RunRecord run) => new()
    {
        TenantId = run.TenantId,
        WorkspaceId = run.WorkspaceId,
        ProjectId = run.ScopeProjectId
    };
}
