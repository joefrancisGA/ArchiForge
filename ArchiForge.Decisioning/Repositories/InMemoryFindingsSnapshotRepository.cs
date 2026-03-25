using System.Data;
using System.Text.Json;

using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Repositories;

public class InMemoryFindingsSnapshotRepository : IFindingsSnapshotRepository
{
    private readonly Dictionary<Guid, string> _store = [];
    private readonly Lock _lock = new();

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public Task SaveAsync(
        FindingsSnapshot snapshot,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        _ = ct;
        _ = connection;
        _ = transaction;
        // Store as JSON to simulate durable persistence and ensure payload round-trips.
        string json = JsonSerializer.Serialize(snapshot, JsonOptions);
        lock (_lock)
        {
            _store[snapshot.FindingsSnapshotId] = json;
        }

        return Task.CompletedTask;
    }

    public Task<FindingsSnapshot?> GetByIdAsync(Guid findingsSnapshotId, CancellationToken ct)
    {
        _ = ct;
        string? json;
        lock (_lock)
        {
            _store.TryGetValue(findingsSnapshotId, out json);
        }

        if (json is null)
            return Task.FromResult<FindingsSnapshot?>(null);

        FindingsSnapshot? snapshot = JsonSerializer.Deserialize<FindingsSnapshot>(json, JsonOptions);
        return Task.FromResult(snapshot);
    }
}
