using System.Text.Json;

using ArchLucid.AgentRuntime.Evaluation;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;

using FluentAssertions;

namespace ArchLucid.AgentRuntime.Tests.Evaluation;

/// <summary>
/// Golden JSON fixtures deserialized to <see cref="AgentResult"/> then scored by <see cref="IAgentOutputEvaluationHarness"/> (serializer round-trip).
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class AgentOutputEvaluationHarnessGoldenFixtureTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    private readonly IAgentOutputEvaluationHarness _harness = new AgentOutputEvaluationHarness(
        new AgentOutputEvaluator(),
        new AgentOutputSemanticEvaluator());

    [Fact]
    public void Harness_topology_fixture_meets_full_structural_completeness()
    {
        AgentResult actual = LoadAgentResult("harness-agent-result-topology.json");

        AgentOutputExpectation expected = new()
        {
            MinimumStructuralCompleteness = 1.0,
            RequiredJsonKeys = ["evidenceRefs"],
        };

        AgentOutputHarnessResult result = _harness.Evaluate(AgentType.Topology, actual, expected);

        result.Passed.Should().BeTrue();
        result.StructuralCompletenessRatio.Should().Be(1.0);
    }

    [Fact]
    public void Harness_compliance_fixture_meets_full_structural_completeness()
    {
        AgentResult actual = LoadAgentResult("harness-agent-result-compliance.json");

        AgentOutputExpectation expected = new()
        {
            MinimumStructuralCompleteness = 1.0,
            RequiredJsonKeys = ["evidenceRefs", "findings"],
        };

        AgentOutputHarnessResult result = _harness.Evaluate(AgentType.Compliance, actual, expected);

        result.Passed.Should().BeTrue();
    }

    [Fact]
    public void Harness_topology_fixture_fails_when_findings_cleared_after_load()
    {
        AgentResult actual = LoadAgentResult("harness-agent-result-topology.json");
        actual.Findings.Clear();

        AgentOutputExpectation expected = new()
        {
            MinimumStructuralCompleteness = 1.0,
            MinimumFindingCount = 1,
        };

        AgentOutputHarnessResult result = _harness.Evaluate(AgentType.Topology, actual, expected);

        result.Passed.Should().BeFalse();
        result.Failures.Should().Contain(f => f.Contains("Finding count", StringComparison.Ordinal));
    }

    private static AgentResult LoadAgentResult(string fileName)
    {
        string dir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "GoldenAgentResults");
        string path = Path.Combine(dir, fileName);
        string json = File.ReadAllText(path);

        AgentResult? actual = JsonSerializer.Deserialize<AgentResult>(json, WebJson);

        actual.Should().NotBeNull();

        return actual;
    }
}
