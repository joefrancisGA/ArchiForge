using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Persistence.Data.Repositories;

/// <summary>
/// Persistence contract for <see cref="RunExportRecord"/> records that track every export
/// generated for an architecture run.
/// </summary>
public interface IRunExportRecordRepository
{
    /// <summary>
    /// Persists a new export record.
    /// <paramref name="record"/> must have a non-empty <c>ExportRecordId</c>.
    /// </summary>
    Task CreateAsync(
        RunExportRecord record,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all export records for the given <paramref name="runId"/>, ordered by creation time descending.
    /// Returns an empty list (never <see langword="null"/>) when no records are found.
    /// </summary>
    Task<IReadOnlyList<RunExportRecord>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the export record with the specified <paramref name="exportRecordId"/>,
    /// or <see langword="null"/> when not found.
    /// </summary>
    Task<RunExportRecord?> GetByIdAsync(
        string exportRecordId,
        CancellationToken cancellationToken = default);
}
