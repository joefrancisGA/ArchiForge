using System.Text.Json;

using ArchiForge.Provenance;

using FluentAssertions;

namespace ArchiForge.Provenance.Tests;

[Trait("Category", "Unit")]
public sealed class ProvenanceGraphSerializerTests
{
    [Fact]
    public void Deserialize_ReturnsNull_ForNullOrWhitespace()
    {
        ProvenanceGraphSerializer.Deserialize(null!).Should().BeNull();
        ProvenanceGraphSerializer.Deserialize("").Should().BeNull();
        ProvenanceGraphSerializer.Deserialize("   ").Should().BeNull();
    }

    [Fact]
    public void SerializeDeserialize_RoundTrips_MinimalGraph()
    {
        Guid graphId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid runId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid nodeId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        DecisionProvenanceGraph graph = new()
        {
            Id = graphId,
            RunId = runId,
            Nodes =
            [
                new ProvenanceNode
                {
                    Id = nodeId,
                    Type = ProvenanceNodeType.Decision,
                    ReferenceId = "dec-1",
                    Name = "D1"
                }
            ],
            Edges = []
        };

        string json = ProvenanceGraphSerializer.Serialize(graph);
        DecisionProvenanceGraph? back = ProvenanceGraphSerializer.Deserialize(json);

        back.Should().NotBeNull();
        back!.Id.Should().Be(graphId);
        back.RunId.Should().Be(runId);
        back.Nodes.Should().HaveCount(1);
        back.Nodes[0].Id.Should().Be(nodeId);
        back.Nodes[0].ReferenceId.Should().Be("dec-1");
    }

    [Fact]
    public void Deserialize_ThrowsInvalidOperation_WhenJsonIsCorrupt()
    {
        Action act = () => ProvenanceGraphSerializer.Deserialize("{ not json");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Provenance graph JSON is corrupt and cannot be deserialized.")
            .WithInnerException<JsonException>();
    }
}
