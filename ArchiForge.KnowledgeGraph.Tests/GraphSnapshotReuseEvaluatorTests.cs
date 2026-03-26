using ArchiForge.ContextIngestion.Models;
using ArchiForge.KnowledgeGraph.Interfaces;
using ArchiForge.KnowledgeGraph.Models;
using ArchiForge.KnowledgeGraph.Repositories;
using ArchiForge.KnowledgeGraph.Services;

using FluentAssertions;

using Moq;

namespace ArchiForge.KnowledgeGraph.Tests;

[Trait("Category", "Unit")]
public sealed class GraphSnapshotReuseEvaluatorTests
{
    [Fact]
    public async Task ResolveAsync_WhenPriorNull_BuildsFresh()
    {
        Mock<IKnowledgeGraphService> kg = new();
        GraphSnapshot built = new()
        {
            GraphSnapshotId = Guid.NewGuid(),
            ContextSnapshotId = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            CreatedUtc = DateTime.UtcNow
        };
        kg.Setup(x => x.BuildSnapshotAsync(It.IsAny<ContextSnapshot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(built);

        InMemoryGraphSnapshotRepository graphs = new();
        ContextSnapshot current = CreateSnapshot("p1", [new CanonicalObject { ObjectId = "a", ObjectType = "t", Name = "n", SourceType = "s", SourceId = "1" }]);

        GraphSnapshot result = await GraphSnapshotReuseEvaluator.ResolveAsync(
            null,
            current,
            Guid.NewGuid(),
            kg.Object,
            graphs,
            CancellationToken.None);

        result.Should().BeSameAs(built);
        kg.Verify(x => x.BuildSnapshotAsync(current, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_WhenFingerprintDiffers_BuildsFresh()
    {
        Mock<IKnowledgeGraphService> kg = new();
        kg.Setup(x => x.BuildSnapshotAsync(It.IsAny<ContextSnapshot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContextSnapshot c, CancellationToken _) => new GraphSnapshot
            {
                GraphSnapshotId = Guid.NewGuid(),
                ContextSnapshotId = c.SnapshotId,
                RunId = Guid.NewGuid(),
                CreatedUtc = DateTime.UtcNow
            });

        InMemoryGraphSnapshotRepository graphs = new();
        ContextSnapshot prior = CreateSnapshot("p1", [new CanonicalObject { ObjectId = "a", ObjectType = "t", Name = "n", SourceType = "s", SourceId = "1" }]);
        ContextSnapshot current = CreateSnapshot("p1", [new CanonicalObject { ObjectId = "b", ObjectType = "t", Name = "n", SourceType = "s", SourceId = "1" }]);

        _ = await GraphSnapshotReuseEvaluator.ResolveAsync(
            prior,
            current,
            Guid.NewGuid(),
            kg.Object,
            graphs,
            CancellationToken.None);

        kg.Verify(x => x.BuildSnapshotAsync(current, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_WhenEquivalentButNoPriorGraph_BuildsFresh()
    {
        Mock<IKnowledgeGraphService> kg = new();
        kg.Setup(x => x.BuildSnapshotAsync(It.IsAny<ContextSnapshot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContextSnapshot c, CancellationToken _) => new GraphSnapshot
            {
                GraphSnapshotId = Guid.NewGuid(),
                ContextSnapshotId = c.SnapshotId,
                RunId = Guid.NewGuid(),
                CreatedUtc = DateTime.UtcNow
            });

        List<CanonicalObject> objects =
        [
            new() { ObjectId = "a", ObjectType = "TopologyResource", Name = "vnet", SourceType = "h", SourceId = "1" }
        ];
        ContextSnapshot prior = CreateSnapshot("p1", objects);
        ContextSnapshot current = CreateSnapshot("p1", objects);

        _ = await GraphSnapshotReuseEvaluator.ResolveAsync(
            prior,
            current,
            Guid.NewGuid(),
            kg.Object,
            new InMemoryGraphSnapshotRepository(),
            CancellationToken.None);

        kg.Verify(x => x.BuildSnapshotAsync(current, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_WhenEquivalentAndPriorGraphExists_ClonesWithoutCallingBuild()
    {
        Mock<IKnowledgeGraphService> kg = new();
        InMemoryGraphSnapshotRepository graphs = new();

        List<CanonicalObject> objects =
        [
            new() { ObjectId = "a", ObjectType = "TopologyResource", Name = "vnet", SourceType = "h", SourceId = "1" }
        ];
        ContextSnapshot prior = CreateSnapshot("p1", objects);
        ContextSnapshot current = CreateSnapshot("p1", objects);

        GraphSnapshot priorGraph = new()
        {
            GraphSnapshotId = Guid.NewGuid(),
            ContextSnapshotId = prior.SnapshotId,
            RunId = Guid.NewGuid(),
            CreatedUtc = DateTime.UtcNow.AddDays(-1),
            Nodes =
            [
                new GraphNode { NodeId = "n1", NodeType = "TopologyResource", Label = "vnet", Properties = new() }
            ],
            Edges = []
        };
        await graphs.SaveAsync(priorGraph, CancellationToken.None);

        Guid runId = Guid.NewGuid();
        GraphSnapshot result = await GraphSnapshotReuseEvaluator.ResolveAsync(
            prior,
            current,
            runId,
            kg.Object,
            graphs,
            CancellationToken.None);

        kg.Verify(x => x.BuildSnapshotAsync(It.IsAny<ContextSnapshot>(), It.IsAny<CancellationToken>()), Times.Never);
        result.ContextSnapshotId.Should().Be(current.SnapshotId);
        result.RunId.Should().Be(runId);
        result.GraphSnapshotId.Should().NotBe(priorGraph.GraphSnapshotId);
        result.Nodes.Should().HaveCount(1);
        result.Nodes[0].NodeId.Should().Be("n1");
    }

    private static ContextSnapshot CreateSnapshot(string projectId, List<CanonicalObject> objects)
    {
        return new ContextSnapshot
        {
            SnapshotId = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            ProjectId = projectId,
            CreatedUtc = DateTime.UtcNow,
            CanonicalObjects = objects
        };
    }
}
