using System.Collections.Concurrent;
using System.Data;

using ArchiForge.Core.Scoping;
using ArchiForge.Persistence.Interfaces;
using ArchiForge.Persistence.Models;

namespace ArchiForge.Persistence.Repositories;

public sealed class InMemoryRunRepository : IRunRepository
{
    private const int MaxEntries = 2_000;

    private readonly ConcurrentDictionary<Guid, RunRecord> _store = new();

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

        _store[run.RunId] = run;
        return Task.CompletedTask;
    }

    public Task<RunRecord?> GetByIdAsync(ScopeContext scope, Guid runId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!_store.TryGetValue(runId, out RunRecord? r))
            return Task.FromResult<RunRecord?>(null);
        return Task.FromResult(MatchesScope(r, scope) ? r : null);
    }

    public Task<IReadOnlyList<RunRecord>> ListByProjectAsync(ScopeContext scope, string projectId, int take, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        int n = Math.Clamp(take <= 0 ? 20 : take, 1, 200);
        List<RunRecord> list = _store.Values
            .Where(r =>
                MatchesScope(r, scope) &&
                string.Equals(r.ProjectId, projectId, StringComparison.Ordinal))
            .OrderByDescending(r => r.CreatedUtc)
            .Take(n)
            .ToList();
        return Task.FromResult<IReadOnlyList<RunRecord>>(list);
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
        _store[run.RunId] = run;
        return Task.CompletedTask;
    }
}
