using System.Diagnostics;
using System.Text.Json;

using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.ContextIngestion.Interfaces;
using ArchiForge.ContextIngestion.Models;
using ArchiForge.Core.Audit;
using ArchiForge.Core.Diagnostics;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;
using ArchiForge.KnowledgeGraph.Interfaces;
using ArchiForge.KnowledgeGraph.Models;
using ArchiForge.KnowledgeGraph.Services;
using ArchiForge.Persistence.Interfaces;

using Microsoft.Extensions.Logging;
using ArchiForge.Persistence.Models;
using ArchiForge.Persistence.Serialization;
using ArchiForge.Persistence.Transactions;
using ArchiForge.Persistence.Retrieval;
using ArchiForge.Retrieval.Indexing;

namespace ArchiForge.Persistence.Orchestration;

/// <summary>
/// <see cref="IAuthorityRunOrchestrator"/> implementation coordinating ingestion, knowledge graph, findings, decisioning, artifact synthesis, audit, and post-commit retrieval indexing.
/// </summary>
/// <remarks>
/// Persists run, context, graph, findings, trace, manifest, and artifact bundle inside a unit of work, then commits before audit tail events and <see cref="IRetrievalRunCompletionIndexer.IndexAuthorityRunAsync"/>.
/// Builds a <see cref="ArchiForge.Provenance.DecisionProvenanceGraph"/> for retrieval indexing only; provenance snapshot persistence is not part of the current flow.
/// </remarks>
public sealed class AuthorityRunOrchestrator(
    IArchiForgeUnitOfWorkFactory unitOfWorkFactory,
    IScopeContextProvider scopeContextProvider,
    IAuditService auditService,
    IManifestHashService manifestHashService,
    IRunRepository runRepository,
    IContextIngestionService contextIngestionService,
    IContextSnapshotRepository contextSnapshotRepository,
    IKnowledgeGraphService knowledgeGraphService,
    IGraphSnapshotRepository graphSnapshotRepository,
    IFindingsOrchestrator findingsOrchestrator,
    IFindingsSnapshotRepository findingsSnapshotRepository,
    IDecisionEngine decisionEngine,
    IDecisionTraceRepository decisionTraceRepository,
    IGoldenManifestRepository goldenManifestRepository,
    IArtifactSynthesisService artifactSynthesisService,
    IArtifactBundleRepository artifactBundleRepository,
    IRetrievalIndexingOutboxRepository retrievalIndexingOutbox,
    ILogger<AuthorityRunOrchestrator> logger)
    : IAuthorityRunOrchestrator
{
    /// <inheritdoc />
    /// <remarks>
    /// Repository writes use the unit of work’s connection/transaction when <see cref="IArchiForgeUnitOfWork.SupportsExternalTransaction"/> is <see langword="true"/>.
    /// </remarks>
    public async Task<RunRecord> ExecuteAsync(
        ContextIngestionRequest request,
        CancellationToken ct)
    {
        await using IArchiForgeUnitOfWork uow = await unitOfWorkFactory.CreateAsync(ct);

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

            await SaveRunAsync(run, uow, ct);

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

            ContextSnapshot? priorCommittedContext = await contextSnapshotRepository
                .GetLatestAsync(request.ProjectId, ct);

            ContextSnapshot contextSnapshot = await contextIngestionService.IngestAsync(request, ct);
            await SaveContextAsync(contextSnapshot, uow, ct);

            run.ContextSnapshotId = contextSnapshot.SnapshotId;
            await UpdateRunAsync(run, uow, ct);

            GraphSnapshotResolutionResult graphResolution = await GraphSnapshotReuseEvaluator.ResolveAsync(
                priorCommittedContext,
                contextSnapshot,
                run.RunId,
                knowledgeGraphService,
                graphSnapshotRepository,
                ct);

            GraphSnapshot graphSnapshot = graphResolution.Snapshot;

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Authority pipeline graph resolved: RunId={RunId}, GraphResolutionMode={GraphResolutionMode}, GraphSnapshotId={GraphSnapshotId}",
                    run.RunId,
                    graphResolution.ResolutionMode,
                    graphSnapshot.GraphSnapshotId);
            }

            await SaveGraphAsync(graphSnapshot, uow, ct);

            run.GraphSnapshotId = graphSnapshot.GraphSnapshotId;
            await UpdateRunAsync(run, uow, ct);

            FindingsSnapshot findingsSnapshot = await findingsOrchestrator.GenerateFindingsSnapshotAsync(
                run.RunId,
                contextSnapshot.SnapshotId,
                graphSnapshot,
                ct);

            await SaveFindingsAsync(findingsSnapshot, uow, ct);

            run.FindingsSnapshotId = findingsSnapshot.FindingsSnapshotId;
            await UpdateRunAsync(run, uow, ct);

            (GoldenManifest manifest, DecisionTrace trace) = await decisionEngine.DecideAsync(
                run.RunId,
                contextSnapshot.SnapshotId,
                graphSnapshot,
                findingsSnapshot,
                ct);

            ApplyScope(trace, scope);
            ApplyScope(manifest, scope);
            manifest.ManifestHash = manifestHashService.ComputeHash(manifest);

            await SaveTraceAsync(trace, uow, ct);
            await SaveManifestAsync(manifest, uow, ct);

            await auditService.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.ManifestGenerated,
                    RunId = run.RunId,
                    ManifestId = manifest.ManifestId,
                    DataJson = JsonSerializer.Serialize(
                        new
                        {
                            manifest.ManifestHash,
                            manifest.RuleSetId
                        },
                        AuditJsonSerializationOptions.Instance)
                },
                ct);

            run.DecisionTraceId = trace.DecisionTraceId;
            run.GoldenManifestId = manifest.ManifestId;
            await UpdateRunAsync(run, uow, ct);

            ArtifactBundle artifactBundle = await artifactSynthesisService.SynthesizeAsync(manifest, ct);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Authority pipeline artifacts synthesized: RunId={RunId}, BundleId={BundleId}, ArtifactCount={ArtifactCount}, SynthesisTraceId={SynthesisTraceId}",
                    run.RunId,
                    artifactBundle.BundleId,
                    artifactBundle.Artifacts.Count,
                    artifactBundle.Trace.TraceId);
            }

            await SaveArtifactBundleAsync(artifactBundle, uow, ct);

            await auditService.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.ArtifactsGenerated,
                    RunId = run.RunId,
                    ManifestId = manifest.ManifestId,
                    DataJson = JsonSerializer.Serialize(
                        new
                        {
                            artifactBundle.BundleId,
                            ArtifactCount = artifactBundle.Artifacts.Count
                        },
                        AuditJsonSerializationOptions.Instance)
                },
                ct);

            run.ArtifactBundleId = artifactBundle.BundleId;
            await UpdateRunAsync(run, uow, ct);

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

            await retrievalIndexingOutbox
                .EnqueueAsync(run.RunId, scope.TenantId, scope.WorkspaceId, scope.ProjectId, ct)
                ;

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

            return run;
        }
        catch (Exception ex)
        {
            await uow.RollbackAsync(ct);

            logger.LogError(
                ex,
                "Authority pipeline failed; transaction rolled back. RunId={RunId}",
                pipelineRunIdForDiagnostics);

            throw;
        }
    }

    private async Task SaveRunAsync(RunRecord run, IArchiForgeUnitOfWork uow, CancellationToken ct)
    {
        if (uow.SupportsExternalTransaction)
            await runRepository.SaveAsync(run, ct, uow.Connection, uow.Transaction);
        else
            await runRepository.SaveAsync(run, ct);
    }

    private async Task UpdateRunAsync(RunRecord run, IArchiForgeUnitOfWork uow, CancellationToken ct)
    {
        if (uow.SupportsExternalTransaction)
            await runRepository.UpdateAsync(run, ct, uow.Connection, uow.Transaction);
        else
            await runRepository.UpdateAsync(run, ct);
    }

    private async Task SaveContextAsync(ContextSnapshot snapshot, IArchiForgeUnitOfWork uow, CancellationToken ct)
    {
        if (uow.SupportsExternalTransaction)
            await contextSnapshotRepository.SaveAsync(snapshot, ct, uow.Connection, uow.Transaction);
        else
            await contextSnapshotRepository.SaveAsync(snapshot, ct);
    }

    private async Task SaveGraphAsync(GraphSnapshot snapshot, IArchiForgeUnitOfWork uow, CancellationToken ct)
    {
        if (uow.SupportsExternalTransaction)
            await graphSnapshotRepository.SaveAsync(snapshot, ct, uow.Connection, uow.Transaction);
        else
            await graphSnapshotRepository.SaveAsync(snapshot, ct);
    }

    private async Task SaveFindingsAsync(FindingsSnapshot snapshot, IArchiForgeUnitOfWork uow, CancellationToken ct)
    {
        if (uow.SupportsExternalTransaction)
            await findingsSnapshotRepository.SaveAsync(snapshot, ct, uow.Connection, uow.Transaction);
        else
            await findingsSnapshotRepository.SaveAsync(snapshot, ct);
    }

    private async Task SaveTraceAsync(DecisionTrace trace, IArchiForgeUnitOfWork uow, CancellationToken ct)
    {
        if (uow.SupportsExternalTransaction)
            await decisionTraceRepository.SaveAsync(trace, ct, uow.Connection, uow.Transaction);
        else
            await decisionTraceRepository.SaveAsync(trace, ct);
    }

    private async Task SaveManifestAsync(GoldenManifest manifest, IArchiForgeUnitOfWork uow, CancellationToken ct)
    {
        if (uow.SupportsExternalTransaction)
            await goldenManifestRepository.SaveAsync(manifest, ct, uow.Connection, uow.Transaction);
        else
            await goldenManifestRepository.SaveAsync(manifest, ct);
    }

    private async Task SaveArtifactBundleAsync(ArtifactBundle bundle, IArchiForgeUnitOfWork uow, CancellationToken ct)
    {
        if (uow.SupportsExternalTransaction)
            await artifactBundleRepository.SaveAsync(bundle, ct, uow.Connection, uow.Transaction);
        else
            await artifactBundleRepository.SaveAsync(bundle, ct);
    }

    private static void ApplyScope(RunRecord run, ScopeContext scope)
    {
        run.TenantId = scope.TenantId;
        run.WorkspaceId = scope.WorkspaceId;
        run.ScopeProjectId = scope.ProjectId;
    }

    private static void ApplyScope(DecisionTrace trace, ScopeContext scope)
    {
        trace.TenantId = scope.TenantId;
        trace.WorkspaceId = scope.WorkspaceId;
        trace.ProjectId = scope.ProjectId;
    }

    private static void ApplyScope(GoldenManifest manifest, ScopeContext scope)
    {
        manifest.TenantId = scope.TenantId;
        manifest.WorkspaceId = scope.WorkspaceId;
        manifest.ProjectId = scope.ProjectId;
    }
}
