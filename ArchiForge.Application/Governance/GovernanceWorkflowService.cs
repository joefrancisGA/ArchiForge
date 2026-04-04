using System.Transactions;

using ArchiForge.Application.Common;
using ArchiForge.Contracts.Architecture;
using ArchiForge.Contracts.Governance;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Persistence.Data.Repositories;

using Microsoft.Extensions.Logging;

namespace ArchiForge.Application.Governance;

/// <summary>
/// Default implementation of <see cref="IGovernanceWorkflowService"/> backed by
/// <see cref="IGovernanceApprovalRequestRepository"/>,
/// <see cref="IGovernancePromotionRecordRepository"/>, and
/// <see cref="IGovernanceEnvironmentActivationRepository"/>.
/// </summary>
public sealed class GovernanceWorkflowService(
    IGovernanceApprovalRequestRepository approvalRepo,
    IGovernancePromotionRecordRepository promotionRepo,
    IGovernanceEnvironmentActivationRepository activationRepo,
    IRunDetailQueryService runDetailQueryService,
    IBaselineMutationAuditService baselineMutationAudit,
    ILogger<GovernanceWorkflowService> logger)
    : IGovernanceWorkflowService
{
    /// <inheritdoc />
    public async Task<GovernanceApprovalRequest> SubmitApprovalRequestAsync(
        string runId,
        string manifestVersion,
        string sourceEnvironment,
        string targetEnvironment,
        string requestedBy,
        string? requestComment,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceEnvironment);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetEnvironment);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedBy);

        ArchitectureRunDetail runDetail = await runDetailQueryService.GetRunDetailAsync(runId, cancellationToken)
                                          ?? throw new RunNotFoundException(runId);
        ArchitectureRun run = runDetail.Run;

        GovernanceApprovalRequest request = new()
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

        await baselineMutationAudit
            .RecordAsync(
                AuditEventTypes.Governance.ApprovalRequestSubmitted,
                requestedBy,
                request.ApprovalRequestId,
                $"RunId={runId}; ManifestVersion={manifestVersion}; Source={sourceEnvironment}; Target={targetEnvironment}",
                cancellationToken)
            ;

        if (logger.IsEnabled(LogLevel.Information))
        
            logger.LogInformation(
                "Governance approval request submitted: ApprovalRequestId={ApprovalRequestId}, RunId={RunId}, ManifestVersion={ManifestVersion}",
                request.ApprovalRequestId,
                request.RunId,
                request.ManifestVersion);
        

        return request;
    }

    /// <inheritdoc />
    public async Task<GovernanceApprovalRequest> ApproveAsync(
        string approvalRequestId,
        string reviewedBy,
        string? reviewComment,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approvalRequestId);
        ArgumentException.ThrowIfNullOrWhiteSpace(reviewedBy);

        GovernanceApprovalRequest request = await approvalRepo.GetByIdAsync(approvalRequestId, cancellationToken)
                                            ?? throw new InvalidOperationException($"Approval request '{approvalRequestId}' was not found.");

        if (request.Status is not (GovernanceApprovalStatus.Draft or GovernanceApprovalStatus.Submitted))
        
            throw new InvalidOperationException(
                $"Approval request '{approvalRequestId}' cannot be approved from status '{request.Status}'. " +
                "Approve is only valid from Draft or Submitted.");
        

        request.Status = GovernanceApprovalStatus.Approved;
        request.ReviewedBy = reviewedBy;
        request.ReviewComment = reviewComment;
        request.ReviewedUtc = DateTime.UtcNow;

        await approvalRepo.UpdateAsync(request, cancellationToken);

        await baselineMutationAudit
            .RecordAsync(
                AuditEventTypes.Governance.ApprovalRequestApproved,
                reviewedBy,
                approvalRequestId,
                $"Status={GovernanceApprovalStatus.Approved}",
                cancellationToken)
            ;

        if (logger.IsEnabled(LogLevel.Information))
        
            logger.LogInformation(
                "Governance approval request approved: ApprovalRequestId={ApprovalRequestId}, ReviewedBy={ReviewedBy}",
                request.ApprovalRequestId,
                reviewedBy);
        

        return request;
    }

    /// <inheritdoc />
    public async Task<GovernanceApprovalRequest> RejectAsync(
        string approvalRequestId,
        string reviewedBy,
        string? reviewComment,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approvalRequestId);
        ArgumentException.ThrowIfNullOrWhiteSpace(reviewedBy);

        GovernanceApprovalRequest request = await approvalRepo.GetByIdAsync(approvalRequestId, cancellationToken)
                                            ?? throw new InvalidOperationException($"Approval request '{approvalRequestId}' was not found.");

        if (request.Status is not (GovernanceApprovalStatus.Draft or GovernanceApprovalStatus.Submitted))
        
            throw new InvalidOperationException(
                $"Approval request '{approvalRequestId}' cannot be rejected from status '{request.Status}'. " +
                "Reject is only valid from Draft or Submitted.");
        

        request.Status = GovernanceApprovalStatus.Rejected;
        request.ReviewedBy = reviewedBy;
        request.ReviewComment = reviewComment;
        request.ReviewedUtc = DateTime.UtcNow;

        await approvalRepo.UpdateAsync(request, cancellationToken);

        await baselineMutationAudit
            .RecordAsync(
                AuditEventTypes.Governance.ApprovalRequestRejected,
                reviewedBy,
                approvalRequestId,
                $"Status={GovernanceApprovalStatus.Rejected}",
                cancellationToken)
            ;

        if (logger.IsEnabled(LogLevel.Information))
        
            logger.LogInformation(
                "Governance approval request rejected: ApprovalRequestId={ApprovalRequestId}, ReviewedBy={ReviewedBy}",
                request.ApprovalRequestId,
                reviewedBy);
        

        return request;
    }

    /// <inheritdoc />
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
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceEnvironment);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetEnvironment);
        ArgumentException.ThrowIfNullOrWhiteSpace(promotedBy);

        _ = await runDetailQueryService.GetRunDetailAsync(runId, cancellationToken)
            ?? throw new RunNotFoundException(runId);

        if (string.Equals(targetEnvironment, GovernanceEnvironment.Prod, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(approvalRequestId))
            
                throw new InvalidOperationException(
                    "Promotion to prod requires an approved approval request. Provide an approvalRequestId.");
            

            GovernanceApprovalRequest? approvalRequest = await approvalRepo.GetByIdAsync(approvalRequestId, cancellationToken);
            if (approvalRequest?.Status != GovernanceApprovalStatus.Approved)
            
                throw new InvalidOperationException(
                    $"Promotion to prod requires an approved approval request. " +
                    $"Approval request '{approvalRequestId}' has status '{approvalRequest?.Status ?? "not found"}'.");
            

            if (!string.Equals(approvalRequest.RunId, runId, StringComparison.Ordinal))
            
                throw new InvalidOperationException(
                    $"Approval request '{approvalRequestId}' was issued for run '{approvalRequest.RunId}', " +
                    $"not '{runId}'. Use an approval request that matches the promoted run.");
            

            if (!string.Equals(approvalRequest.ManifestVersion, manifestVersion, StringComparison.Ordinal))
            
                throw new InvalidOperationException(
                    $"Approval request '{approvalRequestId}' was issued for manifest version '{approvalRequest.ManifestVersion}', " +
                    $"not '{manifestVersion}'. Use an approval request that matches the promoted manifest version.");
            

            if (!string.Equals(approvalRequest.TargetEnvironment, targetEnvironment, StringComparison.OrdinalIgnoreCase))
            
                throw new InvalidOperationException(
                    $"Approval request '{approvalRequestId}' targets environment '{approvalRequest.TargetEnvironment}', " +
                    $"not '{targetEnvironment}'. Use an approval request that matches the target environment.");
            

            approvalRequest.Status = GovernanceApprovalStatus.Promoted;
            await approvalRepo.UpdateAsync(approvalRequest, cancellationToken);
        }

        GovernancePromotionRecord record = new()
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

        await baselineMutationAudit
            .RecordAsync(
                AuditEventTypes.Governance.ManifestPromoted,
                promotedBy,
                record.PromotionRecordId,
                $"RunId={runId}; ManifestVersion={manifestVersion}; {sourceEnvironment}->{targetEnvironment}",
                cancellationToken)
            ;

        if (logger.IsEnabled(LogLevel.Information))
        
            logger.LogInformation(
                "Manifest promoted: PromotionRecordId={PromotionRecordId}, RunId={RunId}, ManifestVersion={ManifestVersion}, Target={TargetEnvironment}",
                record.PromotionRecordId,
                record.RunId,
                record.ManifestVersion,
                record.TargetEnvironment);
        

        return record;
    }

    /// <inheritdoc />
    public async Task<GovernanceEnvironmentActivation> ActivateAsync(
        string runId,
        string manifestVersion,
        string environment,
        string activatedBy,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(environment);
        ArgumentException.ThrowIfNullOrWhiteSpace(activatedBy);

        _ = await runDetailQueryService.GetRunDetailAsync(runId, cancellationToken)
            ?? throw new RunNotFoundException(runId);

        IReadOnlyList<GovernanceEnvironmentActivation> existing = await activationRepo.GetByEnvironmentAsync(environment, cancellationToken);

        GovernanceEnvironmentActivation activation = new()
        {
            RunId = runId,
            ManifestVersion = manifestVersion,
            Environment = environment,
            IsActive = true,
            ActivatedUtc = DateTime.UtcNow
        };

        using (TransactionScope scope = new(
            TransactionScopeOption.Required,
            TransactionScopeAsyncFlowOption.Enabled))
        {
            foreach (GovernanceEnvironmentActivation active in existing.Where(a => a.IsActive))
            {
                active.IsActive = false;
                await activationRepo.UpdateAsync(active, cancellationToken);
            }

            await activationRepo.CreateAsync(activation, cancellationToken);
            scope.Complete();
        }

        await baselineMutationAudit
            .RecordAsync(
                AuditEventTypes.Governance.EnvironmentActivated,
                activatedBy,
                activation.ActivationId,
                $"RunId={runId}; ManifestVersion={manifestVersion}; Environment={environment}",
                cancellationToken)
            ;

        if (logger.IsEnabled(LogLevel.Information))
        
            logger.LogInformation(
                "Environment activated: ActivationId={ActivationId}, RunId={RunId}, ManifestVersion={ManifestVersion}, Environment={Environment}",
                activation.ActivationId,
                activation.RunId,
                activation.ManifestVersion,
                activation.Environment);
        

        return activation;
    }
}
