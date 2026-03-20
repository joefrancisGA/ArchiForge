using System.Data;
using ArchiForge.ContextIngestion.Interfaces;
using ArchiForge.ContextIngestion.Models;

namespace ArchiForge.ContextIngestion.Repositories;

public class InMemoryContextSnapshotRepository : IContextSnapshotRepository
{
    private readonly List<ContextSnapshot> _store = [];

    public Task<ContextSnapshot?> GetLatestAsync(string projectId, CancellationToken ct)
    {
        _ = ct;
        return Task.FromResult(_store
            .Where(s => string.Equals(s.ProjectId, projectId, StringComparison.Ordinal))
            .OrderByDescending(s => s.CreatedUtc)
            .FirstOrDefault());
    }

    public Task<ContextSnapshot?> GetByIdAsync(Guid snapshotId, CancellationToken ct)
    {
        _ = ct;
        return Task.FromResult(_store.FirstOrDefault(s => s.SnapshotId == snapshotId));
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
        _store.Add(snapshot);
        return Task.CompletedTask;
    }
}

