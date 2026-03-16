using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Data.Repositories;

public interface IRunExportRecordRepository
{
    Task CreateAsync(
        RunExportRecord record,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RunExportRecord>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default);

    Task<RunExportRecord?> GetByIdAsync(
        string exportRecordId,
        CancellationToken cancellationToken = default);
}

