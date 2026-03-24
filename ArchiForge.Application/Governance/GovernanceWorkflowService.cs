using ArchiForge.Contracts.Governance;
using ArchiForge.Data.Repositories;

using Microsoft.Extensions.Logging;

namespace ArchiForge.Application.Governance;

public sealed class GovernanceWorkflowService(
    IGovernanceApprovalRequestRepository approvalRepo,
    IGovernancePromotionRecordRepository promotionRepo,
    IGovernanceEnvironmentActivationRepository activationRepo,
    IArchitectureRunRepository runRepository,
    ILogger<GovernanceWorkflowService> logger)
    : IGovernanceWorkflowService
{
    public async Task<GovernanceApprovalRequest> SubmitApprovalRequestAsync(
        string runId,
        string manifestVersion,
        string sourceEnvironment,
        string targetEnvironment,
        string requestedBy,
        string? requestComment,
        CancellationToken cancellationToken = default)
    {
        var run = await runRepository.GetByIdAsync(runId, cancellationToken)
            ?? throw new RunNotFoundException(runId);

        var request = new GovernanceApprovalRequest
        {
            RunId = run.RunId,
            ManifestVersion = manifestVersion,
            SourceEnvironment = sourceEnvironment,
            TargetEnvironment = targetEnvironment,
            Status = GovernanceApprovalStatus.Submitted,
            RequestedBy = requestedBy,
            RequestComment = requestComment,
            RequestedUtc = DateTime.UtcNow
        };

        await approvalRepo.CreateAsync(request, cancellationToken);

        logger.LogInformation(
            "Governance approval request submitted: ApprovalRequestId={ApprovalRequestId}, RunId={RunId}, ManifestVersion={ManifestVersion}",
            request.ApprovalRequestId,
            request.RunId,
            request.ManifestVersion);

        return request;
    }

    public async Task<GovernanceApprovalRequest> ApproveAsync(
        string approvalRequestId,
        string reviewedBy,
        string? reviewComment,
        CancellationToken cancellationToken = default)
    {
        var request = await approvalRepo.GetByIdAsync(approvalRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"Approval request '{approvalRequestId}' was not found.");

        if (request.Status is not (GovernanceApprovalStatus.Draft or GovernanceApprovalStatus.Submitted))
        {
            throw new InvalidOperationException(
                $"Approval request '{approvalRequestId}' cannot be approved from status '{request.Status}'. " +
                "Approve is only valid from Draft or Submitted.");
        }

        request.Status = GovernanceApprovalStatus.Approved;
        request.ReviewedBy = reviewedBy;
        request.ReviewComment = reviewComment;
        request.ReviewedUtc = DateTime.UtcNow;

        await approvalRepo.UpdateAsync(request, cancellationToken);

        logger.LogInformation(
            "Governance approval request approved: ApprovalRequestId={ApprovalRequestId}, ReviewedBy={ReviewedBy}",
            request.ApprovalRequestId,
            reviewedBy);

        return request;
    }

    public async Task<GovernanceApprovalRequest> RejectAsync(
        string approvalRequestId,
        string reviewedBy,
        string? reviewComment,
        CancellationToken cancellationToken = default)
    {
        var request = await approvalRepo.GetByIdAsync(approvalRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"Approval request '{approvalRequestId}' was not found.");

        if (request.Status is not (GovernanceApprovalStatus.Draft or GovernanceApprovalStatus.Submitted))
        {
            throw new InvalidOperationException(
                $"Approval request '{approvalRequestId}' cannot be rejected from status '{request.Status}'. " +
                "Reject is only valid from Draft or Submitted.");
        }

        request.Status = GovernanceApprovalStatus.Rejected;
        request.ReviewedBy = reviewedBy;
        request.ReviewComment = reviewComment;
        request.ReviewedUtc = DateTime.UtcNow;

        await approvalRepo.UpdateAsync(request, cancellationToken);

        logger.LogInformation(
            "Governance approval request rejected: ApprovalRequestId={ApprovalRequestId}, ReviewedBy={ReviewedBy}",
            request.ApprovalRequestId,
            reviewedBy);

        return request;
    }

    public async Task<GovernancePromotionRecord> PromoteAsync(
        string runId,
        string manifestVersion,
        string sourceEnvironment,
        string targetEnvironment,
        string promotedBy,
        string? approvalRequestId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(targetEnvironment, GovernanceEnvironment.Prod, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(approvalRequestId))
            {
                throw new InvalidOperationException(
                    "Promotion to prod requires an approved approval request. Provide an approvalRequestId.");
            }

            var approvalRequest = await approvalRepo.GetByIdAsync(approvalRequestId, cancellationToken);
            if (approvalRequest?.Status != GovernanceApprovalStatus.Approved)
            {
                throw new InvalidOperationException(
                    $"Promotion to prod requires an approved approval request. " +
                    $"Approval request '{approvalRequestId}' has status '{approvalRequest?.Status ?? "not found"}'.");
            }

            approvalRequest.Status = GovernanceApprovalStatus.Promoted;
            await approvalRepo.UpdateAsync(approvalRequest, cancellationToken);
        }

        var record = new GovernancePromotionRecord
        {
            RunId = runId,
            ManifestVersion = manifestVersion,
            SourceEnvironment = sourceEnvironment,
            TargetEnvironment = targetEnvironment,
            PromotedBy = promotedBy,
            PromotedUtc = DateTime.UtcNow,
            ApprovalRequestId = approvalRequestId,
            Notes = notes
        };

        await promotionRepo.CreateAsync(record, cancellationToken);

        logger.LogInformation(
            "Manifest promoted: PromotionRecordId={PromotionRecordId}, RunId={RunId}, ManifestVersion={ManifestVersion}, Target={TargetEnvironment}",
            record.PromotionRecordId,
            record.RunId,
            record.ManifestVersion,
            record.TargetEnvironment);

        return record;
    }

    public async Task<GovernanceEnvironmentActivation> ActivateAsync(
        string runId,
        string manifestVersion,
        string environment,
        CancellationToken cancellationToken = default)
    {
        _ = await runRepository.GetByIdAsync(runId, cancellationToken)
            ?? throw new RunNotFoundException(runId);

        var existing = await activationRepo.GetByEnvironmentAsync(environment, cancellationToken);
        foreach (var active in existing.Where(a => a.IsActive))
        {
            active.IsActive = false;
            await activationRepo.UpdateAsync(active, cancellationToken);
        }

        var activation = new GovernanceEnvironmentActivation
        {
            RunId = runId,
            ManifestVersion = manifestVersion,
            Environment = environment,
            IsActive = true,
            ActivatedUtc = DateTime.UtcNow
        };

        await activationRepo.CreateAsync(activation, cancellationToken);

        logger.LogInformation(
            "Environment activated: ActivationId={ActivationId}, RunId={RunId}, ManifestVersion={ManifestVersion}, Environment={Environment}",
            activation.ActivationId,
            activation.RunId,
            activation.ManifestVersion,
            activation.Environment);

        return activation;
    }
}
