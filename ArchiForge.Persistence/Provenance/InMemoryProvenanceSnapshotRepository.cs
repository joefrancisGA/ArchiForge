using System.Data;
using ArchiForge.Core.Scoping;
using ArchiForge.Provenance;

namespace ArchiForge.Persistence.Provenance;

public sealed class InMemoryProvenanceSnapshotRepository : IProvenanceSnapshotRepository
{
    private readonly object _gate = new();
    private readonly List<DecisionProvenanceSnapshot> _store = [];

    public Task SaveAsync(
        DecisionProvenanceSnapshot snapshot,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        _ = ct;
        _ = connection;
        _ = transaction;
        lock (_gate)
        {
            _store.Add(snapshot);
        }

        return Task.CompletedTask;
    }

    public Task<DecisionProvenanceSnapshot?> GetByRunIdAsync(ScopeContext scope, Guid runId, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var hit = _store
                .Where(x =>
                    x.TenantId == scope.TenantId &&
                    x.WorkspaceId == scope.WorkspaceId &&
                    x.ProjectId == scope.ProjectId &&
                    x.RunId == runId)
                .OrderByDescending(x => x.CreatedUtc)
                .FirstOrDefault();
            return Task.FromResult(hit);
        }
    }
}
