using ArchLucid.Api.Models.Pilots;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application.Pilots;
using ArchLucid.Core.Authorization;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Pilots;

/// <summary>
/// Pilot-facing read models (sponsor summaries, scorecards).
/// </summary>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/pilots")]
[EnableRateLimiting("fixed")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class PilotsController(
    FirstValueReportBuilder firstValueReportBuilder,
    PilotScorecardBuilder pilotScorecardBuilder) : ControllerBase
{
    /// <summary>
    /// Markdown summary suitable for a sponsor after a first committed run (read-only).
    /// </summary>
    [HttpGet("runs/{runId}/first-value-report")]
    [Produces("text/markdown")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK, "text/markdown")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFirstValueReport(string runId, CancellationToken cancellationToken)
    {
        string baseForLinks = $"{Request.Scheme}://{Request.Host.Value}";
        string? markdown = await firstValueReportBuilder.BuildMarkdownAsync(runId, baseForLinks, cancellationToken);

        if (markdown is null)
            return NotFound();


        return Content(markdown, "text/markdown; charset=utf-8");
    }

    /// <summary>JSON pilot scorecard for the current tenant scope (UTC window).</summary>
    [HttpPost("scorecard")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(PilotScorecardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostScorecard(
        [FromBody] PilotScorecardPostRequest? body,
        CancellationToken cancellationToken)
    {
        DateTimeOffset end = body?.PeriodEnd ?? DateTimeOffset.UtcNow;
        DateTimeOffset start = body?.PeriodStart ?? end.AddDays(-30);

        if (end <= start)
            return this.BadRequestProblem("PeriodEnd must be after PeriodStart.", ProblemTypes.ValidationFailed);

        PilotScorecardSummary summary = await pilotScorecardBuilder.BuildAsync(start, end, cancellationToken);

        PilotScorecardResponse response = new()
        {
            TenantId = summary.TenantId,
            PeriodStart = summary.PeriodStart,
            PeriodEnd = summary.PeriodEnd,
            RunsInPeriod = summary.RunsInPeriod,
            RunsWithCommittedManifest = summary.RunsWithCommittedManifest,
        };

        return Ok(response);
    }
}
