using ArchLucid.AgentRuntime.Evaluation;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Findings;

using FluentAssertions;

namespace ArchLucid.AgentRuntime.Tests.Evaluation;

public sealed class AgentOutputEvaluationHarnessTests
{
    private readonly IAgentOutputEvaluationHarness _harness = new AgentOutputEvaluationHarness(
        new AgentOutputEvaluator(),
        new AgentOutputSemanticEvaluator());

    [SkippableFact]
    [Trait("Suite", "Core")]
    public void Evaluate_empty_expectation_passes_for_minimal_result()
    {
        AgentResult actual = new()
        {
            TaskId = "t1",
            RunId = Guid.NewGuid().ToString("N"),
            AgentType = AgentType.Topology,
            Claims = ["c1"],
            EvidenceRefs = [],
            Confidence = 0.5
        };

        AgentOutputHarnessResult result = _harness.Evaluate(AgentType.Topology, actual, new AgentOutputExpectation());

        result.Passed.Should().BeTrue();
        result.Failures.Should().BeEmpty();
    }

    [SkippableFact]
    [Trait("Suite", "Core")]
    public void Evaluate_fails_when_minimum_finding_count_not_met()
    {
        AgentResult actual = new()
        {
            TaskId = "t1",
            RunId = Guid.NewGuid().ToString("N"),
            AgentType = AgentType.Topology,
            Claims = ["c1"],
            EvidenceRefs = [],
            Confidence = 0.5,
            Findings = []
        };

        AgentOutputExpectation expected = new() { MinimumFindingCount = 1 };

        AgentOutputHarnessResult result = _harness.Evaluate(AgentType.Topology, actual, expected);

        result.Passed.Should().BeFalse();
        result.Failures.Should().Contain(f => f.Contains("Finding count", StringComparison.Ordinal));
    }

    [SkippableFact]
    [Trait("Suite", "Core")]
    public void Evaluate_fails_when_expected_category_missing()
    {
        AgentResult actual = new()
        {
            TaskId = "t1",
            RunId = Guid.NewGuid().ToString("N"),
            AgentType = AgentType.Topology,
            Claims = ["c1"],
            EvidenceRefs = [],
            Confidence = 0.5,
            Findings =
            [
                new ArchitectureFinding { Category = "other", Message = "m", Severity = FindingSeverity.Info }
            ]
        };

        AgentOutputExpectation expected = new() { ExpectedFindingCategories = ["topology-gap"] };

        AgentOutputHarnessResult result = _harness.Evaluate(AgentType.Topology, actual, expected);

        result.Passed.Should().BeFalse();
        result.Failures.Should().Contain(f => f.Contains("topology-gap", StringComparison.OrdinalIgnoreCase));
    }

    [SkippableFact]
    [Trait("Suite", "Core")]
    public void Evaluate_passes_when_category_present()
    {
        AgentResult actual = new()
        {
            TaskId = "t1",
            RunId = Guid.NewGuid().ToString("N"),
            AgentType = AgentType.Topology,
            Claims = ["c1"],
            EvidenceRefs = [],
            Confidence = 0.5,
            Findings =
            [
                new ArchitectureFinding { Category = "topology-gap", Message = "m", Severity = FindingSeverity.Warning }
            ]
        };

        AgentOutputExpectation expected = new()
        {
            ExpectedFindingCategories = ["topology-gap"], MinimumFindingCount = 1
        };

        AgentOutputHarnessResult result = _harness.Evaluate(AgentType.Topology, actual, expected);

        result.Passed.Should().BeTrue();
        result.CategoryCoverageRatio.Should().Be(1.0);
    }

    [SkippableFact]
    [Trait("Suite", "Core")]
    public void Evaluate_fails_when_required_json_key_missing()
    {
        AgentResult actual = new()
        {
            TaskId = "t1",
            RunId = Guid.NewGuid().ToString("N"),
            AgentType = AgentType.Topology,
            Claims = ["c1"],
            EvidenceRefs = [],
            Confidence = 0.5
        };

        AgentOutputExpectation expected = new() { RequiredJsonKeys = ["nonexistentPropertyKey"] };

        AgentOutputHarnessResult result = _harness.Evaluate(AgentType.Topology, actual, expected);

        result.Passed.Should().BeFalse();
        result.Failures.Should().Contain(f => f.Contains("nonexistentPropertyKey", StringComparison.Ordinal));
    }
}
