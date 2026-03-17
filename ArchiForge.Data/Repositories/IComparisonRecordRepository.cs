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
        int limit,
        CancellationToken cancellationToken = default);
}

