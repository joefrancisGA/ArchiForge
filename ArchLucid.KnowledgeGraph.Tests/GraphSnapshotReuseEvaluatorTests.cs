using ArchLucid.ContextIngestion.Models;
using ArchLucid.KnowledgeGraph.Interfaces;
using ArchLucid.KnowledgeGraph.Models;
using ArchLucid.KnowledgeGraph.Repositories;
using ArchLucid.KnowledgeGraph.Services;

using FluentAssertions;

using Moq;

namespace ArchLucid.KnowledgeGraph.Tests;

/// <summary>
///     Tests for Graph Snapshot Reuse Evaluator.
/// </summary>
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
        ContextSnapshot current = CreateSnapshot("p1",
        [
            new CanonicalObject
            {
                ObjectId = "a",
                ObjectType = "t",
                Name = "n",
                SourceType = "s",
                SourceId = "1"
            }
        ]);

        GraphSnapshotResolutionResult result = await GraphSnapshotReuseEvaluator.ResolveAsync(
            null,
            current,
            Guid.NewGuid(),
            kg.Object,
            graphs,
            CancellationToken.None);

        result.Snapshot.Should().BeSameAs(built);
        result.ResolutionMode.Should().Be("fresh_canonical_change");
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
        ContextSnapshot prior = CreateSnapshot("p1",
        [
            new CanonicalObject
            {
                ObjectId = "a",
                ObjectType = "t",
                Name = "n",
                SourceType = "s",
                SourceId = "1"
            }
        ]);
        ContextSnapshot current = CreateSnapshot("p1",
        [
            new CanonicalObject
            {
                ObjectId = "b",
                ObjectType = "t",
                Name = "n",
                SourceType = "s",
                SourceId = "1"
            }
        ]);

        GraphSnapshotResolutionResult diff = await GraphSnapshotReuseEvaluator.ResolveAsync(
            prior,
            current,
            Guid.NewGuid(),
            kg.Object,
            graphs,
            CancellationToken.None);

        diff.ResolutionMode.Should().Be("fresh_canonical_change");
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
            new()
            {
                ObjectId = "a",
                ObjectType = "TopologyResource",
                Name = "vnet",
                SourceType = "h",
                SourceId = "1"
            }
        ];
        ContextSnapshot prior = CreateSnapshot("p1", objects);
        ContextSnapshot current = CreateSnapshot("p1", objects);

        GraphSnapshotResolutionResult noGraph = await GraphSnapshotReuseEvaluator.ResolveAsync(
            prior,
            current,
            Guid.NewGuid(),
            kg.Object,
            new InMemoryGraphSnapshotRepository(),
            CancellationToken.None);

        noGraph.ResolutionMode.Should().Be("fresh_no_stored_graph");
        kg.Verify(x => x.BuildSnapshotAsync(current, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_WhenEquivalentAndPriorGraphExists_ClonesWithoutCallingBuild()
    {
        Mock<IKnowledgeGraphService> kg = new();
        InMemoryGraphSnapshotRepository graphs = new();

        List<CanonicalObject> objects =
        [
            new()
            {
                ObjectId = "a",
                ObjectType = "TopologyResource",
                Name = "vnet",
                SourceType = "h",
                SourceId = "1"
            }
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
                new GraphNode
                {
                    NodeId = "n1",
                    NodeType = "TopologyResource",
                    Label = "vnet",
                    Properties = new Dictionary<string, string>()
                }
            ],
            Edges = []
        };
        await graphs.SaveAsync(priorGraph, CancellationToken.None);

        Guid runId = Guid.NewGuid();
        GraphSnapshotResolutionResult resolution = await GraphSnapshotReuseEvaluator.ResolveAsync(
            prior,
            current,
            runId,
            kg.Object,
            graphs,
            CancellationToken.None);

        kg.Verify(x => x.BuildSnapshotAsync(It.IsAny<ContextSnapshot>(), It.IsAny<CancellationToken>()), Times.Never);
        resolution.ResolutionMode.Should().Be("cloned_from_prior_graph");
        GraphSnapshot result = resolution.Snapshot;
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
