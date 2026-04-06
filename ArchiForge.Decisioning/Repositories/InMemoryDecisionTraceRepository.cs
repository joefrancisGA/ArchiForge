using System.Data;
using System.Text.Json;

using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.DecisionTraces;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Interfaces;

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
        trace.RequireRuleAudit();
        ct.ThrowIfCancellationRequested();
        _ = connection;
        _ = transaction;

        lock (_lock)
        {
            _store.Add(Clone(trace));
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
                x.RuleAudit!.DecisionTraceId == decisionTraceId &&
                x.RuleAudit.TenantId == scope.TenantId &&
                x.RuleAudit.WorkspaceId == scope.WorkspaceId &&
                x.RuleAudit.ProjectId == scope.ProjectId);

            return Task.FromResult(result is null ? null : Clone(result));
        }
    }

    private static DecisionTrace Clone(DecisionTrace source)
    {
        string json = JsonSerializer.Serialize(source.RuleAudit, ContractJson.Default);
        RuleAuditTracePayload? copy = JsonSerializer.Deserialize<RuleAuditTracePayload>(json, ContractJson.Default);

        return copy is null
            ? throw new InvalidOperationException("Clone produced null RuleAuditTracePayload.")
            : DecisionTrace.FromRuleAudit(copy);
    }
}
