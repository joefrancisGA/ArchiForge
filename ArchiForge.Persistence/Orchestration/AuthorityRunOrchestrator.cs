using System.Text.Json;

using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.ContextIngestion.Interfaces;
using ArchiForge.ContextIngestion.Models;
using ArchiForge.Core.Audit;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;
using ArchiForge.KnowledgeGraph.Interfaces;
using ArchiForge.KnowledgeGraph.Models;
using ArchiForge.KnowledgeGraph.Services;
using ArchiForge.Persistence.Interfaces;
using ArchiForge.Persistence.Models;
using ArchiForge.Persistence.Transactions;
using ArchiForge.Provenance;
using ArchiForge.Retrieval.Indexing;

using Microsoft.Extensions.Logging;

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
    IProvenanceBuilder provenanceBuilder,
    IRetrievalRunCompletionIndexer retrievalRunCompletionIndexer,
    ILogger<AuthorityRunOrchestrator> logger)
    : IAuthorityRunOrchestrator
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <inheritdoc />
    /// <remarks>
    /// Repository writes use the unit of work’s connection/transaction when <see cref="IArchiForgeUnitOfWork.SupportsExternalTransaction"/> is <see langword="true"/>.
    /// </remarks>
    public async Task<RunRecord> ExecuteAsync(
        ContextIngestionRequest request,
        CancellationToken ct)
    {
        await using IArchiForgeUnitOfWork uow = await unitOfWorkFactory.CreateAsync(ct).ConfigureAwait(false);

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

            await SaveRunAsync(run, uow, ct).ConfigureAwait(false);

            request.RunId = run.RunId;

            ContextSnapshot? priorCommittedContext = await contextSnapshotRepository
                .GetLatestAsync(request.ProjectId, ct).ConfigureAwait(false);

            ContextSnapshot contextSnapshot = await contextIngestionService.IngestAsync(request, ct).ConfigureAwait(false);
            await SaveContextAsync(contextSnapshot, uow, ct).ConfigureAwait(false);

            run.ContextSnapshotId = contextSnapshot.SnapshotId;
            await UpdateRunAsync(run, uow, ct).ConfigureAwait(false);

            GraphSnapshot graphSnapshot = await GraphSnapshotReuseEvaluator.ResolveAsync(
                priorCommittedContext,
                contextSnapshot,
                run.RunId,
                knowledgeGraphService,
                graphSnapshotRepository,
                ct).ConfigureAwait(false);
            await SaveGraphAsync(graphSnapshot, uow, ct).ConfigureAwait(false);

            run.GraphSnapshotId = graphSnapshot.GraphSnapshotId;
            await UpdateRunAsync(run, uow, ct).ConfigureAwait(false);

            FindingsSnapshot findingsSnapshot = await findingsOrchestrator.GenerateFindingsSnapshotAsync(
                run.RunId,
                contextSnapshot.SnapshotId,
                graphSnapshot,
                ct).ConfigureAwait(false);

            await SaveFindingsAsync(findingsSnapshot, uow, ct).ConfigureAwait(false);

            run.FindingsSnapshotId = findingsSnapshot.FindingsSnapshotId;
            await UpdateRunAsync(run, uow, ct).ConfigureAwait(false);

            (GoldenManifest manifest, DecisionTrace trace) = await decisionEngine.DecideAsync(
                run.RunId,
                contextSnapshot.SnapshotId,
                graphSnapshot,
                findingsSnapshot,
                ct).ConfigureAwait(false);

            ApplyScope(trace, scope);
            ApplyScope(manifest, scope);
            manifest.ManifestHash = manifestHashService.ComputeHash(manifest);

            await SaveTraceAsync(trace, uow, ct).ConfigureAwait(false);
            await SaveManifestAsync(manifest, uow, ct).ConfigureAwait(false);

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
                        AuditJsonOptions)
                },
                ct).ConfigureAwait(false);

            run.DecisionTraceId = trace.DecisionTraceId;
            run.GoldenManifestId = manifest.ManifestId;
            await UpdateRunAsync(run, uow, ct).ConfigureAwait(false);

            ArtifactBundle artifactBundle = await artifactSynthesisService.SynthesizeAsync(manifest, ct).ConfigureAwait(false);
            await SaveArtifactBundleAsync(artifactBundle, uow, ct).ConfigureAwait(false);

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
                        AuditJsonOptions)
                },
                ct).ConfigureAwait(false);

            run.ArtifactBundleId = artifactBundle.BundleId;
            await UpdateRunAsync(run, uow, ct).ConfigureAwait(false);

            DecisionProvenanceGraph provenanceGraph = provenanceBuilder.Build(
                run.RunId,
                findingsSnapshot,
                graphSnapshot,
                manifest,
                trace,
                artifactBundle.Artifacts);

            await uow.CommitAsync(ct).ConfigureAwait(false);

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
                        AuditJsonOptions)
                },
                ct).ConfigureAwait(false);

            try
            {
                await retrievalRunCompletionIndexer.IndexAuthorityRunAsync(
                    scope.TenantId,
                    scope.WorkspaceId,
                    scope.ProjectId,
                    manifest,
                    artifactBundle.Artifacts,
                    provenanceGraph,
                    ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Retrieval indexing failed for run {RunId}", run.RunId);
            }

            return run;
        }
        catch
        {
            await uow.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }

    private async Task SaveRunAsync(RunRecord run, IArchiForgeUnitOfWork uow, CancellationToken ct)
    {
        if (uow.SupportsExternalTransaction)
            await runRepository.SaveAsync(run, ct, uow.Connection, uow.Transaction).ConfigureAwait(false);
        else
            await runRepository.SaveAsync(run, ct).ConfigureAwait(false);
    }

    private async Task UpdateRunAsync(RunRecord run, IArchiForgeUnitOfWork uow, CancellationToken ct)
    {
        if (uow.SupportsExternalTransaction)
            await runRepository.UpdateAsync(run, ct, uow.Connection, uow.Transaction).ConfigureAwait(false);
        else
            await runRepository.UpdateAsync(run, ct).ConfigureAwait(false);
    }

    private async Task SaveContextAsync(ContextSnapshot snapshot, IArchiForgeUnitOfWork uow, CancellationToken ct)
    {
        if (uow.SupportsExternalTransaction)
            await contextSnapshotRepository.SaveAsync(snapshot, ct, uow.Connection, uow.Transaction).ConfigureAwait(false);
        else
            await contextSnapshotRepository.SaveAsync(snapshot, ct).ConfigureAwait(false);
    }

    private async Task SaveGraphAsync(GraphSnapshot snapshot, IArchiForgeUnitOfWork uow, CancellationToken ct)
    {
        if (uow.SupportsExternalTransaction)
            await graphSnapshotRepository.SaveAsync(snapshot, ct, uow.Connection, uow.Transaction).ConfigureAwait(false);
        else
            await graphSnapshotRepository.SaveAsync(snapshot, ct).ConfigureAwait(false);
    }

    private async Task SaveFindingsAsync(FindingsSnapshot snapshot, IArchiForgeUnitOfWork uow, CancellationToken ct)
    {
        if (uow.SupportsExternalTransaction)
            await findingsSnapshotRepository.SaveAsync(snapshot, ct, uow.Connection, uow.Transaction).ConfigureAwait(false);
        else
            await findingsSnapshotRepository.SaveAsync(snapshot, ct).ConfigureAwait(false);
    }

    private async Task SaveTraceAsync(DecisionTrace trace, IArchiForgeUnitOfWork uow, CancellationToken ct)
    {
        if (uow.SupportsExternalTransaction)
            await decisionTraceRepository.SaveAsync(trace, ct, uow.Connection, uow.Transaction).ConfigureAwait(false);
        else
            await decisionTraceRepository.SaveAsync(trace, ct).ConfigureAwait(false);
    }

    private async Task SaveManifestAsync(GoldenManifest manifest, IArchiForgeUnitOfWork uow, CancellationToken ct)
    {
        if (uow.SupportsExternalTransaction)
            await goldenManifestRepository.SaveAsync(manifest, ct, uow.Connection, uow.Transaction).ConfigureAwait(false);
        else
            await goldenManifestRepository.SaveAsync(manifest, ct).ConfigureAwait(false);
    }

    private async Task SaveArtifactBundleAsync(ArtifactBundle bundle, IArchiForgeUnitOfWork uow, CancellationToken ct)
    {
        if (uow.SupportsExternalTransaction)
            await artifactBundleRepository.SaveAsync(bundle, ct, uow.Connection, uow.Transaction).ConfigureAwait(false);
        else
            await artifactBundleRepository.SaveAsync(bundle, ct).ConfigureAwait(false);
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
