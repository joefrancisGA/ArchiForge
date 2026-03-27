using ArchiForge.Retrieval.Embedding;
using ArchiForge.Retrieval.Indexing;
using ArchiForge.Retrieval.Models;
using ArchiForge.Retrieval.Queries;

using FluentAssertions;

using Moq;

namespace ArchiForge.Retrieval.Tests;

/// <summary>
/// <see cref="RetrievalQueryService"/> embeds query text then delegates to <see cref="IVectorIndex"/>; covers empty index, ranking order, TopK, and validation.
/// </summary>
[Trait("Category", "Unit")]
public sealed class RetrievalQueryServiceTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public async Task SearchAsync_EmptyIndex_ReturnsNoHits()
    {
        Mock<IEmbeddingService> embeddings = new();
        float[] queryVector = [1f, 0f, 0f];
        embeddings.Setup(e => e.EmbedAsync("hello", It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryVector);

        InMemoryVectorIndex index = new();
        RetrievalQueryService sut = new(embeddings.Object, index);

        IReadOnlyList<RetrievalHit> hits = await sut.SearchAsync(
            new RetrievalQuery
            {
                TenantId = TenantId,
                WorkspaceId = WorkspaceId,
                ProjectId = ProjectId,
                QueryText = "hello",
                TopK = 8
            },
            CancellationToken.None);

        hits.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_OrdersHitsByScoreDescending()
    {
        Mock<IEmbeddingService> embeddings = new();
        float[] queryVector = [1f, 0f, 0f];
        embeddings.Setup(e => e.EmbedAsync("q", It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryVector);

        InMemoryVectorIndex index = new();
        await index.UpsertChunksAsync(
            [
                Chunk("c-low", [0f, 1f, 0f], "low"),
                Chunk("c-high", [1f, 0f, 0f], "high"),
                Chunk("c-mid", [1f, 1f, 0f], "mid"),
                Chunk("c-opp", [-1f, 0f, 0f], "opp")
            ],
            CancellationToken.None);

        RetrievalQueryService sut = new(embeddings.Object, index);

        IReadOnlyList<RetrievalHit> hits = await sut.SearchAsync(
            ScopedQuery("q", topK: 10),
            CancellationToken.None);

        hits.Should().HaveCount(4);
        hits.Select(h => h.ChunkId).Should().ContainInOrder("c-high", "c-mid", "c-low", "c-opp");
        hits[0].Score.Should().BeGreaterThan(hits[1].Score);
        hits[1].Score.Should().BeGreaterThan(hits[2].Score);
        hits[2].Score.Should().BeGreaterThan(hits[3].Score);
    }

    [Fact]
    public async Task SearchAsync_RespectsTopK()
    {
        Mock<IEmbeddingService> embeddings = new();
        float[] queryVector = [1f, 0f, 0f];
        embeddings.Setup(e => e.EmbedAsync("q", It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryVector);

        InMemoryVectorIndex index = new();
        await index.UpsertChunksAsync(
            [
                Chunk("a", [0f, 1f, 0f], "a"),
                Chunk("b", [1f, 1f, 0f], "b"),
                Chunk("c", [1f, 0f, 0f], "c")
            ],
            CancellationToken.None);

        RetrievalQueryService sut = new(embeddings.Object, index);

        IReadOnlyList<RetrievalHit> hits = await sut.SearchAsync(
            ScopedQuery("q", topK: 2),
            CancellationToken.None);

        hits.Should().HaveCount(2);
        hits[0].ChunkId.Should().Be("c");
        hits[1].ChunkId.Should().Be("b");
        hits[0].Score.Should().BeGreaterThan(hits[1].Score);
    }

    [Fact]
    public async Task SearchAsync_PassesEmbeddingFromServiceToVectorIndex()
    {
        Mock<IEmbeddingService> embeddings = new();
        Mock<IVectorIndex> index = new();
        float[] expected = [0.1f, 0.2f, 0.3f];
        embeddings.Setup(e => e.EmbedAsync("needle", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        RetrievalQuery query = ScopedQuery("needle", topK: 5);
        index.Setup(i => i.SearchAsync(query, expected, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RetrievalHit>());

        RetrievalQueryService sut = new(embeddings.Object, index.Object);

        await sut.SearchAsync(query, CancellationToken.None);

        index.Verify(i => i.SearchAsync(query, expected, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_NullQuery_ThrowsArgumentNullException()
    {
        RetrievalQueryService sut = new(new Mock<IEmbeddingService>().Object, new InMemoryVectorIndex());

        Func<Task> act = async () => await sut.SearchAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SearchAsync_BlankQueryText_ThrowsArgumentException()
    {
        RetrievalQueryService sut = new(new Mock<IEmbeddingService>().Object, new InMemoryVectorIndex());

        Func<Task> act = async () =>
            await sut.SearchAsync(
                new RetrievalQuery
                {
                    TenantId = TenantId,
                    WorkspaceId = WorkspaceId,
                    ProjectId = ProjectId,
                    QueryText = "   "
                },
                CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    private static RetrievalQuery ScopedQuery(string queryText, int topK)
    {
        return new RetrievalQuery
        {
            TenantId = TenantId,
            WorkspaceId = WorkspaceId,
            ProjectId = ProjectId,
            QueryText = queryText,
            TopK = topK
        };
    }

    private static RetrievalChunk Chunk(string chunkId, float[] embedding, string text)
    {
        return new RetrievalChunk
        {
            ChunkId = chunkId,
            DocumentId = "doc-1",
            TenantId = TenantId,
            WorkspaceId = WorkspaceId,
            ProjectId = ProjectId,
            SourceType = "Test",
            SourceId = chunkId,
            Title = chunkId,
            Text = text,
            ChunkOrdinal = 0,
            Embedding = embedding
        };
    }
}
