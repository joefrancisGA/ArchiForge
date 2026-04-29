namespace ArchLucid.Decisioning.Models;

/// <summary>List projection for findings keyset queries (no payloads / trace graph).</summary>
public sealed record FindingRecordMetadataRow(
    Guid FindingRecordId,
    int SortOrder,
    string FindingId,
    string FindingType,
    string Category,
    string EngineType,
    string Severity,
    string Title);

/// <summary>
///     Keyset page of finding metadata rows ordered by <c>SortOrder ASC</c>, <c>FindingRecordId ASC</c>.
/// </summary>
public sealed record FindingRecordMetadataPage(IReadOnlyList<FindingRecordMetadataRow> Items, bool HasMore);
