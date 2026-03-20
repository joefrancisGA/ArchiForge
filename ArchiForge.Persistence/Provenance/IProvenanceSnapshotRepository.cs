using System.Data;
using ArchiForge.Core.Scoping;
using ArchiForge.Provenance;

namespace ArchiForge.Persistence.Provenance;

public interface IProvenanceSnapshotRepository
{
    Task SaveAsync(
        DecisionProvenanceSnapshot snapshot,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);

    Task<DecisionProvenanceSnapshot?> GetByRunIdAsync(ScopeContext scope, Guid runId, CancellationToken ct);
}
