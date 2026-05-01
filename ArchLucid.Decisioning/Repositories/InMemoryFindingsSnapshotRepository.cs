using System.Data;
using System.Security.Cryptography;
using System.Text;

using ArchLucid.Core.Pagination;
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

    /// <inheritdoc />
    public Task<FindingRecordMetadataPage> ListFindingRecordsKeysetAsync(
        Guid findingsSnapshotId,
        int? cursorSortOrder,
        Guid? cursorFindingRecordId,
        string? severity,
        string? category,
        string? findingType,
        int take,
        CancellationToken ct)
    {
        _ = ct;
        if (cursorSortOrder.HasValue ^ cursorFindingRecordId.HasValue)
            throw new ArgumentException(
                "Cursor requires both sortOrder and findingRecordId, or neither for the first page.");

        int cappedTake = Math.Clamp(take <= 0 ? FindingPagination.DefaultTake : take, 1, FindingPagination.MaxTake);
        int fetch = cappedTake + 1;

        string? json;
        lock (_lock)

            _store.TryGetValue(findingsSnapshotId, out json);

        if (json is null)
            return Task.FromResult(new FindingRecordMetadataPage([], false));

        FindingsSnapshot snapshot = FindingsSerialization.DeserializeSnapshot(json);

        IEnumerable<FindingEnvelope> envelopes =
            Enumerable.Range(0, snapshot.Findings.Count)
                .Select(i =>
                {
                    Finding f = snapshot.Findings[i];
                    Guid recordId = StableFindingRecordId(findingsSnapshotId, i, f.FindingId);

                    return new FindingEnvelope(
                        SortOrder: i,
                        RecordId: recordId,
                        Finding: f);
                });

        string? sev = NormalizeFilter(severity);
        string? cat = NormalizeFilter(category);
        string? ftype = NormalizeFilter(findingType);

        envelopes = envelopes.Where(e =>
        {
            Finding f = e.Finding;

            if (sev is not null && !string.Equals(f.Severity.ToString(), sev, StringComparison.OrdinalIgnoreCase))
                return false;

            if (cat is not null && !string.Equals(f.Category, cat, StringComparison.OrdinalIgnoreCase))
                return false;

            if (ftype is not null && !string.Equals(f.FindingType, ftype, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        });

        List<FindingEnvelope> ordered =
            envelopes.OrderBy(e => e.SortOrder).ThenBy(e => e.RecordId).ToList();

        bool hasCursor = cursorSortOrder.HasValue && cursorFindingRecordId.HasValue;

        IEnumerable<FindingEnvelope> pageSource = ordered;

        if (hasCursor)

        {
            int cs = cursorSortOrder!.Value;
            Guid cid = cursorFindingRecordId!.Value;

            pageSource =
                ordered.Where(e =>
                    e.SortOrder > cs || (e.SortOrder == cs && e.RecordId.CompareTo(cid) > 0));
        }

        List<FindingEnvelope> slice = pageSource.Take(fetch).ToList();
        bool hasMore = slice.Count > cappedTake;

        if (hasMore)

            slice.RemoveAt(slice.Count - 1);

        FindingRecordMetadataRow[] rows =
            slice.Select(static e =>
                    new FindingRecordMetadataRow(
                        e.RecordId,
                        e.SortOrder,
                        e.Finding.FindingId,
                        e.Finding.FindingType,
                        e.Finding.Category,
                        e.Finding.EngineType,
                        e.Finding.Severity.ToString(),
                        e.Finding.Title))
                .ToArray();

        return Task.FromResult(new FindingRecordMetadataPage(rows, hasMore));
    }

    /// <remarks>Stable surrogate key for deterministic in-memory paging (differs from SQL <c>NewGuid()</c> row ids).</remarks>
    private sealed record FindingEnvelope(int SortOrder, Guid RecordId, Finding Finding);

    /// <remarks>Matches SQL surrogate key stability for deterministic in-memory paging / tests.</remarks>
    private static Guid StableFindingRecordId(Guid findingsSnapshotId, int sortOrder, string findingId)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes($"{findingsSnapshotId:N}:{sortOrder}:{findingId}");
        Span<byte> hash = stackalloc byte[32];

        SHA256.HashData(utf8, hash);

        return new Guid(hash[..16]);
    }

    private static string? NormalizeFilter(string? raw) =>
        string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
}
