using ArchLucid.Api.Models.Admin;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.GoToMarket;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.Controllers.Admin;

/// <summary>Quarterly aggregate ROI bulletin preview (admin-only; anonymized statistics).</summary>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.AdminAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/admin/roi-bulletin-preview")]
public sealed class RoiBulletinAdminController(IRoiBulletinAggregateReader reader) : ControllerBase
{
    private readonly IRoiBulletinAggregateReader _reader = reader ?? throw new ArgumentNullException(nameof(reader));

    /// <summary>Returns mean / median / p90 of tenant-supplied baseline review-cycle hours for the quarter.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(RoiBulletinPreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PreviewAsync(
        [FromQuery] string quarter,
        [FromQuery] int minTenants = 5,
        CancellationToken cancellationToken = default)
    {
        if (!RoiBulletinQuarterParser.TryParse(quarter, out RoiBulletinQuarterWindow window, out string? parseError))
            return this.BadRequestProblem(parseError ?? "Invalid quarter.", ProblemTypes.ValidationFailed);

        if (minTenants < 1)
            return this.BadRequestProblem("minTenants must be at least 1.", ProblemTypes.ValidationFailed);

        RoiBulletinAggregateReadResult result = await _reader.ReadAsync(window, minTenants, cancellationToken);

        if (!result.IsSufficientSample)
        {
            return this.BadRequestProblem(
                $"Aggregate bulletin requires at least {minTenants} tenants with tenant-supplied baseline hours captured in {result.QuarterLabel}; found {result.TenantCount}.",
                ProblemTypes.ValidationFailed);
        }

        return Ok(
            new RoiBulletinPreviewResponse
            {
                Quarter = result.QuarterLabel,
                TenantCount = result.TenantCount,
                MeanBaselineHours = result.MeanBaselineHours,
                MedianBaselineHours = result.MedianBaselineHours,
                P90BaselineHours = result.P90BaselineHours,
            });
    }
}
