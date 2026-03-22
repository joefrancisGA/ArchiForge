using System.Security.Claims;
using System.Text.Json;
using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.Contracts;
using ArchiForge.Core.Audit;
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

[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("api/advisory")]
[EnableRateLimiting("fixed")]
public sealed class AdvisoryController : ControllerBase
{
    private readonly IAuthorityQueryService _authorityQueryService;
    private readonly IComparisonService _comparisonService;
    private readonly IImprovementAdvisorService _improvementAdvisorService;
    private readonly IScopeContextProvider _scopeProvider;
    private readonly IRecommendationWorkflowService _recommendationWorkflowService;
    private readonly IRecommendationRepository _recommendationRepository;
    private readonly IAuditService _auditService;

    public AdvisoryController(
        IAuthorityQueryService authorityQueryService,
        IComparisonService comparisonService,
        IImprovementAdvisorService improvementAdvisorService,
        IScopeContextProvider scopeProvider,
        IRecommendationWorkflowService recommendationWorkflowService,
        IRecommendationRepository recommendationRepository,
        IAuditService auditService)
    {
        _authorityQueryService = authorityQueryService;
        _comparisonService = comparisonService;
        _improvementAdvisorService = improvementAdvisorService;
        _scopeProvider = scopeProvider;
        _recommendationWorkflowService = recommendationWorkflowService;
        _recommendationRepository = recommendationRepository;
        _auditService = auditService;
    }

    [HttpGet("runs/{runId:guid}/improvements")]
    [ProducesResponseType(typeof(ImprovementPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ImprovementPlanResponse>> GetImprovements(
        Guid runId,
        [FromQuery] Guid? compareToRunId = null,
        CancellationToken ct = default)
    {
        var scope = _scopeProvider.GetCurrentScope();
        var run = await _authorityQueryService.GetRunDetailAsync(scope, runId, ct);
        if (run?.GoldenManifest is null)
            return NotFound();

        var findings = run.FindingsSnapshot ?? CreateEmptyFindings(run.GoldenManifest);

        ImprovementPlan plan;
        if (compareToRunId.HasValue)
        {
            var baseRun = await _authorityQueryService.GetRunDetailAsync(scope, compareToRunId.Value, ct);
            if (baseRun?.GoldenManifest is null)
                return NotFound();

            var comparison = _comparisonService.Compare(baseRun.GoldenManifest, run.GoldenManifest);

            plan = await _improvementAdvisorService.GeneratePlanAsync(
                run.GoldenManifest,
                findings,
                comparison,
                ct);
        }
        else
        {
            plan = await _improvementAdvisorService.GeneratePlanAsync(
                run.GoldenManifest,
                findings,
                ct);
        }

        await _recommendationWorkflowService.PersistPlanAsync(
            plan,
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            ct);

        await _auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.RecommendationGenerated,
                RunId = plan.RunId,
                DataJson = JsonSerializer.Serialize(new { recommendationCount = plan.Recommendations.Count }),
            },
            ct);

        return Ok(ToResponse(plan));
    }

    [HttpGet("runs/{runId:guid}/recommendations")]
    [ProducesResponseType(typeof(IReadOnlyList<RecommendationRecordResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RecommendationRecordResponse>>> ListRecommendations(
        Guid runId,
        CancellationToken ct = default)
    {
        var scope = _scopeProvider.GetCurrentScope();

        var items = await _recommendationRepository.ListByRunAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            runId,
            ct);

        return Ok(items.Select(ToRecordResponse).ToList());
    }

    [HttpPost("recommendations/{recommendationId:guid}/action")]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(RecommendationRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecommendationRecordResponse>> ApplyRecommendationAction(
        Guid recommendationId,
        [FromBody] RecommendationActionRequest request,
        CancellationToken ct = default)
    {
        if (!IsKnownRecommendationAction(request.Action))
            return BadRequest(new { error = "Unknown or missing action." });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        var userName = User.Identity?.Name ?? "unknown";

        var updated = await _recommendationWorkflowService.ApplyActionAsync(
            recommendationId,
            userId,
            userName,
            request,
            ct);

        if (updated is null)
            return NotFound();

        var eventType = request.Action switch
        {
            RecommendationActionType.Accept => AuditEventTypes.RecommendationAccepted,
            RecommendationActionType.Reject => AuditEventTypes.RecommendationRejected,
            RecommendationActionType.Defer => AuditEventTypes.RecommendationDeferred,
            RecommendationActionType.MarkImplemented => AuditEventTypes.RecommendationImplemented,
            _ => "RecommendationAction"
        };

        await _auditService.LogAsync(
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
