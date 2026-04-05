using System.Diagnostics;
using System.Text.Json;

using ArchiForge.ContextIngestion.Models;
using ArchiForge.Core.Audit;
using ArchiForge.Core.Authority;
using ArchiForge.Core.Diagnostics;
using ArchiForge.Core.Integration;
using ArchiForge.Core.Scoping;
using ArchiForge.Persistence.Integration;
using ArchiForge.Persistence.Interfaces;
using ArchiForge.Persistence.Models;
using ArchiForge.Persistence.Orchestration.Pipeline;
using ArchiForge.Persistence.Retrieval;
using ArchiForge.Persistence.Serialization;
using ArchiForge.Persistence.Transactions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchiForge.Persistence.Orchestration;

/// <summary>
/// <see cref="IAuthorityRunOrchestrator"/> implementation coordinating ingestion, knowledge graph, findings, decisioning, artifact synthesis, audit, and post-commit retrieval indexing.
/// </summary>
public sealed class AuthorityRunOrchestrator(
    IArchiForgeUnitOfWorkFactory unitOfWorkFactory,
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
    private const string AuthorityRunCompletedEventType = "com.archiforge.authority.run.completed";
    /// <inheritdoc />
    public async Task<RunRecord> ExecuteAsync(
        ContextIngestionRequest request,
        CancellationToken cancellationToken = default,
        string? evidenceBundleIdForDeferredWork = null)
    {
        await using IArchiForgeUnitOfWork uow = await unitOfWorkFactory.CreateAsync(cancellationToken);

        Guid? pipelineRunIdForDiagnostics = null;

        try
        {
            ScopeContext scope = scopeContextProvider.GetCurrentScope();
            RunRecord run = new()
            {
                RunId = Guid.NewGuid(),
                ProjectId = request.ProjectId,
                Description = request.Description,
                CreatedUtc = DateTime.UtcNow
            };
            ApplyScope(run, scope);

            using Activity? runActivity = ArchiForgeInstrumentation.AuthorityRun.StartActivity();
            runActivity?.SetTag("archiforge.run_id", run.RunId.ToString("D"));

            string logicalCorrelation =
                ActivityCorrelation.FindTagValueInChain(runActivity?.Parent, ActivityCorrelation.LogicalCorrelationIdTag)
                ?? run.RunId.ToString("D");
            runActivity?.SetTag(ActivityCorrelation.LogicalCorrelationIdTag, logicalCorrelation);

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
        await using IArchiForgeUnitOfWork uow = await unitOfWorkFactory.CreateAsync(cancellationToken);

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

            using Activity? runActivity = ArchiForgeInstrumentation.AuthorityRun.StartActivity();
            runActivity?.SetTag("archiforge.run_id", run.RunId.ToString("D"));

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
        Decisioning.Models.DecisionTrace trace,
        ScopeContext scope,
        IArchiForgeUnitOfWork uow,
        CancellationToken ct)
    {
        IntegrationEventsOptions integrationOpts = integrationEventsOptions.CurrentValue;

        bool useTransactionalIntegrationOutbox =
            integrationOpts.TransactionalOutboxEnabled && uow.SupportsExternalTransaction;

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

        if (useTransactionalIntegrationOutbox)
        {
            byte[] integrationPayload = SerializeAuthorityRunCompletedPayload(run, manifest.ManifestId, scope);
            string integrationMessageId = BuildAuthorityRunCompletedMessageId(run.RunId);

            await integrationEventOutbox.EnqueueAsync(
                run.RunId,
                AuthorityRunCompletedEventType,
                integrationMessageId,
                integrationPayload,
                scope.TenantId,
                scope.WorkspaceId,
                scope.ProjectId,
                uow.Connection,
                uow.Transaction,
                ct);
        }

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
                trace.DecisionTraceId);
        }

        ArchiForgeInstrumentation.AuthorityRunsCompletedTotal.Add(1);

        if (!useTransactionalIntegrationOutbox)
        {
            await TryPublishAuthorityRunCompletedAsync(run, manifest.ManifestId, scope, ct);
        }

        return run;
    }

    private async Task TryPublishAuthorityRunCompletedAsync(
        RunRecord run,
        Guid manifestId,
        ScopeContext scope,
        CancellationToken ct)
    {
        try
        {
            byte[] json = SerializeAuthorityRunCompletedPayload(run, manifestId, scope);
            string messageId = BuildAuthorityRunCompletedMessageId(run.RunId);

            await integrationEventPublisher.PublishAsync(AuthorityRunCompletedEventType, json, messageId, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(
                    ex,
                    "Integration event publish failed after authority run completed. RunId={RunId}",
                    run.RunId);
            }
        }
    }

    private static byte[] SerializeAuthorityRunCompletedPayload(RunRecord run, Guid manifestId, ScopeContext scope)
    {
        return JsonSerializer.SerializeToUtf8Bytes(
            new
            {
                runId = run.RunId,
                manifestId,
                tenantId = scope.TenantId,
                workspaceId = scope.WorkspaceId,
                projectId = scope.ProjectId,
            });
    }

    private static string BuildAuthorityRunCompletedMessageId(Guid runId)
    {
        return $"{runId:D}:{AuthorityRunCompletedEventType}";
    }

    private async Task SaveRunAsync(RunRecord run, IArchiForgeUnitOfWork uow, CancellationToken ct)
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
