using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.HttpContracts;
using ArchiForge.Persistence.Compare;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("api/authority/compare")]
[EnableRateLimiting("fixed")]
public sealed class AuthorityCompareController(IAuthorityCompareService compareService) : ControllerBase
{
    [HttpGet("manifests")]
    [ProducesResponseType(typeof(ManifestComparisonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManifestComparisonResponse>> CompareManifests(
        [FromQuery] Guid leftManifestId,
        [FromQuery] Guid rightManifestId,
        CancellationToken ct = default)
    {
        var result = await compareService.CompareManifestsAsync(leftManifestId, rightManifestId, ct);
        if (result is null)
            return NotFound();

        return Ok(MapManifest(result));
    }

    [HttpGet("runs")]
    [ProducesResponseType(typeof(RunComparisonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RunComparisonResponse>> CompareRuns(
        [FromQuery] Guid leftRunId,
        [FromQuery] Guid rightRunId,
        CancellationToken ct = default)
    {
        var result = await compareService.CompareRunsAsync(leftRunId, rightRunId, ct);
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
