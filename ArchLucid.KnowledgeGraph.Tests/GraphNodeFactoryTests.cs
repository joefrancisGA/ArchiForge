using ArchLucid.ContextIngestion.Models;
using ArchLucid.KnowledgeGraph.Mapping;
using ArchLucid.KnowledgeGraph.Models;

using FluentAssertions;

namespace ArchLucid.KnowledgeGraph.Tests;

/// <summary>
///     Tests for Graph Node Factory.
/// </summary>
[Trait("Category", "Unit")]
public sealed class GraphNodeFactoryTests
{
    private readonly GraphNodeFactory _sut = new();

    [Fact]
    public void CreateNode_NullItem_ThrowsArgumentNullException()
    {
        Action act = () => _sut.CreateNode(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateNode_SetsNodeIdFromObjectId()
    {
        CanonicalObject item = BuildItem();

        GraphNode node = _sut.CreateNode(item);

        node.NodeId.Should().Be($"obj-{item.ObjectId}");
    }

    [Fact]
    public void CreateNode_SetsNodeTypeFromObjectType()
    {
        CanonicalObject item = BuildItem(GraphNodeTypes.TopologyResource);

        GraphNode node = _sut.CreateNode(item);

        node.NodeType.Should().Be(GraphNodeTypes.TopologyResource);
    }

    [Fact]
    public void CreateNode_SetsLabelFromName()
    {
        CanonicalObject item = BuildItem(name: "My Resource");

        GraphNode node = _sut.CreateNode(item);

        node.Label.Should().Be("My Resource");
    }

    [Fact]
    public void CreateNode_SetsCategoryFromProperties()
    {
        CanonicalObject item = BuildItem(properties: new Dictionary<string, string>
        {
            ["category"] = GraphTopologyCategories.Storage
        });

        GraphNode node = _sut.CreateNode(item);

        node.Category.Should().Be(GraphTopologyCategories.Storage);
    }

    [Fact]
    public void CreateNode_NoCategoryProperty_CategoryIsNull()
    {
        CanonicalObject item = BuildItem();

        GraphNode node = _sut.CreateNode(item);

        node.Category.Should().BeNull();
    }

    [Fact]
    public void CreateNode_CopiesAllProperties()
    {
        CanonicalObject item = BuildItem(properties: new Dictionary<string, string>
        {
            ["text"] = "hello", ["ref"] = "POL-001"
        });

        GraphNode node = _sut.CreateNode(item);

        node.Properties.Should().ContainKey("text").WhoseValue.Should().Be("hello");
        node.Properties.Should().ContainKey("ref").WhoseValue.Should().Be("POL-001");
    }

    [Fact]
    public void CreateNode_SetsSourceTypeAndSourceId()
    {
        CanonicalObject item = BuildItem(sourceType: "InlineConnector", sourceId: "src-42");

        GraphNode node = _sut.CreateNode(item);

        node.SourceType.Should().Be("InlineConnector");
        node.SourceId.Should().Be("src-42");
    }

    private static CanonicalObject BuildItem(
        string objectType = GraphNodeTypes.TopologyResource,
        string name = "test-item",
        string sourceType = "Test",
        string sourceId = "s1",
        Dictionary<string, string>? properties = null)
    {
        return new CanonicalObject
        {
            ObjectType = objectType,
            Name = name,
            SourceType = sourceType,
            SourceId = sourceId,
            Properties = properties ?? []
        };
    }
}
