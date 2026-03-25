using ArchiForge.KnowledgeGraph.Models;
using ArchiForge.Persistence.Serialization;

using FluentAssertions;

namespace ArchiForge.Decisioning.Tests;

/// <summary>
/// Ensures legacy / alternate JSON shapes for stored graph rows deserialize via <see cref="JsonEntitySerializer"/>.
/// </summary>
public sealed class JsonEntitySerializerGraphCompatibilityTests
{
    [Fact]
    public void Deserializes_graph_nodes_using_legacy_id_type_name_aliases()
    {
        const string json = """
            [
              {
                "id": "n1",
                "type": "TopologyResource",
                "name": "core-vnet",
                "category": "network",
                "properties": {}
              }
            ]
            """;

        List<GraphNode> nodes = JsonEntitySerializer.Deserialize<List<GraphNode>>(json);

        nodes.Should().ContainSingle();
        nodes[0].NodeId.Should().Be("n1");
        nodes[0].NodeType.Should().Be("TopologyResource");
        nodes[0].Label.Should().Be("core-vnet");
        nodes[0].Category.Should().Be("network");
    }

    [Fact]
    public void Deserializes_graph_edges_using_legacy_property_aliases()
    {
        const string json = """
            [
              {
                "id": "e1",
                "from": "a",
                "to": "b",
                "type": "CONTAINS"
              }
            ]
            """;

        List<GraphEdge> edges = JsonEntitySerializer.Deserialize<List<GraphEdge>>(json);

        edges.Should().ContainSingle();
        edges[0].EdgeId.Should().Be("e1");
        edges[0].FromNodeId.Should().Be("a");
        edges[0].ToNodeId.Should().Be("b");
        edges[0].EdgeType.Should().Be("CONTAINS");
    }
}
