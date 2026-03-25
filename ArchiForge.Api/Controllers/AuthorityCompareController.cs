using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.Contracts;
using ArchiForge.Core.Scoping;
using ArchiForge.Persistence.Compare;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

/// <summary>
/// HTTP API for comparing golden manifests or two authority runs within the caller’s scope.
/// </summary>
/// <remarks>Routes under <c>api/authority/compare</c>; delegates to <see cref="IAuthorityCompareService"/>.</remarks>
[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("api/authority/compare")]
[EnableRateLimiting("fixed")]
public sealed class AuthorityCompareController(
    IAuthorityCompareService compareService,
    IScopeContextProvider scopeProvider) : ControllerBase
{
    /// <summary>Compares two manifests by id in the current scope.</summary>
    /// <param name="leftManifestId">First manifest.</param>
    /// <param name="rightManifestId">Second manifest.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="ManifestComparisonResponse"/>, or 404 when either id is missing or manifests are not in the same scope.</returns>
    [HttpGet("manifests")]
    [ProducesResponseType(typeof(ManifestComparisonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManifestComparisonResponse>> CompareManifests(
        [FromQuery] Guid leftManifestId,
        [FromQuery] Guid rightManifestId,
        CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        ManifestComparisonResult? result = await compareService.CompareManifestsAsync(scope, leftManifestId, rightManifestId, ct);
        if (result is null)
            return NotFound();

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
    public async Task<ActionResult<RunComparisonResponse>> CompareRuns(
        [FromQuery] Guid leftRunId,
        [FromQuery] Guid rightRunId,
        CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        RunComparisonResult? result = await compareService.CompareRunsAsync(scope, leftRunId, rightRunId, ct);
        if (result is null)
            return NotFound();

        return Ok(new RunComparisonResponse
        {
            LeftRunId = result.LeftRunId,
            RightRunId = result.RightRunId,
            RunLevelDiffs = result.RunLevelDiffs.Select(MapDiff).ToList(),
            ManifestComparison = result.ManifestComparison is null
                ? null
                : MapManifest(result.ManifestComparison)
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
            Diffs = result.Diffs.Select(MapDiff).ToList()
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
