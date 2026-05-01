using System.Text.Json;

using ArchLucid.KnowledgeGraph.Models;
using ArchLucid.Persistence.Serialization;

namespace ArchLucid.Persistence.Tests.Serialization;

public sealed class GraphJsonConverterTests
{
    private static JsonSerializerOptions NodeOptions()
    {
        JsonSerializerOptions o = new();
        o.Converters.Add(new GraphNodeJsonConverter());

        return o;
    }

    private static JsonSerializerOptions EdgeOptions()
    {
        JsonSerializerOptions o = new();
        o.Converters.Add(new GraphEdgeJsonConverter());

        return o;
    }

    [SkippableFact]
    public void GraphNodeJsonConverter_Read_uses_alternate_property_names()
    {
        const string json = """{"id":"n1","type":"svc","name":"API","category":"c","sourceType":"st","sourceId":"sid","properties":{"k":"v"}}""";

        GraphNode node = JsonSerializer.Deserialize<GraphNode>(json, NodeOptions())!;

        node.NodeId.Should().Be("n1");
        node.NodeType.Should().Be("svc");
        node.Label.Should().Be("API");
        node.Category.Should().Be("c");
        node.SourceType.Should().Be("st");
        node.SourceId.Should().Be("sid");
        node.Properties.Should().ContainKey("k").WhoseValue.Should().Be("v");
    }

    [SkippableFact]
    public void GraphNodeJsonConverter_Read_non_object_throws()
    {
        Action act = () => JsonSerializer.Deserialize<GraphNode>("[]", NodeOptions());

        act.Should().Throw<JsonException>();
    }

    [SkippableFact]
    public void GraphNodeJsonConverter_Read_missing_properties_yields_empty_dictionary()
    {
        const string json = """{"nodeId":"n1","nodeType":"t","label":"l"}""";

        GraphNode node = JsonSerializer.Deserialize<GraphNode>(json, NodeOptions())!;

        node.Properties.Should().BeEmpty();
    }

    [SkippableFact]
    public void GraphNodeJsonConverter_Read_invalid_properties_object_yields_empty_dictionary_after_deserialize_failure()
    {
        const string json = """{"nodeId":"n1","nodeType":"t","label":"l","properties":{"bad":123}}""";

        GraphNode node = JsonSerializer.Deserialize<GraphNode>(json, NodeOptions())!;

        node.Properties.Should().BeEmpty();
    }

    [SkippableFact]
    public void GraphNodeJsonConverter_Write_round_trips_null_optional_fields()
    {
        GraphNode original = new()
        {
            NodeId = "a",
            NodeType = "b",
            Label = "c",
            Category = null,
            SourceType = null,
            SourceId = null,
            Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["x"] = "y" }
        };

        string json = JsonSerializer.Serialize(original, NodeOptions());
        GraphNode back = JsonSerializer.Deserialize<GraphNode>(json, NodeOptions())!;

        back.Should().BeEquivalentTo(original);
    }

    [SkippableFact]
    public void GraphEdgeJsonConverter_Read_uses_alternate_property_names()
    {
        const string json =
            """{"id":"e1","from":"a","to":"b","type":"calls","label":"L","properties":{"p":"q"}}""";

        GraphEdge edge = JsonSerializer.Deserialize<GraphEdge>(json, EdgeOptions())!;

        edge.EdgeId.Should().Be("e1");
        edge.FromNodeId.Should().Be("a");
        edge.ToNodeId.Should().Be("b");
        edge.EdgeType.Should().Be("calls");
        edge.Label.Should().Be("L");
        edge.Properties["p"].Should().Be("q");
    }

    [SkippableFact]
    public void GraphEdgeJsonConverter_Read_source_target_aliases_resolve()
    {
        const string json = """{"edgeId":"e","source":"s","target":"t","relation":"r"}""";

        GraphEdge edge = JsonSerializer.Deserialize<GraphEdge>(json, EdgeOptions())!;

        edge.FromNodeId.Should().Be("s");
        edge.ToNodeId.Should().Be("t");
        edge.EdgeType.Should().Be("r");
    }

    [SkippableFact]
    public void GraphEdgeJsonConverter_Read_non_object_throws()
    {
        Action act = () => JsonSerializer.Deserialize<GraphEdge>("true", EdgeOptions());

        act.Should().Throw<JsonException>();
    }

    [SkippableFact]
    public void GraphEdgeJsonConverter_Write_null_label_emits_null_json()
    {
        GraphEdge edge = new()
        {
            EdgeId = "e",
            FromNodeId = "a",
            ToNodeId = "b",
            EdgeType = "t",
            Label = null,
            Properties = []
        };

        string json = JsonSerializer.Serialize(edge, EdgeOptions());

        json.Should().Contain("\"label\":null");
    }
}
