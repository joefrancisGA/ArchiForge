using System.Data;
using ArchiForge.KnowledgeGraph.Interfaces;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.KnowledgeGraph.Repositories;

public class InMemoryGraphSnapshotRepository : IGraphSnapshotRepository
{
    private readonly List<GraphSnapshot> _store = [];

    public Task SaveAsync(
        GraphSnapshot snapshot,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        _ = ct;
        _ = connection;
        _ = transaction;
        _store.Add(snapshot);
        return Task.CompletedTask;
    }

    public Task<GraphSnapshot?> GetByIdAsync(Guid graphSnapshotId, CancellationToken ct)
    {
        var result = _store.FirstOrDefault(x => x.GraphSnapshotId == graphSnapshotId);
        return Task.FromResult(result);
    }
}

