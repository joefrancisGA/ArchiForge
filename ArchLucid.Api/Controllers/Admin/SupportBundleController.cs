using System.Text.Json;

using ArchLucid.Application.Support;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Authorization;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Admin;

/// <summary>
///     In-product support-bundle download endpoint — gated on
///     <see cref="ArchLucidPolicies.ExecuteAuthority" /> per owner decision F
///     (PENDING_QUESTIONS.md item 37, 2026-04-23). Reuses the shared
///     <see cref="ISupportBundleAssembler" /> service so the artefact shape stays
///     in lock-step with the CLI's <c>archlucid support-bundle</c> command.
/// </summary>
/// <remarks>
///     <b>Why a separate controller (not <c>AdminController</c>)?</b> <c>AdminController</c>
///     is class-attributed with <see cref="ArchLucidPolicies.AdminAuthority" />, which is
///     stricter than the policy decision F prescribed for this endpoint. Hosting it here
///     keeps the policy correct without adding a per-action override that contradicts the
///     class-level guard.
///
///     <b>Streaming.</b> The assembler returns the full ZIP in memory because the bundle
///     is small (a handful of JSON sections + a README). When future redaction work
///     forces larger bundles (e.g. recent run summaries, last 200 audit events — see
///     PENDING_QUESTIONS.md item 37 part c), this should switch to a streamed
///     <see cref="System.IO.Pipelines" /> producer.
/// </remarks>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/admin")]
[EnableRateLimiting("expensive")]
public sealed class SupportBundleController(
    ISupportBundleAssembler assembler,
    IAuditService auditService) : ControllerBase
{
    private readonly ISupportBundleAssembler _assembler =
        assembler ?? throw new ArgumentNullException(nameof(assembler));

    private readonly IAuditService _auditService =
        auditService ?? throw new ArgumentNullException(nameof(auditService));

    /// <summary>Downloads a freshly-assembled, redacted support bundle ZIP.</summary>
    /// <returns>200 OK with <c>application/zip</c> body and a content-disposition file name.</returns>
    [HttpPost("support-bundle")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [Produces("application/zip")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DownloadSupportBundle(CancellationToken cancellationToken = default)
    {
        SupportBundleRequest request = new(
            RequesterDisplayId: User?.Identity?.Name,
            TenantDisplayName: null);

        SupportBundleArtifact artifact = await _assembler.AssembleAsync(request, cancellationToken);

        await _auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.SupportBundleDownloaded,
                DataJson = JsonSerializer.Serialize(
                    new { fileName = artifact.FileName, sizeBytes = artifact.Bytes.Length })
            },
            cancellationToken);

        return File(artifact.Bytes, artifact.ContentType, artifact.FileName);
    }
}
