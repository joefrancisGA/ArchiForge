using System.Globalization;
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
///     <see cref="ArchLucidPolicies.AdminAuthority" /> (tenant administrators and workspace administrators).
///     Reuses the shared
///     <see cref="ISupportBundleAssembler" /> service so the artefact shape stays
///     in lock-step with the CLI's <c>archlucid support-bundle</c> command.
/// </summary>
/// <remarks>
///     <b>Why a separate controller (not <c>AdminController</c>)?</b> <c>AdminController</c>
///     mixes diagnostics routes with the same class-level <see cref="ArchLucidPolicies.AdminAuthority" />; this
///     controller keeps the support-bundle action isolated for documentation and contract tests.
///     <b>Streaming.</b> The assembler returns the full ZIP in memory because the bundle
///     is small (a handful of JSON sections + a README). When future redaction work
///     producer if ZIP size materially grows beyond the diagnostic JSON snapshots.
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
    [Authorize(Policy = ArchLucidPolicies.AdminAuthority)]
    [Produces("application/zip")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DownloadSupportBundle(CancellationToken cancellationToken = default)
    {
        SupportBundleRequest request = new(
            User.Identity?.Name,
            null);

        SupportBundleArtifact artifact = await _assembler.AssembleAsync(request, cancellationToken);

        await _auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.SupportBundleDownloaded,
                DataJson = JsonSerializer.Serialize(
                    new { fileName = artifact.FileName, sizeBytes = artifact.Bytes.Length })
            },
            cancellationToken);

        Response.Headers.Append(
            "X-Support-Bundle-Retention-Discard-After-Utc",
            artifact.RetentionDiscardAfterUtc.ToString("O", CultureInfo.InvariantCulture));

        return File(artifact.Bytes, artifact.ContentType, artifact.FileName);
    }
}
