using System.Data;

using ArchLucid.Decisioning.Findings.Serialization;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Models;

namespace ArchLucid.Decisioning.Repositories;

/// <summary>
///     In-memory implementation of <see cref="IFindingsSnapshotRepository" /> for testing and local development.
///     Stores snapshots as serialized JSON, capped at 500 entries (evicting the oldest by insertion order).
/// </summary>
/// <remarks>
///     Uses <see cref="FindingsSerialization" /> (same as SQL <c>FindingsJson</c> writes) so <see cref="Finding" />
///     payloads
///     and <see cref="FindingJsonConverter" /> round-trip; generic Web JSON drops typed payload fidelity and can empty
///     <c>findings</c>.
/// </remarks>
public class InMemoryFindingsSnapshotRepository : IFindingsSnapshotRepository
{
    private const int MaxEntries = 500;
    private readonly Lock _lock = new();

    private readonly Dictionary<Guid, string> _store = [];

    public Task SaveAsync(
        FindingsSnapshot snapshot,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        _ = ct;
        _ = connection;
        _ = transaction;
        FindingsSnapshotMigrator.Apply(snapshot);
        string json = FindingsSerialization.SerializeSnapshot(snapshot);
        lock (_lock)
        {
            if (_store.Count >= MaxEntries && !_store.ContainsKey(snapshot.FindingsSnapshotId))
            {
                Guid evict = _store.Keys.First();
                _store.Remove(evict);
            }

            _store[snapshot.FindingsSnapshotId] = json;
        }

        return Task.CompletedTask;
    }

    public Task<FindingsSnapshot?> GetByIdAsync(Guid findingsSnapshotId, CancellationToken ct)
    {
        _ = ct;
        string? json;
        lock (_lock)

            _store.TryGetValue(findingsSnapshotId, out json);


        if (json is null)
            return Task.FromResult<FindingsSnapshot?>(null);

        FindingsSnapshot snapshot = FindingsSerialization.DeserializeSnapshot(json);
        return Task.FromResult<FindingsSnapshot?>(snapshot);
    }
}
