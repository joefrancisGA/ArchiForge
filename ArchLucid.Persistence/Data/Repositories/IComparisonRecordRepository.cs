using ArchLucid.Contracts.Metadata;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>
///     Persistence contract for <see cref="ComparisonRecord" /> records that capture the outcome
///     of manifest or export diff operations.
/// </summary>
public interface IComparisonRecordRepository
{
    /// <summary>
    ///     Persists a new comparison record.
    ///     <paramref name="record" /> must have a non-empty <c>ComparisonRecordId</c>.
    /// </summary>
    Task CreateAsync(
        ComparisonRecord record,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns the comparison record with the specified <paramref name="comparisonRecordId" />,
    ///     or <see langword="null" /> when not found.
    /// </summary>
    Task<ComparisonRecord?> GetByIdAsync(
        string comparisonRecordId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns all comparison records associated with the given <paramref name="runId" />.
    ///     Returns an empty list (never <see langword="null" />) when none are found.
    /// </summary>
    Task<IReadOnlyList<ComparisonRecord>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns all comparison records associated with the given <paramref name="exportRecordId" />.
    ///     Returns an empty list (never <see langword="null" />) when none are found.
    /// </summary>
    Task<IReadOnlyList<ComparisonRecord>> GetByExportRecordIdAsync(
        string exportRecordId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Searches comparison records using optional filter and sort parameters.
    ///     <paramref name="skip" /> and <paramref name="limit" /> provide offset-based paging.
    ///     All filter parameters are optional; omitting them returns all records up to <paramref name="limit" />.
    /// </summary>
    Task<IReadOnlyList<ComparisonRecord>> SearchAsync(
        string? comparisonType,
        string? leftRunId,
        string? rightRunId,
        DateTime? createdFromUtc,
        DateTime? createdToUtc,
        string? leftExportRecordId,
        string? rightExportRecordId,
        string? label,
        IReadOnlyList<string>? tags,
        string? sortBy,
        string? sortDir,
        int skip,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Searches comparison records using keyset (cursor) paging for stable large-result traversal.
    ///     <paramref name="cursorCreatedUtc" /> and <paramref name="cursorComparisonRecordId" /> together
    ///     identify the last record seen; pass <see langword="null" /> for both to start from the beginning.
    /// </summary>
    Task<IReadOnlyList<ComparisonRecord>> SearchByCursorAsync(
        string? comparisonType,
        string? leftRunId,
        string? rightRunId,
        DateTime? createdFromUtc,
        DateTime? createdToUtc,
        string? leftExportRecordId,
        string? rightExportRecordId,
        string? label,
        IReadOnlyList<string>? tags,
        string? sortBy,
        string? sortDir,
        DateTime? cursorCreatedUtc,
        string? cursorComparisonRecordId,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates the label and tags on an existing comparison record.
    ///     Returns <see langword="true" /> if the record was found and updated; <see langword="false" /> when not found.
    ///     Either parameter may be <see langword="null" /> to leave the respective field unchanged.
    /// </summary>
    Task<bool> UpdateLabelAndTagsAsync(
        string comparisonRecordId,
        string? label,
        IReadOnlyList<string>? tags,
        CancellationToken cancellationToken = default);
}
