using System.Collections.Concurrent;
using System.Data;
using System.Globalization;

using ArchLucid.Core.Pagination;

using ArchLucid.Core.Scoping;using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Tenancy;

namespace ArchLucid.Persistence.Repositories;

/// <summary>
///     In-memory implementation of <see cref="IRunRepository" /> for testing and local development.
///     Capped at <see cref="MaxEntries" /> entries; when full, the oldest run (by <c>CreatedUtc</c>) is
///     evicted on each new insert to prevent unbounded growth.
///     All reads are filtered to the caller's tenant, workspace, and project scope.
/// </summary>
public sealed class InMemoryRunRepository(ITenantRepository? tenantRepository = null) : IRunRepository
{
    private const int MaxEntries = 2_000;

    private readonly ConcurrentDictionary<Guid, RunRecord> _store = new();

    private readonly ITenantRepository _tenantRepository = tenantRepository ?? new InMemoryTenantRepository();

    private long _fakeRowVersion;

    public async Task SaveAsync(
        RunRecord run,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(run);
        ct.ThrowIfCancellationRequested();
        _ = connection;
        _ = transaction;

        await _tenantRepository.TryIncrementActiveTrialRunAsync(run.TenantId, ct, connection, transaction);

        if (_store.Count >= MaxEntries && !_store.ContainsKey(run.RunId))
        {
            RunRecord? oldest = _store.Values.OrderBy(r => r.CreatedUtc).FirstOrDefault();
            if (oldest is not null)
                _store.TryRemove(oldest.RunId, out _);
        }

        run.RowVersion = NextFakeRowVersion();
        _store[run.RunId] = run;
    }

    public Task<RunRecord?> GetByIdAsync(ScopeContext scope, Guid runId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (!_store.TryGetValue(runId, out RunRecord? r) || !MatchesScope(r, scope) || r.ArchivedUtc.HasValue)
            return Task.FromResult<RunRecord?>(null);


        return Task.FromResult<RunRecord?>(r);
    }

    public Task<IReadOnlyList<RunRecord>> ListByProjectAsync(ScopeContext scope, string projectId, int take,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        int n = Math.Clamp(take <= 0 ? 20 : take, 1, 200);
        List<RunRecord> list = _store.Values
            .Where(r =>
                MatchesScope(r, scope) &&
                !r.ArchivedUtc.HasValue &&
                string.Equals(r.ProjectId, projectId, StringComparison.Ordinal))
            .OrderByDescending(r => r.CreatedUtc)
            .Take(n)
            .ToList();
        return Task.FromResult<IReadOnlyList<RunRecord>>(list);
    }

