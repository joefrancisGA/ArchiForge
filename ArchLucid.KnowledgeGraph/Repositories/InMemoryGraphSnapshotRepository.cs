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

    public Task<GraphSnapshot?> GetLatestByContextSnapshotIdAsync(Guid contextSnapshotId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            GraphSnapshot? latest = _store.Values
                .Where(s => s.ContextSnapshotId == contextSnapshotId)
                .OrderByDescending(s => s.CreatedUtc)
                .FirstOrDefault();

            return Task.FromResult(latest);
        }
    }

    public Task<IReadOnlyList<GraphSnapshotIndexedEdge>> ListIndexedEdgesAsync(Guid graphSnapshotId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            if (!_store.TryGetValue(graphSnapshotId, out GraphSnapshot? snapshot))
                return Task.FromResult<IReadOnlyList<GraphSnapshotIndexedEdge>>([]);

            IReadOnlyList<GraphSnapshotIndexedEdge> edges = snapshot.Edges
                .Select(e => new GraphSnapshotIndexedEdge(e.EdgeId, e.FromNodeId, e.ToNodeId, e.EdgeType, e.Weight))
                .OrderBy(e => e.EdgeId, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return Task.FromResult(edges);
        }
    }
}
