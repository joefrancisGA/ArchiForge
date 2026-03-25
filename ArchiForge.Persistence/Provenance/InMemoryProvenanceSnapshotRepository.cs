using System.Data;

using ArchiForge.Core.Scoping;
using ArchiForge.Provenance;

namespace ArchiForge.Persistence.Provenance;

public sealed class InMemoryProvenanceSnapshotRepository : IProvenanceSnapshotRepository
{
    private readonly object _gate = new();

    // Key: (TenantId, WorkspaceId, ProjectId, RunId) → latest snapshot.
    // Last-write-wins semantics prevent unbounded memory growth for repeated saves.
    private readonly Dictionary<(Guid, Guid, Guid, Guid), DecisionProvenanceSnapshot> _store = [];

    public Task SaveAsync(
        DecisionProvenanceSnapshot snapshot,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ct.ThrowIfCancellationRequested();
        _ = connection;
        _ = transaction;

        (Guid TenantId, Guid WorkspaceId, Guid ProjectId, Guid RunId) key = (snapshot.TenantId, snapshot.WorkspaceId, snapshot.ProjectId, snapshot.RunId);
        lock (_gate)
        {
            _store[key] = snapshot;
        }

        return Task.CompletedTask;
    }

    public Task<DecisionProvenanceSnapshot?> GetByRunIdAsync(ScopeContext scope, Guid runId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        (Guid TenantId, Guid WorkspaceId, Guid ProjectId, Guid runId) key = (scope.TenantId, scope.WorkspaceId, scope.ProjectId, runId);
        lock (_gate)
        {
            _store.TryGetValue(key, out DecisionProvenanceSnapshot? hit);
            return Task.FromResult(hit);
        }
    }
}
