using System.Data;

using ArchiForge.ContextIngestion.Interfaces;
using ArchiForge.ContextIngestion.Models;

namespace ArchiForge.ContextIngestion.Repositories;

public class InMemoryContextSnapshotRepository : IContextSnapshotRepository
{
    private const int MaxSnapshots = 500;

    private readonly Dictionary<Guid, ContextSnapshot> _store = [];
    private readonly Lock _lock = new();

    public Task<ContextSnapshot?> GetLatestAsync(string projectId, CancellationToken ct)
    {
        _ = ct;
        lock (_lock)
        {
            ContextSnapshot? result = _store.Values
                .Where(s => string.Equals(s.ProjectId, projectId, StringComparison.Ordinal))
                .OrderByDescending(s => s.CreatedUtc)
                .FirstOrDefault();
            return Task.FromResult(result);
        }
    }

    public Task<ContextSnapshot?> GetByIdAsync(Guid snapshotId, CancellationToken ct)
    {
        _ = ct;
        lock (_lock)
        {
            _store.TryGetValue(snapshotId, out ContextSnapshot? snapshot);
            return Task.FromResult(snapshot);
        }
    }

    public Task SaveAsync(
        ContextSnapshot snapshot,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        _ = ct;
        _ = connection;
        _ = transaction;
        lock (_lock)
        {
            _store[snapshot.SnapshotId] = snapshot;

            // Evict oldest entries when the store exceeds the cap.
            if (_store.Count > MaxSnapshots)
            {
                List<Guid> toRemove = _store.Values
                    .OrderBy(s => s.CreatedUtc)
                    .Take(_store.Count - MaxSnapshots)
                    .Select(s => s.SnapshotId)
                    .ToList();
                foreach (Guid id in toRemove)
                    _store.Remove(id);
            }
        }

        return Task.CompletedTask;
    }
}
