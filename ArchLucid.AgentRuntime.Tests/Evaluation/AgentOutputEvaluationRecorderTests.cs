using System.Diagnostics.Metrics;

using ArchLucid.AgentRuntime.Evaluation;
using ArchLucid.AgentRuntime.Evaluation.ReferenceCases;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Findings;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Persistence.Data.Repositories;

using FluentAssertions;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.AgentRuntime.Tests.Evaluation;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class AgentOutputEvaluationRecorderTests
{
    private static AgentOutputEvaluationRecorder CreateRecorder(
        IAgentExecutionTraceRepository traceRepository,
        ILogger<AgentOutputEvaluationRecorder> logger,
        AgentOutputQualityGateOptions? gateOptions = null)
    {
        AgentOutputQualityGateOptions opts = gateOptions ?? new AgentOutputQualityGateOptions { Enabled = false };

        Mock<IOptionsMonitor<AgentExecutionReferenceEvaluationOptions>> refOpts = new();
        refOpts.Setup(o => o.CurrentValue).Returns(new AgentExecutionReferenceEvaluationOptions { Enabled = false });

        AgentOutputReferenceCaseRunEvaluator referenceEvaluator = new(
            refOpts.Object,
            new EmptyReferenceCatalog(),
            new AgentOutputEvaluator(),
            new AgentOutputSemanticEvaluator(),
            new NoOpAgentOutputEvaluationResultRepository(),
            NullLogger<AgentOutputReferenceCaseRunEvaluator>.Instance);

        Mock<IAgentArchitectureFindingConfidenceEnricher> archFindingConfidence = new();
        archFindingConfidence
            .Setup(e => e.TryEnrichRunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new AgentOutputEvaluationRecorder(
            traceRepository,
            new AgentOutputEvaluator(),
            new AgentOutputSemanticEvaluator(),
            new AgentOutputQualityGate(Options.Create(opts)),
            Options.Create(opts),
            referenceEvaluator,
            archFindingConfidence.Object,
            logger);
    }

    [SkippableFact]
    public async Task EvaluateAndRecordMetricsAsync_logs_warning_when_semantic_score_below_product_threshold()
    {
        InMemoryAgentExecutionTraceRepository repo = new();
        CollectingLogger log = new();
        AgentOutputEvaluationRecorder sut = CreateRecorder(repo, log);

        const string json =
            """
            {"resultId":"a","taskId":"b","runId":"c","agentType":1,"claims":[],"evidenceRefs":[],"confidence":0.5,"findings":[],"proposedChanges":null,"createdUtc":"2026-01-01T00:00:00Z"}
            """;

        await repo.CreateAsync(
            new AgentExecutionTrace
            {
                TraceId = "t-low-semantic",
                RunId = "run-1",
                TaskId = "task-1",
                AgentType = AgentType.Topology,
                ParseSucceeded = true,
                ParsedResultJson = json
            },
            CancellationToken.None);

        await sut.EvaluateAndRecordMetricsAsync("run-1", CancellationToken.None);

        log.Entries.Should().Contain(e =>
            e.Level == LogLevel.Warning
            && e.Text.Contains("semantic score", StringComparison.OrdinalIgnoreCase));
    }

    [SkippableFact]
    public async Task EvaluateAndRecordMetricsAsync_when_no_eligible_traces_does_not_throw()
    {
        InMemoryAgentExecutionTraceRepository repo = new();
        AgentOutputEvaluationRecorder sut = CreateRecorder(repo, NullLogger<AgentOutputEvaluationRecorder>.Instance);

        Func<Task> act = async () => await sut.EvaluateAndRecordMetricsAsync("empty-run", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [SkippableFact]
    public async Task EvaluateAndRecordMetricsAsync_throws_when_EnforceOnReject_is_true_and_trace_is_below_floor()
    {
        InMemoryAgentExecutionTraceRepository repo = new();

        AgentOutputQualityGateOptions enforcingOpts = new()
        {
            Enabled = true,
            StructuralRejectBelow = 0.35,
            SemanticRejectBelow = 0.35,
            EnforceOnReject = true
        };

        AgentOutputEvaluationRecorder sut = CreateRecorder(
            repo,
            NullLogger<AgentOutputEvaluationRecorder>.Instance,
            enforcingOpts);

        // Empty claims + empty findings → structural and semantic scores both below the reject floor.
        const string hollowJson =
            """
            {"resultId":"a","taskId":"b","runId":"c","agentType":1,"claims":[],"evidenceRefs":[],"confidence":0.1,"findings":[],"proposedChanges":null,"createdUtc":"2026-01-01T00:00:00Z"}
            """;

        await repo.CreateAsync(
            new AgentExecutionTrace
            {
                TraceId = "t-hollow",
                RunId = "run-enforce",
                TaskId = "task-1",
                AgentType = AgentType.Topology,
                ParseSucceeded = true,
                ParsedResultJson = hollowJson
            },
            CancellationToken.None);

        Func<Task> act = async () =>
            await sut.EvaluateAndRecordMetricsAsync("run-enforce", CancellationToken.None);

        await act.Should().ThrowAsync<AgentOutputQualityGateRejectedException>()
            .WithMessage("*run-enforce*");
    }

    [SkippableFact]
    public async Task EvaluateAndRecordMetricsAsync_does_not_throw_when_EnforceOnReject_is_false_even_if_rejected()
    {
        InMemoryAgentExecutionTraceRepository repo = new();

        AgentOutputQualityGateOptions nonEnforcingOpts = new()
        {
            Enabled = true,
            StructuralRejectBelow = 0.35,
            SemanticRejectBelow = 0.35,
            EnforceOnReject = false
        };

        AgentOutputEvaluationRecorder sut = CreateRecorder(
            repo,
            NullLogger<AgentOutputEvaluationRecorder>.Instance,
            nonEnforcingOpts);

        const string hollowJson =
            """
            {"resultId":"a","taskId":"b","runId":"c","agentType":1,"claims":[],"evidenceRefs":[],"confidence":0.1,"findings":[],"proposedChanges":null,"createdUtc":"2026-01-01T00:00:00Z"}
            """;

        await repo.CreateAsync(
            new AgentExecutionTrace
            {
                TraceId = "t-hollow-no-enforce",
                RunId = "run-no-enforce",
                TaskId = "task-1",
                AgentType = AgentType.Topology,
                ParseSucceeded = true,
                ParsedResultJson = hollowJson
            },
            CancellationToken.None);

        Func<Task> act = async () =>
            await sut.EvaluateAndRecordMetricsAsync("run-no-enforce", CancellationToken.None);

        await act.Should().NotThrowAsync("EnforceOnReject=false must preserve existing metrics-only behaviour");
    }

    [SkippableFact]
    public async Task EvaluateAndRecordMetricsAsync_sets_quality_warning_flag_when_gate_warns()
    {
        InMemoryAgentExecutionTraceRepository repo = new();

        AgentOutputQualityGateOptions gateOpts = new()
        {
            Enabled = true
        };

        AgentOutputEvaluationRecorder sut = CreateRecorder(
            repo,
            NullLogger<AgentOutputEvaluationRecorder>.Instance,
            gateOpts);

        // Top-level citations array must be non-empty or the recorder upgrades the gate to Rejected and skips QualityWarning patching.
        const string hollowJson =
            """
            {"resultId":"a","taskId":"b","runId":"c","agentType":1,"claims":[],"evidenceRefs":[],"confidence":0.1,"findings":[],"proposedChanges":null,"createdUtc":"2026-01-01T00:00:00Z","citations":[{"source":"stub"}]}
            """;

        await repo.CreateAsync(
            new AgentExecutionTrace
            {
                TraceId = "t-quality-warn",
                RunId = "run-quality-warn",
                TaskId = "task-1",
                AgentType = AgentType.Topology,
                ParseSucceeded = true,
                ParsedResultJson = hollowJson
            },
            CancellationToken.None);

        await sut.EvaluateAndRecordMetricsAsync("run-quality-warn", CancellationToken.None);

        AgentExecutionTrace? updated = await repo.GetByTraceIdAsync("t-quality-warn", CancellationToken.None);

        updated.Should().NotBeNull();
        updated!.QualityWarning.Should().BeTrue("warn-only gate persists UI/summary signal on the trace row");
    }

    [SkippableFact]
    public async Task EvaluateAndRecordMetricsAsync_records_structural_and_semantic_histograms_for_eligible_trace()
    {
        _ = ArchLucidInstrumentation.AgentOutputStructuralCompletenessRatio;
        _ = ArchLucidInstrumentation.AgentOutputSemanticScore;

        InMemoryAgentExecutionTraceRepository repo = new();
        const string json =
            """
            {"resultId":"a","taskId":"b","runId":"c","agentType":1,"claims":[{"text":"x","evidence":"y"}],"evidenceRefs":[],"confidence":0.5,"findings":[{"severity":"High","description":"Long enough description text","recommendation":"Fix it"}],"proposedChanges":null,"createdUtc":"2026-01-01T00:00:00Z"}
            """;

        await repo.CreateAsync(
            new AgentExecutionTrace
            {
                TraceId = "t-metrics",
                RunId = "run-metrics",
                TaskId = "task-1",
                AgentType = AgentType.Topology,
                ParseSucceeded = true,
                ParsedResultJson = json
            },
            CancellationToken.None);

        using EvaluationHistogramCapture capture = EvaluationHistogramCapture.Start();
        AgentOutputEvaluationRecorder sut = CreateRecorder(repo, NullLogger<AgentOutputEvaluationRecorder>.Instance);

        await sut.EvaluateAndRecordMetricsAsync("run-metrics", CancellationToken.None);

        IReadOnlyList<DoubleMeasurementRecord> structural =
            capture.MeasurementsFor("archlucid_agent_output_structural_completeness_ratio");
        IReadOnlyList<DoubleMeasurementRecord> semantic =
            capture.MeasurementsFor("archlucid_agent_output_semantic_score");

        structural.Should().ContainSingle();
        structural[0].Value.Should().Be(1.0);
        semantic.Should().ContainSingle();
        semantic[0].Value.Should().BeGreaterThan(0.0);
    }

    private sealed class EmptyReferenceCatalog : IAgentOutputReferenceCaseCatalog
    {
        public IReadOnlyList<AgentOutputReferenceCaseDefinition> Cases => [];
    }

    private sealed class CollectingLogger : ILogger<AgentOutputEvaluationRecorder>
    {
        public List<(LogLevel Level, string Text)> Entries
        {
            get;
        } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NullDisposable.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }

    private readonly record struct DoubleMeasurementRecord(
        string Name,
        double Value,
        [UsedImplicitly] IReadOnlyList<KeyValuePair<string, object?>> Tags);

    private sealed class EvaluationHistogramCapture : IDisposable
    {
        private readonly MeterListener _listener = new();
        private readonly List<DoubleMeasurementRecord> _measures = [];

        private EvaluationHistogramCapture()
        {
            _listener.InstrumentPublished = OnInstrumentPublished;
            _listener.SetMeasurementEventCallback<double>(OnMeasurementDouble);
            _listener.Start();
        }

        public void Dispose()
        {
            _listener.Dispose();
        }

        public static EvaluationHistogramCapture Start()
        {
            return new EvaluationHistogramCapture();
        }

        public IReadOnlyList<DoubleMeasurementRecord> MeasurementsFor(string instrumentName)
        {
            return _measures.Where(m => m.Name == instrumentName).ToList();
        }

        private void OnInstrumentPublished(Instrument instrument, MeterListener meterListener)
        {
            if (instrument.Meter.Name != ArchLucidInstrumentation.MeterName)
            {
                return;
            }

            if (instrument.Name is "archlucid_agent_output_structural_completeness_ratio"
                or "archlucid_agent_output_semantic_score")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        }

        private void OnMeasurementDouble(
            Instrument instrument,
            double measurement,
            ReadOnlySpan<KeyValuePair<string, object?>> tags,
            object? state)
        {
            _ = state;
            List<KeyValuePair<string, object?>> tagList = [];

            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagList.Add(tag);
            }

            _measures.Add(new DoubleMeasurementRecord(instrument.Name, measurement, tagList));
        }
    }
}
