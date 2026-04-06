using System.Text.Json;

using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Core.Audit;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Alerts.Tuning;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

/// <summary>
/// HTTP API for threshold recommendation (simulation + noise scoring) scoped to the caller’s tenant/workspace/project.
/// </summary>
/// <remarks>
/// Stamps scope ids onto <see cref="ThresholdRecommendationRequest.BaseSimpleRule"/> / <see cref="ThresholdRecommendationRequest.BaseCompositeRule"/> before tuning.
/// </remarks>
[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/alert-tuning")]
[EnableRateLimiting("fixed")]
public sealed class AlertTuningController(
    IScopeContextProvider scopeProvider,
    IThresholdRecommendationService thresholdRecommendationService,
    IAuditService auditService)
    : ControllerBase
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>Invokes <see cref="IThresholdRecommendationService.RecommendAsync"/> and audits candidate/recommended threshold metadata.</summary>
    [HttpPost("recommend-threshold")]
    [ProducesResponseType(typeof(ThresholdRecommendationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecommendThreshold(
        [FromBody] ThresholdRecommendationRequest? request,
        CancellationToken ct = default)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        ScopeContext scope = scopeProvider.GetCurrentScope();
        StampTuningScope(scope, request);

        ThresholdRecommendationResult result = await thresholdRecommendationService.RecommendAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            request,
            ct);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.AlertThresholdRecommendationExecuted,
                DataJson = JsonSerializer.Serialize(new
                {
                    request.RuleKind,
                    request.TunedMetricType,
                    requestedCandidateCount = request.CandidateThresholds.Count,
                    evaluatedCandidateCount = result.Candidates.Count,
                    recommendedThreshold = result.RecommendedCandidate?.Candidate.ThresholdValue,
                }, AuditJsonOptions),
            },
            ct);

        return Ok(result);
    }

    private static void StampTuningScope(ScopeContext scope, ThresholdRecommendationRequest request)
    {
        if (request.BaseSimpleRule is not null)
        {
            request.BaseSimpleRule.TenantId = scope.TenantId;
            request.BaseSimpleRule.WorkspaceId = scope.WorkspaceId;
            request.BaseSimpleRule.ProjectId = scope.ProjectId;
        }

        if (request.BaseCompositeRule is null)
            return;

        request.BaseCompositeRule.TenantId = scope.TenantId;
        request.BaseCompositeRule.WorkspaceId = scope.WorkspaceId;
        request.BaseCompositeRule.ProjectId = scope.ProjectId;
    }
}
