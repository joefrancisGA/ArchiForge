using ArchLucid.Api.Models.Analytics;
using ArchLucid.Core.Authorization;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Analytics;

/// <summary>Executive ROI analytics (aggregates). Data is mocked until the ROI data model is finalized.</summary>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/analytics")]
[EnableRateLimiting("fixed")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class RoiAnalyticsController : ControllerBase
{
    /// <summary>Returns mocked aggregate ROI metrics for executive dashboards.</summary>
    [HttpGet("roi")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ExecutiveRoiAggregatesResponse), StatusCodes.Status200OK)]
    public ActionResult<ExecutiveRoiAggregatesResponse> GetRoiAggregates()
    {
        ExecutiveRoiAggregatesResponse body = new()
        {
            TimeSavedHours = 142.5,
            DecisionsAutomated = 1840,
            ComplianceRisksMitigated = 37,
        };

        return Ok(body);
    }
}
