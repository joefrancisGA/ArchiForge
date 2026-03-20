using System.Data;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Repositories;

public class InMemoryDecisionTraceRepository : IDecisionTraceRepository
{
    private readonly List<DecisionTrace> _store = [];

    public Task SaveAsync(
        DecisionTrace trace,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        _ = ct;
        _ = connection;
        _ = transaction;
        _store.Add(trace);
        return Task.CompletedTask;
    }

    public Task<DecisionTrace?> GetByIdAsync(Guid decisionTraceId, CancellationToken ct)
    {
        var result = _store.FirstOrDefault(x => x.DecisionTraceId == decisionTraceId);
        return Task.FromResult(result);
    }
}

