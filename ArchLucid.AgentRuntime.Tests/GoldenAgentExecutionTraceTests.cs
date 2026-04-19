using System.Text.Json;

using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;

using FluentAssertions;

namespace ArchLucid.AgentRuntime.Tests;

/// <summary>
/// Regression fixtures for persisted <see cref="AgentExecutionTrace"/> JSON shape (see <c>docs/AGENT_OUTPUT_EVALUATION.md</c>).
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class GoldenAgentExecutionTraceTests
{
    [Fact]
    public void Fixture_golden_simulator_deserializes_with_expected_model_metadata()
    {
        AgentExecutionTrace trace = LoadTrace("golden-simulator.json");

        trace.ModelDeploymentName.Should().Be(AgentExecutionTraceModelMetadata.SimulatorDeploymentName);
        trace.ModelVersion.Should().Be(AgentExecutionTraceModelMetadata.SimulatorModelVersion);
        trace.ParseSucceeded.Should().BeTrue();
        trace.ParsedResultJson.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Fixture_golden_unspecified_deserializes_with_expected_sentinels()
    {
        AgentExecutionTrace trace = LoadTrace("golden-unspecified.json");

        trace.ModelDeploymentName.Should().Be(AgentExecutionTraceModelMetadata.UnspecifiedDeploymentName);
        trace.ModelVersion.Should().Be(AgentExecutionTraceModelMetadata.UnspecifiedModelVersion);
        trace.ParseSucceeded.Should().BeFalse();
    }

    [Fact]
    public void Fixture_golden_parse_success_has_parsed_json_when_parse_succeeded()
    {
        AgentExecutionTrace trace = LoadTrace("golden-parse-success.json");

        trace.ParseSucceeded.Should().BeTrue();
        trace.ParsedResultJson.Should().NotBeNullOrWhiteSpace();
    }

    private static AgentExecutionTrace LoadTrace(string fileName)
    {
        string dir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "AgentExecutionTrace");
        string path = Path.Combine(dir, fileName);

        string json = File.ReadAllText(path);
        AgentExecutionTrace? trace = JsonSerializer.Deserialize<AgentExecutionTrace>(json, ContractJson.Default);

        trace.Should().NotBeNull();

        return trace;
    }
}
