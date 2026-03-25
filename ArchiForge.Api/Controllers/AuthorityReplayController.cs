using System.Text.Json;

using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.Contracts;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Core.Audit;
using ArchiForge.Persistence.Replay;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

/// <summary>
/// Executes authority run replay (validate, optionally rebuild manifest/trace and artifacts) for the authenticated scope.
/// </summary>
/// <remarks>
/// POST <c>api/authority/replay</c>; uses <see cref="ReplayMode"/> strings from the request body. Emits <see cref="AuditEventTypes.ReplayExecuted"/> on success.
/// </remarks>
[ApiController]
[Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
[ApiVersion("1.0")]
[Route("api/authority/replay")]
[EnableRateLimiting("fixed")]
public sealed class AuthorityReplayController(
    IAuthorityReplayService replayService,
    IAuditService auditService) : ControllerBase
{
    /// <summary>Runs replay for the run and mode in <paramref name="request"/>.</summary>
    /// <param name="request">Run id and optional mode (defaults to <see cref="ReplayMode.ReconstructOnly"/>).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="ReplayResponse"/> with validation and rebuilt entity ids when applicable, or 404 when the run is unknown.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ReplayResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Replay(
        [FromBody] ReplayRequestResponse? request,
        CancellationToken ct = default)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        string mode = string.IsNullOrWhiteSpace(request.Mode)
            ? ReplayMode.ReconstructOnly
            : request.Mode.Trim();

        ReplayResult? result = await replayService.ReplayAsync(
            new ReplayRequest
            {
                RunId = request.RunId,
                Mode = mode
            },
            ct);

        if (result is null)
            return this.NotFoundProblem($"Run '{request.RunId}' was not found.", ProblemTypes.RunNotFound);

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
