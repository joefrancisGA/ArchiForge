using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;
using System.Text.Json;

namespace ArchiForge.Decisioning.Repositories;

public class InMemoryFindingsSnapshotRepository : IFindingsSnapshotRepository
{
    private readonly List<string> _store = [];
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public Task SaveAsync(FindingsSnapshot snapshot, CancellationToken ct)
    {
        // Store as JSON to simulate durable persistence and ensure payload round-trips.
        var json = JsonSerializer.Serialize(snapshot, _jsonOptions);
        _store.Add(json);
        return Task.CompletedTask;
    }

    public Task<FindingsSnapshot?> GetByIdAsync(Guid findingsSnapshotId, CancellationToken ct)
    {
        foreach (var json in _store)
        {
            var snapshot = JsonSerializer.Deserialize<FindingsSnapshot>(json, _jsonOptions);
            if (snapshot is not null && snapshot.FindingsSnapshotId == findingsSnapshotId)
            {
                return Task.FromResult<FindingsSnapshot?>(snapshot);
            }
        }

        return Task.FromResult<FindingsSnapshot?>(null);
    }
}

