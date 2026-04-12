using System.Diagnostics;
using System.Text.Json;

using ArchLucid.ContextIngestion.Models;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Authority;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Integration;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Transactions;
using ArchLucid.Persistence.Coordination.Retrieval;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Orchestration.Pipeline;
using ArchLucid.Persistence.Serialization;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Serilog.Context;

namespace ArchLucid.Persistence.Orchestration;

/// <summary>
/// <see cref="IAuthorityRunOrchestrator"/> implementation coordinating ingestion, knowledge graph, findings, decisioning, artifact synthesis, audit, and post-commit retrieval indexing.
/// </summary>
public sealed class AuthorityRunOrchestrator(
    IArchLucidUnitOfWorkFactory unitOfWorkFactory,
    IScopeContextProvider scopeContextProvider,
    IAuditService auditService,
    IRunRepository runRepository,
    IAuthorityPipelineStagesExecutor pipelineStagesExecutor,
    IRetrievalIndexingOutboxRepository retrievalIndexingOutbox,
    IAuthorityPipelineWorkRepository authorityPipelineWorkRepository,
    IAsyncAuthorityPipelineModeResolver asyncAuthorityPipelineModeResolver,
    IIntegrationEventPublisher integrationEventPublisher,
    IIntegrationEventOutboxRepository integrationEventOutbox,
    IOptionsMonitor<IntegrationEventsOptions> integrationEventsOptions,
    ILogger<AuthorityRunOrchestrator> logger) : IAuthorityRunOrchestrator
{
    /// <inheritdoc />
    public async Task<RunRecord> ExecuteAsync(
        ContextIngestionRequest request,
        CancellationToken cancellationToken = default,
        string? evidenceBundleIdForDeferredWork = null)
    {
        await using IArchLucidUnitOfWork uow = await unitOfWorkFactory.CreateAsync(cancellationToken);

        Guid? pipelineRunIdForDiagnostics = null;

        try
        {
            ScopeContext scope = scopeContextProvider.GetCurrentScope();
            RunRecord run = new()
            {
                RunId = Guid.NewGuid(),
                ArchitectureRequestId = request.ArchitectureRequestId,
                ProjectId = request.ProjectId,
                Description = request.Description,
                CreatedUtc = DateTime.UtcNow
            };
            ApplyScope(run, scope);

            using Activity? runActivity = ArchLucidInstrumentation.AuthorityRun.StartActivity();
            runActivity?.SetTag("archlucid.run_id", run.RunId.ToString("D"));

            string logicalCorrelation =
                ActivityCorrelation.FindTagValueInChain(runActivity?.Parent, ActivityCorrelation.LogicalCorrelationIdTag)
                ?? run.RunId.ToString("D");
            runActivity?.SetTag(ActivityCorrelation.LogicalCorrelationIdTag, logicalCorrelation);

            using IDisposable _ = LogContext.PushProperty("CorrelationId", logicalCorrelation);

            await SaveRunAsync(run, uow, cancellationToken);

            pipelineRunIdForDiagnostics = run.RunId;

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Authority pipeline started: RunId={RunId}, ProjectId={ProjectId}, TenantId={TenantId}, WorkspaceId={WorkspaceId}",
                    run.RunId,
                    request.ProjectId,
                    scope.TenantId,
                    scope.WorkspaceId);
            }

            await auditService.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.RunStarted,
                    RunId = run.RunId,
                    DataJson = JsonSerializer.Serialize(
                        new
                        {
                            run.ProjectId,
                            Queued = false
                        },
                        AuditJsonSerializationOptions.Instance),
                },
                cancellationToken);

            request.RunId = run.RunId;

            bool queue = await asyncAuthorityPipelineModeResolver.ShouldQueueContextAndGraphStagesAsync(cancellationToken)
                         && !string.IsNullOrWhiteSpace(evidenceBundleIdForDeferredWork);

            if (queue)
            {
                string deferredEvidenceBundleId = evidenceBundleIdForDeferredWork!.Trim();

                AuthorityPipelineWorkPayload payload = new()
                {
                    ContextIngestionRequest = request,
                    EvidenceBundleId = deferredEvidenceBundleId,
                };

                await authorityPipelineWorkRepository.EnqueueAsync(
                    run.RunId,
                    scope.TenantId,
                    scope.WorkspaceId,
                    scope.ProjectId,
                    AuthorityPipelineWorkPayloadJson.Serialize(payload),
                    cancellationToken);

                await uow.CommitAsync(cancellationToken);

                await auditService.LogAsync(
                    new AuditEvent
                    {
                        EventType = AuditEventTypes.RunStarted,
                        RunId = run.RunId,
                        DataJson = JsonSerializer.Serialize(
                            new
                            {
                                run.ProjectId,
                                Queued = true
                            },
                            AuditJsonSerializationOptions.Instance),
                    },
                    cancellationToken);

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(
                        "Authority pipeline deferred (queued): RunId={RunId}, ProjectId={ProjectId}",
                        run.RunId,
                        request.ProjectId);
                }

                return run;
            }

            AuthorityPipelineContext ctx = new()
            {
                Run = run,
                Request = request,
                UnitOfWork = uow,
                Scope = scope,
                RunActivity = runActivity,
            };

            await pipelineStagesExecutor.ExecuteAfterRunPersistedAsync(ctx, cancellationToken);

            return await FinalizeCommittedPipelineAsync(
                run,
                ctx.ContextSnapshot!,
                ctx.FindingsSnapshot!,
                ctx.Manifest!,
                ctx.Trace!,
                scope,
                uow,
                cancellationToken);
        }
        catch (Exception ex)
        {
            await uow.RollbackAsync(cancellationToken);

            logger.LogError(
                ex,
                "Authority pipeline failed; transaction rolled back. RunId={RunId}",
                pipelineRunIdForDiagnostics);

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<RunRecord> CompleteQueuedAuthorityPipelineAsync(
        ContextIngestionRequest request,
        CancellationToken cancellationToken = default)
    {
        await using IArchLucidUnitOfWork uow = await unitOfWorkFactory.CreateAsync(cancellationToken);

        Guid? pipelineRunIdForDiagnostics = request.RunId;

        try
        {
            ScopeContext scope = scopeContextProvider.GetCurrentScope();
            RunRecord? existing = await runRepository.GetByIdAsync(scope, request.RunId, cancellationToken);
            if (existing is null)
                throw new InvalidOperationException($"Run '{request.RunId:D}' was not found for queued authority completion.");

            if (existing.ContextSnapshotId is not null)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(
                        "Queued authority completion skipped (already has context): RunId={RunId}",
                        request.RunId);
                }

                return existing;
            }

            RunRecord run = existing;

            using Activity? runActivity = ArchLucidInstrumentation.AuthorityRun.StartActivity();
            runActivity?.SetTag("archlucid.run_id", run.RunId.ToString("D"));

            string logicalCorrelation =
                ActivityCorrelation.FindTagValueInChain(runActivity?.Parent, ActivityCorrelation.LogicalCorrelationIdTag)
                ?? run.RunId.ToString("D");
            runActivity?.SetTag(ActivityCorrelation.LogicalCorrelationIdTag, logicalCorrelation);

            using IDisposable serilogCorrelation = LogContext.PushProperty("CorrelationId", logicalCorrelation);

            AuthorityPipelineContext ctx = new()
            {
                Run = run,
                Request = request,
                UnitOfWork = uow,
                Scope = scope,
                RunActivity = runActivity,
            };

            await auditService.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.RunStarted,
                    RunId = run.RunId,
                    DataJson = JsonSerializer.Serialize(
                        new
                        {
                            run.ProjectId,
                            Queued = true,
                            ResumedFromQueue = true
                        },
                        AuditJsonSerializationOptions.Instance),
                },
                cancellationToken);

            await pipelineStagesExecutor.ExecuteAfterRunPersistedAsync(ctx, cancellationToken);

            return await FinalizeCommittedPipelineAsync(
                run,
                ctx.ContextSnapshot!,
                ctx.FindingsSnapshot!,
                ctx.Manifest!,
                ctx.Trace!,
                scope,
                uow,
                cancellationToken);
        }
        catch (Exception ex)
        {
            await uow.RollbackAsync(cancellationToken);

            logger.LogError(
                ex,
                "Queued authority pipeline failed; transaction rolled back. RunId={RunId}",
                pipelineRunIdForDiagnostics);

            throw;
        }
    }

    private async Task<RunRecord> FinalizeCommittedPipelineAsync(
        RunRecord run,
        ContextSnapshot contextSnapshot,
        Decisioning.Models.FindingsSnapshot findingsSnapshot,
        Decisioning.Models.GoldenManifest manifest,
        DecisionTrace trace,
        ScopeContext scope,
        IArchLucidUnitOfWork uow,
        CancellationToken ct)
    {
        if (uow.SupportsExternalTransaction)
        {
            await retrievalIndexingOutbox.EnqueueAsync(
                run.RunId,
                scope.TenantId,
                scope.WorkspaceId,
                scope.ProjectId,
                uow.Connection,
                uow.Transaction,
                ct);
        }
        else
        {
            await retrievalIndexingOutbox.EnqueueAsync(
                run.RunId,
                scope.TenantId,
                scope.WorkspaceId,
                scope.ProjectId,
                ct);
        }

        string integrationMessageId = BuildAuthorityRunCompletedMessageId(run.RunId);
        object integrationPayload = new
        {
            schemaVersion = 1,
            runId = run.RunId,
            manifestId = manifest.ManifestId,
            tenantId = scope.TenantId,
            workspaceId = scope.WorkspaceId,
            projectId = scope.ProjectId,
        };

        await OutboxAwareIntegrationEventPublishing.TryPublishOrEnqueueAsync(
            integrationEventOutbox,
            integrationEventPublisher,
            integrationEventsOptions.CurrentValue,
            logger,
            IntegrationEventTypes.AuthorityRunCompletedV1,
            integrationPayload,
            integrationMessageId,
            run.RunId,
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            uow.SupportsExternalTransaction ? uow.Connection : null,
            uow.SupportsExternalTransaction ? uow.Transaction : null,
            ct);

        await uow.CommitAsync(ct);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.RunCompleted,
                RunId = run.RunId,
                ManifestId = run.GoldenManifestId,
                DataJson = JsonSerializer.Serialize(
                    new
                    {
                        run.GoldenManifestId,
                        run.ArtifactBundleId,
                        run.DecisionTraceId
                    },
                    AuditJsonSerializationOptions.Instance)
            },
            ct);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Authority pipeline completed: RunId={RunId}, ManifestId={ManifestId}, ContextSnapshotId={ContextSnapshotId}, FindingsSnapshotId={FindingsSnapshotId}, DecisionTraceId={DecisionTraceId}",
                run.RunId,
                manifest.ManifestId,
                contextSnapshot.SnapshotId,
                findingsSnapshot.FindingsSnapshotId,
                trace.RequireRuleAudit().DecisionTraceId);
        }

        ArchLucidInstrumentation.AuthorityRunsCompletedTotal.Add(1);

        return run;
    }

    private static string BuildAuthorityRunCompletedMessageId(Guid runId)
    {
        return $"{runId:D}:{IntegrationEventTypes.AuthorityRunCompletedV1}";
    }

    private async Task SaveRunAsync(RunRecord run, IArchLucidUnitOfWork uow, CancellationToken ct)
    {
        if (uow.SupportsExternalTransaction)
            await runRepository.SaveAsync(run, ct, uow.Connection, uow.Transaction);
        else
            await runRepository.SaveAsync(run, ct);
    }

    private static void ApplyScope(RunRecord run, ScopeContext scope)
    {
        run.TenantId = scope.TenantId;
        run.WorkspaceId = scope.WorkspaceId;
        run.ScopeProjectId = scope.ProjectId;
    }
}
