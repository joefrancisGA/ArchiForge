using ArchLucid.Provenance.Services;

using FluentAssertions;

namespace ArchLucid.Provenance.Tests;

[Trait("Category", "Unit")]
public sealed class ProvenanceGraphAlgorithmsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryResolveDecisionNodeId_ReturnsFalse_WhenKeyIsMissing(string? decisionKey)
    {
        DecisionProvenanceGraph graph = SampleGraph();

        bool ok = ProvenanceGraphAlgorithms.TryResolveDecisionNodeId(graph, decisionKey, out Guid id);

        ok.Should().BeFalse();
        id.Should().Be(Guid.Empty);
    }

    [Fact]
    public void TryResolveDecisionNodeId_Resolves_ByReferenceId()
    {
        DecisionProvenanceGraph graph = SampleGraph();

        bool ok = ProvenanceGraphAlgorithms.TryResolveDecisionNodeId(graph, "ref-a", out Guid id);

        ok.Should().BeTrue();
        id.Should().Be(graph.Nodes[0].Id);
    }

    [Fact]
    public void TryResolveDecisionNodeId_Resolves_ByDecisionNodeIdString()
    {
        DecisionProvenanceGraph graph = SampleGraph();
        string key = graph.Nodes[0].Id.ToString("D");

        bool ok = ProvenanceGraphAlgorithms.TryResolveDecisionNodeId(graph, key, out Guid id);

        ok.Should().BeTrue();
        id.Should().Be(graph.Nodes[0].Id);
    }

    [Fact]
    public void TryResolveDecisionNodeId_Resolves_ByReferenceIdMatchingGuidFormats()
    {
        Guid external = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        DecisionProvenanceGraph graph = new()
        {
            Id = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            Nodes =
            [
                new ProvenanceNode
                {
                    Id = Guid.NewGuid(),
                    Type = ProvenanceNodeType.Decision,
                    ReferenceId = external.ToString("N"),
                    Name = "D"
                }
            ],
            Edges = []
        };

        bool okN = ProvenanceGraphAlgorithms.TryResolveDecisionNodeId(graph, external.ToString("N"), out Guid idN);
        bool okD = ProvenanceGraphAlgorithms.TryResolveDecisionNodeId(graph, external.ToString("D"), out Guid idD);

        okN.Should().BeTrue();
        okD.Should().BeTrue();
        idN.Should().Be(graph.Nodes[0].Id);
        idD.Should().Be(graph.Nodes[0].Id);
    }

    [Fact]
    public void ExtractDecisionSubgraph_ReturnsEmpty_WhenDecisionIdUnknown()
    {
        DecisionProvenanceGraph graph = SampleGraph();

        DecisionProvenanceGraph sub = ProvenanceGraphAlgorithms.ExtractDecisionSubgraph(graph, Guid.NewGuid());

        sub.Nodes.Should().BeEmpty();
        sub.Edges.Should().BeEmpty();
        sub.Id.Should().Be(graph.Id);
        sub.RunId.Should().Be(graph.RunId);
    }

    [Fact]
    public void ExtractDecisionSubgraph_IncludesDecision_AndIncidentEdges()
    {
        DecisionProvenanceGraph graph = SampleGraph();
        Guid decisionId = graph.Nodes[0].Id;

        DecisionProvenanceGraph sub = ProvenanceGraphAlgorithms.ExtractDecisionSubgraph(graph, decisionId);

        sub.Nodes.Should().HaveCount(2);
        sub.Edges.Should().HaveCount(1);
        sub.Nodes.Select(n => n.Id).Should().Contain(decisionId);
    }

    [Fact]
    public void ExtractNeighborhood_ReturnsEmpty_WhenStartUnknown()
    {
        DecisionProvenanceGraph graph = SampleGraph();

        DecisionProvenanceGraph n = ProvenanceGraphAlgorithms.ExtractNeighborhood(graph, Guid.NewGuid(), 2);

        n.Nodes.Should().BeEmpty();
        n.Edges.Should().BeEmpty();
    }

    [Fact]
    public void ExtractNeighborhood_AtDepthZero_ReturnsStartNodeOnly()
    {
        DecisionProvenanceGraph graph = SampleGraph();
        Guid start = graph.Nodes[0].Id;

        DecisionProvenanceGraph n = ProvenanceGraphAlgorithms.ExtractNeighborhood(graph, start, 0);

        n.Nodes.Should().HaveCount(1);
        n.Nodes[0].Id.Should().Be(start);
        n.Edges.Should().BeEmpty();
    }

    private static DecisionProvenanceGraph SampleGraph()
    {
        Guid decisionId = Guid.Parse("10101010-1010-1010-1010-101010101010");
        Guid otherId = Guid.Parse("20202020-2020-2020-2020-202020202020");
        Guid edgeId = Guid.Parse("30303030-3030-3030-3030-303030303030");

        return new DecisionProvenanceGraph
        {
            Id = Guid.Parse("40404040-4040-4040-4040-404040404040"),
            RunId = Guid.Parse("50505050-5050-5050-5050-505050505050"),
            Nodes =
            [
                new ProvenanceNode
                {
                    Id = decisionId, Type = ProvenanceNodeType.Decision, ReferenceId = "ref-a", Name = "Decision A"
                },
                new ProvenanceNode
                {
                    Id = otherId, Type = ProvenanceNodeType.Finding, ReferenceId = "f-1", Name = "Finding"
                }
            ],
            Edges =
            [
                new ProvenanceEdge
                {
                    Id = edgeId, FromNodeId = decisionId, ToNodeId = otherId, Type = ProvenanceEdgeType.SupportedBy
                }
            ]
        };
    }
}
