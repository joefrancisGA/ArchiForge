using System.Data;

using ArchiForge.KnowledgeGraph.Interfaces;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.KnowledgeGraph.Repositories;

public class InMemoryGraphSnapshotRepository : IGraphSnapshotRepository
{
    private const int MaxEntries = 500;
    private readonly Dictionary<Guid, GraphSnapshot> _store = [];
    private readonly Lock _lock = new();

    public Task SaveAsync(
        GraphSnapshot snapshot,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ct.ThrowIfCancellationRequested();
        _ = connection;
        _ = transaction;
        lock (_lock)
        {
            _store[snapshot.GraphSnapshotId] = snapshot;
            
            if (_store.Count <= MaxEntries) return Task.CompletedTask;
            
            Guid oldest = _store.Keys.First();
            _store.Remove(oldest);
        }

        return Task.CompletedTask;
    }

    public Task<GraphSnapshot?> GetByIdAsync(Guid graphSnapshotId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            _store.TryGetValue(graphSnapshotId, out GraphSnapshot? result);
            return Task.FromResult(result);
        }
    }
}
