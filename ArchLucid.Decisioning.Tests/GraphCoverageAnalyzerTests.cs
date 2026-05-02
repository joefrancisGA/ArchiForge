using ArchLucid.Decisioning.Analysis;
using ArchLucid.KnowledgeGraph;
using ArchLucid.KnowledgeGraph.Models;

using FluentAssertions;

namespace ArchLucid.Decisioning.Tests;

/// <summary>
/// Tests for Graph Coverage Analyzer.
/// </summary>
[Trait("Category", "Unit")]
public sealed class GraphCoverageAnalyzerTests
{
    private readonly GraphCoverageAnalyzer _analyzer = new();

    [Fact]
    public void AnalyzeRequirements_marks_requirement_with_RELATES_TO_as_covered()
    {
        GraphSnapshot graph = new()
        {
            Nodes =
            [
                new GraphNode { NodeId = "r1", NodeType = "Requirement", Label = "R1", Properties = new() },
                new GraphNode { NodeId = "t1", NodeType = "TopologyResource", Label = "net", Category = "network", Properties = new() }
            ],
            Edges =
            [
                new GraphEdge
                {
                    EdgeId = "e1",
                    FromNodeId = "r1",
                    ToNodeId = "t1",
                    EdgeType = "RELATES_TO",
                    Label = "relates to"
                }
            ]
        };

        RequirementCoverageResult result = _analyzer.AnalyzeRequirements(graph);

        result.RelatedRequirementCount.Should().Be(1);
        result.UnrelatedRequirementCount.Should().Be(0);
        result.CoveredRequirements.Should().Contain("R1");
    }

    [Fact]
    public void AnalyzeSecurity_ignores_PROTECTS_edges_below_decisioning_weight()
    {
        GraphSnapshot graph = new()
        {
            Nodes =
            [
                new GraphNode { NodeId = "s1", NodeType = "SecurityBaseline", Label = "baseline", Properties = new() },
                new GraphNode { NodeId = "t1", NodeType = "TopologyResource", Label = "vm", Category = "compute", Properties = new() }
            ],
            Edges =
            [
                new GraphEdge
                {
                    EdgeId = "e1",
                    FromNodeId = "s1",
                    ToNodeId = "t1",
                    EdgeType = "PROTECTS",
                    Label = "protects",
                    Weight = GraphEdgeDecisioningThresholds.MinWeightForSemanticLink - 0.01d
                }
            ]
        };

        SecurityCoverageResult result = _analyzer.AnalyzeSecurity(graph);

        result.ProtectedResourceCount.Should().Be(0);
        result.UnprotectedResourceCount.Should().Be(1);
    }
}
