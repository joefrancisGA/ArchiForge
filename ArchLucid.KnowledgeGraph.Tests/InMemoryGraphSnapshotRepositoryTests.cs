using ArchLucid.KnowledgeGraph.Models;
using ArchLucid.KnowledgeGraph.Repositories;

using FluentAssertions;

namespace ArchLucid.KnowledgeGraph.Tests;

/// <summary>
///     Tests for In Memory Graph Snapshot Repository.
/// </summary>
[Trait("Category", "Unit")]
public sealed class InMemoryGraphSnapshotRepositoryTests
{
    [Fact]
    public async Task GetLatestByContextSnapshotIdAsync_WhenEmpty_ReturnsNull()
    {
        InMemoryGraphSnapshotRepository sut = new();

        GraphSnapshot? found = await sut.GetLatestByContextSnapshotIdAsync(Guid.NewGuid(), CancellationToken.None);

        found.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestByContextSnapshotIdAsync_ReturnsNewestByCreatedUtc()
    {
        InMemoryGraphSnapshotRepository sut = new();
        Guid contextId = Guid.NewGuid();
        DateTime older = DateTime.UtcNow.AddHours(-2);
        DateTime newer = DateTime.UtcNow.AddHours(-1);

        GraphSnapshot oldSnap = CreateSnapshot(contextId, older);
        GraphSnapshot newSnap = CreateSnapshot(contextId, newer);
        await sut.SaveAsync(oldSnap, CancellationToken.None);
        await sut.SaveAsync(newSnap, CancellationToken.None);

        GraphSnapshot? found = await sut.GetLatestByContextSnapshotIdAsync(contextId, CancellationToken.None);

        found.Should().NotBeNull();
        found.GraphSnapshotId.Should().Be(newSnap.GraphSnapshotId);
    }

    [Fact]
    public async Task ListIndexedEdgesAsync_UnknownGraph_ReturnsEmpty()
    {
        InMemoryGraphSnapshotRepository sut = new();

        IReadOnlyList<GraphSnapshotIndexedEdge> edges =
            await sut.ListIndexedEdgesAsync(Guid.NewGuid(), CancellationToken.None);

        edges.Should().BeEmpty();
    }

    [Fact]
    public async Task ListIndexedEdgesAsync_MapsEdgesOrderedByEdgeId()
    {
        InMemoryGraphSnapshotRepository sut = new();
        GraphSnapshot snap = new()
        {
            GraphSnapshotId = Guid.NewGuid(),
            ContextSnapshotId = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            CreatedUtc = DateTime.UtcNow,
            Edges =
            [
                new GraphEdge
                {
                    EdgeId = "b",
                    FromNodeId = "a",
                    ToNodeId = "c",
                    EdgeType = "X",
                    Weight = 2d
                },
                new GraphEdge
                {
                    EdgeId = "a",
                    FromNodeId = "a",
                    ToNodeId = "b",
                    EdgeType = "Y",
                    Weight = 1d
                }
            ]
        };
        await sut.SaveAsync(snap, CancellationToken.None);

        IReadOnlyList<GraphSnapshotIndexedEdge> edges =
            await sut.ListIndexedEdgesAsync(snap.GraphSnapshotId, CancellationToken.None);

        edges.Should().HaveCount(2);
        edges[0].EdgeId.Should().Be("a");
        edges[1].EdgeId.Should().Be("b");
        edges[1].Weight.Should().Be(2d);
    }

    private static GraphSnapshot CreateSnapshot(Guid contextId, DateTime createdUtc)
    {
        return new GraphSnapshot
        {
            GraphSnapshotId = Guid.NewGuid(),
            ContextSnapshotId = contextId,
            RunId = Guid.NewGuid(),
            CreatedUtc = createdUtc,
            Nodes = [],
            Edges = [],
            Warnings = []
        };
    }
}
