namespace ArchLucid.Persistence.Models;

/// <summary>
/// Result of bulk run archival: rows affected and keys used to invalidate read caches.
/// </summary>
public sealed class RunArchiveBatchResult
{
    public int UpdatedCount { get; init; }

    public IReadOnlyList<ArchivedRunScopeRow> ArchivedRuns { get; init; } = [];
}
