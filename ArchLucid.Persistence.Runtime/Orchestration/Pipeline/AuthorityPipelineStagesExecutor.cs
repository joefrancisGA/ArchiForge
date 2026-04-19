using System.Diagnostics;
using System.Text.Json;

using ArchLucid.ArtifactSynthesis.Interfaces;
using ArchLucid.ArtifactSynthesis.Models;
using ArchLucid.ContextIngestion.Interfaces;
using ArchLucid.ContextIngestion.Models;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Transactions;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Models;
using ArchLucid.KnowledgeGraph.Interfaces;
using ArchLucid.KnowledgeGraph.Models;
using ArchLucid.KnowledgeGraph.Services;
using ArchLucid.Persistence.Cosmos;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Serialization;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Persistence.Orchestration.Pipeline;

/// <summary>
/// Default pipeline executor with one OpenTelemetry span per major stage (<c>authority.*</c> activity names),
/// explicitly parented to <see cref="AuthorityPipelineContext.RunActivity"/> when present.
/// </summary>
public sealed class AuthorityPipelineStagesExecutor(
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
    IManifestHashService manifestHashService,
    IArtifactSynthesisService artifactSynthesisService,
    IArtifactBundleRepository artifactBundleRepository,
    IAuditService auditService,
    IOptionsMonitor<CosmosDbOptions> cosmosDbOptionsMonitor,
    ILogger<AuthorityPipelineStagesExecutor> logger) : IAuthorityPipelineStagesExecutor
{
    private readonly IRunRepository _runRepository =
        runRepository ?? throw new ArgumentNullException(nameof(runRepository));

    private readonly IContextIngestionService _contextIngestionService =
        contextIngestionService ?? throw new ArgumentNullException(nameof(contextIngestionService));

    private readonly IContextSnapshotRepository _contextSnapshotRepository =
        contextSnapshotRepository ?? throw new ArgumentNullException(nameof(contextSnapshotRepository));

    private readonly IKnowledgeGraphService _knowledgeGraphService =
        knowledgeGraphService ?? throw new ArgumentNullException(nameof(knowledgeGraphService));

    private readonly IGraphSnapshotRepository _graphSnapshotRepository =
        graphSnapshotRepository ?? throw new ArgumentNullException(nameof(graphSnapshotRepository));

    private readonly IFindingsOrchestrator _findingsOrchestrator =
        findingsOrchestrator ?? throw new ArgumentNullException(nameof(findingsOrchestrator));

    private readonly IFindingsSnapshotRepository _findingsSnapshotRepository =
        findingsSnapshotRepository ?? throw new ArgumentNullException(nameof(findingsSnapshotRepository));

    private readonly IDecisionEngine _decisionEngine =
        decisionEngine ?? throw new ArgumentNullException(nameof(decisionEngine));

    private readonly IDecisionTraceRepository _decisionTraceRepository =
        decisionTraceRepository ?? throw new ArgumentNullException(nameof(decisionTraceRepository));

    private readonly IGoldenManifestRepository _goldenManifestRepository =
        goldenManifestRepository ?? throw new ArgumentNullException(nameof(goldenManifestRepository));

    private readonly IManifestHashService _manifestHashService =
        manifestHashService ?? throw new ArgumentNullException(nameof(manifestHashService));

    private readonly IArtifactSynthesisService _artifactSynthesisService =
        artifactSynthesisService ?? throw new ArgumentNullException(nameof(artifactSynthesisService));

    private readonly IArtifactBundleRepository _artifactBundleRepository =
        artifactBundleRepository ?? throw new ArgumentNullException(nameof(artifactBundleRepository));

    private readonly IAuditService _auditService =
        auditService ?? throw new ArgumentNullException(nameof(auditService));

    private readonly IOptionsMonitor<CosmosDbOptions> _cosmosDbOptionsMonitor =
        cosmosDbOptionsMonitor ?? throw new ArgumentNullException(nameof(cosmosDbOptionsMonitor));

    private readonly ILogger<AuthorityPipelineStagesExecutor> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task ExecuteAfterRunPersistedAsync(AuthorityPipelineContext ctx, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        IArchLucidUnitOfWork uow = ctx.UnitOfWork;
        RunRecord run = ctx.Run;
        ScopeContext scope = ctx.Scope;

        await ExecuteStageAsync(ctx, "authority.context_ingestion", "context_ingestion", async (_, token) =>
        {
            ctx.PriorCommittedContext ??= await _contextSnapshotRepository.GetLatestAsync(ctx.Request.ProjectId, token);

            ContextSnapshot contextSnapshot = await _contextIngestionService.IngestAsync(ctx.Request, token);
            await SaveContextAsync(contextSnapshot, uow, token);
            ctx.ContextSnapshot = contextSnapshot;

            run.ContextSnapshotId = contextSnapshot.SnapshotId;
            await UpdateRunAsync(run, uow, token);
        }, ct);

        await ExecuteStageAsync(ctx, "authority.graph", "graph", async (_, token) =>
        {
            GraphSnapshotResolutionResult graphResolution = await GraphSnapshotReuseEvaluator.ResolveAsync(
                ctx.PriorCommittedContext,
                ctx.ContextSnapshot!,
                run.RunId,
                _knowledgeGraphService,
                _graphSnapshotRepository,
                token);

            ctx.GraphResolution = graphResolution;
            GraphSnapshot graphSnapshot = graphResolution.Snapshot;
            ctx.GraphSnapshot = graphSnapshot;

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Authority pipeline graph resolved: RunId={RunId}, GraphResolutionMode={GraphResolutionMode}, GraphSnapshotId={GraphSnapshotId}",
                    run.RunId,
                    graphResolution.ResolutionMode,
                    graphSnapshot.GraphSnapshotId);
            }

            await SaveGraphAsync(graphSnapshot, uow, token);

            run.GraphSnapshotId = graphSnapshot.GraphSnapshotId;
            await UpdateRunAsync(run, uow, token);
        }, ct);

        await ExecuteStageAsync(ctx, "authority.findings", "findings", async (_, token) =>
        {
            FindingsSnapshot findingsSnapshot = await _findingsOrchestrator.GenerateFindingsSnapshotAsync(
                run.RunId,
                ctx.ContextSnapshot!.SnapshotId,
                ctx.GraphSnapshot!,
                token);

            await SaveFindingsAsync(findingsSnapshot, uow, token);
            ctx.FindingsSnapshot = findingsSnapshot;

            RecordFindingsProducedForMetrics(findingsSnapshot);

            run.FindingsSnapshotId = findingsSnapshot.FindingsSnapshotId;
            await UpdateRunAsync(run, uow, token);
        }, ct);

        await ExecuteStageAsync(ctx, "authority.decisioning", "decisioning", async (_, token) =>
        {
            (GoldenManifest manifest, DecisionTrace trace) = await _decisionEngine.DecideAsync(
                run.RunId,
                ctx.ContextSnapshot!.SnapshotId,
                ctx.GraphSnapshot!,
                ctx.FindingsSnapshot!,
                token);

            ApplyScope(trace, scope);
            ApplyScope(manifest, scope);
            manifest.ManifestHash = _manifestHashService.ComputeHash(manifest);

            await SaveTraceAsync(trace, uow, token);
            await SaveManifestAsync(manifest, uow, token);

            await _auditService.LogAsync(
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
                token);

            ctx.Manifest = manifest;
            ctx.Trace = trace;

            run.DecisionTraceId = trace.RequireRuleAudit().DecisionTraceId;
            run.GoldenManifestId = manifest.ManifestId;
            await UpdateRunAsync(run, uow, token);
        }, ct);

        await ExecuteStageAsync(ctx, "authority.artifacts", "artifacts", async (_, token) =>
        {
            ArtifactBundle artifactBundle = await _artifactSynthesisService.SynthesizeAsync(ctx.Manifest!, token);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Authority pipeline artifacts synthesized: RunId={RunId}, BundleId={BundleId}, ArtifactCount={ArtifactCount}, SynthesisTraceId={SynthesisTraceId}",
                    run.RunId,
                    artifactBundle.BundleId,
                    artifactBundle.Artifacts.Count,
                    artifactBundle.Trace.TraceId);
            }

            await SaveArtifactBundleAsync(artifactBundle, uow, token);

            await _auditService.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.ArtifactsGenerated,
                    RunId = run.RunId,
                    ManifestId = ctx.Manifest!.ManifestId,
                    DataJson = JsonSerializer.Serialize(
                        new
                        {
                            artifactBundle.BundleId,
                            ArtifactCount = artifactBundle.Artifacts.Count
                        },
                        AuditJsonSerializationOptions.Instance)
                },
                token);

            ctx.ArtifactBundle = artifactBundle;

            run.ArtifactBundleId = artifactBundle.BundleId;
            await UpdateRunAsync(run, uow, token);
        }, ct);
    }

    private async Task ExecuteStageAsync(
        AuthorityPipelineContext ctx,
        string activityName,
        string stageName,
        Func<Activity?, CancellationToken, Task> stageWork,
        CancellationToken ct)
    {
        ActivityContext parentContext = ctx.RunActivity?.Context ?? default;

        using Activity? activity = ArchLucidInstrumentation.AuthorityRun.StartActivity(
            activityName,
            ActivityKind.Internal,
            parentContext);

        activity?.SetTag("archlucid.run_id", ctx.Run.RunId.ToString("D"));
        activity?.SetTag("archlucid.stage.name", stageName);

        long startTicks = Stopwatch.GetTimestamp();
        string outcome = "success";

        try
        {
            await stageWork(activity, ct);
        }
        catch (Exception ex)
        {
            outcome = "error";
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);
            throw;
        }
        finally
        {
            double elapsedMs = Stopwatch.GetElapsedTime(startTicks).TotalMilliseconds;
            ArchLucidInstrumentation.AuthorityPipelineStageDurationMilliseconds.Record(
                elapsedMs,
                new KeyValuePair<string, object?>("stage", stageName),
                new KeyValuePair<string, object?>("outcome", outcome));
        }
    }

    private async Task UpdateRunAsync(RunRecord run, IArchLucidUnitOfWork uow, CancellationToken ct)
    {
        if (uow.SupportsExternalTransaction)
            await _runRepository.UpdateAsync(run, ct, uow.Connection, uow.Transaction);
        else
            await _runRepository.UpdateAsync(run, ct);
    }

    private async Task SaveContextAsync(ContextSnapshot snapshot, IArchLucidUnitOfWork uow, CancellationToken ct)
    {
        if (uow.SupportsExternalTransaction)
            await _contextSnapshotRepository.SaveAsync(snapshot, ct, uow.Connection, uow.Transaction);
        else
            await _contextSnapshotRepository.SaveAsync(snapshot, ct);
    }

    private async Task SaveGraphAsync(GraphSnapshot snapshot, IArchLucidUnitOfWork uow, CancellationToken ct)
    {
        // Cosmos graph snapshots are committed outside the SQL authority transaction; SQL graph stays enlisted.
        if (_cosmosDbOptionsMonitor.CurrentValue.GraphSnapshotsEnabled)
        {
            await _graphSnapshotRepository.SaveAsync(snapshot, ct);
            return;
        }

        if (uow.SupportsExternalTransaction)
            await _graphSnapshotRepository.SaveAsync(snapshot, ct, uow.Connection, uow.Transaction);
        else
            await _graphSnapshotRepository.SaveAsync(snapshot, ct);
    }

    private async Task SaveFindingsAsync(FindingsSnapshot snapshot, IArchLucidUnitOfWork uow, CancellationToken ct)
    {
        if (uow.SupportsExternalTransaction)
            await _findingsSnapshotRepository.SaveAsync(snapshot, ct, uow.Connection, uow.Transaction);
        else
            await _findingsSnapshotRepository.SaveAsync(snapshot, ct);
    }

    private async Task SaveTraceAsync(DecisionTrace trace, IArchLucidUnitOfWork uow, CancellationToken ct)
    {
        if (uow.SupportsExternalTransaction)
            await _decisionTraceRepository.SaveAsync(trace, ct, uow.Connection, uow.Transaction);
        else
            await _decisionTraceRepository.SaveAsync(trace, ct);
    }

    private async Task SaveManifestAsync(GoldenManifest manifest, IArchLucidUnitOfWork uow, CancellationToken ct)
    {
        if (uow.SupportsExternalTransaction)
            await _goldenManifestRepository.SaveAsync(manifest, ct, uow.Connection, uow.Transaction);
        else
            await _goldenManifestRepository.SaveAsync(manifest, ct);
    }

    private async Task SaveArtifactBundleAsync(ArtifactBundle bundle, IArchLucidUnitOfWork uow, CancellationToken ct)
    {
        if (uow.SupportsExternalTransaction)
            await _artifactBundleRepository.SaveAsync(bundle, ct, uow.Connection, uow.Transaction);
        else
            await _artifactBundleRepository.SaveAsync(bundle, ct);
    }

    private static void RecordFindingsProducedForMetrics(FindingsSnapshot snapshot)
    {
        if (snapshot.Findings is null || snapshot.Findings.Count == 0)
            return;

        foreach (IGrouping<FindingSeverity, Finding> group in snapshot.Findings.GroupBy(static f => f.Severity))
        {
            TagList tags = new TagList { { "severity", group.Key.ToString() } };

            ArchLucidInstrumentation.FindingsProducedTotal.Add(group.Count(), tags);
        }
    }

    private static void ApplyScope(DecisionTrace trace, ScopeContext scope)
    {
        RuleAuditTracePayload audit = trace.RequireRuleAudit();
        audit.TenantId = scope.TenantId;
        audit.WorkspaceId = scope.WorkspaceId;
        audit.ProjectId = scope.ProjectId;
    }

    private static void ApplyScope(GoldenManifest manifest, ScopeContext scope)
    {
        manifest.TenantId = scope.TenantId;
        manifest.WorkspaceId = scope.WorkspaceId;
        manifest.ProjectId = scope.ProjectId;
    }
}
