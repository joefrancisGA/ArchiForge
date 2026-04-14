using ArchLucid.AgentRuntime.Evaluation;
using ArchLucid.AgentRuntime.Evaluation.ReferenceCases;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Core.Configuration;
using ArchLucid.Persistence.Data.Repositories;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.AgentRuntime.Tests.Evaluation;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class AgentOutputEvaluationRecorderTests
{
    private sealed class EmptyReferenceCatalog : IAgentOutputReferenceCaseCatalog
    {
        public IReadOnlyList<AgentOutputReferenceCaseDefinition> Cases => [];
    }

    private sealed class CollectingLogger : ILogger<AgentOutputEvaluationRecorder>
    {
        public List<(LogLevel Level, string Text)> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => NullDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

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

    private static AgentOutputEvaluationRecorder CreateRecorder(
        IAgentExecutionTraceRepository traceRepository,
        ILogger<AgentOutputEvaluationRecorder> logger)
    {
        Mock<IOptionsMonitor<AgentExecutionReferenceEvaluationOptions>> refOpts = new();
        refOpts.Setup(o => o.CurrentValue).Returns(new AgentExecutionReferenceEvaluationOptions { Enabled = false });

        AgentOutputReferenceCaseRunEvaluator referenceEvaluator = new(
            refOpts.Object,
            new EmptyReferenceCatalog(),
            new AgentOutputEvaluator(),
            new AgentOutputSemanticEvaluator(),
            new NoOpAgentOutputEvaluationResultRepository(),
            NullLogger<AgentOutputReferenceCaseRunEvaluator>.Instance);

        return new AgentOutputEvaluationRecorder(
            traceRepository,
            new AgentOutputEvaluator(),
            new AgentOutputSemanticEvaluator(),
            new AgentOutputQualityGate(Options.Create(new AgentOutputQualityGateOptions { Enabled = false })),
            referenceEvaluator,
            logger);
    }

    [Fact]
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
                ParsedResultJson = json,
            },
            CancellationToken.None);

        await sut.EvaluateAndRecordMetricsAsync("run-1", CancellationToken.None);

        log.Entries.Should().Contain(e =>
            e.Level == LogLevel.Warning
            && e.Text.Contains("semantic score", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EvaluateAndRecordMetricsAsync_when_no_eligible_traces_does_not_throw()
    {
        InMemoryAgentExecutionTraceRepository repo = new();
        AgentOutputEvaluationRecorder sut = CreateRecorder(repo, NullLogger<AgentOutputEvaluationRecorder>.Instance);

        Func<Task> act = async () => await sut.EvaluateAndRecordMetricsAsync("empty-run", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
