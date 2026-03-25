using System.Data;

using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Repositories;

public class InMemoryDecisionTraceRepository : IDecisionTraceRepository
{
    private const int MaxEntries = 500;

    private readonly List<DecisionTrace> _store = [];
    private readonly Lock _lock = new();

    public Task SaveAsync(
        DecisionTrace trace,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(trace);
        ct.ThrowIfCancellationRequested();
        _ = connection;
        _ = transaction;
        lock (_lock)
        {
            _store.Add(trace);
            if (_store.Count > MaxEntries)
                _store.RemoveRange(0, _store.Count - MaxEntries);
        }
        return Task.CompletedTask;
    }

    public Task<DecisionTrace?> GetByIdAsync(ScopeContext scope, Guid decisionTraceId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            DecisionTrace? result = _store.FirstOrDefault(x =>
                x.DecisionTraceId == decisionTraceId &&
                x.TenantId == scope.TenantId &&
                x.WorkspaceId == scope.WorkspaceId &&
                x.ProjectId == scope.ProjectId);
            return Task.FromResult(result);
        }
    }
}

