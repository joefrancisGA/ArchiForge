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
///     Covers <see cref="GraphSnapshotRelationalRead.HydrateAsync" /> early return when
///     <c>dbo.GraphSnapshotEdges</c> has no rows (relational nodes only, no edge load path).
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
[Trait("Suite", "Core")]
public sealed class GraphSnapshotRelationalReadNoEdgesDirectSqlIntegrationTests(SqlServerPersistenceFixture fixture)
{
    [Fact]
    public async Task HydrateAsync_when_no_relational_edges_returns_relational_nodes_and_empty_edges()
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
        Guid nodeRowId = Guid.NewGuid();
        DateTime createdUtc = new(2026, 4, 15, 16, 0, 0, DateTimeKind.Utc);

        await AuthorityRunChainTestSeed.SeedRunAndContextOnlyAsync(
            connection,
            tenantId,
            workspaceId,
            scopeProjectId,
            runId,
            contextId,
            "proj-graph-no-edges",
            CancellationToken.None);

        List<GraphNode> jsonNodes =
        [
            new() { NodeId = "json-only", NodeType = "Ignored", Label = "Should lose to relational" }
        ];

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
                    NodesJson = JsonEntitySerializer.Serialize(jsonNodes),
                    EdgesJson = JsonEntitySerializer.Serialize(new List<GraphEdge>()),
                    WarningsJson = JsonEntitySerializer.Serialize(new List<string>())
                },
                cancellationToken: CancellationToken.None));

        const string insertNode = """
                                  INSERT INTO dbo.GraphSnapshotNodes
                                  (
                                      GraphNodeRowId, GraphSnapshotId, SortOrder,
                                      NodeId, NodeType, Label, Category, SourceType, SourceId
                                  )
                                  VALUES
                                  (
                                      @GraphNodeRowId, @GraphSnapshotId, @SortOrder,
                                      @NodeId, @NodeType, @Label, @Category, @SourceType, @SourceId
                                  );
                                  """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertNode,
                new
                {
                    GraphNodeRowId = nodeRowId,
                    GraphSnapshotId = graphId,
                    SortOrder = 0,
                    NodeId = "n-no-edge",
                    NodeType = "Service",
                    Label = "RelationalOnly",
                    Category = "c",
                    SourceType = "s",
                    SourceId = "sid"
                },
                cancellationToken: CancellationToken.None));

        const string selectRow = """
                                 SELECT
                                     GraphSnapshotId, ContextSnapshotId, RunId, CreatedUtc,
                                     NodesJson, EdgesJson, WarningsJson
                                 FROM dbo.GraphSnapshots
                                 WHERE GraphSnapshotId = @GraphSnapshotId;
                                 """;

        GraphSnapshotStorageRow? row = await connection.QuerySingleOrDefaultAsync<GraphSnapshotStorageRow>(
            new CommandDefinition(selectRow, new { GraphSnapshotId = graphId },
                cancellationToken: CancellationToken.None));

        row.Should().NotBeNull();

        GraphSnapshot snapshot =
            await GraphSnapshotRelationalRead.HydrateAsync(connection, null, row, CancellationToken.None);

        snapshot.Edges.Should().BeEmpty();
        snapshot.Nodes.Should().ContainSingle();
        snapshot.Nodes[0].NodeId.Should().Be("n-no-edge");
        snapshot.Nodes[0].Label.Should().Be("RelationalOnly");
    }
}
