using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Data.Repositories;

public interface IComparisonRecordRepository
{
    Task CreateAsync(
        ComparisonRecord record,
        CancellationToken cancellationToken = default);

    Task<ComparisonRecord?> GetByIdAsync(
        string comparisonRecordId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ComparisonRecord>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ComparisonRecord>> GetByExportRecordIdAsync(
        string exportRecordId,
        CancellationToken cancellationToken = default);

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

    Task<bool> UpdateLabelAndTagsAsync(
        string comparisonRecordId,
        string? label,
        IReadOnlyList<string>? tags,
        CancellationToken cancellationToken = default);
}

