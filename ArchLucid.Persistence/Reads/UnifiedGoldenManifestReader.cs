using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

namespace ArchLucid.Persistence.Reads;

/// <inheritdoc cref="IUnifiedGoldenManifestReader" />
/// <remarks>
/// Phase 1 shipped coordinator-backed <see cref="GetByVersionAsync"/> and a run-scoped read that mirrors
/// <see cref="ArchLucid.Application.RunDetailQueryService"/> manifest-version selection (including the
/// <c>v1-{runId:N}</c> first-commit convention). PR A2 adds authority fallback: when <see cref="RunRecord.GoldenManifestId"/>
/// is set, load <see cref="Decisioning.Models.GoldenManifest"/> from <see cref="IGoldenManifestRepository"/> and project
/// to <see cref="GoldenManifest"/> via <see cref="IAuthorityCommitProjectionBuilder"/>.
/// </remarks>
public sealed class UnifiedGoldenManifestReader(
    ICoordinatorGoldenManifestRepository coordinatorGoldenManifests,
    IRunRepository runRepository,
    IGoldenManifestRepository authorityGoldenManifests,
    IAuthorityCommitProjectionBuilder projectionBuilder,
    IArchitectureRequestRepository requestRepository,
    IScopeContextProvider scopeContextProvider) : IUnifiedGoldenManifestReader
{
    private readonly ICoordinatorGoldenManifestRepository _coordinatorGoldenManifests =
        coordinatorGoldenManifests ?? throw new ArgumentNullException(nameof(coordinatorGoldenManifests));

    private readonly IRunRepository _runRepository = runRepository ?? throw new ArgumentNullException(nameof(runRepository));

    private readonly IGoldenManifestRepository _authorityGoldenManifests =
        authorityGoldenManifests ?? throw new ArgumentNullException(nameof(authorityGoldenManifests));

    private readonly IAuthorityCommitProjectionBuilder _projectionBuilder =
        projectionBuilder ?? throw new ArgumentNullException(nameof(projectionBuilder));

    private readonly IArchitectureRequestRepository _requestRepository =
        requestRepository ?? throw new ArgumentNullException(nameof(requestRepository));

    private readonly IScopeContextProvider _scopeContextProvider =
        scopeContextProvider ?? throw new ArgumentNullException(nameof(scopeContextProvider));

    /// <inheritdoc />
    public async Task<GoldenManifest?> GetByVersionAsync(string manifestVersion, CancellationToken cancellationToken = default)
    {
        ScopeContext scope = _scopeContextProvider.GetCurrentScope();
        ArchLucid.Decisioning.Models.GoldenManifest? authorityModel =
            await _authorityGoldenManifests
                .GetByContractManifestVersionAsync(scope, manifestVersion, cancellationToken)
                .ConfigureAwait(false);

        if (authorityModel is not null)
        {
            RunRecord? run = await _runRepository.GetByIdAsync(scope, authorityModel.RunId, cancellationToken)
                .ConfigureAwait(false);

            string systemName = run is null
                ? "Unknown"
                : await ResolveSystemNameAsync(run, cancellationToken).ConfigureAwait(false);

            return await _projectionBuilder
                .BuildAsync(authorityModel, new() { SystemName = systemName }, cancellationToken)
                .ConfigureAwait(false);
        }

        return await _coordinatorGoldenManifests
            .GetByVersionAsync(manifestVersion, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<GoldenManifest?> ReadByRunIdAsync(
        ScopeContext scope,
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        RunRecord? run = await _runRepository.GetByIdAsync(scope, runId, cancellationToken);

        if (run is null)
            return null;


        if (run.GoldenManifestId is { } goldenId)
        {
            ArchLucid.Decisioning.Models.GoldenManifest? authorityModel =
                await _authorityGoldenManifests.GetByIdAsync(scope, goldenId, cancellationToken);

            if (authorityModel is not null)
            {
                string systemName = await ResolveSystemNameAsync(run, cancellationToken);

                return await _projectionBuilder
                    .BuildAsync(authorityModel, new() { SystemName = systemName }, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        string runKey = runId.ToString("N");
        string manifestVersionKey = string.IsNullOrWhiteSpace(run.CurrentManifestVersion)
            ? $"v1-{runKey}"
            : run.CurrentManifestVersion!;

        ArchLucid.Decisioning.Models.GoldenManifest? authorityByVersion =
            await _authorityGoldenManifests
                .GetByContractManifestVersionAsync(scope, manifestVersionKey, cancellationToken)
                .ConfigureAwait(false);

        if (authorityByVersion is not null)
        {
            string systemName = await ResolveSystemNameAsync(run, cancellationToken);

            return await _projectionBuilder
                .BuildAsync(authorityByVersion, new() { SystemName = systemName }, cancellationToken)
                .ConfigureAwait(false);
        }

        GoldenManifest? manifest = await _coordinatorGoldenManifests
            .GetByVersionAsync(manifestVersionKey, cancellationToken)
            .ConfigureAwait(false);

        if (manifest is null)
            return null;


        string dashedRunId = runId.ToString("D");
        string compactRunId = runId.ToString("N");

        if (!string.Equals(manifest.RunId, dashedRunId, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(manifest.RunId, compactRunId, StringComparison.OrdinalIgnoreCase))
            return null;


        return manifest;
    }

    private async Task<string> ResolveSystemNameAsync(RunRecord run, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(run.ArchitectureRequestId) is false)
        {
            ArchitectureRequest? request =
                await _requestRepository.GetByIdAsync(run.ArchitectureRequestId, cancellationToken);

            if (request is not null && string.IsNullOrWhiteSpace(request.SystemName) is false)
                return request.SystemName;
        }

        if (string.IsNullOrWhiteSpace(run.ProjectId) is false)
            return run.ProjectId;

        return "Unknown";
    }
}
