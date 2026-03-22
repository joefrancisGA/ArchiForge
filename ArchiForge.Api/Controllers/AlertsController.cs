using System.Security.Claims;
using ArchiForge.Api.Auth.Models;
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
[Route("api/alerts")]
[EnableRateLimiting("fixed")]
public sealed class AlertsController : ControllerBase
{
    private readonly IScopeContextProvider _scopeProvider;
    private readonly IAlertRecordRepository _alertRepository;
    private readonly IAlertService _alertService;

    public AlertsController(
        IScopeContextProvider scopeProvider,
        IAlertRecordRepository alertRepository,
        IAlertService alertService)
    {
        _scopeProvider = scopeProvider;
        _alertRepository = alertRepository;
        _alertService = alertService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AlertRecord>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AlertRecord>>> List(
        [FromQuery] string? status = null,
        [FromQuery] int take = 100,
        CancellationToken ct = default)
    {
        var scope = _scopeProvider.GetCurrentScope();

        var alerts = await _alertRepository.ListByScopeAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            status,
            take,
            ct);

        return Ok(alerts);
    }

    [HttpPost("{alertId:guid}/action")]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(AlertRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlertRecord>> ApplyAction(
        Guid alertId,
        [FromBody] AlertActionRequest request,
        CancellationToken ct = default)
    {
        var scope = _scopeProvider.GetCurrentScope();
        var existing = await _alertRepository.GetByIdAsync(alertId, ct);
        if (existing is null || !MatchesScope(existing, scope))
            return NotFound();

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        var userName = User.Identity?.Name ?? "unknown";

        var updated = await _alertService.ApplyActionAsync(
            alertId,
            userId,
            userName,
            request,
            ct);

        return updated is null ? NotFound() : Ok(updated);
    }

    private static bool MatchesScope(AlertRecord alert, ScopeContext scope) =>
        alert.TenantId == scope.TenantId &&
        alert.WorkspaceId == scope.WorkspaceId &&
        alert.ProjectId == scope.ProjectId;
}
