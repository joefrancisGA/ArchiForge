using System.Security.Claims;
using System.Text.Json;

using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.Contracts;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Core.Audit;
using ArchiForge.Core.Comparison;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Advisory.Models;
using ArchiForge.Decisioning.Advisory.Services;
using ArchiForge.Decisioning.Advisory.Workflow;
using ArchiForge.Decisioning.Comparison;
using ArchiForge.Decisioning.Models;
using ArchiForge.Persistence.Queries;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

/// <summary>
/// Advisory workflow HTTP surface: improvement plans from authority runs, persisted recommendations, and operator actions (accept/defer/etc.).
/// </summary>
/// <remarks>
/// Routes are under <c>api/advisory</c> (unversioned path). Uses <see cref="IScopeContextProvider"/> for tenant/workspace/project.
/// Plans feed learning and composite alert metrics; scheduled scans extend this path via <see cref="AdvisorySchedulingController"/> and <c>AdvisoryScanRunner</c>.
/// </remarks>
[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("api/advisory")]
[EnableRateLimiting("fixed")]
public sealed class AdvisoryController(
    IAuthorityQueryService authorityQueryService,
    IComparisonService comparisonService,
    IImprovementAdvisorService improvementAdvisorService,
    IScopeContextProvider scopeProvider,
    IRecommendationWorkflowService recommendationWorkflowService,
    IRecommendationRepository recommendationRepository,
    IAuditService auditService)
    : ControllerBase
{
    /// <summary>
    /// Builds an <see cref="ImprovementPlan"/> from the run’s golden manifest and findings, optionally compared to another run, then persists recommendations for the scope.
    /// </summary>
    /// <param name="runId">Authority run whose golden manifest and findings drive the plan.</param>
    /// <param name="compareToRunId">When set, manifests are compared and diff-based signals are included.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="ImprovementPlanResponse"/> after persisting rows via <see cref="IRecommendationWorkflowService.PersistPlanAsync"/>.</returns>
    /// <remarks>Audits <see cref="AuditEventTypes.RecommendationGenerated"/>. Returns 404 when the run or optional baseline lacks a golden manifest.</remarks>
    [HttpGet("runs/{runId:guid}/improvements")]
    [ProducesResponseType(typeof(ImprovementPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImprovements(
        Guid runId,
        [FromQuery] Guid? compareToRunId = null,
        CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        RunDetailDto? run = await authorityQueryService.GetRunDetailAsync(scope, runId, ct);
        if (run is null)
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);
        if (run.GoldenManifest is null)
            return this.NotFoundProblem($"Run '{runId}' does not have a committed golden manifest.", ProblemTypes.ManifestNotFound);

        FindingsSnapshot findings = run.FindingsSnapshot ?? CreateEmptyFindings(run.GoldenManifest);

        ImprovementPlan plan;
        if (compareToRunId.HasValue)
        {
            RunDetailDto? baseRun = await authorityQueryService.GetRunDetailAsync(scope, compareToRunId.Value, ct);
            if (baseRun is null)
                return this.NotFoundProblem($"Comparison run '{compareToRunId.Value}' was not found.", ProblemTypes.RunNotFound);
            if (baseRun.GoldenManifest is null)
                return this.NotFoundProblem($"Comparison run '{compareToRunId.Value}' does not have a committed golden manifest.", ProblemTypes.ManifestNotFound);

            ComparisonResult comparison = comparisonService.Compare(baseRun.GoldenManifest, run.GoldenManifest);

            plan = await improvementAdvisorService.GeneratePlanAsync(
                run.GoldenManifest,
                findings,
                comparison,
                ct);
        }
        else
        {
            plan = await improvementAdvisorService.GeneratePlanAsync(
                run.GoldenManifest,
                findings,
                ct);
        }

        await recommendationWorkflowService.PersistPlanAsync(
            plan,
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            ct);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.RecommendationGenerated,
                RunId = plan.RunId,
                DataJson = JsonSerializer.Serialize(new { recommendationCount = plan.Recommendations.Count }),
            },
            ct);

        return Ok(ToResponse(plan));
    }

    /// <summary>Lists recommendation rows previously stored for the given run in the current scope.</summary>

    /// <param name="runId">Authority run id; must match persisted <see cref="RecommendationRecord.RunId"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Rows ordered per <see cref="IRecommendationRepository.ListByRunAsync"/>.</returns>
    [HttpGet("runs/{runId:guid}/recommendations")]
    [ProducesResponseType(typeof(IReadOnlyList<RecommendationRecordResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RecommendationRecordResponse>>> ListRecommendations(
        Guid runId,
        CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();

        IReadOnlyList<RecommendationRecord> items = await recommendationRepository.ListByRunAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            runId,
            ct);

        return Ok(items.Select(ToRecordResponse).ToList());
    }

    /// <summary>
    /// Applies accept/reject/defer/implemented to a recommendation; requires execute authority and audits the outcome.
    /// </summary>
    /// <param name="recommendationId">Primary key of the recommendation row.</param>
    /// <param name="request">Must use a <see cref="RecommendationActionRequest.Action"/> value matching <see cref="RecommendationActionType"/> constants.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated <see cref="RecommendationRecordResponse"/>.</returns>
    /// <remarks>400 when action is unknown; 404 when the id does not exist. Audit event type follows the action.</remarks>
    [HttpPost("recommendations/{recommendationId:guid}/action")]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(RecommendationRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApplyRecommendationAction(
        Guid recommendationId,
        [FromBody] RecommendationActionRequest? request,
        CancellationToken ct = default)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        if (!IsKnownRecommendationAction(request.Action))
            return this.BadRequestProblem("Unknown or missing action.", ProblemTypes.ValidationFailed);

        string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        string userName = User.Identity?.Name ?? "unknown";

        RecommendationRecord? updated = await recommendationWorkflowService.ApplyActionAsync(
            recommendationId,
            userId,
            userName,
            request,
            ct);

        if (updated is null)
            return this.NotFoundProblem($"Recommendation '{recommendationId}' was not found.", ProblemTypes.ResourceNotFound);

        string eventType = request.Action switch
        {
            RecommendationActionType.Accept => AuditEventTypes.RecommendationAccepted,
            RecommendationActionType.Reject => AuditEventTypes.RecommendationRejected,
            RecommendationActionType.Defer => AuditEventTypes.RecommendationDeferred,
            RecommendationActionType.MarkImplemented => AuditEventTypes.RecommendationImplemented,
            _ => "RecommendationAction"
        };

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = eventType,
                RunId = updated.RunId,
                DataJson = JsonSerializer.Serialize(new { recommendationId, action = request.Action }),
            },
            ct);

        return Ok(ToRecordResponse(updated));
    }

    private static bool IsKnownRecommendationAction(string? action) =>
        action is RecommendationActionType.Accept
            or RecommendationActionType.Reject
            or RecommendationActionType.Defer
            or RecommendationActionType.MarkImplemented;

    private static FindingsSnapshot CreateEmptyFindings(GoldenManifest manifest) =>
        new()
        {
            SchemaVersion = FindingsSchema.CurrentSnapshotVersion,
            FindingsSnapshotId = manifest.FindingsSnapshotId,
            RunId = manifest.RunId,
            ContextSnapshotId = manifest.ContextSnapshotId,
            GraphSnapshotId = manifest.GraphSnapshotId,
            CreatedUtc = manifest.CreatedUtc,
            Findings = []
        };

    private static ImprovementPlanResponse ToResponse(ImprovementPlan plan) =>
        new()
        {
            RunId = plan.RunId,
            ComparedToRunId = plan.ComparedToRunId,
            GeneratedUtc = plan.GeneratedUtc,
            SummaryNotes = plan.SummaryNotes.ToList(),
            Recommendations = plan.Recommendations.Select(x => new ImprovementRecommendationResponse
            {
                RecommendationId = x.RecommendationId,
                Title = x.Title,
                Category = x.Category,
                Rationale = x.Rationale,
                SuggestedAction = x.SuggestedAction,
                Urgency = x.Urgency,
                ExpectedImpact = x.ExpectedImpact,
                PriorityScore = x.PriorityScore
            }).ToList()
        };

    private static RecommendationRecordResponse ToRecordResponse(RecommendationRecord r) =>
        new()
        {
            RecommendationId = r.RecommendationId,
            TenantId = r.TenantId,
            WorkspaceId = r.WorkspaceId,
            ProjectId = r.ProjectId,
            RunId = r.RunId,
            ComparedToRunId = r.ComparedToRunId,
            Title = r.Title,
            Category = r.Category,
            Rationale = r.Rationale,
            SuggestedAction = r.SuggestedAction,
            Urgency = r.Urgency,
            ExpectedImpact = r.ExpectedImpact,
            PriorityScore = r.PriorityScore,
            Status = r.Status,
            CreatedUtc = r.CreatedUtc,
            LastUpdatedUtc = r.LastUpdatedUtc,
            ReviewedByUserId = r.ReviewedByUserId,
            ReviewedByUserName = r.ReviewedByUserName,
            ReviewComment = r.ReviewComment,
            ResolutionRationale = r.ResolutionRationale,
        };
}
