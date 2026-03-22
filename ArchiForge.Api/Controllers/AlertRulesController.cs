using System.Text.Json;
using ArchiForge.Api.Auth.Models;
using ArchiForge.Core.Audit;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Alerts;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/alert-rules")]
[EnableRateLimiting("fixed")]
public sealed class AlertRulesController(
    IScopeContextProvider scopeProvider,
    IAlertRuleRepository ruleRepository,
    IAuditService auditService)
    : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(AlertRule), StatusCodes.Status200OK)]
    public async Task<ActionResult<AlertRule>> Create([FromBody] AlertRule rule, CancellationToken ct = default)
    {
        var scope = scopeProvider.GetCurrentScope();

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

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AlertRule>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AlertRule>>> List(CancellationToken ct = default)
    {
        var scope = scopeProvider.GetCurrentScope();

        var rules = await ruleRepository.ListByScopeAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            ct);

        return Ok(rules);
    }
}
