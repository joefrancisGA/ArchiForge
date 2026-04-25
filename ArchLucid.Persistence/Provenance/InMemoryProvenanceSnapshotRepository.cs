using System.Data;

using ArchLucid.Core.Scoping;
using ArchLucid.Provenance;

namespace ArchLucid.Persistence.Provenance;

/// <summary>
///     In-memory implementation of <see cref="IProvenanceSnapshotRepository" /> for testing and local development.
///     Uses last-write-wins semantics per <c>(TenantId, WorkspaceId, ProjectId, RunId)</c> key to prevent
///     unbounded memory growth when the same run is saved multiple times.
/// </summary>
public sealed class InMemoryProvenanceSnapshotRepository : IProvenanceSnapshotRepository
{
    private readonly Lock _gate = new();

    // Key: (TenantId, WorkspaceId, ProjectId, RunId) → latest snapshot.
    // Last-write-wins semantics prevent unbounded memory growth for repeated saves.
    private readonly Dictionary<(Guid, Guid, Guid, Guid), DecisionProvenanceSnapshot> _store = [];

    public Task SaveAsync(
        DecisionProvenanceSnapshot snapshot,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ct.ThrowIfCancellationRequested();
        _ = connection;
        _ = transaction;

        (Guid TenantId, Guid WorkspaceId, Guid ProjectId, Guid RunId) key = (snapshot.TenantId, snapshot.WorkspaceId,
            snapshot.ProjectId, snapshot.RunId);
        lock (_gate)

            _store[key] = snapshot;


        return Task.CompletedTask;
    }

    public Task<DecisionProvenanceSnapshot?> GetByRunIdAsync(ScopeContext scope, Guid runId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        (Guid TenantId, Guid WorkspaceId, Guid ProjectId, Guid runId) key = (scope.TenantId, scope.WorkspaceId,
            scope.ProjectId, runId);
        lock (_gate)
        {
            _store.TryGetValue(key, out DecisionProvenanceSnapshot? hit);
            return Task.FromResult(hit);
        }
    }
}
