using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.HttpContracts;
using ArchiForge.Persistence.Queries;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("api/authority")]
[EnableRateLimiting("fixed")]
public sealed class AuthorityQueryController(IAuthorityQueryService queryService) : ControllerBase
{
    [HttpGet("projects/{projectId}/runs")]
    [ProducesResponseType(typeof(IReadOnlyList<RunSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RunSummaryResponse>>> ListRunsByProject(
        string projectId,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var results = await queryService.ListRunsByProjectAsync(projectId, take, ct);

        return Ok(results.Select(x => new RunSummaryResponse
        {
            RunId = x.RunId,
            ProjectId = x.ProjectId,
            Description = x.Description,
            CreatedUtc = x.CreatedUtc,
            ContextSnapshotId = x.ContextSnapshotId,
            GraphSnapshotId = x.GraphSnapshotId,
            FindingsSnapshotId = x.FindingsSnapshotId,
            GoldenManifestId = x.GoldenManifestId,
            DecisionTraceId = x.DecisionTraceId,
            ArtifactBundleId = x.ArtifactBundleId
        }).ToList());
    }

    [HttpGet("runs/{runId:guid}/summary")]
    [ProducesResponseType(typeof(RunSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RunSummaryResponse>> GetRunSummary(
        Guid runId,
        CancellationToken ct = default)
    {
        var result = await queryService.GetRunSummaryAsync(runId, ct);
        if (result is null)
            return NotFound();

        return Ok(new RunSummaryResponse
        {
            RunId = result.RunId,
            ProjectId = result.ProjectId,
            Description = result.Description,
            CreatedUtc = result.CreatedUtc,
            ContextSnapshotId = result.ContextSnapshotId,
            GraphSnapshotId = result.GraphSnapshotId,
            FindingsSnapshotId = result.FindingsSnapshotId,
            GoldenManifestId = result.GoldenManifestId,
            DecisionTraceId = result.DecisionTraceId,
            ArtifactBundleId = result.ArtifactBundleId
        });
    }

    [HttpGet("runs/{runId:guid}")]
    [ProducesResponseType(typeof(RunDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RunDetailDto>> GetRunDetail(
        Guid runId,
        CancellationToken ct = default)
    {
        var result = await queryService.GetRunDetailAsync(runId, ct);
        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet("manifests/{manifestId:guid}/summary")]
    [ProducesResponseType(typeof(ManifestSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManifestSummaryResponse>> GetManifestSummary(
        Guid manifestId,
        CancellationToken ct = default)
    {
        var result = await queryService.GetManifestSummaryAsync(manifestId, ct);
        if (result is null)
            return NotFound();

        return Ok(new ManifestSummaryResponse
        {
            ManifestId = result.ManifestId,
            RunId = result.RunId,
            CreatedUtc = result.CreatedUtc,
            ManifestHash = result.ManifestHash,
            RuleSetId = result.RuleSetId,
            RuleSetVersion = result.RuleSetVersion,
            DecisionCount = result.DecisionCount,
            WarningCount = result.WarningCount,
            UnresolvedIssueCount = result.UnresolvedIssueCount,
            Status = result.Status
        });
    }
}
