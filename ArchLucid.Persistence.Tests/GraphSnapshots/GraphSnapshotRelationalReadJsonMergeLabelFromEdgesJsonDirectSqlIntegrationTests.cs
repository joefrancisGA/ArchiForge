using ArchLucid.KnowledgeGraph.Models;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.GraphSnapshots;
using ArchLucid.Persistence.Repositories;
using ArchLucid.Persistence.Serialization;
using ArchLucid.Persistence.Tests.Support;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.GraphSnapshots;

/// <summary>
///     <see cref="GraphSnapshotRelationalRead.LoadEdgesRelationalAsync" /> merge path: relational edges exist,
///     <c>GraphSnapshotEdgeProperties</c> is empty (<c>mergeMetadataFromJson</c> true), relational row has no label/props,
///     <c>EdgesJson</c> supplies the label.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
[Trait("Suite", "Core")]
public sealed class GraphSnapshotRelationalReadJsonMergeLabelFromEdgesJsonDirectSqlIntegrationTests(
    SqlServerPersistenceFixture fixture)
{
    [Fact]
    public async Task HydrateAsync_merges_edge_label_from_EdgesJson_when_relational_properties_absent()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

        Guid tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid workspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid scopeProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        Guid runId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        DateTime createdUtc = new(2026, 4, 23, 16, 0, 0, DateTimeKind.Utc);

        await AuthorityRunChainTestSeed.SeedRunAndContextOnlyAsync(
            connection,
            tenantId,
            workspaceId,
            scopeProjectId,
            runId,
            contextId,
            "proj-graph-merge-label",
            CancellationToken.None);

        List<GraphEdge> jsonEdges =
        [
            new()
            {
                EdgeId = "e-merge-label",
                FromNodeId = "a",
                ToNodeId = "b",
                EdgeType = "REL",
                Weight = 2d,
                Label = "from-json-label",
                Properties = new Dictionary<string, string>(StringComparer.Ordinal) { ["jk"] = "jv" }
            }
        ];

        string edgesJson = JsonEntitySerializer.Serialize(jsonEdges);

        const string insertHeader = """
                                    INSERT INTO dbo.GraphSnapshots
                                    (
                                        GraphSnapshotId, ContextSnapshotId, RunId, CreatedUtc,
                                        NodesJson, EdgesJson, WarningsJson
                                    )
                                    VALUES
                                    (
                                        @GraphSnapshotId, @ContextSnapshotId, @RunId, @CreatedUtc,
                                        @NodesJson, @EdgesJson, @WarningsJson
                                    );
                                    """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertHeader,
                new
                {
                    GraphSnapshotId = graphId,
                    ContextSnapshotId = contextId,
                    RunId = runId,
                    CreatedUtc = createdUtc,
                    NodesJson = JsonEntitySerializer.Serialize(new List<GraphNode>()),
                    EdgesJson = edgesJson,
                    WarningsJson = JsonEntitySerializer.Serialize(new List<string>())
                },
                cancellationToken: CancellationToken.None));

        const string insertEdge = """
                                  INSERT INTO dbo.GraphSnapshotEdges (GraphSnapshotId, EdgeId, FromNodeId, ToNodeId, EdgeType, Weight)
                                  VALUES (@GraphSnapshotId, @EdgeId, @FromNodeId, @ToNodeId, @EdgeType, @Weight);
                                  """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertEdge,
                new
                {
                    GraphSnapshotId = graphId,
                    EdgeId = "e-merge-label",
                    FromNodeId = "a",
                    ToNodeId = "b",
                    EdgeType = "REL",
                    Weight = 1d
                },
                cancellationToken: CancellationToken.None));

        GraphSnapshotStorageRow? row = await connection.QuerySingleOrDefaultAsync<GraphSnapshotStorageRow>(
            new CommandDefinition(
                """
                SELECT GraphSnapshotId, ContextSnapshotId, RunId, CreatedUtc, NodesJson, EdgesJson, WarningsJson
                FROM dbo.GraphSnapshots WHERE GraphSnapshotId = @GraphSnapshotId;
                """,
                new { GraphSnapshotId = graphId },
                cancellationToken: CancellationToken.None));

        row.Should().NotBeNull();

        GraphSnapshot snapshot =
            await GraphSnapshotRelationalRead.HydrateAsync(connection, null, row, CancellationToken.None);

        snapshot.Edges.Should().ContainSingle();
        snapshot.Edges[0].Label.Should().Be("from-json-label");
        snapshot.Edges[0].Properties.Should().ContainKey("jk").WhoseValue.Should().Be("jv");
    }
}
