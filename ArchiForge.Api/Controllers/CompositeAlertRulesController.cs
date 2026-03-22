using System.Text.Json;
using ArchiForge.Api.Auth.Models;
using ArchiForge.Core.Audit;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Alerts.Composite;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/composite-alert-rules")]
[EnableRateLimiting("fixed")]
public sealed class CompositeAlertRulesController(
    IScopeContextProvider scopeProvider,
    ICompositeAlertRuleRepository repository,
    IAuditService auditService)
    : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(CompositeAlertRule), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompositeAlertRule>> Create(
        [FromBody] CompositeAlertRule rule,
        CancellationToken ct = default)
    {
        var scope = scopeProvider.GetCurrentScope();

        rule.CompositeRuleId = Guid.NewGuid();
        rule.TenantId = scope.TenantId;
        rule.WorkspaceId = scope.WorkspaceId;
        rule.ProjectId = scope.ProjectId;
        rule.CreatedUtc = DateTime.UtcNow;

        foreach (var c in rule.Conditions)
        {
            if (c.ConditionId == Guid.Empty)
                c.ConditionId = Guid.NewGuid();
        }

        await repository.CreateAsync(rule, ct);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.CompositeAlertRuleCreated,
                DataJson = JsonSerializer.Serialize(new
                {
                    rule.CompositeRuleId,
                    rule.Name,
                    rule.Operator,
                    conditionCount = rule.Conditions.Count,
                }),
            },
            ct);

        return Ok(rule);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CompositeAlertRule>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CompositeAlertRule>>> List(CancellationToken ct = default)
    {
        var scope = scopeProvider.GetCurrentScope();

        var result = await repository.ListByScopeAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            ct);

        return Ok(result);
    }
}
