using ArchiForge.Provenance;
using ArchiForge.Provenance.Services;

using FluentAssertions;

namespace ArchiForge.Provenance.Tests;

[Trait("Category", "Unit")]
public sealed class ProvenanceGraphViewMapperTests
{
    [Fact]
    public void ToViewModel_EmptyGraph_YieldsEmptyListsAndIsEmpty()
    {
        DecisionProvenanceGraph graph = new();

        GraphViewModel vm = ProvenanceGraphViewMapper.ToViewModel(graph);

        vm.Nodes.Should().BeEmpty();
        vm.Edges.Should().BeEmpty();
        vm.IsEmpty.Should().BeTrue();
        vm.NodeCount.Should().Be(0);
        vm.EdgeCount.Should().Be(0);
    }

    [Fact]
    public void ToViewModel_MapsNodes_WithMetadataAndTypes()
    {
        Guid nodeId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        DecisionProvenanceGraph graph = new()
        {
            Nodes =
            [
                new ProvenanceNode
                {
                    Id = nodeId,
                    Name = "Node A",
                    Type = ProvenanceNodeType.Decision,
                    ReferenceId = "ref",
                    Metadata = new Dictionary<string, string> { ["k"] = "v" }
                }
            ]
        };

        GraphViewModel vm = ProvenanceGraphViewMapper.ToViewModel(graph);

        GraphNodeVm node = vm.Nodes.Should().ContainSingle().Subject;
        node.Id.Should().Be(nodeId.ToString("D"));
        node.Label.Should().Be("Node A");
        node.Type.Should().Be(ProvenanceNodeType.Decision.ToString());
        node.Metadata.Should().NotBeNull();
        node.Metadata!.Should().ContainKey("k");
        node.Metadata["k"].Should().Be("v");
        node.Metadata.Should().NotBeSameAs(graph.Nodes[0].Metadata);
    }

    [Fact]
    public void ToViewModel_MapsNodes_EmptyMetadata_AsNull()
    {
        Guid nodeId = Guid.NewGuid();
        DecisionProvenanceGraph graph = new()
        {
            Nodes =
            [
                new ProvenanceNode
                {
                    Id = nodeId,
                    Name = "Bare",
                    Type = ProvenanceNodeType.Finding,
                    ReferenceId = "r",
                    Metadata = []
                }
            ]
        };

        GraphViewModel vm = ProvenanceGraphViewMapper.ToViewModel(graph);

        GraphNodeVm node = vm.Nodes[0];
        node.Metadata.Should().BeNull();
    }

    [Fact]
    public void ToViewModel_MapsEdges_SourceTargetAndType()
    {
        Guid from = Guid.Parse("10000000-0000-0000-0000-000000000001");
        Guid to = Guid.Parse("20000000-0000-0000-0000-000000000002");
        DecisionProvenanceGraph graph = new()
        {
            Edges =
            [
                new ProvenanceEdge
                {
                    Id = Guid.NewGuid(),
                    FromNodeId = from,
                    ToNodeId = to,
                    Type = ProvenanceEdgeType.SupportedBy
                }
            ]
        };

        GraphViewModel vm = ProvenanceGraphViewMapper.ToViewModel(graph);

        GraphEdgeVm edge = vm.Edges.Should().ContainSingle().Subject;
        edge.Source.Should().Be(from.ToString("D"));
        edge.Target.Should().Be(to.ToString("D"));
        edge.Type.Should().Be(ProvenanceEdgeType.SupportedBy.ToString());
    }
}
