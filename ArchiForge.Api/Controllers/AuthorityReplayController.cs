using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.HttpContracts;
using ArchiForge.Core.Audit;
using ArchiForge.Persistence.Replay;
using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

[ApiController]
[Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
[ApiVersion("1.0")]
[Route("api/authority/replay")]
[EnableRateLimiting("fixed")]
public sealed class AuthorityReplayController(
    IAuthorityReplayService replayService,
    IAuditService auditService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ReplayResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReplayResponse>> Replay(
        [FromBody] ReplayRequestResponse request,
        CancellationToken ct = default)
    {
        var mode = string.IsNullOrWhiteSpace(request.Mode)
            ? ReplayMode.ReconstructOnly
            : request.Mode.Trim();

        var result = await replayService.ReplayAsync(
            new ReplayRequest
            {
                RunId = request.RunId,
                Mode = mode
            },
            ct);

        if (result is null)
            return NotFound();

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.ReplayExecuted,
                RunId = request.RunId,
                DataJson = JsonSerializer.Serialize(new { mode, result.RebuiltManifest?.ManifestId })
            },
            ct);

        return Ok(new ReplayResponse
        {
            RunId = result.RunId,
            Mode = result.Mode,
            ReplayedUtc = result.ReplayedUtc,
            RebuiltManifestId = result.RebuiltManifest?.ManifestId,
            RebuiltManifestHash = result.RebuiltManifest?.ManifestHash,
            RebuiltArtifactBundleId = result.RebuiltArtifactBundle?.BundleId,
            Validation = new ReplayValidationResponse
            {
                ContextPresent = result.Validation.ContextPresent,
                GraphPresent = result.Validation.GraphPresent,
                FindingsPresent = result.Validation.FindingsPresent,
                ManifestPresent = result.Validation.ManifestPresent,
                TracePresent = result.Validation.TracePresent,
                ArtifactsPresent = result.Validation.ArtifactsPresent,
                ManifestHashMatches = result.Validation.ManifestHashMatches,
                ArtifactBundlePresentAfterReplay = result.Validation.ArtifactBundlePresentAfterReplay,
                Notes = result.Validation.Notes
            }
        });
    }
}
