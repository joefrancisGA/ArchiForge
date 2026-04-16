using System.Text.Json;

using ArchLucid.Api.Models.Tenancy;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.Controllers.Tenancy;

/// <summary>Self-service trial status for the tenant in <see cref="IScopeContextProvider"/> scope.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/tenant")]
public sealed class TenantTrialController(
    ITenantRepository tenantRepository,
    IScopeContextProvider scopeProvider,
    IAuditService auditService) : ControllerBase
{
    private readonly ITenantRepository _tenantRepository =
        tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));

    private readonly IScopeContextProvider _scopeProvider =
        scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));

    private readonly IAuditService _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));

    /// <summary>Returns trial window metadata when the tenant row was provisioned via self-service bootstrap.</summary>
    [HttpGet("trial-status")]
    [Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
    [ProducesResponseType(typeof(TenantTrialStatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTrialStatusAsync(CancellationToken cancellationToken)
    {
        ScopeContext scope = _scopeProvider.GetCurrentScope();
        TenantRecord? tenant = await _tenantRepository.GetByIdAsync(scope.TenantId, cancellationToken);

        if (tenant is null)
            return this.NotFoundProblem("Tenant not found.", ProblemTypes.ResourceNotFound);

        if (string.IsNullOrWhiteSpace(tenant.TrialStatus))
        {
            return Ok(
                new TenantTrialStatusResponse
                {
                    Status = "None",
                    TrialRunsUsed = tenant.TrialRunsUsed,
                    TrialSeatsUsed = tenant.TrialSeatsUsed,
                });
        }

        int? daysRemaining = null;

        if (tenant.TrialExpiresUtc is { } expires)
        {
            double totalDays = (expires - DateTimeOffset.UtcNow).TotalDays;
            daysRemaining = (int)Math.Floor(totalDays);

            if (daysRemaining < 0)
                daysRemaining = 0;
        }

        return Ok(
            new TenantTrialStatusResponse
            {
                Status = tenant.TrialStatus,
                TrialStartUtc = tenant.TrialStartUtc,
                TrialExpiresUtc = tenant.TrialExpiresUtc,
                DaysRemaining = daysRemaining,
                TrialRunsUsed = tenant.TrialRunsUsed,
                TrialRunsLimit = tenant.TrialRunsLimit,
                TrialSeatsUsed = tenant.TrialSeatsUsed,
                TrialSeatsLimit = tenant.TrialSeatsLimit,
                TrialSampleRunId = tenant.TrialSampleRunId,
            });
    }

    /// <summary>Marks an active trial as converted (billing bridge placeholder).</summary>
    [HttpPost("convert")]
    [Authorize(Policy = ArchLucidPolicies.AdminAuthority)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ConvertTrialAsync(
        [FromBody] TenantTrialConvertRequest? body,
        CancellationToken cancellationToken)
    {
        ScopeContext scope = _scopeProvider.GetCurrentScope();
        TenantRecord? tenant = await _tenantRepository.GetByIdAsync(scope.TenantId, cancellationToken);

        if (tenant is null)
            return this.NotFoundProblem("Tenant not found.", ProblemTypes.ResourceNotFound);

        if (!string.Equals(tenant.TrialStatus, TrialLifecycleStatus.Active, StringComparison.Ordinal))
            return this.ConflictProblem("Tenant is not on an active self-service trial.", ProblemTypes.Conflict);

        await _tenantRepository.MarkTrialConvertedAsync(tenant.Id, cancellationToken);

        string actor = User.Identity?.Name ?? "admin";

        await _auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.TenantTrialConverted,
                ActorUserId = actor,
                ActorUserName = actor,
                TenantId = tenant.Id,
                WorkspaceId = scope.WorkspaceId,
                ProjectId = scope.ProjectId,
                DataJson = JsonSerializer.Serialize(
                    new
                    {
                        targetTier = body?.TargetTier,
                    }),
            },
            cancellationToken);

        return NoContent();
    }
}
