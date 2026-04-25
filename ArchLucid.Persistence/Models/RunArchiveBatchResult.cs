namespace ArchLucid.Persistence.Models;

/// <summary>
///     Result of bulk run archival: rows affected and keys used to invalidate read caches.
/// </summary>
public sealed class RunArchiveBatchResult
{
    public int UpdatedCount
    {
        get;
        init;
    }

    public IReadOnlyList<ArchivedRunScopeRow> ArchivedRuns
    {
        get;
        init;
    } = [];

    /// <summary>Child <c>ArchivedUtc</c> row counts in the same batch as <see cref="ArchivedRuns" />.</summary>
    public RunArchiveChildCascadeCounts ChildCascade
    {
        get;
        init;
    } = new();
}
