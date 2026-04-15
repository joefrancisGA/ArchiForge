using ArchLucid.Decisioning.Findings.Payloads;
using ArchLucid.Decisioning.Models;
using ArchLucid.Decisioning.Services;
using ArchLucid.KnowledgeGraph.Models;

using FluentAssertions;

namespace ArchLucid.Decisioning.Tests;

/// <summary>
/// Tests for Cost Constraint Finding Engine.
/// </summary>

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class CostConstraintFindingEngineTests
{
    private readonly CostConstraintFindingEngine _sut = new();

    [Fact]
    public async Task AnalyzeAsync_ReturnsEmpty_WhenNoCostNodes()
    {
        GraphSnapshot graph = new() { Nodes = [], Edges = [] };

        IReadOnlyList<Finding> findings = await _sut.AnalyzeAsync(graph, CancellationToken.None);

        findings.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_MapsHighCostRisk_ToWarningSeverity()
    {
        GraphSnapshot graph = new()
        {
            Nodes =
            [
                new GraphNode
                {
                    NodeId = "c1",
                    NodeType = "CostConstraint",
                    Label = "prod-budget",
                    Properties = new Dictionary<string, string>
                    {
                        ["budgetName"] = "Prod",
                        ["maxMonthlyCost"] = "5000",
                        ["costRisk"] = "high"
                    }
                }
            ],
            Edges = []
        };

        IReadOnlyList<Finding> findings = await _sut.AnalyzeAsync(graph, CancellationToken.None);

        Finding f = findings.Should().ContainSingle().Subject;
        f.Severity.Should().Be(FindingSeverity.Warning);
        f.PayloadType.Should().Be(nameof(CostConstraintFindingPayload));
        CostConstraintFindingPayload payload = f.Payload.Should().BeOfType<CostConstraintFindingPayload>().Subject;
        payload.BudgetName.Should().Be("Prod");
        payload.MaxMonthlyCost.Should().Be(5000m);
        payload.CostRisk.Should().Be("high");
        f.Trace.DecisionsTaken.Should().NotBeEmpty();
        f.Trace.RulesApplied.Should().Contain("cost-constraint-surface");
        f.Trace.Notes.Should().NotBeEmpty();
        f.Trace.AlternativePathsConsidered.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_NonHighRisk_UsesInfoSeverity()
    {
        GraphSnapshot graph = new()
        {
            Nodes =
            [
                new GraphNode
                {
                    NodeId = "c2",
                    NodeType = "CostConstraint",
                    Label = "low",
                    Properties = new Dictionary<string, string> { ["costRisk"] = "low" }
                }
            ],
            Edges = []
        };

        IReadOnlyList<Finding> findings = await _sut.AnalyzeAsync(graph, CancellationToken.None);

        Finding f = findings.Should().ContainSingle().Subject;
        f.Severity.Should().Be(FindingSeverity.Info);
        f.Trace.DecisionsTaken.Should().NotBeEmpty();
        f.Trace.AlternativePathsConsidered.Should().NotBeEmpty();
    }
}
