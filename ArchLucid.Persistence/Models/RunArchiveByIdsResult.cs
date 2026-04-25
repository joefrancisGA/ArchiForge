namespace ArchLucid.Persistence.Models;

/// <summary>
///     Result of archiving specific runs by id (admin bulk operation).
/// </summary>
public sealed class RunArchiveByIdsResult
{
    /// <summary>Run ids that were newly archived in this call.</summary>
    public IReadOnlyList<Guid> SucceededRunIds
    {
        get;
        init;
    } = [];

    /// <summary>Scope rows for cache eviction (same shape as <see cref="RunArchiveBatchResult" />).</summary>
    public IReadOnlyList<ArchivedRunScopeRow> ArchivedRuns
    {
        get;
        init;
    } = [];

    /// <summary>Requested ids that were not archived (missing, already archived, or validation).</summary>
    public IReadOnlyList<RunArchiveByIdFailure> Failed
    {
        get;
        init;
    } = [];

    /// <summary>Child <c>ArchivedUtc</c> row counts in the same transaction as the run updates.</summary>
    public RunArchiveChildCascadeCounts ChildCascade
    {
        get;
        init;
    } = new();
}

/// <param name="RunId">Requested run id.</param>
/// <param name="Reason">Human-readable reason (e.g. not found, already archived).</param>
public sealed record RunArchiveByIdFailure(Guid RunId, string Reason);
