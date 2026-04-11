using System.Data;

using ArchLucid.Contracts.Metadata;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>
/// Persistence interface for <see cref="ArchitectureRun"/> lifecycle management.
/// </summary>
public interface IArchitectureRunRepository
{
    /// <summary>Creates a new run record in storage.</summary>
    /// <param name="run">The run to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [Obsolete("RunsAuthorityConvergence write-freeze 2026-09-30: migrate to dbo.Runs. See docs/adr/0012.", error: false)]
    Task CreateAsync(
        ArchitectureRun run,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);

    /// <summary>
    /// Returns the run with the given <paramref name="runId"/>, or
    /// <see langword="null"/> when no matching record exists.
    /// </summary>
    /// <param name="runId">The run identifier to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ArchitectureRun?> GetByIdAsync(string runId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Updates the run status. When <paramref name="expectedStatus"/> is supplied the SQL
    /// <c>WHERE</c> clause includes <c>AND Status = @ExpectedStatus</c>; if no row is modified
    /// an <see cref="InvalidOperationException"/> is thrown, indicating a concurrent transition
    /// already moved the run to a different status.
    /// <c>CurrentManifestVersion</c> is only overwritten when <paramref name="currentManifestVersion"/>
    /// is non-<see langword="null"/>; passing <see langword="null"/> preserves the existing value.
    /// </summary>
    [Obsolete("RunsAuthorityConvergence write-freeze 2026-09-30: migrate to dbo.Runs. See docs/adr/0012.", error: false)]
    Task UpdateStatusAsync(
        string runId,
        Contracts.Common.ArchitectureRunStatus status,
        string? currentManifestVersion = null,
        DateTime? completedUtc = null,
        CancellationToken cancellationToken = default,
        Contracts.Common.ArchitectureRunStatus? expectedStatus = null,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);

    /// <summary>
    /// Returns summary items for all runs, ordered by <c>CreatedUtc</c> descending
    /// (most recent first). The result set may be bounded by the repository implementation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<ArchitectureRunListItem>> ListAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// After a deferred authority pipeline completes, updates snapshot pointers on the architecture run and sets status to <see cref="Contracts.Common.ArchitectureRunStatus.TasksGenerated"/>.
    /// </summary>
    [Obsolete("RunsAuthorityConvergence write-freeze 2026-09-30: migrate to dbo.Runs. See docs/adr/0012.", error: false)]
    Task ApplyDeferredAuthoritySnapshotsAsync(
        string runId,
        string? contextSnapshotId,
        Guid? graphSnapshotId,
        Guid? artifactBundleId,
        CancellationToken cancellationToken = default);
}
