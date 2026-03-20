using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.ContextIngestion.Interfaces;
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
    private readonly IArchiForgeUnitOfWorkFactory _unitOfWorkFactory;
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
        string projectId,
        string? description,
        CancellationToken ct)
    {
        await using var uow = await _unitOfWorkFactory.CreateAsync(ct);

        try
        {
            var run = new RunRecord
            {
                RunId = Guid.NewGuid(),
                ProjectId = projectId,
                Description = description,
                CreatedUtc = DateTime.UtcNow
            };

            await SaveRunAsync(run, ct, uow);

            var ingestionRequest = new ContextIngestionRequest
            {
                RunId = run.RunId,
                ProjectId = projectId,
                Description = description
            };

            var contextSnapshot = await _contextIngestionService.IngestAsync(ingestionRequest, ct);
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

            await SaveTraceAsync(decisionResult.Trace, ct, uow);
            await SaveManifestAsync(decisionResult.Manifest, ct, uow);

            run.DecisionTraceId = decisionResult.Trace.DecisionTraceId;
            run.GoldenManifestId = decisionResult.Manifest.ManifestId;
            await UpdateRunAsync(run, ct, uow);

            var artifactBundle = await _artifactSynthesisService.SynthesizeAsync(decisionResult.Manifest, ct);
            await SaveArtifactBundleAsync(artifactBundle, ct, uow);

            run.ArtifactBundleId = artifactBundle.BundleId;
            await UpdateRunAsync(run, ct, uow);

            await uow.CommitAsync(ct);

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
}
