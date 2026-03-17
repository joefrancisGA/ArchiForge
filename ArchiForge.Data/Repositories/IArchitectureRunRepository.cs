using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Data.Repositories;

public interface IArchitectureRunRepository
{
    Task CreateAsync(ArchitectureRun run, CancellationToken cancellationToken = default);
    Task<ArchitectureRun?> GetByIdAsync(string runId, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(
        string runId,
        ArchiForge.Contracts.Common.ArchitectureRunStatus status,
        string? currentManifestVersion = null,
        DateTime? completedUtc = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ArchitectureRunListItem>> ListAsync(
        CancellationToken cancellationToken = default);
}