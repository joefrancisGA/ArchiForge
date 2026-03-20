using System.Data;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Interfaces;

public interface IFindingsSnapshotRepository
{
    Task SaveAsync(
        FindingsSnapshot snapshot,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);

    Task<FindingsSnapshot?> GetByIdAsync(Guid findingsSnapshotId, CancellationToken ct);
}

