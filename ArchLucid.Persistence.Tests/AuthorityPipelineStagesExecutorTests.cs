using System.Diagnostics;
using System.Diagnostics.Metrics;

using ArchLucid.ArtifactSynthesis.Interfaces;
using ArchLucid.ArtifactSynthesis.Models;
using ArchLucid.ContextIngestion.Interfaces;
using ArchLucid.ContextIngestion.Models;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Findings;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Authority;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Transactions;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Models;
using ArchLucid.KnowledgeGraph.Interfaces;
using ArchLucid.KnowledgeGraph.Models;
using ArchLucid.Persistence.Cosmos;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Persistence.Tests;

/// <summary>
///     <see cref="AuthorityPipelineStagesExecutor" /> OTel span parenting, stage tags, histogram, and error propagation.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class AuthorityPipelineStagesExecutorTests
{
    private const string AuthorityRunSourceName = "ArchLucid.AuthorityRun";

    [SkippableFact]
    public async Task ExecuteAfterRunPersistedAsync_creates_child_activities_under_run_activity()
    {
        _ = ArchLucidInstrumentation.AuthorityPipelineStageDurationMilliseconds;

        List<Activity> stopped = [];

        using ActivityListener listener = new();
        listener.ShouldListenTo = s => s.Name == AuthorityRunSourceName;
        listener.Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded;
        listener.ActivityStopped = stopped.Add;

        ActivitySource.AddActivityListener(listener);

        using Activity? parent = ArchLucidInstrumentation.AuthorityRun.StartActivity(
            "authority.run.test");

        parent.Should().NotBeNull();

        (AuthorityPipelineStagesExecutor sut, _, _) = CreateExecutor();
        AuthorityPipelineContext ctx = CreateContext(parent, Guid.NewGuid());

        await sut.ExecuteAfterRunPersistedAsync(ctx, CancellationToken.None);

        string[] expectedOps =
        [
            "authority.context_ingestion",
                "authority.graph",
                "authority.findings",
                "authority.decisioning",
                "authority.artifacts"
        ];

        string[] expectedStages =
        [
            "context_ingestion",
                "graph",
                "findings",
                "decisioning",
                "artifacts"
        ];

        foreach (string op in expectedOps)
        {
            stopped.Should().Contain(a => a.OperationName == op);
        }

        List<Activity> stages = stopped
            .Where(a => expectedOps.Contains(a.OperationName))
            .OrderBy(a => Array.IndexOf(expectedOps, a.OperationName))
            .ToList();

        stages.Should().HaveCount(expectedStages.Length);

        for (int i = 0; i < stages.Count; i++)
        {
            Activity child = stages[i];
            child.ParentId.Should().Be(parent.Id);
            child.GetTagItem("archlucid.run_id").Should().Be(ctx.Run.RunId.ToString("D"));
            child.GetTagItem("archlucid.stage.name").Should().Be(expectedStages[i]);
        }

    }

    [SkippableFact]
    public async Task ExecuteAfterRunPersistedAsync_records_stage_duration_metrics()
    {
        _ = ArchLucidInstrumentation.AuthorityPipelineStageDurationMilliseconds;

        List<HistogramMeasurement> histograms = [];

        using MeterListener meterListener = new();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name != ArchLucidInstrumentation.MeterName)
            {
                return;
            }

            if (instrument.Name != "archlucid_authority_pipeline_stage_duration_ms")
            {
                return;
            }

            listener.EnableMeasurementEvents(instrument);
        };

        meterListener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name != "archlucid_authority_pipeline_stage_duration_ms")
            {
                return;
            }

            List<KeyValuePair<string, object?>> tagList = [];
            foreach (KeyValuePair<string, object?> t in tags)
            {
                tagList.Add(t);
            }

            histograms.Add(new HistogramMeasurement(measurement, tagList));
        });

        meterListener.Start();

        (AuthorityPipelineStagesExecutor sut, _, _) = CreateExecutor();
        AuthorityPipelineContext ctx = CreateContext(runId: Guid.NewGuid());

        await sut.ExecuteAfterRunPersistedAsync(ctx, CancellationToken.None);

        histograms.Should().HaveCount(5);

        string[] stages = ["context_ingestion", "graph", "findings", "decisioning", "artifacts"];

        foreach (string stage in stages)
        {
            histograms.Should().Contain(h =>
                h.Tags.Any(t => t.Key == "stage" && Equals(t.Value, stage))
                && h.Tags.Any(t => t.Key == "outcome" && Equals(t.Value, "success")));
        }
    }

    [SkippableFact]
    public async Task ExecuteAfterRunPersistedAsync_propagates_error_status_on_stage_failure()
    {
        _ = ArchLucidInstrumentation.AuthorityPipelineStageDurationMilliseconds;

        List<Activity> stopped = [];

        using ActivityListener activityListener = new();
        activityListener.ShouldListenTo = s => s.Name == AuthorityRunSourceName;
        activityListener.Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded;
        activityListener.ActivityStopped = stopped.Add;

        ActivitySource.AddActivityListener(activityListener);

        List<HistogramMeasurement> histograms = [];

        using MeterListener meterListener = new();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == ArchLucidInstrumentation.MeterName
                && instrument.Name == "archlucid_authority_pipeline_stage_duration_ms")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        meterListener.SetMeasurementEventCallback<double>((_, measurement, tags, _) =>
        {
            List<KeyValuePair<string, object?>> tagList = [];
            foreach (KeyValuePair<string, object?> t in tags)
            {
                tagList.Add(t);
            }

            histograms.Add(new HistogramMeasurement(measurement, tagList));
        });

        meterListener.Start();

        Mock<IContextIngestionService> ingest = new();
        ingest
            .Setup(s => s.IngestAsync(It.IsAny<ContextIngestionRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("ingest failed"));

        AuthorityPipelineStagesExecutor sut = CreateExecutor(ingestMock: ingest).Executor;
        AuthorityPipelineContext ctx = CreateContext(runId: Guid.NewGuid());

        Func<Task> act = async () => await sut.ExecuteAfterRunPersistedAsync(ctx, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("ingest failed");

        Activity? failed = stopped.LastOrDefault(a => a.OperationName == "authority.context_ingestion");
        failed.Should().NotBeNull();
        failed.Status.Should().Be(ActivityStatusCode.Error);
        failed.StatusDescription.Should().Contain("ingest failed");
        failed.GetTagItem("error.type").Should().Be("InvalidOperationException");

        histograms.Should().ContainSingle(h =>
            h.Tags.Any(t => t.Key == "stage" && Equals(t.Value, "context_ingestion"))
            && h.Tags.Any(t => t.Key == "outcome" && Equals(t.Value, "error")));

    }

    [SkippableFact]
    public async Task ExecuteAfterRunPersistedAsync_works_when_run_activity_is_null()
    {
        _ = ArchLucidInstrumentation.AuthorityPipelineStageDurationMilliseconds;

        (AuthorityPipelineStagesExecutor sut, _, _) = CreateExecutor();
        AuthorityPipelineContext ctx = CreateContext(null, Guid.NewGuid());

        Func<Task> act = async () => await sut.ExecuteAfterRunPersistedAsync(ctx, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [SkippableFact]
    public async Task ExecuteAfterRunPersistedAsync_aborts_decisioning_when_findings_generation_failed()
    {
        _ = ArchLucidInstrumentation.AuthorityPipelineStageDurationMilliseconds;

        DateTime utc = DateTime.UtcNow;
        (AuthorityPipelineStagesExecutor sut, Mock<IDecisionEngine> decision, _) = CreateExecutor(
            configureFindings: s =>
            {
                s.GenerationStatus = FindingsSnapshotGenerationStatus.Failed;
                s.EngineFailures.Add(
                    new FindingEngineFailure
                    {
                        EngineType = "test",
                        Category = "Test",
                        ErrorMessage = "all engines failed",
                        ExceptionType = nameof(InvalidOperationException),
                        DurationMs = 1,
                        OccurredUtc = utc
                    });
            });

        AuthorityPipelineContext ctx = CreateContext(runId: Guid.NewGuid());

        Func<Task> act = async () => await sut.ExecuteAfterRunPersistedAsync(ctx, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*failed for all engines*");

        decision.Verify(
            d => d.DecideAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<GraphSnapshot>(),
                It.IsAny<FindingsSnapshot>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [SkippableFact]
    public async Task ExecuteAfterRunPersistedAsync_aborts_decisioning_when_findings_partial_and_halt_enabled()
    {
        _ = ArchLucidInstrumentation.AuthorityPipelineStageDurationMilliseconds;

        DateTime utc = DateTime.UtcNow;
        (AuthorityPipelineStagesExecutor sut, Mock<IDecisionEngine> decision, _) = CreateExecutor(
            configureFindings: s =>
            {
                s.GenerationStatus = FindingsSnapshotGenerationStatus.PartiallyComplete;
                s.EngineFailures.Add(
                    new FindingEngineFailure
                    {
                        EngineType = "llm-engine",
                        Category = "Test",
                        ErrorMessage = "circuit",
                        ExceptionType = nameof(InvalidOperationException),
                        DurationMs = 1,
                        OccurredUtc = utc
                    });
                s.Findings.Add(
                    new Finding
                    {
                        FindingType = "RequirementFinding",
                        Category = "Requirement",
                        Title = "partial",
                        Rationale = "r",
                        EngineType = "requirement",
                        Severity = FindingSeverity.Info
                    });
            },
            authorityPipelineOptions: new AuthorityPipelineOptions { HaltOnPartialFindings = true });

        AuthorityPipelineContext ctx = CreateContext(runId: Guid.NewGuid());

        Func<Task> act = async () => await sut.ExecuteAfterRunPersistedAsync(ctx, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*only partially complete*");

        decision.Verify(
            d => d.DecideAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<GraphSnapshot>(),
                It.IsAny<FindingsSnapshot>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [SkippableFact]
    public async Task ExecuteAfterRunPersistedAsync_runs_decisioning_when_findings_partial_and_halt_disabled()
    {
        _ = ArchLucidInstrumentation.AuthorityPipelineStageDurationMilliseconds;

        DateTime utc = DateTime.UtcNow;
        (AuthorityPipelineStagesExecutor sut, Mock<IDecisionEngine> decision, _) = CreateExecutor(
            configureFindings: s =>
            {
                s.GenerationStatus = FindingsSnapshotGenerationStatus.PartiallyComplete;
                s.EngineFailures.Add(
                    new FindingEngineFailure
                    {
                        EngineType = "llm-engine",
                        Category = "Test",
                        ErrorMessage = "circuit",
                        ExceptionType = nameof(InvalidOperationException),
                        DurationMs = 1,
                        OccurredUtc = utc
                    });
                s.Findings.Add(
                    new Finding
                    {
                        FindingType = "RequirementFinding",
                        Category = "Requirement",
                        Title = "partial",
                        Rationale = "r",
                        EngineType = "requirement",
                        Severity = FindingSeverity.Info
                    });
            },
            authorityPipelineOptions: new AuthorityPipelineOptions { HaltOnPartialFindings = false });

        AuthorityPipelineContext ctx = CreateContext(runId: Guid.NewGuid());

        await sut.ExecuteAfterRunPersistedAsync(ctx, CancellationToken.None);

        decision.Verify(
            d => d.DecideAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<GraphSnapshot>(),
                It.IsAny<FindingsSnapshot>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [SkippableFact]
    public async Task ExecuteAfterRunPersistedAsync_when_artifact_synthesis_throws_logs_ArtifactSynthesisFailed_and_rethrows()
    {
        Guid runGuid = Guid.NewGuid();
        (AuthorityPipelineStagesExecutor sut, _, Mock<IAuditService> audit) = CreateExecutor(
            configureSynthesis: s =>
                s.Setup(x => x.SynthesizeAsync(It.IsAny<ManifestDocument>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("synthesis failed")));

        AuthorityPipelineContext ctx = CreateContext(runId: runGuid);

        Func<Task> act = async () => await sut.ExecuteAfterRunPersistedAsync(ctx, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("synthesis failed");

        audit.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e =>
                    e.EventType == AuditEventTypes.ArtifactSynthesisFailed
                    && e.RunId == runGuid
                    && e.ManifestId.HasValue
                    && e.ManifestId.Value != Guid.Empty),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static AuthorityPipelineContext CreateContext(Activity? runActivity = null, Guid? runId = null)
    {
        Guid rid = runId ?? Guid.NewGuid();
        RunRecord run = new()
        {
            RunId = rid,
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ScopeProjectId = Guid.NewGuid(),
            ProjectId = "p1",
            CreatedUtc = DateTime.UtcNow
        };

        Mock<IArchLucidUnitOfWork> uow = new();
        uow.SetupGet(x => x.SupportsExternalTransaction).Returns(false);

        return new AuthorityPipelineContext
        {
            Run = run,
            Request = new ContextIngestionRequest { RunId = rid, ProjectId = "p1" },
            UnitOfWork = uow.Object,
            Scope = new ScopeContext
            {
                TenantId = run.TenantId,
                WorkspaceId = run.WorkspaceId,
                ProjectId = run.ScopeProjectId
            },
            RunActivity = runActivity
        };
    }

    private static (AuthorityPipelineStagesExecutor Executor, Mock<IDecisionEngine> Decision, Mock<IAuditService> Audit)
        CreateExecutor(
        Mock<IContextIngestionService>? ingestMock = null,
        Action<FindingsSnapshot>? configureFindings = null,
        AuthorityPipelineOptions? authorityPipelineOptions = null,
        Action<Mock<IArtifactSynthesisService>>? configureSynthesis = null)
    {
        Guid snapshotId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        Guid findingsId = Guid.NewGuid();
        Guid manifestId = Guid.NewGuid();
        Guid traceId = Guid.NewGuid();
        Guid bundleId = Guid.NewGuid();

        Mock<IRunRepository> runRepo = new();
        runRepo
            .Setup(r => r.UpdateAsync(It.IsAny<RunRecord>(), It.IsAny<CancellationToken>(), null, null))
            .Returns(Task.CompletedTask);

        Mock<IContextIngestionService> ingest = ingestMock ?? new Mock<IContextIngestionService>();

        if (ingestMock is null)
        {
            ingest
                .Setup(s => s.IngestAsync(It.IsAny<ContextIngestionRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    new ContextSnapshot
                    {
                        SnapshotId = snapshotId,
                        RunId = Guid.Empty,
                        ProjectId = "p1",
                        CreatedUtc = DateTime.UtcNow
                    });
        }

        Mock<IContextSnapshotRepository> ctxRepo = new();
        ctxRepo
            .Setup(r => r.GetLatestAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContextSnapshot?)null);
        ctxRepo
            .Setup(r => r.SaveAsync(It.IsAny<ContextSnapshot>(), It.IsAny<CancellationToken>(), null, null))
            .Returns(Task.CompletedTask);

        Mock<IKnowledgeGraphService> kg = new();
        kg
            .Setup(k => k.BuildSnapshotAsync(It.IsAny<ContextSnapshot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new GraphSnapshot
                {
                    GraphSnapshotId = graphId,
                    ContextSnapshotId = snapshotId,
                    RunId = Guid.Empty,
                    CreatedUtc = DateTime.UtcNow
                });

        Mock<IGraphSnapshotRepository> graphRepo = new();
        graphRepo
            .Setup(r => r.SaveAsync(It.IsAny<GraphSnapshot>(), It.IsAny<CancellationToken>(), null, null))
            .Returns(Task.CompletedTask);

        Mock<IFindingsOrchestrator> findingsOrch = new();
        FindingsSnapshot findingsReturn = new()
        {
            FindingsSnapshotId = findingsId,
            RunId = Guid.Empty,
            ContextSnapshotId = snapshotId,
            GraphSnapshotId = graphId,
            CreatedUtc = DateTime.UtcNow
        };

        configureFindings?.Invoke(findingsReturn);

        findingsOrch
            .Setup(f => f.GenerateFindingsSnapshotAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<GraphSnapshot>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(findingsReturn);

        Mock<IFindingsSnapshotRepository> findingsRepo = new();
        findingsRepo
            .Setup(r => r.SaveAsync(It.IsAny<FindingsSnapshot>(), It.IsAny<CancellationToken>(), null, null))
            .Returns(Task.CompletedTask);

        ManifestDocument manifest = new()
        {
            ManifestId = manifestId,
            RunId = Guid.Empty,
            ContextSnapshotId = snapshotId,
            GraphSnapshotId = graphId,
            FindingsSnapshotId = findingsId,
            DecisionTraceId = traceId,
            CreatedUtc = DateTime.UtcNow,
            ManifestHash = "h",
            RuleSetId = "r",
            RuleSetVersion = "1",
            RuleSetHash = "rh"
        };

        DecisionTrace trace = RuleAuditTrace.From(
            new RuleAuditTracePayload { DecisionTraceId = traceId, RunId = Guid.Empty, CreatedUtc = DateTime.UtcNow });

        Mock<IDecisionEngine> decision = new();
        decision
            .Setup(d => d.DecideAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<GraphSnapshot>(),
                It.IsAny<FindingsSnapshot>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((manifest, trace));

        Mock<IDecisionTraceRepository> traceRepo = new();
        traceRepo
            .Setup(r => r.SaveAsync(It.IsAny<DecisionTrace>(), It.IsAny<CancellationToken>(), null, null))
            .Returns(Task.CompletedTask);

        Mock<IGoldenManifestRepository> manifestRepo = new();
        manifestRepo
            .Setup(r => r.SaveAsync(It.IsAny<ManifestDocument>(), It.IsAny<CancellationToken>(), null, null))
            .Returns(Task.CompletedTask);

        Mock<IManifestHashService> hash = new();
        hash.Setup(h => h.ComputeHash(It.IsAny<ManifestDocument>())).Returns("computed");

        SynthesizedArtifact oneArtifact = new()
        {
            ArtifactId = Guid.NewGuid(),
            Name = "n",
            ArtifactType = "t",
            Format = "json",
            Content = "{}",
            ContentHash = "x"
        };

        Mock<IArtifactSynthesisService> synth = new();
        synth
            .Setup(s => s.SynthesizeAsync(It.IsAny<ManifestDocument>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ArtifactBundle
                {
                    BundleId = bundleId,
                    RunId = Guid.Empty,
                    ManifestId = manifestId,
                    CreatedUtc = DateTime.UtcNow,
                    Artifacts = [oneArtifact],
                    Trace = new SynthesisTrace { TraceId = Guid.NewGuid() }
                });

        configureSynthesis?.Invoke(synth);

        Mock<IArtifactBundleRepository> bundleRepo = new();
        bundleRepo
            .Setup(r => r.SaveAsync(It.IsAny<ArtifactBundle>(), It.IsAny<CancellationToken>(), null, null))
            .Returns(Task.CompletedTask);

        Mock<IAuditService> audit = new();
        audit.Setup(a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IOptionsMonitor<CosmosDbOptions>> cosmosDb = new();
        cosmosDb.SetupGet(m => m.CurrentValue).Returns(new CosmosDbOptions());

        Mock<IOptionsMonitor<AuthorityPipelineOptions>> apPipeline = new();
        apPipeline.Setup(m => m.CurrentValue).Returns(authorityPipelineOptions ?? new AuthorityPipelineOptions());

        return (new AuthorityPipelineStagesExecutor(
            runRepo.Object,
            ingest.Object,
            ctxRepo.Object,
            kg.Object,
            graphRepo.Object,
            findingsOrch.Object,
            findingsRepo.Object,
            decision.Object,
            traceRepo.Object,
            manifestRepo.Object,
            hash.Object,
            synth.Object,
            bundleRepo.Object,
            audit.Object,
            cosmosDb.Object,
            apPipeline.Object,
            NullLogger<AuthorityPipelineStagesExecutor>.Instance), decision, audit);
    }

    private sealed record HistogramMeasurement([UsedImplicitly] double Value, List<KeyValuePair<string, object?>> Tags);
}
