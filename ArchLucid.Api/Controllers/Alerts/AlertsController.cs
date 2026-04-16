using System.Security.Claims;

using ArchLucid.Core.Authorization;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Core.Pagination;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Alerts;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers;

/// <summary>
/// Lists alerts and applies lifecycle actions (acknowledge / resolve / suppress) for the caller’s tenant/workspace/project scope.
/// </summary>
/// <remarks>
/// Scope comes from <see cref="IScopeContextProvider"/>; alert <strong>evaluation</strong> is performed by orchestration paths
/// (<c>AlertService</c> / composite service), not from this controller.
/// </remarks>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/alerts")]
[EnableRateLimiting("fixed")]
public sealed class AlertsController(
    IScopeContextProvider scopeProvider,
    IAlertRecordRepository alertRepository,
    IAlertService alertService)
    : ControllerBase
{
    /// <summary>Lists recent alerts for the current scope, optionally filtered by status.</summary>
    /// <param name="status">When set, restricts to alerts with this status string (repository-defined).</param>
    /// <param name="take">Max rows (capped by repository). Used when <paramref name="page"/> is not set.</param>
    /// <param name="page">One-based page number. When provided, the response is a <see cref="PagedResponse{T}"/>.</param>
    /// <param name="pageSize">Items per page (clamped 1–200; default 50). Only used when <paramref name="page"/> is set.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AlertRecord>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PagedResponse<AlertRecord>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] string? status = null,
        [FromQuery] int take = 100,
        [FromQuery] int? page = null,
        [FromQuery] int pageSize = PaginationDefaults.DefaultPageSize,
        CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();

        if (page.HasValue)
        {
            (int safePage, int safePageSize) = PaginationDefaults.Normalize(page.Value, pageSize);
            int skip = PaginationDefaults.ToSkip(safePage, safePageSize);
            (IReadOnlyList<AlertRecord> items, int total) = await alertRepository.ListByScopePagedAsync(
                scope.TenantId,
                scope.WorkspaceId,
                scope.ProjectId,
                status,
                skip,
                safePageSize,
                ct);

            return Ok(PagedResponseBuilder.FromDatabasePage(items, total, safePage, safePageSize));
        }

        take = Math.Clamp(take, 1, 500);

        IReadOnlyList<AlertRecord> alerts = await alertRepository.ListByScopeAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            status,
            take,
            ct);

        return Ok(alerts);
    }

    /// <summary>Applies an operator action to an alert if it belongs to the current scope.</summary>
    /// <param name="alertId">Target alert id.</param>
    /// <param name="request">Action type and optional comment.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated alert, or 404 when missing or out of scope.</returns>
    [HttpPost("{alertId:guid}/action")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(AlertRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApplyAction(
        Guid alertId,
        [FromBody] AlertActionRequest? request,
        CancellationToken ct = default)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        ScopeContext scope = scopeProvider.GetCurrentScope();
        AlertRecord? existing = await alertRepository.GetByIdAsync(alertId, ct);
        if (existing is null || !MatchesScope(existing, scope))
            return this.NotFoundProblem(
                $"Alert '{alertId}' was not found in the current scope.",
                ProblemTypes.ResourceNotFound);

        string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        string userName = User.Identity?.Name ?? "unknown";

        AlertRecord? updated = await alertService.ApplyActionAsync(
            alertId,
            userId,
            userName,
            request,
            ct);

        if (updated is null)
            return this.NotFoundProblem(
                $"Alert '{alertId}' could not be updated.",
                ProblemTypes.ResourceNotFound);

        return Ok(updated);
    }

    /// <summary>Acknowledges many alerts in the current scope; each id is processed independently (partial success).</summary>
    [HttpPost("acknowledge-batch")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(AlertsAcknowledgeBatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AcknowledgeBatch(
        [FromBody] AlertsAcknowledgeBatchRequest? body,
        CancellationToken ct = default)
    {
        if (body is null)
        {
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);
        }

        if (body.AlertIds is null || body.AlertIds.Count == 0)
        {
            return this.BadRequestProblem("AlertIds must contain at least one id.", ProblemTypes.ValidationFailed);
        }

        if (body.AlertIds.Count > 100)
        {
            return this.BadRequestProblem("At most 100 alert ids are allowed per request.", ProblemTypes.ValidationFailed);
        }

        ScopeContext scope = scopeProvider.GetCurrentScope();
        string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        string userName = User.Identity?.Name ?? "unknown";

        AlertActionRequest action = new()
        {
            Action = AlertActionType.Acknowledge,
            Comment = body.Comment,
        };

        List<AlertsAcknowledgeBatchItemResult> results = [];
        HashSet<Guid> seen = [];

        foreach (Guid alertId in body.AlertIds)
        {
            if (!seen.Add(alertId))
            {
                continue;
            }

            AlertRecord? existing = await alertRepository.GetByIdAsync(alertId, ct);

            if (existing is null || !MatchesScope(existing, scope))
            {
                results.Add(
                    new AlertsAcknowledgeBatchItemResult
                    {
                        AlertId = alertId,
                        Succeeded = false,
                        Message = "Alert not found in the current scope.",
                    });
                continue;
            }

            AlertRecord? updated = await alertService.ApplyActionAsync(alertId, userId, userName, action, ct);

            if (updated is null)
            {
                results.Add(
                    new AlertsAcknowledgeBatchItemResult
                    {
                        AlertId = alertId,
                        Succeeded = false,
                        Message = "Alert could not be acknowledged.",
                    });
                continue;
            }

            results.Add(
                new AlertsAcknowledgeBatchItemResult
                {
                    AlertId = alertId,
                    Succeeded = true,
                });
        }

        return Ok(new AlertsAcknowledgeBatchResponse { Results = results });
    }

    private static bool MatchesScope(AlertRecord alert, ScopeContext scope) =>
        alert.TenantId == scope.TenantId &&
        alert.WorkspaceId == scope.WorkspaceId &&
        alert.ProjectId == scope.ProjectId;
}
