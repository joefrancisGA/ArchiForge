using ArchLucid.Contracts.Manifest;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

namespace ArchLucid.Persistence.Reads;

/// <inheritdoc cref="IUnifiedGoldenManifestReader" />
/// <remarks>
/// Phase 1 ships coordinator-backed <see cref="GetByVersionAsync"/> and a run-scoped read that mirrors
/// <see cref="ArchLucid.Application.RunDetailQueryService"/> manifest-version selection (including the
/// <c>v1-{runId:N}</c> first-commit convention). Authority <see cref="IGoldenManifestRepository"/> joins this façade
/// once a stable contract ↔ decisioning manifest mapper exists — do not guess-map two <c>GoldenManifest</c> types here.
/// </remarks>
public sealed class UnifiedGoldenManifestReader(
    ICoordinatorGoldenManifestRepository coordinatorGoldenManifests,
    IRunRepository runRepository) : IUnifiedGoldenManifestReader
{
    private readonly ICoordinatorGoldenManifestRepository _coordinatorGoldenManifests =
        coordinatorGoldenManifests ?? throw new ArgumentNullException(nameof(coordinatorGoldenManifests));

    private readonly IRunRepository _runRepository = runRepository ?? throw new ArgumentNullException(nameof(runRepository));

    /// <inheritdoc />
    public Task<GoldenManifest?> GetByVersionAsync(string manifestVersion, CancellationToken cancellationToken = default) =>
        _coordinatorGoldenManifests.GetByVersionAsync(manifestVersion, cancellationToken);

    /// <inheritdoc />
    public async Task<GoldenManifest?> ReadByRunIdAsync(
        ScopeContext scope,
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        RunRecord? run = await _runRepository.GetByIdAsync(scope, runId, cancellationToken);

        if (run is null)
            return null;


        string runKey = runId.ToString("N");
        string manifestVersionKey = string.IsNullOrWhiteSpace(run.CurrentManifestVersion)
            ? $"v1-{runKey}"
            : run.CurrentManifestVersion!;

        GoldenManifest? manifest = await _coordinatorGoldenManifests.GetByVersionAsync(manifestVersionKey, cancellationToken);

        if (manifest is null)
            return null;


        string dashedRunId = runId.ToString("D");
        string compactRunId = runId.ToString("N");

        if (!string.Equals(manifest.RunId, dashedRunId, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(manifest.RunId, compactRunId, StringComparison.OrdinalIgnoreCase))
            return null;


        return manifest;
    }
}
