using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application.Pilots;
using ArchLucid.Core.Authorization;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.Controllers.Admin;

/// <summary>Admin-only export of reference-evidence ZIP bundles per tenant.</summary>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.AdminAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/admin/tenants/{tenantId:guid}/reference-evidence")]
public sealed class ReferenceEvidenceAdminController(IReferenceEvidenceAdminExportService exportService)
    : ControllerBase
{
    private readonly IReferenceEvidenceAdminExportService _exportService =
        exportService ?? throw new ArgumentNullException(nameof(exportService));

    /// <summary>
    ///     ZIP containing <c>pilot-run-deltas.json</c>, first-value Markdown/PDF, optional sponsor one-pager, and a README.
    /// </summary>
    /// <param name="tenantId">Tenant whose latest committed (non-demo by default) run anchors the bundle.</param>
    /// <param name="includeDemo">When <see langword="true" />, allow Contoso demo seed runs as the anchor.</param>
    [HttpGet]
    [Produces("application/zip")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReferenceEvidenceZipAsync(
        Guid tenantId,
        [FromQuery] bool includeDemo = false,
        CancellationToken cancellationToken = default)
    {
        string baseForLinks = $"{Request.Scheme}://{Request.Host.Value}";
        byte[]? zip = await _exportService.BuildZipAsync(tenantId, includeDemo, baseForLinks, cancellationToken);

        if (zip is null || zip.Length == 0)
        {
            return this.NotFoundProblem(
                "No reference evidence ZIP could be built for this tenant (no qualifying committed run, or export produced no content).",
                ProblemTypes.ResourceNotFound);
        }

        return File(zip, "application/zip", $"reference-evidence-{tenantId:D}.zip");
    }
}
