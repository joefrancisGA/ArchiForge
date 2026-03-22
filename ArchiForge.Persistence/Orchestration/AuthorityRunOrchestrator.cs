using System.Text.Json;
using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.ContextIngestion.Interfaces;
using ArchiForge.Core.Audit;
using ArchiForge.Core.Scoping;
using ArchiForge.ContextIngestion.Models;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;
using ArchiForge.KnowledgeGraph.Interfaces;
using ArchiForge.KnowledgeGraph.Models;
using ArchiForge.Persistence.Interfaces;
using ArchiForge.Persistence.Models;
using ArchiForge.Persistence.Provenance;
using ArchiForge.Persistence.Transactions;
using ArchiForge.Provenance;
using ArchiForge.Retrieval.Indexing;
using Microsoft.Extensions.Logging;

namespace ArchiForge.Persistence.Orchestration;

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
    IProvenanceSnapshotRepository provenanceSnapshotRepository,
    IRetrievalRunCompletionIndexer retrievalRunCompletionIndexer,
    ILogger<AuthorityRunOrchestrator> logger)
    : IAuthorityRunOrchestrator
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task<RunRecord> ExecuteAsync(
        ContextIngestionRequest request,
        CancellationToken ct)
    {
        await using var uow = await unitOfWorkFactory.CreateAsync(ct);

        try
        {
            var scope = scopeContextProvider.GetCurrentScope();
            var run = new RunRecord
            {
                RunId = Guid.NewGuid(),
                ProjectId = request.ProjectId,
                Description = request.Description,
                CreatedUtc = DateTime.UtcNow
            };
            ApplyScope(run, scope);

            await SaveRunAsync(run, ct, uow);

            request.RunId = run.RunId;
            var contextSnapshot = await contextIngestionService.IngestAsync(request, ct);
            await SaveContextAsync(contextSnapshot, ct, uow);

            run.ContextSnapshotId = contextSnapshot.SnapshotId;
            await UpdateRunAsync(run, ct, uow);

            var graphSnapshot = await knowledgeGraphService.BuildSnapshotAsync(contextSnapshot, ct);
            await SaveGraphAsync(graphSnapshot, ct, uow);

            run.GraphSnapshotId = graphSnapshot.GraphSnapshotId;
            await UpdateRunAsync(run, ct, uow);

            var findingsSnapshot = await findingsOrchestrator.GenerateFindingsSnapshotAsync(
                run.RunId,
                contextSnapshot.SnapshotId,
                graphSnapshot,
                ct);

            await SaveFindingsAsync(findingsSnapshot, ct, uow);

            run.FindingsSnapshotId = findingsSnapshot.FindingsSnapshotId;
            await UpdateRunAsync(run, ct, uow);

            var decisionResult = await decisionEngine.DecideAsync(
                run.RunId,
                contextSnapshot.SnapshotId,
                graphSnapshot,
                findingsSnapshot,
                ct);

            ApplyScope(decisionResult.Trace, scope);
            ApplyScope(decisionResult.Manifest, scope);
            decisionResult.Manifest.ManifestHash = manifestHashService.ComputeHash(decisionResult.Manifest);

            await SaveTraceAsync(decisionResult.Trace, ct, uow);
            await SaveManifestAsync(decisionResult.Manifest, ct, uow);

            await auditService.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.ManifestGenerated,
                    RunId = run.RunId,
                    ManifestId = decisionResult.Manifest.ManifestId,
                    DataJson = JsonSerializer.Serialize(
                        new { decisionResult.Manifest.ManifestHash, decisionResult.Manifest.RuleSetId },
                        AuditJsonOptions)
                },
                ct);

            run.DecisionTraceId = decisionResult.Trace.DecisionTraceId;
            run.GoldenManifestId = decisionResult.Manifest.ManifestId;
            await UpdateRunAsync(run, ct, uow);

            var artifactBundle = await artifactSynthesisService.SynthesizeAsync(decisionResult.Manifest, ct);
            await SaveArtifactBundleAsync(artifactBundle, ct, uow);

            await auditService.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.ArtifactsGenerated,
                    RunId = run.RunId,
                    ManifestId = decisionResult.Manifest.ManifestId,
                    DataJson = JsonSerializer.Serialize(
                        new { artifactBundle.BundleId, ArtifactCount = artifactBundle.Artifacts.Count },
                        AuditJsonOptions)
                },
                ct);

            run.ArtifactBundleId = artifactBundle.BundleId;
            await UpdateRunAsync(run, ct, uow);

            var provenanceGraph = provenanceBuilder.Build(
                run.RunId,
                findingsSnapshot,
                graphSnapshot,
                decisionResult.Manifest,
                decisionResult.Trace,
                artifactBundle.Artifacts);

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
                        AuditJsonOptions)
                },
                ct);

            try
            {
                await retrievalRunCompletionIndexer.IndexAuthorityRunAsync(
                    scope.TenantId,
                    scope.WorkspaceId,
                    scope.ProjectId,
                    decisionResult.Manifest,
                    artifactBundle.Artifacts,
                    provenanceGraph,
                    ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Retrieval indexing failed for run {RunId}", run.RunId);
            }

            return run;
        }
        catch
        {
            await uow.RollbackAsync(ct);
            throw;
        }
    }

    private async Task SaveRunAsync(RunRecord run, CancellationToken ct, IArchiForgeUnitOfWork uow)
    {
        if (uow.SupportsExternalTransaction)
            await runRepository.SaveAsync(run, ct, uow.Connection, uow.Transaction);
        else
            await runRepository.SaveAsync(run, ct);
    }

    private async Task UpdateRunAsync(RunRecord run, CancellationToken ct, IArchiForgeUnitOfWork uow)
    {
        if (uow.SupportsExternalTransaction)
            await runRepository.UpdateAsync(run, ct, uow.Connection, uow.Transaction);
        else
            await runRepository.UpdateAsync(run, ct);
    }

    private async Task SaveContextAsync(ContextSnapshot snapshot, CancellationToken ct, IArchiForgeUnitOfWork uow)
    {
        if (uow.SupportsExternalTransaction)
            await contextSnapshotRepository.SaveAsync(snapshot, ct, uow.Connection, uow.Transaction);
        else
            await contextSnapshotRepository.SaveAsync(snapshot, ct);
    }

    private async Task SaveGraphAsync(GraphSnapshot snapshot, CancellationToken ct, IArchiForgeUnitOfWork uow)
    {
        if (uow.SupportsExternalTransaction)
            await graphSnapshotRepository.SaveAsync(snapshot, ct, uow.Connection, uow.Transaction);
        else
            await graphSnapshotRepository.SaveAsync(snapshot, ct);
    }

    private async Task SaveFindingsAsync(FindingsSnapshot snapshot, CancellationToken ct, IArchiForgeUnitOfWork uow)
    {
        if (uow.SupportsExternalTransaction)
            await findingsSnapshotRepository.SaveAsync(snapshot, ct, uow.Connection, uow.Transaction);
        else
            await findingsSnapshotRepository.SaveAsync(snapshot, ct);
    }

    private async Task SaveTraceAsync(DecisionTrace trace, CancellationToken ct, IArchiForgeUnitOfWork uow)
    {
        if (uow.SupportsExternalTransaction)
            await decisionTraceRepository.SaveAsync(trace, ct, uow.Connection, uow.Transaction);
        else
            await decisionTraceRepository.SaveAsync(trace, ct);
    }

    private async Task SaveManifestAsync(GoldenManifest manifest, CancellationToken ct, IArchiForgeUnitOfWork uow)
    {
        if (uow.SupportsExternalTransaction)
            await goldenManifestRepository.SaveAsync(manifest, ct, uow.Connection, uow.Transaction);
        else
            await goldenManifestRepository.SaveAsync(manifest, ct);
    }

    private async Task SaveArtifactBundleAsync(ArtifactBundle bundle, CancellationToken ct, IArchiForgeUnitOfWork uow)
    {
        if (uow.SupportsExternalTransaction)
            await artifactBundleRepository.SaveAsync(bundle, ct, uow.Connection, uow.Transaction);
        else
            await artifactBundleRepository.SaveAsync(bundle, ct);
    }

    // ReSharper disable once UnusedMember.Local
    private async Task SaveProvenanceAsync(
        DecisionProvenanceSnapshot snapshot,
        CancellationToken ct,
        IArchiForgeUnitOfWork uow)
    {
        if (uow.SupportsExternalTransaction)
            await provenanceSnapshotRepository.SaveAsync(snapshot, ct, uow.Connection, uow.Transaction);
        else
            await provenanceSnapshotRepository.SaveAsync(snapshot, ct);
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
