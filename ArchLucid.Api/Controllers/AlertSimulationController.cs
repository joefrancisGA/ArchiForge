using System.Text.Json;

using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Core.Audit;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Alerts;
using ArchiForge.Decisioning.Alerts.Composite;
using ArchiForge.Decisioning.Alerts.Simulation;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

/// <summary>
/// HTTP API for alert rule what-if simulation and A/B comparison over the caller’s scope (read authority).
/// </summary>
/// <remarks>
/// Stamps tenant/workspace/project on embedded rule DTOs from <see cref="IScopeContextProvider"/> before invoking <see cref="IRuleSimulationService"/>.
/// </remarks>
[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/alert-simulation")]
[EnableRateLimiting("fixed")]
public sealed class AlertSimulationController(
    IScopeContextProvider scopeProvider,
    IRuleSimulationService simulationService,
    IAuditService auditService)
    : ControllerBase
{
    /// <summary>Runs <see cref="IRuleSimulationService.SimulateAsync"/> and audits aggregate counts.</summary>
    [HttpPost("simulate")]
    [ProducesResponseType(typeof(RuleSimulationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Simulate(
        [FromBody] RuleSimulationRequest? request,
        CancellationToken ct = default)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        ScopeContext scope = scopeProvider.GetCurrentScope();
        StampSimulationScope(scope, request);

        RuleSimulationResult result = await simulationService.SimulateAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            request,
            ct);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.AlertRuleSimulationExecuted,
                DataJson = JsonSerializer.Serialize(new
                {
                    request.RuleKind,
                    result.EvaluatedRunCount,
                    result.MatchedCount,
                    result.WouldCreateCount,
                    result.WouldSuppressCount,
                }),
            },
            ct);

        return Ok(result);
    }

    /// <summary>Runs <see cref="IRuleSimulationService.CompareCandidatesAsync"/> and audits would-create counts per candidate.</summary>
    [HttpPost("compare-candidates")]
    [ProducesResponseType(typeof(RuleCandidateComparisonResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompareCandidates(
        [FromBody] RuleCandidateComparisonRequest? request,
        CancellationToken ct = default)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        ScopeContext scope = scopeProvider.GetCurrentScope();
        StampComparisonScope(scope, request);

        RuleCandidateComparisonResult result = await simulationService.CompareCandidatesAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            request,
            ct);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.AlertRuleCandidateComparisonExecuted,
                DataJson = JsonSerializer.Serialize(new
                {
                    request.RuleKind,
                    candidateAWouldCreate = result.CandidateA.WouldCreateCount,
                    candidateBWouldCreate = result.CandidateB.WouldCreateCount,
                }),
            },
            ct);

        return Ok(result);
    }

    private static void StampSimulationScope(ScopeContext scope, RuleSimulationRequest request)
    {
        if (request.SimpleRule is not null)
        {
            request.SimpleRule.TenantId = scope.TenantId;
            request.SimpleRule.WorkspaceId = scope.WorkspaceId;
            request.SimpleRule.ProjectId = scope.ProjectId;
        }

        if (request.CompositeRule is null)
            return;

        request.CompositeRule.TenantId = scope.TenantId;
        request.CompositeRule.WorkspaceId = scope.WorkspaceId;
        request.CompositeRule.ProjectId = scope.ProjectId;
    }

    private static void StampComparisonScope(ScopeContext scope, RuleCandidateComparisonRequest request)
    {
        StampSimple(request.CandidateASimpleRule);
        StampSimple(request.CandidateBSimpleRule);
        StampComposite(request.CandidateACompositeRule);
        StampComposite(request.CandidateBCompositeRule);
        return;

        void StampSimple(AlertRule? r)
        {
            if (r is null)
                return;
            r.TenantId = scope.TenantId;
            r.WorkspaceId = scope.WorkspaceId;
            r.ProjectId = scope.ProjectId;
        }

        void StampComposite(CompositeAlertRule? r)
        {
            if (r is null)
                return;
            r.TenantId = scope.TenantId;
            r.WorkspaceId = scope.WorkspaceId;
            r.ProjectId = scope.ProjectId;
        }
    }
}
