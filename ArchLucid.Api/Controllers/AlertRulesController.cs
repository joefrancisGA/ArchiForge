using System.Text.Json;

using ArchLucid.Core.Authorization;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Alerts;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers;

/// <summary>
/// CRUD-style API for simple (metric) <see cref="AlertRule"/> rows scoped to the caller’s tenant/workspace/project.
/// </summary>
/// <remarks>
/// Create uses FluentValidation (<see cref="ArchLucid.Api.Validators.AlertRuleBodyValidator"/>). Rules are later filtered at evaluation time by effective policy packs
/// (<c>PolicyPackGovernanceFilter.FilterAlertRules</c>). Mutations require <see cref="ArchLucidPolicies.ExecuteAuthority"/>.
/// </remarks>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/alert-rules")]
[EnableRateLimiting("fixed")]
public sealed class AlertRulesController(
    IScopeContextProvider scopeProvider,
    IAlertRuleRepository ruleRepository,
    IAuditService auditService)
    : ControllerBase
{
    /// <summary>Creates a rule with new id, scope from <see cref="IScopeContextProvider"/>, and default metadata JSON.</summary>
    [HttpPost]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(AlertRule), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create([FromBody] AlertRule? rule, CancellationToken ct = default)
    {
        if (rule is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        ScopeContext scope = scopeProvider.GetCurrentScope();

        rule.RuleId = Guid.NewGuid();
        rule.TenantId = scope.TenantId;
        rule.WorkspaceId = scope.WorkspaceId;
        rule.ProjectId = scope.ProjectId;
        rule.CreatedUtc = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(rule.MetadataJson))
            rule.MetadataJson = "{}";

        await ruleRepository.CreateAsync(rule, ct);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.AlertRuleCreated,
                DataJson = JsonSerializer.Serialize(new
                {
                    ruleId = rule.RuleId,
                    rule.Name,
                    rule.RuleType,
                }),
            },
            ct);

        return Ok(rule);
    }

    /// <summary>Lists all simple alert rules for the current scope (enabled and disabled).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AlertRule>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AlertRule>>> List(CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();

        IReadOnlyList<AlertRule> rules = await ruleRepository.ListByScopeAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            ct);

        return Ok(rules);
    }
}
