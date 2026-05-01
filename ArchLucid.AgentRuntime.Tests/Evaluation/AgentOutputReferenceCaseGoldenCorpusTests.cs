using System.Text.Json;
using System.Text.Json.Nodes;

using ArchLucid.AgentRuntime.Evaluation;
using ArchLucid.AgentRuntime.Evaluation.ReferenceCases;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Persistence.Data.Repositories;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.AgentRuntime.Tests.Evaluation;

/// <summary>Golden corpus under tests/golden-corpus â€” CI validates deserialization + evaluator wiring.</summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class AgentOutputReferenceCaseGoldenCorpusTests
{
    private const string GoldenCorpusRelativePath = "golden-corpus/agent-output-reference-cases.json";

    [SkippableFact]
    public void Golden_corpus_json_deserializes_one_case_per_agent_type()
    {
        string file = Path.Combine(AppContext.BaseDirectory, GoldenCorpusRelativePath);

        File.Exists(file).Should().BeTrue($"Copy tests/golden-corpus to output (see .csproj): {file}");

        string json = File.ReadAllText(file);
        JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        List<AgentOutputReferenceCaseDefinition>? list =
            JsonSerializer.Deserialize<List<AgentOutputReferenceCaseDefinition>>(json, options);

        list.Should().NotBeNull();
        list.Count.Should().Be(4);
        list.Select(c => c.AgentType).Distinct().Count().Should().Be(4);
        list.Should().OnlyContain(c => !string.IsNullOrWhiteSpace(c.CaseId));
    }

    [SkippableFact]
    public void Catalog_loads_golden_corpus_when_enabled()
    {
        Mock<IOptionsMonitor<AgentExecutionReferenceEvaluationOptions>> options = new();
        options.Setup(o => o.CurrentValue).Returns(
            new AgentExecutionReferenceEvaluationOptions
            {
                Enabled = true,
                ReferenceCasesPath = GoldenCorpusRelativePath
            });

        AgentOutputReferenceCaseCatalog catalog = new(
            options.Object,
            AppContext.BaseDirectory,
            NullLogger<AgentOutputReferenceCaseCatalog>.Instance);

        catalog.Cases.Count.Should().Be(4);
    }

    [Theory]
    [InlineData(AgentType.Topology)]
    [InlineData(AgentType.Cost)]
    [InlineData(AgentType.Compliance)]
    [InlineData(AgentType.Critic)]
    public async Task Reference_evaluator_records_row_for_each_agent_type_against_golden_payload(AgentType agentType)
    {
        string basePath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "GoldenAgentResults",
            "golden-agent-result-valid.json");

        string baseJson = await File.ReadAllTextAsync(basePath);

        JsonNode? root = JsonNode.Parse(baseJson);
        root!["agentType"] = (int)agentType;

        string payload = root.ToJsonString(new JsonSerializerOptions { WriteIndented = false });

        Mock<IOptionsMonitor<AgentExecutionReferenceEvaluationOptions>> evaluatorOpts = new();
        evaluatorOpts.Setup(o => o.CurrentValue).Returns(
            new AgentExecutionReferenceEvaluationOptions { Enabled = true });

        Mock<IOptionsMonitor<AgentExecutionReferenceEvaluationOptions>> catalogOpts = new();
        catalogOpts.Setup(o => o.CurrentValue).Returns(
            new AgentExecutionReferenceEvaluationOptions
            {
                Enabled = true,
                ReferenceCasesPath = GoldenCorpusRelativePath
            });

        AgentOutputReferenceCaseCatalog catalog = new(
            catalogOpts.Object,
            AppContext.BaseDirectory,
            NullLogger<AgentOutputReferenceCaseCatalog>.Instance);

        Mock<IAgentOutputEvaluationResultRepository> results = new();

        results
            .Setup(r => r.AppendAsync(It.IsAny<AgentOutputEvaluationResultInsert>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        AgentOutputReferenceCaseRunEvaluator sut = new(
            evaluatorOpts.Object,
            catalog,
            new AgentOutputEvaluator(),
            new AgentOutputSemanticEvaluator(),
            results.Object,
            NullLogger<AgentOutputReferenceCaseRunEvaluator>.Instance);

        AgentExecutionTrace trace = new()
        {
            TraceId = $"golden-{agentType}",
            RunId = "run-golden-corps",
            TaskId = "task-1",
            AgentType = agentType,
            ParseSucceeded = true,
            ParsedResultJson = payload
        };

        await sut.EvaluateTraceAsync(trace, "run-golden-corps", CancellationToken.None);

        results.Verify(
            r => r.AppendAsync(
                It.Is<AgentOutputEvaluationResultInsert>(row =>
                    row.AgentType == agentType && row.TraceId == trace.TraceId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
