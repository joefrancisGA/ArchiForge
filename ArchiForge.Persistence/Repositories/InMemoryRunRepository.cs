using System.Collections.Concurrent;
using System.Data;
using ArchiForge.Persistence.Interfaces;
using ArchiForge.Persistence.Models;

namespace ArchiForge.Persistence.Repositories;

public sealed class InMemoryRunRepository : IRunRepository
{
    private readonly ConcurrentDictionary<Guid, RunRecord> _store = new();

    public Task SaveAsync(
        RunRecord run,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        _ = ct;
        _ = connection;
        _ = transaction;
        _store[run.RunId] = run;
        return Task.CompletedTask;
    }

    public Task<RunRecord?> GetByIdAsync(Guid runId, CancellationToken ct)
    {
        _ = ct;
        return Task.FromResult(_store.TryGetValue(runId, out var r) ? r : null);
    }

    public Task<IReadOnlyList<RunRecord>> ListByProjectAsync(string projectId, int take, CancellationToken ct)
    {
        _ = ct;
        var n = take <= 0 ? 20 : take;
        var list = _store.Values
            .Where(r => string.Equals(r.ProjectId, projectId, StringComparison.Ordinal))
            .OrderByDescending(r => r.CreatedUtc)
            .Take(n)
            .ToList();
        return Task.FromResult<IReadOnlyList<RunRecord>>(list);
    }

    public Task UpdateAsync(
        RunRecord run,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        _ = ct;
        _ = connection;
        _ = transaction;
        _store[run.RunId] = run;
        return Task.CompletedTask;
    }
}
