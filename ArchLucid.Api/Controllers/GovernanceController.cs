using ArchLucid.Api.Auth.Models;
using ArchLucid.Api.Http;
using ArchLucid.Api.Models;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application;
using ArchLucid.Application.Common;
using ArchLucid.Application.Governance;
using ArchLucid.Contracts.Governance;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Data.Repositories;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers;

/// <summary>
/// Governance workflow: approval requests, promotions, and environment activations for run manifests.
/// </summary>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/governance")]
[EnableRateLimiting("fixed")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class GovernanceController(
    IGovernanceWorkflowService workflowService,
    IGovernanceApprovalRequestRepository approvalRepo,
    IGovernancePromotionRecordRepository promotionRepo,
    IGovernanceEnvironmentActivationRepository activationRepo,
    IActorContext actorContext,
    IScopeContextProvider scopeContextProvider,
    IGovernanceDashboardService governanceDashboardService,
    IGovernanceLineageService governanceLineageService,
    IGovernanceRationaleService governanceRationaleService,
    IComplianceDriftTrendService complianceDriftTrendService,
    ILogger<GovernanceController> logger)
    : ControllerBase
{
    private readonly IScopeContextProvider _scopeContextProvider =
        scopeContextProvider ?? throw new ArgumentNullException(nameof(scopeContextProvider));

    private readonly IGovernanceDashboardService _governanceDashboardService =
        governanceDashboardService ?? throw new ArgumentNullException(nameof(governanceDashboardService));

    private readonly IComplianceDriftTrendService _complianceDriftTrendService =
        complianceDriftTrendService ?? throw new ArgumentNullException(nameof(complianceDriftTrendService));

    private readonly IGovernanceLineageService _governanceLineageService =
        governanceLineageService ?? throw new ArgumentNullException(nameof(governanceLineageService));

    private readonly IGovernanceRationaleService _governanceRationaleService =
        governanceRationaleService ?? throw new ArgumentNullException(nameof(governanceRationaleService));
    [HttpPost("approval-requests")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(GovernanceApprovalRequest), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitApprovalRequest(
        [FromBody] CreateGovernanceApprovalRequest? request,
        [FromQuery] bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        string requestedBy = actorContext.GetActor();

        try
        {
            GovernanceApprovalRequest result = await workflowService.SubmitApprovalRequestAsync(
                request.RunId,
                request.ManifestVersion,
                request.SourceEnvironment,
                request.TargetEnvironment,
                requestedBy,
                request.RequestComment,
                dryRun,
                cancellationToken);

            if (dryRun)
                Response.Headers[ArchLucidHttpHeaders.DryRun] = "true";

            return Ok(result);
        }
        catch (RunNotFoundException ex)
        {
            logger.LogWarning(ex, "SubmitApprovalRequest failed: run not found.");
            return this.NotFoundProblem(ex.Message, ProblemTypes.RunNotFound);
        }
    }

    [HttpPost("approval-requests/{approvalRequestId}/approve")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(GovernanceApprovalRequest), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Approve(
        [FromRoute] string approvalRequestId,
        [FromBody] ApproveGovernanceRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        string reviewedBy = string.IsNullOrWhiteSpace(request.ReviewedBy)
            ? actorContext.GetActor()
            : request.ReviewedBy;

        try
        {
            GovernanceApprovalRequest result = await workflowService.ApproveAsync(
                approvalRequestId,
                reviewedBy,
                request.ReviewComment,
                cancellationToken);

            return Ok(result);
        }
        catch (GovernanceSelfApprovalException ex)
        {
            logger.LogWarning(ex, "Approve blocked: segregation of duties for approval request '{ApprovalRequestId}'.", approvalRequestId);
            return this.BadRequestProblem(ex.Message, ProblemTypes.GovernanceSelfApproval);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Approve failed for approval request '{ApprovalRequestId}'.", approvalRequestId);
            return this.BadRequestProblem(ex.Message, ProblemTypes.BadRequest);
        }
    }

    [HttpPost("approval-requests/{approvalRequestId}/reject")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(GovernanceApprovalRequest), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reject(
        [FromRoute] string approvalRequestId,
        [FromBody] RejectGovernanceRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        string reviewedBy = string.IsNullOrWhiteSpace(request.ReviewedBy)
            ? actorContext.GetActor()
            : request.ReviewedBy;

        try
        {
            GovernanceApprovalRequest result = await workflowService.RejectAsync(
                approvalRequestId,
                reviewedBy,
                request.ReviewComment,
                cancellationToken);

            return Ok(result);
        }
        catch (GovernanceSelfApprovalException ex)
        {
            logger.LogWarning(ex, "Reject blocked: segregation of duties for approval request '{ApprovalRequestId}'.", approvalRequestId);
            return this.BadRequestProblem(ex.Message, ProblemTypes.GovernanceSelfApproval);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Reject failed for approval request '{ApprovalRequestId}'.", approvalRequestId);
            return this.BadRequestProblem(ex.Message, ProblemTypes.BadRequest);
        }
    }

    [HttpPost("promotions")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(GovernancePromotionRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Promote(
        [FromBody] CreateGovernancePromotionRequest? request,
        [FromQuery] bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        string promotedBy = string.IsNullOrWhiteSpace(request.PromotedBy)
            ? User.Identity?.Name ?? "anonymous"
            : request.PromotedBy;

        try
        {
            GovernancePromotionRecord result = await workflowService.PromoteAsync(
                request.RunId,
                request.ManifestVersion,
                request.SourceEnvironment,
                request.TargetEnvironment,
                promotedBy,
                request.ApprovalRequestId,
                request.Notes,
                dryRun,
                cancellationToken);

            if (dryRun)
                Response.Headers[ArchLucidHttpHeaders.DryRun] = "true";

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Promote failed for run '{RunId}'.", LogSanitizer.Sanitize(request.RunId));
            return this.BadRequestProblem(ex.Message, ProblemTypes.BadRequest);
        }
    }

    [HttpPost("activations")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(GovernanceEnvironmentActivation), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(
        [FromBody] CreateGovernanceActivationRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        try
        {
            GovernanceEnvironmentActivation result = await workflowService.ActivateAsync(
                request.RunId,
                request.ManifestVersion,
                request.Environment,
                actorContext.GetActor(),
                cancellationToken);

            return Ok(result);
        }
        catch (RunNotFoundException ex)
        {
            logger.LogWarning(ex, "Activate failed: run not found.");
            return this.NotFoundProblem(ex.Message, ProblemTypes.RunNotFound);
        }
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(GovernanceDashboardSummary), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] int maxPending = 20,
        [FromQuery] int maxDecisions = 20,
        [FromQuery] int maxChanges = 20,
        CancellationToken cancellationToken = default)
    {
        ScopeContext scope = _scopeContextProvider.GetCurrentScope();

        GovernanceDashboardSummary summary = await _governanceDashboardService.GetDashboardAsync(
            scope.TenantId,
            maxPending,
            maxDecisions,
            maxChanges,
            cancellationToken);

        return Ok(summary);
    }

    [HttpGet("compliance-drift-trend")]
    [ProducesResponseType(typeof(IReadOnlyList<ComplianceDriftTrendPoint>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetComplianceDriftTrend(
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] int bucketMinutes = 1440,
        CancellationToken cancellationToken = default)
    {
        if (fromUtc >= toUtc)
        {
            return this.BadRequestProblem("fromUtc must be before toUtc.", ProblemTypes.BadRequest);
        }

        if (bucketMinutes is < 60 or > 43_200)
        {
            return this.BadRequestProblem("bucketMinutes must be between 60 and 43200.", ProblemTypes.BadRequest);
        }

        ScopeContext scope = _scopeContextProvider.GetCurrentScope();
        TimeSpan bucketSize = TimeSpan.FromMinutes(bucketMinutes);

        IReadOnlyList<ComplianceDriftTrendPoint> points = await _complianceDriftTrendService.GetTrendAsync(
            scope.TenantId,
            fromUtc,
            toUtc,
            bucketSize,
            cancellationToken);

        return Ok(points);
    }

    [HttpGet("approval-requests/{approvalRequestId}/lineage")]
    [ProducesResponseType(typeof(GovernanceLineageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApprovalRequestLineage(
        [FromRoute] string approvalRequestId,
        CancellationToken cancellationToken)
    {
        GovernanceLineageResult? result = await _governanceLineageService.GetApprovalRequestLineageAsync(
            approvalRequestId,
            cancellationToken);

        if (result is null)
        {
            return this.NotFoundProblem(
                $"Approval request '{approvalRequestId}' was not found.",
                ProblemTypes.ResourceNotFound);
        }

        return Ok(result);
    }

    [HttpGet("approval-requests/{approvalRequestId}/rationale")]
    [ProducesResponseType(typeof(GovernanceRationaleResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApprovalRequestRationale(
        [FromRoute] string approvalRequestId,
        CancellationToken cancellationToken)
    {
        GovernanceRationaleResult? result = await _governanceRationaleService.GetApprovalRequestRationaleAsync(
            approvalRequestId,
            cancellationToken);

        if (result is null)
        {
            return this.NotFoundProblem(
                $"Approval request '{approvalRequestId}' was not found.",
                ProblemTypes.ResourceNotFound);
        }

        return Ok(result);
    }

    [HttpGet("runs/{runId}/approval-requests")]
    [ProducesResponseType(typeof(IReadOnlyList<GovernanceApprovalRequest>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApprovalRequests(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<GovernanceApprovalRequest> items = await approvalRepo.GetByRunIdAsync(runId, cancellationToken);
        return Ok(items);
    }

    [HttpGet("runs/{runId}/promotions")]
    [ProducesResponseType(typeof(IReadOnlyList<GovernancePromotionRecord>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPromotions(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<GovernancePromotionRecord> items = await promotionRepo.GetByRunIdAsync(runId, cancellationToken);
        return Ok(items);
    }

    [HttpGet("runs/{runId}/activations")]
    [ProducesResponseType(typeof(IReadOnlyList<GovernanceEnvironmentActivation>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActivations(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<GovernanceEnvironmentActivation> items = await activationRepo.GetByRunIdAsync(runId, cancellationToken);
        return Ok(items);
    }
}
