using System.Data;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.KnowledgeGraph.Interfaces;

public interface IGraphSnapshotRepository
{
    Task SaveAsync(
        GraphSnapshot snapshot,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);

    Task<GraphSnapshot?> GetByIdAsync(Guid graphSnapshotId, CancellationToken ct);
}

