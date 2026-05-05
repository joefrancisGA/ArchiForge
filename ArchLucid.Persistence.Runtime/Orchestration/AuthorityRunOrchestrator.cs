using System.Diagnostics;
using System.Text.Json;

using ArchLucid.ContextIngestion.Models;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Authority;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Integration;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Transactions;
using ArchLucid.Decisioning.Models;
using ArchLucid.KnowledgeGraph.Interfaces;
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
///     <see cref="IAuthorityRunOrchestrator" /> implementation coordinating ingestion, knowledge graph, findings,
///     decisioning, artifact synthesis, audit, and post-commit retrieval indexing.
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
    IOptionsMonitor<AuthorityPipelineOptions> authorityPipelineOptions,
    IOptionsMonitor<PublicSiteOptions> publicSiteOptions,
    IGraphSnapshotProjectionCache graphSnapshotProjectionCache,
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
            TimeSpan pipelineTimeout = authorityPipelineOptions.CurrentValue.PipelineTimeout;
            using CancellationTokenSource
                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            if (pipelineTimeout > TimeSpan.Zero)

                linkedCts.CancelAfter(pipelineTimeout);


            CancellationToken pipelineCt = linkedCts.Token;

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
                ActivityCorrelation.FindTagValueInChain(runActivity?.Parent,
                    ActivityCorrelation.LogicalCorrelationIdTag)
                ?? run.RunId.ToString("D");
            runActivity?.SetTag(ActivityCorrelation.LogicalCorrelationIdTag, logicalCorrelation);

            using IDisposable _ = LogContext.PushProperty("CorrelationId", logicalCorrelation);

            run.OtelTraceId = Activity.Current?.TraceId.ToString();

            await SaveRunAsync(run, uow, pipelineCt);

            ArchLucidInstrumentation.RunsCreatedTotal.Add(1);

            pipelineRunIdForDiagnostics = run.RunId;

            if (logger.IsEnabled(LogLevel.Information))

                logger.LogInformation(
                    "Authority pipeline started: RunId={RunId}, ProjectId={ProjectId}, TenantId={TenantId}, WorkspaceId={WorkspaceId}",
                    run.RunId,
                    LogSanitizer.Sanitize(request.ProjectId),
                    scope.TenantId,
                    scope.WorkspaceId);


            await auditService.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.RunStarted,
                    RunId = run.RunId,
                    DataJson = JsonSerializer.Serialize(
                        new { run.ProjectId, Queued = false },
                        AuditJsonSerializationOptions.Instance)
                },
                pipelineCt);

            request.RunId = run.RunId;

            bool queue = await asyncAuthorityPipelineModeResolver.ShouldQueueContextAndGraphStagesAsync(pipelineCt)
                         && !string.IsNullOrWhiteSpace(evidenceBundleIdForDeferredWork);

            if (queue)
            {
                string deferredEvidenceBundleId = evidenceBundleIdForDeferredWork!.Trim();

                AuthorityPipelineWorkPayload payload = new()
                {
                    ContextIngestionRequest = request, EvidenceBundleId = deferredEvidenceBundleId
                };

                await authorityPipelineWorkRepository.EnqueueAsync(
                    run.RunId,
                    scope.TenantId,
                    scope.WorkspaceId,
                    scope.ProjectId,
                    AuthorityPipelineWorkPayloadJson.Serialize(payload),
                    pipelineCt);

                await uow.CommitAsync(pipelineCt);

                await auditService.LogAsync(
                    new AuditEvent
                    {
                        EventType = AuditEventTypes.RunStarted,
                        RunId = run.RunId,
                        DataJson = JsonSerializer.Serialize(
                            new { run.ProjectId, Queued = true },
                            AuditJsonSerializationOptions.Instance)
                    },
                    pipelineCt);

                if (logger.IsEnabled(LogLevel.Information))

                    logger.LogInformation(
                        "Authority pipeline deferred (queued): RunId={RunId}, ProjectId={ProjectId}",
                        run.RunId,
                        LogSanitizer.Sanitize(request.ProjectId));


                return run;
            }

            AuthorityPipelineContext ctx = new()
            {
                Run = run,
                Request = request,
                UnitOfWork = uow,
                Scope = scope,
                RunActivity = runActivity
            };

            await pipelineStagesExecutor.ExecuteAfterRunPersistedAsync(ctx, pipelineCt);

            return await FinalizeCommittedPipelineAsync(
                run,
                ctx.ContextSnapshot!,
                ctx.FindingsSnapshot!,
                ctx.Manifest!,
                ctx.Trace!,
                scope,
                uow,
                pipelineCt);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await uow.RollbackAsync(cancellationToken);

            logger.LogError(
                "Authority pipeline timed out after {PipelineTimeout}. RunId={RunId}",
                authorityPipelineOptions.CurrentValue.PipelineTimeout,
                pipelineRunIdForDiagnostics);

            ArchLucidInstrumentation.PipelineTimeoutsTotal.Add(1);

            throw;
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
                throw new InvalidOperationException(
                    $"Run '{request.RunId:D}' was not found for queued authority completion.");

            if (existing.ContextSnapshotId is not null)
            {
                if (logger.IsEnabled(LogLevel.Information))

                    logger.LogInformation(
                        "Queued authority completion skipped (already has context): RunId={RunId}",
                        request.RunId);


                return existing;
            }

            TimeSpan pipelineTimeout = authorityPipelineOptions.CurrentValue.PipelineTimeout;
            using CancellationTokenSource
                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            if (pipelineTimeout > TimeSpan.Zero)

                linkedCts.CancelAfter(pipelineTimeout);


            CancellationToken pipelineCt = linkedCts.Token;

            RunRecord run = existing;

            using Activity? runActivity = ArchLucidInstrumentation.AuthorityRun.StartActivity();
            runActivity?.SetTag("archlucid.run_id", run.RunId.ToString("D"));

            string logicalCorrelation =
                ActivityCorrelation.FindTagValueInChain(runActivity?.Parent,
                    ActivityCorrelation.LogicalCorrelationIdTag)
                ?? run.RunId.ToString("D");
            runActivity?.SetTag(ActivityCorrelation.LogicalCorrelationIdTag, logicalCorrelation);

            using IDisposable serilogCorrelation = LogContext.PushProperty("CorrelationId", logicalCorrelation);

            AuthorityPipelineContext ctx = new()
            {
                Run = run,
                Request = request,
                UnitOfWork = uow,
                Scope = scope,
                RunActivity = runActivity
            };

            await auditService.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.RunStarted,
                    RunId = run.RunId,
                    DataJson = JsonSerializer.Serialize(
                        new { run.ProjectId, Queued = true, ResumedFromQueue = true },
                        AuditJsonSerializationOptions.Instance)
                },
                pipelineCt);

            await pipelineStagesExecutor.ExecuteAfterRunPersistedAsync(ctx, pipelineCt);

            return await FinalizeCommittedPipelineAsync(
                run,
                ctx.ContextSnapshot!,
                ctx.FindingsSnapshot!,
                ctx.Manifest!,
                ctx.Trace!,
                scope,
                uow,
                pipelineCt);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await uow.RollbackAsync(cancellationToken);

            logger.LogError(
                "Queued authority pipeline timed out after {PipelineTimeout}. RunId={RunId}",
                authorityPipelineOptions.CurrentValue.PipelineTimeout,
                pipelineRunIdForDiagnostics);

            ArchLucidInstrumentation.PipelineTimeoutsTotal.Add(1);

            throw;
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
        FindingsSnapshot findingsSnapshot,
        ManifestDocument manifest,
        DecisionTrace trace,
        ScopeContext scope,
        IArchLucidUnitOfWork uow,
        CancellationToken ct)
    {
        if (uow.SupportsExternalTransaction)

            await retrievalIndexingOutbox.EnqueueAsync(
                run.RunId,
                scope.TenantId,
                scope.WorkspaceId,
                scope.ProjectId,
                uow.Connection,
                uow.Transaction,
                ct);

        else

            await retrievalIndexingOutbox.EnqueueAsync(
                run.RunId,
                scope.TenantId,
                scope.WorkspaceId,
                scope.ProjectId,
                ct);


        string integrationMessageId = BuildAuthorityRunCompletedMessageId(run.RunId);
        string publicBaseUrl = NormalizePublicSiteBaseUrl(publicSiteOptions.CurrentValue.BaseUrl);
        Guid? previousRunId = await TryResolvePreviousCommittedGoldenRunIdAsync(scope, run, ct);
        object[] findingLinks = BuildAuthorityRunCompletedFindingLinks(run.RunId, findingsSnapshot.Findings, publicBaseUrl);
        object integrationPayload = new
        {
            schemaVersion = 1,
            runId = run.RunId,
            manifestId = manifest.ManifestId,
            tenantId = scope.TenantId,
            workspaceId = scope.WorkspaceId,
            projectId = scope.ProjectId,
            previousRunId,
            findings = findingLinks
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

        if (run.GraphSnapshotId is Guid graphSnapshotId)
            graphSnapshotProjectionCache.Invalidate(scope, run.RunId, graphSnapshotId);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.RunCompleted,
                RunId = run.RunId,
                ManifestId = run.GoldenManifestId,
                DataJson = JsonSerializer.Serialize(
                    new { run.GoldenManifestId, run.ArtifactBundleId, run.DecisionTraceId },
                    AuditJsonSerializationOptions.Instance)
            },
            ct);

        if (logger.IsEnabled(LogLevel.Information))

            logger.LogInformation(
                "Authority pipeline completed: RunId={RunId}, ManifestId={ManifestId}, ContextSnapshotId={ContextSnapshotId}, FindingsSnapshotId={FindingsSnapshotId}, DecisionTraceId={DecisionTraceId}",
                run.RunId,
                manifest.ManifestId,
                contextSnapshot.SnapshotId,
                findingsSnapshot.FindingsSnapshotId,
                trace.RequireRuleAudit().DecisionTraceId);


        ArchLucidInstrumentation.AuthorityRunsCompletedTotal.Add(1);

        return run;
    }

    private static string BuildAuthorityRunCompletedMessageId(Guid runId)
    {
        return $"{runId:D}:{IntegrationEventTypes.AuthorityRunCompletedV1}";
    }

    private static string NormalizePublicSiteBaseUrl(string? raw)
    {
        const string fallback = "https://archlucid.net";

        if (string.IsNullOrWhiteSpace(raw))
            return fallback;

        string trimmed = raw.Trim().TrimEnd('/');

        return trimmed.Length == 0 ? fallback : trimmed;
    }

    private async Task<Guid?> TryResolvePreviousCommittedGoldenRunIdAsync(ScopeContext scope, RunRecord run, CancellationToken ct)
    {
        IReadOnlyList<RunRecord> recent =
            await runRepository.ListByProjectAsync(scope, run.ProjectId, 100, ct) ?? [];

        foreach (RunRecord candidate in recent)
        {
            if (candidate.RunId == run.RunId)
                continue;


            if (candidate.ArchivedUtc is not null)
                continue;


            if (candidate.GoldenManifestId is null)
                continue;

            return candidate.RunId;
        }

        return null;
    }

    /// <summary>Per-finding deep links for integration consumers (webhooks, SIEM enrichment).</summary>
    private static object[] BuildAuthorityRunCompletedFindingLinks(Guid runId, List<Finding> findings, string publicBaseUrl)
    {
        if (findings.Count == 0)
            return [];

        List<object> rows = [];

        foreach (Finding f in findings)
        {
            if (f is null)
                continue;

            if (string.IsNullOrWhiteSpace(f.FindingId))
                continue;

            string id = f.FindingId.Trim();
            string deepLink = $"{publicBaseUrl}/runs/{runId:D}/findings/{Uri.EscapeDataString(id)}";
            rows.Add(new { findingId = id, deepLinkUrl = deepLink, severity = f.Severity.ToString() });
        }

        return [.. rows];
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
