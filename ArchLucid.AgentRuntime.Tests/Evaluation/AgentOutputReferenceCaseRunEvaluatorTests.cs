using ArchLucid.AgentRuntime.Evaluation;
using ArchLucid.AgentRuntime.Evaluation.ReferenceCases;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Persistence.Data.Repositories;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.AgentRuntime.Tests.Evaluation;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class AgentOutputReferenceCaseRunEvaluatorTests
{
    [Fact]
    public async Task EvaluateTraceAsync_when_enabled_appends_row_for_matching_agent_type()
    {
        Mock<IOptionsMonitor<AgentExecutionReferenceEvaluationOptions>> options = new();
        options.Setup(o => o.CurrentValue).Returns(
            new AgentExecutionReferenceEvaluationOptions { Enabled = true });

        string parsedJson =
            """
            {"resultId":"r1","taskId":"t1","runId":"run-1","agentType":"Topology","claims":[],"evidenceRefs":[],"confidence":0.5,"findings":[]}
            """;

        IReadOnlyList<AgentOutputReferenceCaseDefinition> cases =
        [
            new() { CaseId = "case-a", AgentType = AgentType.Topology }
        ];

        FixedCatalog catalog = new(cases);
        Mock<IAgentOutputEvaluationResultRepository> results = new();
        results
            .Setup(r => r.AppendAsync(It.IsAny<AgentOutputEvaluationResultInsert>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        AgentOutputReferenceCaseRunEvaluator sut = new(
            options.Object,
            catalog,
            new AgentOutputEvaluator(),
            new AgentOutputSemanticEvaluator(),
            results.Object,
            NullLogger<AgentOutputReferenceCaseRunEvaluator>.Instance);

        AgentExecutionTrace trace = new()
        {
            TraceId = "tr1",
            RunId = "run-1",
            TaskId = "t1",
            AgentType = AgentType.Topology,
            ParseSucceeded = true,
            ParsedResultJson = parsedJson
        };

        await sut.EvaluateTraceAsync(trace, "run-1", CancellationToken.None);

        results.Verify(
            r => r.AppendAsync(
                It.Is<AgentOutputEvaluationResultInsert>(row =>
                    row.CaseId == "case-a"
                    && row.TraceId == "tr1"
                    && row.RunId == "run-1"
                    && row.AgentType == AgentType.Topology),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EvaluateTraceAsync_when_disabled_does_not_append()
    {
        Mock<IOptionsMonitor<AgentExecutionReferenceEvaluationOptions>> options = new();
        options.Setup(o => o.CurrentValue).Returns(
            new AgentExecutionReferenceEvaluationOptions { Enabled = false });

        FixedCatalog catalog =
            new([new AgentOutputReferenceCaseDefinition { CaseId = "x", AgentType = AgentType.Topology }]);
        Mock<IAgentOutputEvaluationResultRepository> results = new();

        AgentOutputReferenceCaseRunEvaluator sut = new(
            options.Object,
            catalog,
            new AgentOutputEvaluator(),
            new AgentOutputSemanticEvaluator(),
            results.Object,
            NullLogger<AgentOutputReferenceCaseRunEvaluator>.Instance);

        AgentExecutionTrace trace = new()
        {
            TraceId = "tr1", AgentType = AgentType.Topology, ParseSucceeded = true, ParsedResultJson = "{}"
        };

        await sut.EvaluateTraceAsync(trace, "run-1", CancellationToken.None);

        results.Verify(
            r => r.AppendAsync(It.IsAny<AgentOutputEvaluationResultInsert>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private sealed class FixedCatalog(IReadOnlyList<AgentOutputReferenceCaseDefinition> cases)
        : IAgentOutputReferenceCaseCatalog
    {
        public IReadOnlyList<AgentOutputReferenceCaseDefinition> Cases
        {
            get;
        } = cases;
    }
}
