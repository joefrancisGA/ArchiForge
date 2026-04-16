using ArchLucid.Core.Authorization;
using ArchLucid.Api.Contracts;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Coordination.Compare;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers;

/// <summary>
/// HTTP API for comparing golden manifests or two authority runs within the caller’s scope.
/// </summary>
/// <remarks>Routes under <c>api/authority/compare</c>; delegates to <see cref="IAuthorityCompareService"/>.</remarks>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/authority/compare")]
[EnableRateLimiting("fixed")]
public sealed class AuthorityCompareController(
    IAuthorityCompareService compareService,
    IScopeContextProvider scopeProvider) : ControllerBase
{
    /// <summary>Compares two manifests by id in the current scope.</summary>
    /// <param name="leftManifestId">First manifest.</param>
    /// <param name="rightManifestId">Second manifest.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="ManifestComparisonResponse"/>, 404 when either id is missing in scope, or 409 when both exist but belong to different stored scopes.</returns>
    [HttpGet("manifests")]
    [ProducesResponseType(typeof(ManifestComparisonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CompareManifests(
        [FromQuery] Guid leftManifestId,
        [FromQuery] Guid rightManifestId,
        CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();

        ManifestComparisonResult? result;

        try
        {
            result = await compareService.CompareManifestsAsync(scope, leftManifestId, rightManifestId, ct);
        }
        catch (InvalidOperationException ex)
        {
            return this.ConflictProblem(ex.Message, ProblemTypes.Conflict);
        }

        if (result is null)
            return this.NotFoundProblem(
                $"One or both manifests ('{leftManifestId}', '{rightManifestId}') were not found in the current scope.",
                ProblemTypes.ManifestNotFound);

        return Ok(MapManifest(result));
    }

    /// <summary>Compares two runs (summary fields and nested manifests when both have golden manifest ids).</summary>
    /// <param name="leftRunId">First run.</param>
    /// <param name="rightRunId">Second run.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="RunComparisonResponse"/>, or 404 when either run is missing from the current scope.</returns>
    [HttpGet("runs")]
    [ProducesResponseType(typeof(RunComparisonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareRuns(
        [FromQuery] Guid leftRunId,
        [FromQuery] Guid rightRunId,
        CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        RunComparisonResult? result = await compareService.CompareRunsAsync(scope, leftRunId, rightRunId, ct);
        if (result is null)
            return this.NotFoundProblem(
                $"One or both runs ('{leftRunId}', '{rightRunId}') were not found in the current scope.",
                ProblemTypes.RunNotFound);

        return Ok(new RunComparisonResponse
        {
            LeftRunId = result.LeftRunId,
            RightRunId = result.RightRunId,
            RunLevelDiffs = result.RunLevelDiffs.Select(MapDiff).ToList(),
            ManifestComparison = result.ManifestComparison is null
                ? null
                : MapManifest(result.ManifestComparison),
            RunLevelDiffCount = result.RunLevelDiffs.Count,
            HasManifestComparison = result.ManifestComparison is not null,
        });
    }

    private static ManifestComparisonResponse MapManifest(ManifestComparisonResult result)
    {
        return new ManifestComparisonResponse
        {
            LeftManifestId = result.LeftManifestId,
            RightManifestId = result.RightManifestId,
            LeftManifestHash = result.LeftManifestHash,
            RightManifestHash = result.RightManifestHash,
            AddedCount = result.AddedCount,
            RemovedCount = result.RemovedCount,
            ChangedCount = result.ChangedCount,
            Diffs = result.Diffs.Select(MapDiff).ToList(),
            DiffCount = result.Diffs.Count,
        };
    }

    private static DiffItemResponse MapDiff(DiffItem item)
    {
        return new DiffItemResponse
        {
            Section = item.Section,
            Key = item.Key,
            DiffKind = item.DiffKind,
            BeforeValue = item.BeforeValue,
            AfterValue = item.AfterValue,
            Notes = item.Notes
        };
    }
}
