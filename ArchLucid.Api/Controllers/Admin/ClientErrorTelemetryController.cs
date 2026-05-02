using ArchLucid.Api.Models;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application.Telemetry;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Admin;

/// <summary>
///     Accepts operator-shell client error reports for structured Serilog emission (no persistence).
/// </summary>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/diagnostics")]
[EnableRateLimiting("fixed")]
public sealed class ClientErrorTelemetryController(
    ILogger<ClientErrorTelemetryController> logger,
    IScopeContextProvider scopeContextProvider,
    IFirstTenantFunnelEmitter firstTenantFunnelEmitter) : ControllerBase
{
    private static readonly HashSet<string> SponsorBannerDayBuckets =
    [
        "0",
        "1-3",
        "4-7",
        "8-30",
        "30+"
    ];

    private readonly IFirstTenantFunnelEmitter _firstTenantFunnelEmitter =
        firstTenantFunnelEmitter ?? throw new ArgumentNullException(nameof(firstTenantFunnelEmitter));

    private readonly ILogger<ClientErrorTelemetryController> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IScopeContextProvider _scopeContextProvider =
        scopeContextProvider ?? throw new ArgumentNullException(nameof(scopeContextProvider));

    /// <summary>Records sponsor-banner first-commit badge render (low-cardinality counter).</summary>
    [HttpPost("sponsor-banner-first-commit-badge")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult PostSponsorBannerFirstCommitBadge([FromBody] SponsorBannerFirstCommitBadgeRequest? body)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.DaysSinceFirstCommitBucket))
            return this.BadRequestProblem(
                "daysSinceFirstCommitBucket is required.",
                ProblemTypes.ValidationFailed);

        string bucket = body.DaysSinceFirstCommitBucket.Trim();

        if (!SponsorBannerDayBuckets.Contains(bucket))
            return this.BadRequestProblem(
                "daysSinceFirstCommitBucket must be one of: 0, 1-3, 4-7, 8-30, 30+.",
                ProblemTypes.ValidationFailed);

        ScopeContext scope = _scopeContextProvider.GetCurrentScope();
        ArchLucidInstrumentation.RecordSponsorBannerFirstCommitBadgeRendered(scope.TenantId, bucket);

        return NoContent();
    }

    /// <summary>
    ///     Records one first-tenant onboarding funnel event (Improvement 12). Server infers the
    ///     tenant id from request scope; the body carries only the event name. Default emission is
    ///     aggregated-only (no <c>tenant_id</c> tag, no SQL row); per-tenant emission is gated by the
    ///     owner-only flag <c>Telemetry:FirstTenantFunnel:PerTenantEmission</c>.
    /// </summary>
    [HttpPost("first-tenant-funnel")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostFirstTenantFunnelEvent(
        [FromBody] FirstTenantFunnelEventRequest? body,
        CancellationToken ct)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.Event))
            return this.BadRequestProblem(
                "event is required.",
                ProblemTypes.ValidationFailed);

        string eventName = body.Event.Trim();

        if (!FirstTenantFunnelEventNames.IsValid(eventName))
            return this.BadRequestProblem(
                $"event must be one of: {string.Join(", ", FirstTenantFunnelEventNames.All)}.",
                ProblemTypes.ValidationFailed);

        ScopeContext scope = _scopeContextProvider.GetCurrentScope();
        await _firstTenantFunnelEmitter.EmitAsync(eventName, scope.TenantId, ct);

        return NoContent();
    }

    /// <summary>
    ///     Records one Core Pilot first-session checklist step from the operator UI (Improvement QA-2026-05-01). Aggregated
    ///     counter only — safe for anonymous calls with rate limiting.
    /// </summary>
    [HttpPost("core-pilot-rail-step")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult PostCorePilotRailChecklistStep([FromBody] CorePilotRailStepRequest? body)
    {
        if (body is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.ValidationFailed);

        if (body.StepIndex is < 0 or > 3)
            return this.BadRequestProblem(
                "stepIndex must be between 0 and 3 inclusive (Core Pilot checklist).",
                ProblemTypes.ValidationFailed);

        ArchLucidInstrumentation.RecordCorePilotRailChecklistStep(body.StepIndex);

        return NoContent();
    }

    /// <summary>Records a client-side error report at Warning level (sanitized).</summary>
    [HttpPost("client-error")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult PostClientError([FromBody] ClientErrorReport? body)
    {
        if (body is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.ValidationFailed);

        string message = body.Message.Trim();

        if (message.Length == 0)
            return this.BadRequestProblem("Message is required.", ProblemTypes.ValidationFailed);

        if (message.Length > ClientErrorTelemetryIngestLimits.MaxMessageLength)
            return this.BadRequestProblem(
                $"Message must be at most {ClientErrorTelemetryIngestLimits.MaxMessageLength} characters.",
                ProblemTypes.ValidationFailed);

        string? stack = TruncateNullable(body.Stack, ClientErrorTelemetryIngestLimits.MaxStackLength);
        string? pathname = TruncateNullable(body.Pathname, ClientErrorTelemetryIngestLimits.MaxPathnameLength);
        string? userAgent = TruncateNullable(body.UserAgent, ClientErrorTelemetryIngestLimits.MaxUserAgentLength);
        string? timestampUtc = TruncateNullable(body.TimestampUtc, 64);

        if (body.Context is not null)
        {
            if (body.Context.Count > ClientErrorTelemetryIngestLimits.MaxContextEntries)
                return this.BadRequestProblem(
                    $"Context may contain at most {ClientErrorTelemetryIngestLimits.MaxContextEntries} entries.",
                    ProblemTypes.ValidationFailed);

            foreach (KeyValuePair<string, string> pair in body.Context)

                if (pair.Key.Length > ClientErrorTelemetryIngestLimits.MaxContextKeyLength
                    || pair.Value.Length > ClientErrorTelemetryIngestLimits.MaxContextValueLength)

                    return this.BadRequestProblem(
                        $"Context keys must be at most {ClientErrorTelemetryIngestLimits.MaxContextKeyLength} characters and values at most {ClientErrorTelemetryIngestLimits.MaxContextValueLength} characters.",
                        ProblemTypes.ValidationFailed);
        }

        if (_logger.IsEnabled(LogLevel.Warning))
            _logger.LogWarningOperatorShellClientError(message, pathname, userAgent, timestampUtc, stack);

        return NoContent();
    }

    private static string? TruncateNullable(string? value, int maxLen)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        string trimmed = value.Trim();

        return trimmed.Length <= maxLen ? trimmed : trimmed[..maxLen];
    }
}
