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
using ArchiForge.Persistence.Transactions;

namespace ArchiForge.Persistence.Orchestration;

public sealed class AuthorityRunOrchestrator : IAuthorityRunOrchestrator
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IArchiForgeUnitOfWorkFactory _unitOfWorkFactory;
    private readonly IScopeContextProvider _scopeContextProvider;
    private readonly IAuditService _auditService;
    private readonly IManifestHashService _manifestHashService;
    private readonly IRunRepository _runRepository;
    private readonly IContextIngestionService _contextIngestionService;
    private readonly IContextSnapshotRepository _contextSnapshotRepository;
    private readonly IKnowledgeGraphService _knowledgeGraphService;
    private readonly IGraphSnapshotRepository _graphSnapshotRepository;
    private readonly IFindingsOrchestrator _findingsOrchestrator;
    private readonly IFindingsSnapshotRepository _findingsSnapshotRepository;
    private readonly IDecisionEngine _decisionEngine;
    private readonly IDecisionTraceRepository _decisionTraceRepository;
    private readonly IGoldenManifestRepository _goldenManifestRepository;
    private readonly IArtifactSynthesisService _artifactSynthesisService;
    private readonly IArtifactBundleRepository _artifactBundleRepository;

    public AuthorityRunOrchestrator(
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
        IArtifactBundleRepository artifactBundleRepository)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
        _scopeContextProvider = scopeContextProvider;
        _auditService = auditService;
        _manifestHashService = manifestHashService;
        _runRepository = runRepository;
        _contextIngestionService = contextIngestionService;
        _contextSnapshotRepository = contextSnapshotRepository;
        _knowledgeGraphService = knowledgeGraphService;
        _graphSnapshotRepository = graphSnapshotRepository;
        _findingsOrchestrator = findingsOrchestrator;
        _findingsSnapshotRepository = findingsSnapshotRepository;
        _decisionEngine = decisionEngine;
        _decisionTraceRepository = decisionTraceRepository;
        _goldenManifestRepository = goldenManifestRepository;
        _artifactSynthesisService = artifactSynthesisService;
        _artifactBundleRepository = artifactBundleRepository;
    }

    public async Task<RunRecord> ExecuteAsync(
        ContextIngestionRequest request,
        CancellationToken ct)
    {
        await using var uow = await _unitOfWorkFactory.CreateAsync(ct);

        try
        {
            var scope = _scopeContextProvider.GetCurrentScope();
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
            var contextSnapshot = await _contextIngestionService.IngestAsync(request, ct);
            await SaveContextAsync(contextSnapshot, ct, uow);

            run.ContextSnapshotId = contextSnapshot.SnapshotId;
            await UpdateRunAsync(run, ct, uow);

            var graphSnapshot = await _knowledgeGraphService.BuildSnapshotAsync(contextSnapshot, ct);
            await SaveGraphAsync(graphSnapshot, ct, uow);

            run.GraphSnapshotId = graphSnapshot.GraphSnapshotId;
            await UpdateRunAsync(run, ct, uow);

            var findingsSnapshot = await _findingsOrchestrator.GenerateFindingsSnapshotAsync(
                run.RunId,
                contextSnapshot.SnapshotId,
                graphSnapshot,
                ct);

            await SaveFindingsAsync(findingsSnapshot, ct, uow);

            run.FindingsSnapshotId = findingsSnapshot.FindingsSnapshotId;
            await UpdateRunAsync(run, ct, uow);

            var decisionResult = await _decisionEngine.DecideAsync(
                run.RunId,
                contextSnapshot.SnapshotId,
                graphSnapshot,
                findingsSnapshot,
                ct);

            ApplyScope(decisionResult.Trace, scope);
            ApplyScope(decisionResult.Manifest, scope);
            decisionResult.Manifest.ManifestHash = _manifestHashService.ComputeHash(decisionResult.Manifest);

            await SaveTraceAsync(decisionResult.Trace, ct, uow);
            await SaveManifestAsync(decisionResult.Manifest, ct, uow);

            await _auditService.LogAsync(
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

            var artifactBundle = await _artifactSynthesisService.SynthesizeAsync(decisionResult.Manifest, ct);
            await SaveArtifactBundleAsync(artifactBundle, ct, uow);

            await _auditService.LogAsync(
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

            await uow.CommitAsync(ct);

            await _auditService.LogAsync(
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
            await _runRepository.SaveAsync(run, ct, uow.Connection, uow.Transaction);
        else
            await _runRepository.SaveAsync(run, ct);
    }

    private async Task UpdateRunAsync(RunRecord run, CancellationToken ct, IArchiForgeUnitOfWork uow)
    {
        if (uow.SupportsExternalTransaction)
            await _runRepository.UpdateAsync(run, ct, uow.Connection, uow.Transaction);
        else
            await _runRepository.UpdateAsync(run, ct);
    }

    private async Task SaveContextAsync(ContextSnapshot snapshot, CancellationToken ct, IArchiForgeUnitOfWork uow)
    {
        if (uow.SupportsExternalTransaction)
            await _contextSnapshotRepository.SaveAsync(snapshot, ct, uow.Connection, uow.Transaction);
        else
            await _contextSnapshotRepository.SaveAsync(snapshot, ct);
    }

    private async Task SaveGraphAsync(GraphSnapshot snapshot, CancellationToken ct, IArchiForgeUnitOfWork uow)
    {
        if (uow.SupportsExternalTransaction)
            await _graphSnapshotRepository.SaveAsync(snapshot, ct, uow.Connection, uow.Transaction);
        else
            await _graphSnapshotRepository.SaveAsync(snapshot, ct);
    }

    private async Task SaveFindingsAsync(FindingsSnapshot snapshot, CancellationToken ct, IArchiForgeUnitOfWork uow)
    {
        if (uow.SupportsExternalTransaction)
            await _findingsSnapshotRepository.SaveAsync(snapshot, ct, uow.Connection, uow.Transaction);
        else
            await _findingsSnapshotRepository.SaveAsync(snapshot, ct);
    }

    private async Task SaveTraceAsync(DecisionTrace trace, CancellationToken ct, IArchiForgeUnitOfWork uow)
    {
        if (uow.SupportsExternalTransaction)
            await _decisionTraceRepository.SaveAsync(trace, ct, uow.Connection, uow.Transaction);
        else
            await _decisionTraceRepository.SaveAsync(trace, ct);
    }

    private async Task SaveManifestAsync(GoldenManifest manifest, CancellationToken ct, IArchiForgeUnitOfWork uow)
    {
        if (uow.SupportsExternalTransaction)
            await _goldenManifestRepository.SaveAsync(manifest, ct, uow.Connection, uow.Transaction);
        else
            await _goldenManifestRepository.SaveAsync(manifest, ct);
    }

    private async Task SaveArtifactBundleAsync(ArtifactBundle bundle, CancellationToken ct, IArchiForgeUnitOfWork uow)
    {
        if (uow.SupportsExternalTransaction)
            await _artifactBundleRepository.SaveAsync(bundle, ct, uow.Connection, uow.Transaction);
        else
            await _artifactBundleRepository.SaveAsync(bundle, ct);
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
