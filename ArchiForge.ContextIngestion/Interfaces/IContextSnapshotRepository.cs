using System.Data;

namespace ArchiForge.ContextIngestion.Interfaces;

using Models;

public interface IContextSnapshotRepository
{
    Task<ContextSnapshot?> GetLatestAsync(string projectId, CancellationToken ct);

    Task<ContextSnapshot?> GetByIdAsync(Guid snapshotId, CancellationToken ct);

    Task SaveAsync(
        ContextSnapshot snapshot,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);
}

