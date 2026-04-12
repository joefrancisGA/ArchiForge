using System.Data;
using System.Text.Json;

using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Governance;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Application.Common;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Integration;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Transactions;
using ArchLucid.Persistence;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Serialization;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Governance;

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
    IAuditService auditService,
    IScopeContextProvider scopeContextProvider,
    IIntegrationEventPublisher integrationEventPublisher,
    IIntegrationEventOutboxRepository integrationEventOutbox,
    IOptionsMonitor<IntegrationEventsOptions> integrationEventsOptions,
    IArchLucidUnitOfWorkFactory unitOfWorkFactory,
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
        bool dryRun = false,
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

        if (dryRun)
            return request;

        await approvalRepo.CreateAsync(request, cancellationToken);

        await baselineMutationAudit
            .RecordAsync(
                AuditEventTypes.Baseline.Governance.ApprovalRequestSubmitted,
                requestedBy,
                request.ApprovalRequestId,
                $"RunId={runId}; ManifestVersion={manifestVersion}; Source={sourceEnvironment}; Target={targetEnvironment}",
                cancellationToken)
            ;

        Guid? auditRunId = Guid.TryParse(request.RunId, out Guid submittedRunGuid) ? submittedRunGuid : null;
        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = Core.Audit.AuditEventTypes.GovernanceApprovalSubmitted,
                RunId = auditRunId,
                DataJson = JsonSerializer.Serialize(
                    new
                    {
                        approvalRequestId = request.ApprovalRequestId,
                        runId = request.RunId,
                        manifestVersion = request.ManifestVersion,
                        sourceEnvironment = request.SourceEnvironment,
                        targetEnvironment = request.TargetEnvironment,
                    },
                    AuditJsonSerializationOptions.Instance),
            },
            cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Governance approval request submitted: ApprovalRequestId={ApprovalRequestId}, RunId={RunId}, ManifestVersion={ManifestVersion}",
                request.ApprovalRequestId,
                request.RunId,
                request.ManifestVersion);
        }

        await TryPublishGovernanceApprovalSubmittedAsync(request, cancellationToken);

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

        await EnforceSegregationOfDutiesForReviewAsync(request, approvalRequestId, reviewedBy, cancellationToken);

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
                AuditEventTypes.Baseline.Governance.ApprovalRequestApproved,
                reviewedBy,
                approvalRequestId,
                $"Status={GovernanceApprovalStatus.Approved}",
                cancellationToken)
            ;

        Guid? approvedRunId = Guid.TryParse(request.RunId, out Guid approvedRunGuid) ? approvedRunGuid : null;
        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = Core.Audit.AuditEventTypes.GovernanceApprovalApproved,
                RunId = approvedRunId,
                DataJson = JsonSerializer.Serialize(
                    new
                    {
                        approvalRequestId = request.ApprovalRequestId,
                        runId = request.RunId,
                        reviewedBy,
                        reviewComment = request.ReviewComment,
                    },
                    AuditJsonSerializationOptions.Instance),
            },
            cancellationToken);

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

        await EnforceSegregationOfDutiesForReviewAsync(request, approvalRequestId, reviewedBy, cancellationToken);

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
                AuditEventTypes.Baseline.Governance.ApprovalRequestRejected,
                reviewedBy,
                approvalRequestId,
                $"Status={GovernanceApprovalStatus.Rejected}",
                cancellationToken)
            ;

        Guid? rejectedRunId = Guid.TryParse(request.RunId, out Guid rejectedRunGuid) ? rejectedRunGuid : null;
        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = Core.Audit.AuditEventTypes.GovernanceApprovalRejected,
                RunId = rejectedRunId,
                DataJson = JsonSerializer.Serialize(
                    new
                    {
                        approvalRequestId = request.ApprovalRequestId,
                        runId = request.RunId,
                        reviewedBy,
                        reviewComment = request.ReviewComment,
                    },
                    AuditJsonSerializationOptions.Instance),
            },
            cancellationToken);

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
        bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceEnvironment);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetEnvironment);
        ArgumentException.ThrowIfNullOrWhiteSpace(promotedBy);

        _ = await runDetailQueryService.GetRunDetailAsync(runId, cancellationToken)
            ?? throw new RunNotFoundException(runId);

        GovernanceApprovalRequest? prodApprovalToMarkPromoted = null;

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


            prodApprovalToMarkPromoted = approvalRequest;
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

        if (dryRun)
            return record;

        if (prodApprovalToMarkPromoted is not null)
        {
            prodApprovalToMarkPromoted.Status = GovernanceApprovalStatus.Promoted;
            await approvalRepo.UpdateAsync(prodApprovalToMarkPromoted, cancellationToken);
        }

        await promotionRepo.CreateAsync(record, cancellationToken);

        await baselineMutationAudit
            .RecordAsync(
                AuditEventTypes.Baseline.Governance.ManifestPromoted,
                promotedBy,
                record.PromotionRecordId,
                $"RunId={runId}; ManifestVersion={manifestVersion}; {sourceEnvironment}->{targetEnvironment}",
                cancellationToken)
            ;

        Guid? promotedRunId = Guid.TryParse(record.RunId, out Guid promotedRunGuid) ? promotedRunGuid : null;
        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = Core.Audit.AuditEventTypes.GovernanceManifestPromoted,
                RunId = promotedRunId,
                DataJson = JsonSerializer.Serialize(
                    new
                    {
                        promotionRecordId = record.PromotionRecordId,
                        runId = record.RunId,
                        manifestVersion = record.ManifestVersion,
                        sourceEnvironment = record.SourceEnvironment,
                        targetEnvironment = record.TargetEnvironment,
                        approvalRequestId = record.ApprovalRequestId,
                    },
                    AuditJsonSerializationOptions.Instance),
            },
            cancellationToken);

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

        await using IArchLucidUnitOfWork uow = await unitOfWorkFactory.CreateAsync(cancellationToken);

        IntegrationEventsOptions integrationOpts = integrationEventsOptions.CurrentValue;
        bool enqueuePromotionInSqlTx =
            integrationOpts.TransactionalOutboxEnabled && uow.SupportsExternalTransaction;

        try
        {
            if (uow.SupportsExternalTransaction)
            {
                foreach (GovernanceEnvironmentActivation active in existing.Where(a => a.IsActive))
                {
                    active.IsActive = false;
                    await activationRepo.UpdateAsync(active, cancellationToken, uow.Connection, uow.Transaction);
                }

                await activationRepo.CreateAsync(activation, cancellationToken, uow.Connection, uow.Transaction);

                if (enqueuePromotionInSqlTx)
                {
                    await TryPublishGovernancePromotionActivatedAsync(
                        activation,
                        activatedBy,
                        uow.Connection,
                        uow.Transaction,
                        cancellationToken);
                }
            }
            else
            {
                foreach (GovernanceEnvironmentActivation active in existing.Where(a => a.IsActive))
                {
                    active.IsActive = false;
                    await activationRepo.UpdateAsync(active, cancellationToken);
                }

                await activationRepo.CreateAsync(activation, cancellationToken);
            }

            await uow.CommitAsync(cancellationToken);
        }
        catch
        {
            await uow.RollbackAsync(cancellationToken);
            throw;
        }

        await baselineMutationAudit
            .RecordAsync(
                AuditEventTypes.Baseline.Governance.EnvironmentActivated,
                activatedBy,
                activation.ActivationId,
                $"RunId={runId}; ManifestVersion={manifestVersion}; Environment={environment}",
                cancellationToken)
            ;

        Guid? activationRunId = Guid.TryParse(activation.RunId, out Guid activationRunGuid) ? activationRunGuid : null;
        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = Core.Audit.AuditEventTypes.GovernanceEnvironmentActivated,
                RunId = activationRunId,
                DataJson = JsonSerializer.Serialize(
                    new
                    {
                        activationId = activation.ActivationId,
                        runId = activation.RunId,
                        manifestVersion = activation.ManifestVersion,
                        environment = activation.Environment,
                        activatedBy,
                    },
                    AuditJsonSerializationOptions.Instance),
            },
            cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Environment activated: ActivationId={ActivationId}, RunId={RunId}, ManifestVersion={ManifestVersion}, Environment={Environment}",
                activation.ActivationId,
                activation.RunId,
                activation.ManifestVersion,
                activation.Environment);
        }

        if (!enqueuePromotionInSqlTx)
        {
            await TryPublishGovernancePromotionActivatedAsync(
                activation,
                activatedBy,
                connection: null,
                transaction: null,
                cancellationToken);
        }

        return activation;
    }

    private async Task EnforceSegregationOfDutiesForReviewAsync(
        GovernanceApprovalRequest request,
        string approvalRequestId,
        string reviewedBy,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(request.RequestedBy, reviewedBy, StringComparison.OrdinalIgnoreCase))
            return;

        Guid? auditRunId = Guid.TryParse(request.RunId, out Guid runGuid) ? runGuid : null;
        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.GovernanceSelfApprovalBlocked,
                RunId = auditRunId,
                DataJson = JsonSerializer.Serialize(
                    new
                    {
                        approvalRequestId,
                        requestedBy = request.RequestedBy,
                        attemptedReviewerBy = reviewedBy,
                    },
                    AuditJsonSerializationOptions.Instance),
            },
            cancellationToken);

        throw new GovernanceSelfApprovalException(approvalRequestId, reviewedBy);
    }

    private Task TryPublishGovernanceApprovalSubmittedAsync(
        GovernanceApprovalRequest request,
        CancellationToken cancellationToken)
    {
        ScopeContext scope = scopeContextProvider.GetCurrentScope();

        object payload = new
        {
            schemaVersion = 1,
            tenantId = scope.TenantId,
            workspaceId = scope.WorkspaceId,
            projectId = scope.ProjectId,
            approvalRequestId = request.ApprovalRequestId,
            runId = request.RunId,
            manifestVersion = request.ManifestVersion,
            sourceEnvironment = request.SourceEnvironment,
            targetEnvironment = request.TargetEnvironment,
            requestedBy = request.RequestedBy,
        };

        string messageId = $"{request.ApprovalRequestId}:{IntegrationEventTypes.GovernanceApprovalSubmittedV1}";

        Guid? runKey = Guid.TryParse(request.RunId, out Guid rid) ? rid : null;

        return OutboxAwareIntegrationEventPublishing.TryPublishOrEnqueueAsync(
            integrationEventOutbox,
            integrationEventPublisher,
            integrationEventsOptions.CurrentValue,
            logger,
            IntegrationEventTypes.GovernanceApprovalSubmittedV1,
            payload,
            messageId,
            runKey,
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            connection: null,
            transaction: null,
            cancellationToken);
    }

    private Task TryPublishGovernancePromotionActivatedAsync(
        GovernanceEnvironmentActivation activation,
        string activatedBy,
        IDbConnection? connection,
        IDbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        ScopeContext scope = scopeContextProvider.GetCurrentScope();

        object payload = new
        {
            schemaVersion = 1,
            tenantId = scope.TenantId,
            workspaceId = scope.WorkspaceId,
            projectId = scope.ProjectId,
            activationId = activation.ActivationId,
            runId = activation.RunId,
            manifestVersion = activation.ManifestVersion,
            environment = activation.Environment,
            activatedBy,
            activatedUtc = activation.ActivatedUtc,
        };

        string messageId = $"{activation.ActivationId}:{IntegrationEventTypes.GovernancePromotionActivatedV1}";

        Guid? runKey = Guid.TryParse(activation.RunId, out Guid rid) ? rid : null;

        return OutboxAwareIntegrationEventPublishing.TryPublishOrEnqueueAsync(
            integrationEventOutbox,
            integrationEventPublisher,
            integrationEventsOptions.CurrentValue,
            logger,
            IntegrationEventTypes.GovernancePromotionActivatedV1,
            payload,
            messageId,
            runKey,
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            connection,
            transaction,
            cancellationToken);
    }
}
