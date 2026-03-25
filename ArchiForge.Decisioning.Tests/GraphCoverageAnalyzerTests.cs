using ArchiForge.Decisioning.Analysis;
using ArchiForge.KnowledgeGraph.Models;

using FluentAssertions;

namespace ArchiForge.Decisioning.Tests;

public sealed class GraphCoverageAnalyzerTests
{
    private readonly GraphCoverageAnalyzer _analyzer = new();

    [Fact]
    public void AnalyzeRequirements_marks_requirement_with_RELATES_TO_as_covered()
    {
        GraphSnapshot graph = new GraphSnapshot
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
}
