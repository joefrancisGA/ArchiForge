using ArchiForge.ContextIngestion.Interfaces;
using ArchiForge.ContextIngestion.Models;

namespace ArchiForge.ContextIngestion.Repositories;

public class InMemoryContextSnapshotRepository : IContextSnapshotRepository
{
    private readonly List<ContextSnapshot> _store = [];

    public Task<ContextSnapshot?> GetLatestAsync(string projectId, CancellationToken ct)
    {
        return Task.FromResult(_store.LastOrDefault());
    }

    public Task SaveAsync(ContextSnapshot snapshot, CancellationToken ct)
    {
        _store.Add(snapshot);
        return Task.CompletedTask;
    }
}

