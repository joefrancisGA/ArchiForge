using System.Text.Json;

using ArchLucid.Api.Attributes;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;
using ArchLucid.Decisioning.Alerts.Composite;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Alerts;

/// <summary>
///     API to create and list <see cref="CompositeAlertRule" /> definitions (multi-metric AND/OR rules) for the caller’s
///     scope.
/// </summary>
/// <remarks>
///     Validated with <see cref="ArchLucid.Api.Validators.CompositeAlertRuleBodyValidator" /> on create. Child
///     <see cref="AlertRuleCondition.ConditionId" /> values are generated when empty.
///     Evaluated in production by <c>CompositeAlertService</c> after governance filtering. Mutations require
///     <see cref="ArchLucidPolicies.ExecuteAuthority" />.
/// </remarks>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/composite-alert-rules")]
[EnableRateLimiting("fixed")]
[RequiresCommercialTenantTier(TenantTier.Standard)]
public sealed class CompositeAlertRulesController(
    IScopeContextProvider scopeProvider,
    ICompositeAlertRuleRepository repository,
    IAuditService auditService)
    : ControllerBase
{
    /// <summary>Persists the rule and conditions in one repository operation; stamps scope and rule id.</summary>
    [HttpPost]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(CompositeAlertRule), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CompositeAlertRule? rule,
        CancellationToken ct = default)
    {
        if (rule is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        ScopeContext scope = scopeProvider.GetCurrentScope();

        rule.CompositeRuleId = Guid.NewGuid();
        rule.TenantId = scope.TenantId;
        rule.WorkspaceId = scope.WorkspaceId;
        rule.ProjectId = scope.ProjectId;
        rule.CreatedUtc = DateTime.UtcNow;

        foreach (AlertRuleCondition c in rule.Conditions.Where(c => c.ConditionId == Guid.Empty))

            c.ConditionId = Guid.NewGuid();


        await repository.CreateAsync(rule, ct);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.CompositeAlertRuleCreated,
                DataJson = JsonSerializer.Serialize(new
                {
                    rule.CompositeRuleId, rule.Name, rule.Operator, conditionCount = rule.Conditions.Count
                })
            },
            ct);

        return Ok(rule);
    }

    /// <summary>Lists composite rules for the scope including conditions as loaded from persistence.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CompositeAlertRule>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CompositeAlertRule>>> List(CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();

        IReadOnlyList<CompositeAlertRule> result = await repository.ListByScopeAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            ct);

        return Ok(result);
    }
}
