using ArchLucid.Api.Attributes;
using ArchLucid.Api.Models.Pilots;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application;
using ArchLucid.Application.Pilots;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Pilots;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Tenancy;

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
    FirstValueReportPdfBuilder firstValueReportPdfBuilder,
    PilotScorecardBuilder pilotScorecardBuilder,
    SponsorOnePagerPdfBuilder sponsorOnePagerPdfBuilder,
    IWhyArchLucidSnapshotService whyArchLucidSnapshotService,
    IRunDetailQueryService runDetailQueryService,
    IPilotRunDeltaComputer pilotRunDeltaComputer) : ControllerBase
{
    /// <summary>
    /// Read-only telemetry snapshot for the operator-shell <c>/why-archlucid</c> proof page (cumulative since
    /// API host start) plus the canonical Contoso Retail demo run id used by the page's other read endpoints.
    /// </summary>
    [HttpGet("why-archlucid-snapshot")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(WhyArchLucidSnapshotResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<WhyArchLucidSnapshotResponse>> GetWhyArchLucidSnapshot(CancellationToken cancellationToken)
    {
        WhyArchLucidSnapshotResponse snapshot = await whyArchLucidSnapshotService.BuildAsync(cancellationToken);

        return Ok(snapshot);
    }

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

        return markdown is null ? this.NotFoundProblem($"First-value report is not available for run '{runId}'.", ProblemTypes.RunNotFound) : Content(markdown, "text/markdown; charset=utf-8");
    }

    /// <summary>
    /// JSON proof-of-ROI deltas for <paramref name="runId"/> (same numbers as the first-value report and sponsor PDF).
    /// </summary>
    [HttpGet("runs/{runId}/pilot-run-deltas")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(PilotRunDeltasResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPilotRunDeltas(string runId, CancellationToken cancellationToken)
    {
        ArchitectureRunDetail? detail = await runDetailQueryService.GetRunDetailAsync(runId, cancellationToken);

        if (detail is null)
            return this.NotFoundProblem($"Run '{runId}' was not found (or is out of scope).", ProblemTypes.RunNotFound);

        PilotRunDeltas deltas = await pilotRunDeltaComputer.ComputeAsync(detail, cancellationToken);

        return Ok(PilotRunDeltasResponseMapper.ToResponse(deltas));
    }

    /// <summary>
    /// PDF projection of the first-value-report Markdown — a one-shot sponsor email attachment for a committed run.
    /// Mirrors the auth surface of <see cref="GetFirstValueReport"/> (ReadAuthority) so the operator-shell post-commit
    /// CTA does not introduce a new commercial gate at the click site.
    /// </summary>
    [HttpPost("runs/{runId}/first-value-report.pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PostFirstValueReportPdf(string runId, CancellationToken cancellationToken)
    {
        string baseForLinks = $"{Request.Scheme}://{Request.Host.Value}";
        byte[]? pdf = await firstValueReportPdfBuilder.BuildPdfAsync(runId, baseForLinks, cancellationToken);

        return pdf is null ? this.NotFoundProblem($"First-value report PDF is not available for run '{runId}'.", ProblemTypes.RunNotFound) : File(pdf, "application/pdf", $"first-value-report-{runId}.pdf");
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

    /// <summary>
    /// One-page sponsor PDF for a run (Standard tier) — headline timing plus 30-day pilot scorecard mix.
    /// </summary>
    [HttpPost("runs/{runId}/sponsor-one-pager")]
    [RequiresCommercialTenantTier(TenantTier.Standard)]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    public async Task<IActionResult> PostSponsorOnePager(string runId, CancellationToken cancellationToken)
    {
        string baseForLinks = $"{Request.Scheme}://{Request.Host.Value}";
        byte[]? pdf = await sponsorOnePagerPdfBuilder.BuildPdfAsync(runId, baseForLinks, cancellationToken);

        return pdf is null ? this.NotFoundProblem($"Sponsor one-pager is not available for run '{runId}'.", ProblemTypes.RunNotFound) : File(pdf, "application/pdf", $"sponsor-one-pager-{runId}.pdf");
    }
}
