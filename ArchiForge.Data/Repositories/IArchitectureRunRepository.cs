using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Data.Repositories;

public interface IArchitectureRunRepository
{
    Task CreateAsync(ArchitectureRun run, CancellationToken cancellationToken = default);
    Task<ArchitectureRun?> GetByIdAsync(string runId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Updates the run status. When <paramref name="expectedStatus"/> is supplied the SQL
    /// <c>WHERE</c> clause includes <c>AND Status = @ExpectedStatus</c>; if no row is modified
    /// an <see cref="InvalidOperationException"/> is thrown, indicating a concurrent transition
    /// already moved the run to a different status.
    /// <c>CurrentManifestVersion</c> is only overwritten when <paramref name="currentManifestVersion"/>
    /// is non-<see langword="null"/>; passing <see langword="null"/> preserves the existing value.
    /// </summary>
    Task UpdateStatusAsync(
        string runId,
        Contracts.Common.ArchitectureRunStatus status,
        string? currentManifestVersion = null,
        DateTime? completedUtc = null,
        CancellationToken cancellationToken = default,
        Contracts.Common.ArchitectureRunStatus? expectedStatus = null);

    Task<IReadOnlyList<ArchitectureRunListItem>> ListAsync(
        CancellationToken cancellationToken = default);
}
