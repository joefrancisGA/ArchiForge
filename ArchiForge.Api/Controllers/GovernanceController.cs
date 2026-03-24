using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.Models;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Application;
using ArchiForge.Application.Governance;
using ArchiForge.Data.Repositories;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace ArchiForge.Api.Controllers;

/// <summary>
/// Governance workflow: approval requests, promotions, and environment activations for run manifests.
/// </summary>
[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/governance")]
[EnableRateLimiting("fixed")]
public sealed class GovernanceController(
    IGovernanceWorkflowService workflowService,
    IGovernanceApprovalRequestRepository approvalRepo,
    IGovernancePromotionRecordRepository promotionRepo,
    IGovernanceEnvironmentActivationRepository activationRepo,
    ILogger<GovernanceController> logger)
    : ControllerBase
{
    [HttpPost("approval-requests")]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitApprovalRequest(
        [FromBody] CreateGovernanceApprovalRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        var requestedBy = User.Identity?.Name ?? "anonymous";

        try
        {
            var result = await workflowService.SubmitApprovalRequestAsync(
                request.RunId,
                request.ManifestVersion,
                request.SourceEnvironment,
                request.TargetEnvironment,
                requestedBy,
                request.RequestComment,
                cancellationToken);

            return Ok(result);
        }
        catch (RunNotFoundException ex)
        {
            logger.LogWarning(ex, "SubmitApprovalRequest failed: run not found.");
            return this.NotFoundProblem(ex.Message, ProblemTypes.RunNotFound);
        }
    }

    [HttpPost("approval-requests/{approvalRequestId}/approve")]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Approve(
        [FromRoute] string approvalRequestId,
        [FromBody] ApproveGovernanceRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        var reviewedBy = string.IsNullOrWhiteSpace(request.ReviewedBy)
            ? (User.Identity?.Name ?? "anonymous")
            : request.ReviewedBy;

        try
        {
            var result = await workflowService.ApproveAsync(
                approvalRequestId,
                reviewedBy,
                request.ReviewComment,
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Approve failed for approval request '{ApprovalRequestId}'.", approvalRequestId);
            return this.BadRequestProblem(ex.Message, ProblemTypes.BadRequest);
        }
    }

    [HttpPost("approval-requests/{approvalRequestId}/reject")]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reject(
        [FromRoute] string approvalRequestId,
        [FromBody] RejectGovernanceRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        var reviewedBy = string.IsNullOrWhiteSpace(request.ReviewedBy)
            ? (User.Identity?.Name ?? "anonymous")
            : request.ReviewedBy;

        try
        {
            var result = await workflowService.RejectAsync(
                approvalRequestId,
                reviewedBy,
                request.ReviewComment,
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Reject failed for approval request '{ApprovalRequestId}'.", approvalRequestId);
            return this.BadRequestProblem(ex.Message, ProblemTypes.BadRequest);
        }
    }

    [HttpPost("promotions")]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Promote(
        [FromBody] CreateGovernancePromotionRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        var promotedBy = string.IsNullOrWhiteSpace(request.PromotedBy)
            ? (User.Identity?.Name ?? "anonymous")
            : request.PromotedBy;

        try
        {
            var result = await workflowService.PromoteAsync(
                request.RunId,
                request.ManifestVersion,
                request.SourceEnvironment,
                request.TargetEnvironment,
                promotedBy,
                request.ApprovalRequestId,
                request.Notes,
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Promote failed for run '{RunId}'.", request.RunId);
            return this.BadRequestProblem(ex.Message, ProblemTypes.BadRequest);
        }
    }

    [HttpPost("activations")]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
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
            var result = await workflowService.ActivateAsync(
                request.RunId,
                request.ManifestVersion,
                request.Environment,
                cancellationToken);

            return Ok(result);
        }
        catch (RunNotFoundException ex)
        {
            logger.LogWarning(ex, "Activate failed: run not found.");
            return this.NotFoundProblem(ex.Message, ProblemTypes.RunNotFound);
        }
    }

    [HttpGet("runs/{runId}/approval-requests")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApprovalRequests(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        var items = await approvalRepo.GetByRunIdAsync(runId, cancellationToken);
        return Ok(items);
    }

    [HttpGet("runs/{runId}/promotions")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPromotions(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        var items = await promotionRepo.GetByRunIdAsync(runId, cancellationToken);
        return Ok(items);
    }

    [HttpGet("runs/{runId}/activations")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActivations(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        var items = await activationRepo.GetByRunIdAsync(runId, cancellationToken);
        return Ok(items);
    }
}
