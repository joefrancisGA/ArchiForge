using ArchLucid.ContextIngestion.Connectors;
using ArchLucid.ContextIngestion.Models;
using ArchLucid.ContextIngestion.Topology;

using FluentAssertions;

namespace ArchLucid.ContextIngestion.Tests;

/// <summary>
///     Tests for Topology Hints Connector Parent.
/// </summary>
[Trait("Category", "Unit")]
public sealed class TopologyHintsConnectorParentTests
{
    [Fact]
    public async Task NormalizeAsync_SlashSeparatedHint_SetsParentNodeIdToStableParent()
    {
        TopologyHintsConnector sut = new();
        RawContextPayload raw = new() { TopologyHints = ["parentNet/childSubnet"] };

        NormalizedContextBatch batch = await sut.NormalizeAsync(raw, CancellationToken.None);

        CanonicalObject child = batch.CanonicalObjects.Single();
        child.Properties.Should().ContainKey("parentNodeId");
        string expectedParentId = $"obj-{TopologyHintStableObjectIds.FromHintName("parentNet")}";
        child.Properties["parentNodeId"].Should().Be(expectedParentId);
        child.ObjectId.Should().Be(TopologyHintStableObjectIds.FromHintName("parentNet/childSubnet"));
    }

    [Fact]
    public async Task NormalizeAsync_PlainHint_HasNoParentNodeId()
    {
        TopologyHintsConnector sut = new();
        RawContextPayload raw = new() { TopologyHints = ["standalone-vnet"] };

        NormalizedContextBatch batch = await sut.NormalizeAsync(raw, CancellationToken.None);

        batch.CanonicalObjects.Single().Properties.Should().NotContainKey("parentNodeId");
    }
}
