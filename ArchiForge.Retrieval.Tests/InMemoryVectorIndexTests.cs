using ArchiForge.Retrieval.Indexing;
using ArchiForge.Retrieval.Models;

using FluentAssertions;

namespace ArchiForge.Retrieval.Tests;

/// <summary>
/// <see cref="InMemoryVectorIndex"/> scope filters and cosine edge cases used by <see cref="Queries.RetrievalQueryService"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class InMemoryVectorIndexTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid WorkspaceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ProjectId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task SearchAsync_WrongTenant_ReturnsEmpty()
    {
        InMemoryVectorIndex sut = new();
        await sut.UpsertChunksAsync(
            [MakeChunk("x", TenantId, WorkspaceId, ProjectId, [1f, 0f])],
            CancellationToken.None);

        RetrievalQuery query = BaseQuery();
        query.TenantId = Guid.NewGuid();
        float[] embedding = [1f, 0f];

        IReadOnlyList<RetrievalHit> hits = await sut.SearchAsync(query, embedding, CancellationToken.None);

        hits.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_WhenQuerySpecifiesRunId_ExcludesChunksWithoutSameRun()
    {
        Guid runId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        InMemoryVectorIndex sut = new();
        RetrievalChunk withRun = MakeChunk("with-run", TenantId, WorkspaceId, ProjectId, [1f, 0f]);
        withRun.RunId = runId;
        RetrievalChunk noRun = MakeChunk("no-run", TenantId, WorkspaceId, ProjectId, [1f, 0f]);
        noRun.RunId = null;
        await sut.UpsertChunksAsync([withRun, noRun], CancellationToken.None);

        RetrievalQuery query = BaseQuery();
        query.RunId = runId;

        IReadOnlyList<RetrievalHit> hits = await sut.SearchAsync(query, [1f, 0f], CancellationToken.None);

        hits.Should().ContainSingle().Which.ChunkId.Should().Be("with-run");
    }

    [Fact]
    public async Task SearchAsync_MismatchedEmbeddingLength_AssignsZeroScore()
    {
        InMemoryVectorIndex sut = new();
        await sut.UpsertChunksAsync(
            [MakeChunk("wide", TenantId, WorkspaceId, ProjectId, [1f, 0f, 0f])],
            CancellationToken.None);

        RetrievalQuery query = BaseQuery();
        float[] shortQuery = [1f, 0f];

        IReadOnlyList<RetrievalHit> hits = await sut.SearchAsync(query, shortQuery, CancellationToken.None);

        hits.Should().ContainSingle().Which.Score.Should().Be(0);
    }

    [Fact]
    public async Task UpsertChunksAsync_ReplacesChunkWithSameChunkId()
    {
        InMemoryVectorIndex sut = new();
        RetrievalChunk first = MakeChunk("same", TenantId, WorkspaceId, ProjectId, [0f, 1f]);
        first.Text = "v1";
        RetrievalChunk second = MakeChunk("same", TenantId, WorkspaceId, ProjectId, [1f, 0f]);
        second.Text = "v2";

        await sut.UpsertChunksAsync([first], CancellationToken.None);
        await sut.UpsertChunksAsync([second], CancellationToken.None);

        IReadOnlyList<RetrievalHit> hits = await sut.SearchAsync(BaseQuery(), [1f, 0f], CancellationToken.None);

        hits.Should().ContainSingle().Which.Text.Should().Be("v2");
    }

    private static RetrievalQuery BaseQuery()
    {
        return new RetrievalQuery
        {
            TenantId = TenantId,
            WorkspaceId = WorkspaceId,
            ProjectId = ProjectId,
            QueryText = "ignored-here",
            TopK = 8
        };
    }

    private static RetrievalChunk MakeChunk(
        string chunkId,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        float[] embedding)
    {
        return new RetrievalChunk
        {
            ChunkId = chunkId,
            DocumentId = "d",
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            SourceType = "Test",
            SourceId = chunkId,
            Title = chunkId,
            Text = chunkId,
            ChunkOrdinal = 0,
            Embedding = embedding
        };
    }
}
