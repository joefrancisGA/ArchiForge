using ArchLucid.Contracts.Metadata;

namespace ArchLucid.Application.Analysis;

/// <summary>
///     Persists comparison outputs as immutable audit records so they can be replayed or inspected later.
/// </summary>
public interface IComparisonAuditService
{
    /// <summary>
    ///     Persists an end-to-end replay comparison report together with its Markdown summary.
    /// </summary>
    /// <returns>The newly created <c>ComparisonRecordId</c>.</returns>
    Task<string> RecordEndToEndAsync(
        EndToEndReplayComparisonReport report,
        string summaryMarkdown,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Persists an export-record diff result together with its Markdown summary.
    /// </summary>
    /// <returns>The newly created <c>ComparisonRecordId</c>.</returns>
    Task<string> RecordExportDiffAsync(
        ExportRecordDiffResult diff,
        string summaryMarkdown,
        CancellationToken cancellationToken = default);

    /// <summary>Persists a replay of an existing comparison record as a new record (same payload, new id and timestamp).</summary>
    /// <returns>The newly created <c>ComparisonRecordId</c>.</returns>
    Task<string> RecordReplayOfAsync(
        ComparisonRecord sourceRecord,
        string? notes = null,
        CancellationToken cancellationToken = default);
}
