using System.Text.Json;

using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Core.Audit;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Advisory.Scheduling;
using ArchiForge.Persistence.Advisory;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

/// <summary>
/// CRON-style advisory scan schedules, on-demand runs, execution history, and persisted architecture digests for the caller’s scope.
/// </summary>
/// <remarks>
/// <see cref="IAdvisoryScanRunner.RunScheduleAsync"/> loads effective governance once per successful scan, merges advisory defaults into the plan,
/// and drives alert evaluation (see <c>docs/API_CONTRACTS.md</c> and the governance piece tracker in <c>docs/METHOD_DOCUMENTATION.md</c>). Routes: <c>api/advisory-scheduling</c>.
/// </remarks>
[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("api/advisory-scheduling")]
[EnableRateLimiting("fixed")]
public sealed class AdvisorySchedulingController(
    IScopeContextProvider scopeProvider,
    IAdvisoryScanScheduleRepository scheduleRepository,
    IAdvisoryScanExecutionRepository executionRepository,
    IArchitectureDigestRepository digestRepository,
    IAdvisoryScanRunner scanRunner,
    IScanScheduleCalculator scheduleCalculator,
    IAuditService auditService)
    : ControllerBase
{
    /// <summary>Creates a schedule with scope ids, normalizes slug, and computes initial <see cref="AdvisoryScanSchedule.NextRunUtc"/>.</summary>
    /// <param name="request">Client payload; <see cref="AdvisoryScanSchedule.ScheduleId"/> and scope ids are overwritten from the authenticated scope.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The persisted schedule including assigned id and computed <see cref="AdvisoryScanSchedule.NextRunUtc"/>.</returns>
    [HttpPost("schedules")]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(AdvisoryScanSchedule), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateSchedule(
        [FromBody] AdvisoryScanSchedule? request,
        CancellationToken ct = default)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        ScopeContext scope = scopeProvider.GetCurrentScope();

        request.ScheduleId = Guid.NewGuid();
        request.TenantId = scope.TenantId;
        request.WorkspaceId = scope.WorkspaceId;
        request.ProjectId = scope.ProjectId;
        if (string.IsNullOrWhiteSpace(request.RunProjectSlug))
            request.RunProjectSlug = "default";
        request.CreatedUtc = DateTime.UtcNow;
        request.NextRunUtc = scheduleCalculator.ComputeNextRunUtc(request.CronExpression, DateTime.UtcNow);

        await scheduleRepository.CreateAsync(request, ct);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.AdvisoryScanScheduled,
                DataJson = JsonSerializer.Serialize(new { scheduleId = request.ScheduleId, request.Name }),
            },
            ct);

        return Ok(request);
    }

    /// <summary>Lists all advisory schedules for the current scope.</summary>
    [HttpGet("schedules")]
    [ProducesResponseType(typeof(IReadOnlyList<AdvisoryScanSchedule>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdvisoryScanSchedule>>> ListSchedules(CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();

        IReadOnlyList<AdvisoryScanSchedule> result = await scheduleRepository.ListByScopeAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            ct);

        return Ok(result);
    }

    /// <summary>Returns recent execution rows for a schedule in scope.</summary>
    /// <param name="scheduleId">Schedule to load history for.</param>
    /// <param name="take">Maximum rows (newest <see cref="AdvisoryScanExecution.StartedUtc"/> first).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Execution history, or 404 when the schedule is missing or not in the caller’s scope.</returns>
    [HttpGet("schedules/{scheduleId:guid}/executions")]
    [ProducesResponseType(typeof(IReadOnlyList<AdvisoryScanExecution>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AdvisoryScanExecution>>> ListExecutions(
        Guid scheduleId,
        [FromQuery] int take = 30,
        CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 200);
        AdvisoryScanSchedule? schedule = await scheduleRepository.GetByIdAsync(scheduleId, ct);
        if (schedule is null)
            return NotFound();

        ScopeContext scope = scopeProvider.GetCurrentScope();
        if (!MatchesScope(schedule, scope))
            return NotFound();

        IReadOnlyList<AdvisoryScanExecution> items = await executionRepository.ListByScheduleAsync(scheduleId, take, ct);
        return Ok(items);
    }

    /// <summary>Runs the advisory pipeline immediately for the schedule (same path as the background worker).</summary>
    /// <param name="scheduleId">Schedule to run.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 when the runner was invoked; 404 when the schedule is unknown or out of scope.</returns>
    /// <remarks>Advances <see cref="AdvisoryScanSchedule.LastRunUtc"/> / <see cref="AdvisoryScanSchedule.NextRunUtc"/> like a scheduled tick.</remarks>
    [HttpPost("schedules/{scheduleId:guid}/run")]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RunNow(Guid scheduleId, CancellationToken ct = default)
    {
        AdvisoryScanSchedule? schedule = await scheduleRepository.GetByIdAsync(scheduleId, ct);
        if (schedule is null)
            return NotFound();

        ScopeContext scope = scopeProvider.GetCurrentScope();
        if (!MatchesScope(schedule, scope))
            return NotFound();

        await scanRunner.RunScheduleAsync(schedule, ct);
        return Ok();
    }

    /// <summary>Lists recent architecture digests for the scope (newest first, capped by <paramref name="take"/>).</summary>
    /// <param name="take">Maximum digests to return (default 20).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Persisted <see cref="ArchitectureDigest"/> rows from <see cref="IArchitectureDigestRepository.ListByScopeAsync"/>.</returns>
    /// <remarks>Populated by scheduled/on-demand scans via <c>AdvisoryScanRunner</c> after <see cref="IArchitectureDigestBuilder.Build"/>.</remarks>
    [HttpGet("digests")]
    [ProducesResponseType(typeof(IReadOnlyList<ArchitectureDigest>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ArchitectureDigest>>> ListDigests(
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 200);
        ScopeContext scope = scopeProvider.GetCurrentScope();

        IReadOnlyList<ArchitectureDigest> digests = await digestRepository.ListByScopeAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            take,
            ct);

        return Ok(digests);
    }

    /// <summary>Gets a single digest by id when it belongs to the current scope.</summary>
    /// <param name="digestId">Primary key of the digest.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The digest body when found and scope matches.</returns>
    /// <remarks>Returns 404 when the id is unknown or tenant/workspace/project do not match the caller’s scope.</remarks>
    [HttpGet("digests/{digestId:guid}")]
    [ProducesResponseType(typeof(ArchitectureDigest), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArchitectureDigest>> GetDigest(Guid digestId, CancellationToken ct = default)
    {
        ArchitectureDigest? digest = await digestRepository.GetByIdAsync(digestId, ct);
        if (digest is null)
            return NotFound();

        ScopeContext scope = scopeProvider.GetCurrentScope();
        if (digest.TenantId != scope.TenantId ||
            digest.WorkspaceId != scope.WorkspaceId ||
            digest.ProjectId != scope.ProjectId)
            return NotFound();

        return Ok(digest);
    }

    private static bool MatchesScope(AdvisoryScanSchedule schedule, ScopeContext scope) =>
        schedule.TenantId == scope.TenantId &&
        schedule.WorkspaceId == scope.WorkspaceId &&
        schedule.ProjectId == scope.ProjectId;
}
