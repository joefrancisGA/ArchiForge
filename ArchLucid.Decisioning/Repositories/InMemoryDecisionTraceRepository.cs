using System.Data;
using System.Text.Json;

using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Interfaces;

namespace ArchLucid.Decisioning.Repositories;

public class InMemoryDecisionTraceRepository : IDecisionTraceRepository
{
    private const int MaxEntries = 500;
    private readonly Lock _lock = new();

    private readonly List<DecisionTrace> _store = [];

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
                x is RuleAuditTrace rat &&
                rat.RuleAudit.DecisionTraceId == decisionTraceId &&
                rat.RuleAudit.TenantId == scope.TenantId &&
                rat.RuleAudit.WorkspaceId == scope.WorkspaceId &&
                rat.RuleAudit.ProjectId == scope.ProjectId);

            return Task.FromResult(result is null ? null : Clone(result));
        }
    }

    private static DecisionTrace Clone(DecisionTrace source)
    {
        string json = JsonSerializer.Serialize(source.RequireRuleAudit(), ContractJson.Default);
        RuleAuditTracePayload? copy = JsonSerializer.Deserialize<RuleAuditTracePayload>(json, ContractJson.Default);

        return copy is null
            ? throw new InvalidOperationException("Clone produced null RuleAuditTracePayload.")
            : RuleAuditTrace.From(copy);
    }
}
