using FluentAssertions;

namespace ArchLucid.Provenance.Tests;

[Trait("Category", "Unit")]
public sealed class GraphViewModelTests
{
    [Fact]
    public void GraphViewModel_ComputedCounts_AndIsEmpty()
    {
        GraphViewModel empty = new();
        empty.NodeCount.Should().Be(0);
        empty.EdgeCount.Should().Be(0);
        empty.IsEmpty.Should().BeTrue();

        GraphViewModel populated = new()
        {
            Nodes = [new GraphNodeVm(), new GraphNodeVm()], Edges = [new GraphEdgeVm()]
        };

        populated.NodeCount.Should().Be(2);
        populated.EdgeCount.Should().Be(1);
        populated.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void GraphNodesPageResponse_Defaults_AreEmptyAndZeroed()
    {
        GraphNodesPageResponse sut = new();

        sut.Page.Should().Be(0);
        sut.PageSize.Should().Be(0);
        sut.TotalNodes.Should().Be(0);
        sut.HasMore.Should().BeFalse();
        sut.Nodes.Should().NotBeNull().And.BeEmpty();
        sut.Edges.Should().NotBeNull().And.BeEmpty();
    }
}
