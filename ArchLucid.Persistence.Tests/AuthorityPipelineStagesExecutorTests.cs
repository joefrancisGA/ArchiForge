using System.Diagnostics;
using System.Diagnostics.Metrics;

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
using ArchLucid.Persistence.Cosmos;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Orchestration.Pipeline;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Persistence.Tests;

/// <summary>
/// <see cref="AuthorityPipelineStagesExecutor"/> OTel span parenting, stage tags, histogram, and error propagation.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class AuthorityPipelineStagesExecutorTests
{
    private const string AuthorityRunSourceName = "ArchLucid.AuthorityRun";

    [Fact]
    public async Task ExecuteAfterRunPersistedAsync_creates_child_activities_under_run_activity()
    {
        _ = ArchLucidInstrumentation.AuthorityPipelineStageDurationMilliseconds;

        List<Activity> stopped = [];

        using ActivityListener listener = new()
        {
            ShouldListenTo = s => s.Name == AuthorityRunSourceName,
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stopped.Add
        };

        ActivitySource.AddActivityListener(listener);

        try
        {
            using Activity? parent = ArchLucidInstrumentation.AuthorityRun.StartActivity(
                "authority.run.test",
                ActivityKind.Internal);

            parent.Should().NotBeNull();

            AuthorityPipelineStagesExecutor sut = CreateExecutor();
            AuthorityPipelineContext ctx = CreateContext(parent, runId: Guid.NewGuid());

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
        finally
        {
            listener.Dispose();
        }
    }

    [Fact]
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

        meterListener.SetMeasurementEventCallback<double>(
            (instrument, measurement, tags, _) =>
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

        try
        {
            AuthorityPipelineStagesExecutor sut = CreateExecutor();
            AuthorityPipelineContext ctx = CreateContext(runId: Guid.NewGuid());

            await sut.ExecuteAfterRunPersistedAsync(ctx, CancellationToken.None);
        }
        finally
        {
            meterListener.Dispose();
        }

        histograms.Should().HaveCount(5);

        string[] stages = ["context_ingestion", "graph", "findings", "decisioning", "artifacts"];

        foreach (string stage in stages)
        {
            histograms.Should().Contain(h =>
                h.Tags.Any(t => t.Key == "stage" && Equals(t.Value, stage))
                && h.Tags.Any(t => t.Key == "outcome" && Equals(t.Value, "success")));
        }
    }

    [Fact]
    public async Task ExecuteAfterRunPersistedAsync_propagates_error_status_on_stage_failure()
    {
        _ = ArchLucidInstrumentation.AuthorityPipelineStageDurationMilliseconds;

        List<Activity> stopped = [];

        using ActivityListener activityListener = new()
        {
            ShouldListenTo = s => s.Name == AuthorityRunSourceName,
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stopped.Add
        };

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

        meterListener.SetMeasurementEventCallback<double>(
            (_, measurement, tags, _) =>
            {
                List<KeyValuePair<string, object?>> tagList = [];
                foreach (KeyValuePair<string, object?> t in tags)
                {
                    tagList.Add(t);
                }

                histograms.Add(new HistogramMeasurement(measurement, tagList));
            });

        meterListener.Start();

        try
        {
            Mock<IContextIngestionService> ingest = new();
            ingest
                .Setup(s => s.IngestAsync(It.IsAny<ContextIngestionRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("ingest failed"));

            AuthorityPipelineStagesExecutor sut = CreateExecutor(ingest);
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
        finally
        {
            meterListener.Dispose();
            activityListener.Dispose();
        }
    }

    [Fact]
    public async Task ExecuteAfterRunPersistedAsync_works_when_run_activity_is_null()
    {
        _ = ArchLucidInstrumentation.AuthorityPipelineStageDurationMilliseconds;

        AuthorityPipelineStagesExecutor sut = CreateExecutor();
        AuthorityPipelineContext ctx = CreateContext(runActivity: null, runId: Guid.NewGuid());

        Func<Task> act = async () => await sut.ExecuteAfterRunPersistedAsync(ctx, CancellationToken.None);

        await act.Should().NotThrowAsync();
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

    private static AuthorityPipelineStagesExecutor CreateExecutor(Mock<IContextIngestionService>? ingestMock = null)
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
        findingsOrch
            .Setup(
                f => f.GenerateFindingsSnapshotAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<GraphSnapshot>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new FindingsSnapshot
                {
                    FindingsSnapshotId = findingsId,
                    RunId = Guid.Empty,
                    ContextSnapshotId = snapshotId,
                    GraphSnapshotId = graphId,
                    CreatedUtc = DateTime.UtcNow
                });

        Mock<IFindingsSnapshotRepository> findingsRepo = new();
        findingsRepo
            .Setup(r => r.SaveAsync(It.IsAny<FindingsSnapshot>(), It.IsAny<CancellationToken>(), null, null))
            .Returns(Task.CompletedTask);

        GoldenManifest manifest = new()
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
            new RuleAuditTracePayload
            {
                DecisionTraceId = traceId,
                RunId = Guid.Empty,
                CreatedUtc = DateTime.UtcNow
            });

        Mock<IDecisionEngine> decision = new();
        decision
            .Setup(
                d => d.DecideAsync(
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
            .Setup(r => r.SaveAsync(It.IsAny<GoldenManifest>(), It.IsAny<CancellationToken>(), null, null))
            .Returns(Task.CompletedTask);

        Mock<IManifestHashService> hash = new();
        hash.Setup(h => h.ComputeHash(It.IsAny<GoldenManifest>())).Returns("computed");

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
            .Setup(s => s.SynthesizeAsync(It.IsAny<GoldenManifest>(), It.IsAny<CancellationToken>()))
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

        Mock<IArtifactBundleRepository> bundleRepo = new();
        bundleRepo
            .Setup(r => r.SaveAsync(It.IsAny<ArtifactBundle>(), It.IsAny<CancellationToken>(), null, null))
            .Returns(Task.CompletedTask);

        Mock<IAuditService> audit = new();
        audit.Setup(a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IOptionsMonitor<CosmosDbOptions>> cosmosDb = new();
        cosmosDb.SetupGet(m => m.CurrentValue).Returns(new CosmosDbOptions());

        return new AuthorityPipelineStagesExecutor(
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
            NullLogger<AuthorityPipelineStagesExecutor>.Instance);
    }

    private sealed record HistogramMeasurement(double Value, List<KeyValuePair<string, object?>> Tags);
}
