using System.Text.Json;
using ArchiForge.Api.Auth.Models;
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

[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("api/alert-simulation")]
[EnableRateLimiting("fixed")]
public sealed class AlertSimulationController : ControllerBase
{
    private readonly IScopeContextProvider _scopeProvider;
    private readonly IRuleSimulationService _simulationService;
    private readonly IAuditService _auditService;

    public AlertSimulationController(
        IScopeContextProvider scopeProvider,
        IRuleSimulationService simulationService,
        IAuditService auditService)
    {
        _scopeProvider = scopeProvider;
        _simulationService = simulationService;
        _auditService = auditService;
    }

    [HttpPost("simulate")]
    [ProducesResponseType(typeof(RuleSimulationResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<RuleSimulationResult>> Simulate(
        [FromBody] RuleSimulationRequest request,
        CancellationToken ct = default)
    {
        var scope = _scopeProvider.GetCurrentScope();
        StampSimulationScope(scope, request);

        var result = await _simulationService.SimulateAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            request,
            ct);

        await _auditService.LogAsync(
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

    [HttpPost("compare-candidates")]
    [ProducesResponseType(typeof(RuleCandidateComparisonResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<RuleCandidateComparisonResult>> CompareCandidates(
        [FromBody] RuleCandidateComparisonRequest request,
        CancellationToken ct = default)
    {
        var scope = _scopeProvider.GetCurrentScope();
        StampComparisonScope(scope, request);

        var result = await _simulationService.CompareCandidatesAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            request,
            ct);

        await _auditService.LogAsync(
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

        if (request.CompositeRule is not null)
        {
            request.CompositeRule.TenantId = scope.TenantId;
            request.CompositeRule.WorkspaceId = scope.WorkspaceId;
            request.CompositeRule.ProjectId = scope.ProjectId;
        }
    }

    private static void StampComparisonScope(ScopeContext scope, RuleCandidateComparisonRequest request)
    {
        void stampSimple(AlertRule? r)
        {
            if (r is null) return;
            r.TenantId = scope.TenantId;
            r.WorkspaceId = scope.WorkspaceId;
            r.ProjectId = scope.ProjectId;
        }

        void stampComposite(CompositeAlertRule? r)
        {
            if (r is null) return;
            r.TenantId = scope.TenantId;
            r.WorkspaceId = scope.WorkspaceId;
            r.ProjectId = scope.ProjectId;
        }

        stampSimple(request.CandidateA_SimpleRule);
        stampSimple(request.CandidateB_SimpleRule);
        stampComposite(request.CandidateA_CompositeRule);
        stampComposite(request.CandidateB_CompositeRule);
    }
}
