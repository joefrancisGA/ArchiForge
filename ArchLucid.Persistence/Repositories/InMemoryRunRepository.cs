using System.Collections.Concurrent;
using System.Data;
using System.Globalization;

using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

namespace ArchLucid.Persistence.Repositories;

/// <summary>
/// In-memory implementation of <see cref="IRunRepository"/> for testing and local development.
/// Capped at <see cref="MaxEntries"/> entries; when full, the oldest run (by <c>CreatedUtc</c>) is
/// evicted on each new insert to prevent unbounded growth.
/// All reads are filtered to the caller's tenant, workspace, and project scope.
/// </summary>
public sealed class InMemoryRunRepository : IRunRepository
{
    private const int MaxEntries = 2_000;

    private readonly ConcurrentDictionary<Guid, RunRecord> _store = new();

    private long _fakeRowVersion;

    public Task SaveAsync(
        RunRecord run,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(run);
        ct.ThrowIfCancellationRequested();
        _ = connection;
        _ = transaction;

        if (_store.Count >= MaxEntries && !_store.ContainsKey(run.RunId))
        {
            RunRecord? oldest = _store.Values.OrderBy(r => r.CreatedUtc).FirstOrDefault();
            if (oldest is not null)
                _store.TryRemove(oldest.RunId, out _);
        }

        run.RowVersion = NextFakeRowVersion();
        _store[run.RunId] = run;
        return Task.CompletedTask;
    }

    public Task<RunRecord?> GetByIdAsync(ScopeContext scope, Guid runId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (!_store.TryGetValue(runId, out RunRecord? r) || !MatchesScope(r, scope) || r.ArchivedUtc.HasValue)
        
            return Task.FromResult<RunRecord?>(null);
        

        return Task.FromResult<RunRecord?>(r);
    }

    public Task<IReadOnlyList<RunRecord>> ListByProjectAsync(ScopeContext scope, string projectId, int take, CancellationToken ct)
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

    public Task<(IReadOnlyList<RunRecord> Items, int TotalCount)> ListByProjectPagedAsync(
        ScopeContext scope,
        string projectId,
        int skip,
        int take,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        int safeTake = Math.Clamp(take <= 0 ? 20 : take, 1, 200);
        int safeSkip = Math.Max(skip, 0);

        List<RunRecord> ordered = _store.Values
            .Where(r =>
                MatchesScope(r, scope) &&
                !r.ArchivedUtc.HasValue &&
                string.Equals(r.ProjectId, projectId, StringComparison.Ordinal))
            .OrderByDescending(r => r.CreatedUtc)
            .ToList();

        int total = ordered.Count;
        IReadOnlyList<RunRecord> page = ordered.Skip(safeSkip).Take(safeTake).ToList();

        return Task.FromResult<(IReadOnlyList<RunRecord>, int)>((page, total));
    }

    private static bool MatchesScope(RunRecord r, ScopeContext scope) =>
        r.TenantId == scope.TenantId &&
        r.WorkspaceId == scope.WorkspaceId &&
        r.ScopeProjectId == scope.ProjectId;

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
        {
            throw new InvalidOperationException(
                string.Format(CultureInfo.InvariantCulture, "Run '{0:D}' was not found for update.", run.RunId));
        }

        if (run.RowVersion is not null &&
            _store.TryGetValue(run.RunId, out RunRecord? existing) &&
            existing.RowVersion is not null &&
            !existing.RowVersion.AsSpan().SequenceEqual(run.RowVersion))
        {
            throw new RunConcurrencyConflictException(run.RunId);
        }

        run.RowVersion = NextFakeRowVersion();
        _store[run.RunId] = run;
        return Task.CompletedTask;
    }

    private byte[] NextFakeRowVersion()
    {
        long v = Interlocked.Increment(ref _fakeRowVersion);

        return BitConverter.GetBytes(v);
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
            {
                continue;
            }

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

        return Task.FromResult(new RunArchiveBatchResult
        {
            UpdatedCount = archived.Count,
            ArchivedRuns = archived
        });
    }
}