    public Task<RunListPage> ListByProjectKeysetAsync(
        ScopeContext scope,
        string projectId,
        DateTime? cursorCreatedUtc,
        Guid? cursorRunId,
        int take,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ValidateRunKeysetCursor(cursorCreatedUtc, cursorRunId);

        int safeTake = RunPagination.ClampTake(take);
        int fetch = safeTake + 1;

        List<RunRecord> filtered = _store.Values
            .Where(r =>
                MatchesScope(r, scope) &&
                !r.ArchivedUtc.HasValue &&
                string.Equals(r.ProjectId, projectId, StringComparison.Ordinal))
            .Where(r =>
                !cursorRunId.HasValue ||
                (r.CreatedUtc < cursorCreatedUtc!.Value
                 || (r.CreatedUtc == cursorCreatedUtc.Value && r.RunId < cursorRunId!.Value)))
            .OrderByDescending(r => r.CreatedUtc)
            .ThenByDescending(r => r.RunId)
            .Take(fetch)
            .ToList();

        bool hasMore = filtered.Count > safeTake;

        if (hasMore)

            filtered.RemoveAt(filtered.Count - 1);


        return Task.FromResult(new RunListPage(filtered, hasMore));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RunRecord>> ListRecentInScopeAsync(ScopeContext scope, int take, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ct.ThrowIfCancellationRequested();
        int n = Math.Clamp(take <= 0 ? 200 : take, 1, 200);

        List<RunRecord> list = _store.Values
            .Where(r =>
                MatchesScope(r, scope) &&
                !r.ArchivedUtc.HasValue)
            .OrderByDescending(r => r.CreatedUtc)
            .Take(n)
            .ToList();

        return Task.FromResult<IReadOnlyList<RunRecord>>(list);
    }

    /// <inheritdoc />
    public Task<RunListPage> ListRecentInScopeKeysetAsync(
        ScopeContext scope,
        DateTime? cursorCreatedUtc,
        Guid? cursorRunId,
        int take,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ct.ThrowIfCancellationRequested();
        ValidateRunKeysetCursor(cursorCreatedUtc, cursorRunId);

        int safeTake = RunPagination.ClampTake(take);
        int fetch = safeTake + 1;

        List<RunRecord> filtered = _store.Values
            .Where(r =>
                MatchesScope(r, scope) &&
                !r.ArchivedUtc.HasValue)
            .Where(r =>
                !cursorRunId.HasValue ||
                (r.CreatedUtc < cursorCreatedUtc!.Value
                 || (r.CreatedUtc == cursorCreatedUtc.Value && r.RunId < cursorRunId!.Value)))
            .OrderByDescending(r => r.CreatedUtc)
            .ThenByDescending(r => r.RunId)
            .Take(fetch)
            .ToList();

        bool hasMore = filtered.Count > safeTake;

        if (hasMore)

            filtered.RemoveAt(filtered.Count - 1);


        return Task.FromResult(new RunListPage(filtered, hasMore));
    }

    public Task UpdateAsync(
        RunRecord run,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(run);
        ct.ThrowIfCancellationRequested();
        _ = connection;
        _ = transaction;

        if (!_store.ContainsKey(run.RunId))

            throw new InvalidOperationException(
                string.Format(CultureInfo.InvariantCulture, "Run '{0:D}' was not found for update.", run.RunId));


        if (run.RowVersion is not null &&
            _store.TryGetValue(run.RunId, out RunRecord? existing) &&
            existing.RowVersion is not null &&
            !existing.RowVersion.AsSpan().SequenceEqual(run.RowVersion))

            throw new RunConcurrencyConflictException(run.RunId);


        run.RowVersion = NextFakeRowVersion();
        _store[run.RunId] = run;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<RunArchiveBatchResult> ArchiveRunsCreatedBeforeAsync(DateTimeOffset cutoffUtc, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        DateTime cutoff = cutoffUtc.UtcDateTime;
        DateTime stamp = DateTime.UtcNow;
        List<ArchivedRunScopeRow> archived = [];

        foreach (KeyValuePair<Guid, RunRecord> kv in _store.ToArray())
        {
            RunRecord r = kv.Value;

            if (r.ArchivedUtc.HasValue || r.CreatedUtc >= cutoff)
                continue;


            archived.Add(new ArchivedRunScopeRow
            {
                RunId = r.RunId,
                TenantId = r.TenantId,
                WorkspaceId = r.WorkspaceId,
                ScopeProjectId = r.ScopeProjectId
            });

            r.ArchivedUtc = stamp;
            _store[kv.Key] = r;
        }

        return Task.FromResult(new RunArchiveBatchResult { UpdatedCount = archived.Count, ArchivedRuns = archived });
    }

    /// <inheritdoc />
    public Task<RunArchiveByIdsResult> ArchiveRunsByIdsAsync(IReadOnlyList<Guid> runIds, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (runIds.Count == 0)
            return Task.FromResult(new RunArchiveByIdsResult());

        List<Guid> distinctOrdered = [];
        HashSet<Guid> seen = [];

        distinctOrdered.AddRange(runIds.Where(id => seen.Add(id)));

        DateTime stamp = DateTime.UtcNow;
        List<ArchivedRunScopeRow> archived = [];
        List<RunArchiveByIdFailure> failed = [];

        foreach (Guid id in distinctOrdered)
        {
            if (!_store.TryGetValue(id, out RunRecord? run))
            {
                failed.Add(new RunArchiveByIdFailure(id, "Run not found."));
                continue;
            }

            if (run.ArchivedUtc.HasValue)
            {
                failed.Add(new RunArchiveByIdFailure(id, "Run already archived."));
                continue;
            }

            archived.Add(new ArchivedRunScopeRow
            {
                RunId = run.RunId,
                TenantId = run.TenantId,
                WorkspaceId = run.WorkspaceId,
                ScopeProjectId = run.ScopeProjectId
            });

            run.ArchivedUtc = stamp;
            _store[id] = run;
        }

        return Task.FromResult(new RunArchiveByIdsResult
        {
            SucceededRunIds = archived.Select(static r => r.RunId).ToList(),
            ArchivedRuns = archived,
            Failed = failed
        });
    }

    private static void ValidateRunKeysetCursor(DateTime? cursorCreatedUtc, Guid? cursorRunId)
    {
        if (cursorCreatedUtc.HasValue != cursorRunId.HasValue)
            throw new ArgumentException(
                "Run keyset cursor requires both CreatedUtc and RunId together, or both omitted for the first page.");
    }

    private static bool MatchesScope(RunRecord r, ScopeContext scope)
    {
        return r.TenantId == scope.TenantId &&
               r.WorkspaceId == scope.WorkspaceId &&
               r.ScopeProjectId == scope.ProjectId;
    }

    private byte[] NextFakeRowVersion()
    {
        long v = Interlocked.Increment(ref _fakeRowVersion);

        return BitConverter.GetBytes(v);
    }
}
