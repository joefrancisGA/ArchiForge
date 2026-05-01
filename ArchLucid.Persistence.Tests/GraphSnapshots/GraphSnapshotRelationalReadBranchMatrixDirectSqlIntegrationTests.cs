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
///     Additional branch coverage for <see cref="GraphSnapshotRelationalRead.HydrateAsync" /> (nodes / warnings /
///     edges, relational vs JSON merge, label key vs generic property keys).
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
[Trait("Suite", "Core")]
public sealed class GraphSnapshotRelationalReadBranchMatrixDirectSqlIntegrationTests(
    SqlServerPersistenceFixture fixture)
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ScopeProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static string EmptyGraphJsonList<T>()
    {
        return JsonEntitySerializer.Serialize(new List<T>());
    }

    private static async Task<(Guid RunId, Guid ContextId, Guid GraphId, DateTime CreatedUtc)> SeedHeaderIntoAsync(
        SqlConnection connection,
        string slugSuffix)
    {
        Guid runId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        DateTime createdUtc = new(2026, 4, 20, 10, 0, 0, DateTimeKind.Utc);

        await AuthorityRunChainTestSeed.SeedRunAndContextOnlyAsync(
            connection,
            TenantId,
            WorkspaceId,
            ScopeProjectId,
            runId,
            contextId,
            "proj-graph-branch-" + slugSuffix,
            CancellationToken.None);

        return (runId, contextId, graphId, createdUtc);
    }

    private static async Task InsertGraphHeaderAsync(
        SqlConnection connection,
        Guid graphId,
        Guid contextId,
        Guid runId,
        DateTime createdUtc,
        string nodesJson,
        string edgesJson,
        string warningsJson)
    {
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
                    NodesJson = nodesJson,
                    EdgesJson = edgesJson,
                    WarningsJson = warningsJson
                },
                cancellationToken: CancellationToken.None));
    }

    private static async Task<GraphSnapshot> LoadAsync(SqlConnection connection, Guid graphId)
    {
        GraphSnapshotStorageRow row = await connection.QuerySingleAsync<GraphSnapshotStorageRow>(
            new CommandDefinition(
                """
                SELECT GraphSnapshotId, ContextSnapshotId, RunId, CreatedUtc, NodesJson, EdgesJson, WarningsJson
                FROM dbo.GraphSnapshots WHERE GraphSnapshotId = @Id;
                """,
                new { Id = graphId },
                cancellationToken: CancellationToken.None));

        return await GraphSnapshotRelationalRead.HydrateAsync(connection, null, row, CancellationToken.None);
    }

    [Fact]
    public async Task HydrateAsync_edgesCount_zero_returns_early_with_relational_nodes_only()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);
        (Guid runId, Guid contextId, Guid graphId, DateTime createdUtc) = await SeedHeaderIntoAsync(connection, "e0");

        await InsertGraphHeaderAsync(
            connection,
            graphId,
            contextId,
            runId,
            createdUtc,
            EmptyGraphJsonList<GraphNode>(),
            EmptyGraphJsonList<GraphEdge>(),
            EmptyGraphJsonList<string>());

        Guid nodeRowId = Guid.NewGuid();

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO dbo.GraphSnapshotNodes
                (GraphNodeRowId, GraphSnapshotId, SortOrder, NodeId, NodeType, Label, Category, SourceType, SourceId)
                VALUES (@RowId, @G, 0, N'n1', N't', N'L', NULL, NULL, NULL);
                """,
                new { RowId = nodeRowId, G = graphId },
                cancellationToken: CancellationToken.None));

        GraphSnapshot snap = await LoadAsync(connection, graphId);
        snap.Nodes.Should().ContainSingle(n => n.NodeId == "n1");
        snap.Edges.Should().BeEmpty();
    }

    [Fact]
    public async Task HydrateAsync_edgesCount_zero_relational_warnings_only()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);
        (Guid runId, Guid contextId, Guid graphId, DateTime createdUtc) = await SeedHeaderIntoAsync(connection, "w0");

        await InsertGraphHeaderAsync(
            connection,
            graphId,
            contextId,
            runId,
            createdUtc,
            EmptyGraphJsonList<GraphNode>(),
            EmptyGraphJsonList<GraphEdge>(),
            EmptyGraphJsonList<string>());

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO dbo.GraphSnapshotWarnings (GraphSnapshotId, SortOrder, WarningText)
                VALUES (@G, 0, N'w-a');
                """,
                new { G = graphId },
                cancellationToken: CancellationToken.None));

        GraphSnapshot snap = await LoadAsync(connection, graphId);
        snap.Warnings.Should().Equal("w-a");
    }

    [Fact]
    public async Task HydrateAsync_relational_edge_label_from_reserved_property_key()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);
        (Guid runId, Guid contextId, Guid graphId, DateTime createdUtc) = await SeedHeaderIntoAsync(connection, "elbl");

        List<GraphEdge> edges =
        [
            new()
            {
                EdgeId = "e1",
                FromNodeId = "a",
                ToNodeId = "b",
                EdgeType = "t",
                Weight = 1
            }
        ];
        await InsertGraphHeaderAsync(
            connection,
            graphId,
            contextId,
            runId,
            createdUtc,
            EmptyGraphJsonList<GraphNode>(),
            JsonEntitySerializer.Serialize(edges),
            EmptyGraphJsonList<string>());

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO dbo.GraphSnapshotEdges (GraphSnapshotId, EdgeId, FromNodeId, ToNodeId, EdgeType, Weight)
                VALUES (@G, N'e1', N'a', N'b', N't', 1.0);
                INSERT INTO dbo.GraphSnapshotEdgeProperties (GraphSnapshotId, EdgeId, PropertySortOrder, PropertyKey, PropertyValue)
                VALUES (@G, N'e1', 0, N'$ArchLucid:EdgeLabel', N'from-sql-label');
                """,
                new { G = graphId },
                cancellationToken: CancellationToken.None));

        GraphSnapshot snap = await LoadAsync(connection, graphId);
        snap.Edges.Should().ContainSingle(e => e.EdgeId == "e1" && e.Label == "from-sql-label");
    }

    [Fact]
    public async Task HydrateAsync_relational_edge_non_label_properties_only()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);
        (Guid runId, Guid contextId, Guid graphId, DateTime createdUtc) =
            await SeedHeaderIntoAsync(connection, "eprop");

        List<GraphEdge> edges =
        [
            new()
            {
                EdgeId = "e2",
                FromNodeId = "a",
                ToNodeId = "b",
                EdgeType = "t",
                Weight = 1
            }
        ];
        await InsertGraphHeaderAsync(
            connection,
            graphId,
            contextId,
            runId,
            createdUtc,
            EmptyGraphJsonList<GraphNode>(),
            JsonEntitySerializer.Serialize(edges),
            EmptyGraphJsonList<string>());

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO dbo.GraphSnapshotEdges (GraphSnapshotId, EdgeId, FromNodeId, ToNodeId, EdgeType, Weight)
                VALUES (@G, N'e2', N'a', N'b', N't', 1.0);
                INSERT INTO dbo.GraphSnapshotEdgeProperties (GraphSnapshotId, EdgeId, PropertySortOrder, PropertyKey, PropertyValue)
                VALUES (@G, N'e2', 0, N'k1', N'v1');
                """,
                new { G = graphId },
                cancellationToken: CancellationToken.None));

        GraphSnapshot snap = await LoadAsync(connection, graphId);
        GraphEdge edge = snap.Edges.Should().ContainSingle(e => e.EdgeId == "e2").Subject;
        edge.Label.Should().BeNullOrEmpty();
        edge.Properties.Should().ContainKey("k1").WhoseValue.Should().Be("v1");
    }

    [Fact]
    public async Task HydrateAsync_relational_edge_label_and_extra_properties()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);
        (Guid runId, Guid contextId, Guid graphId, DateTime createdUtc) = await SeedHeaderIntoAsync(connection, "emix");

        List<GraphEdge> edges =
        [
            new()
            {
                EdgeId = "e3",
                FromNodeId = "a",
                ToNodeId = "b",
                EdgeType = "t",
                Weight = 1
            }
        ];
        await InsertGraphHeaderAsync(
            connection,
            graphId,
            contextId,
            runId,
            createdUtc,
            EmptyGraphJsonList<GraphNode>(),
            JsonEntitySerializer.Serialize(edges),
            EmptyGraphJsonList<string>());

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO dbo.GraphSnapshotEdges (GraphSnapshotId, EdgeId, FromNodeId, ToNodeId, EdgeType, Weight)
                VALUES (@G, N'e3', N'a', N'b', N't', 1.0);
                INSERT INTO dbo.GraphSnapshotEdgeProperties (GraphSnapshotId, EdgeId, PropertySortOrder, PropertyKey, PropertyValue)
                VALUES
                (@G, N'e3', 0, N'$ArchLucid:EdgeLabel', N'Lbl'),
                (@G, N'e3', 1, N'p', N'q');
                """,
                new { G = graphId },
                cancellationToken: CancellationToken.None));

        GraphSnapshot snap = await LoadAsync(connection, graphId);
        GraphEdge edge = snap.Edges.Should().ContainSingle(e => e.EdgeId == "e3").Subject;
        edge.Label.Should().Be("Lbl");
        edge.Properties.Should().ContainKey("p").WhoseValue.Should().Be("q");
    }

    [Fact]
    public async Task HydrateAsync_mergeMetadata_fills_label_from_json_when_relational_label_missing()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);
        (Guid runId, Guid contextId, Guid graphId, DateTime createdUtc) = await SeedHeaderIntoAsync(connection, "mj1");

        List<GraphEdge> jsonEdges =
        [
            new()
            {
                EdgeId = "e4",
                FromNodeId = "a",
                ToNodeId = "b",
                EdgeType = "t",
                Weight = 1,
                Label = "json-label"
            }
        ];

        await InsertGraphHeaderAsync(
            connection,
            graphId,
            contextId,
            runId,
            createdUtc,
            EmptyGraphJsonList<GraphNode>(),
            JsonEntitySerializer.Serialize(jsonEdges),
            EmptyGraphJsonList<string>());

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO dbo.GraphSnapshotEdges (GraphSnapshotId, EdgeId, FromNodeId, ToNodeId, EdgeType, Weight)
                VALUES (@G, N'e4', N'a', N'b', N't', 1.0);
                """,
                new { G = graphId },
                cancellationToken: CancellationToken.None));

        GraphSnapshot snap = await LoadAsync(connection, graphId);
        snap.Edges.Should().ContainSingle(e => e.EdgeId == "e4" && e.Label == "json-label");
    }

    [Fact]
    public async Task HydrateAsync_mergeMetadata_fills_properties_from_json_when_relational_props_empty()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);
        (Guid runId, Guid contextId, Guid graphId, DateTime createdUtc) = await SeedHeaderIntoAsync(connection, "mj2");

        Dictionary<string, string> props = new(StringComparer.Ordinal) { ["x"] = "y" };

        List<GraphEdge> jsonEdges =
        [
            new()
            {
                EdgeId = "e5",
                FromNodeId = "a",
                ToNodeId = "b",
                EdgeType = "t",
                Weight = 1,
                Properties = props
            }
        ];

        await InsertGraphHeaderAsync(
            connection,
            graphId,
            contextId,
            runId,
            createdUtc,
            EmptyGraphJsonList<GraphNode>(),
            JsonEntitySerializer.Serialize(jsonEdges),
            EmptyGraphJsonList<string>());

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO dbo.GraphSnapshotEdges (GraphSnapshotId, EdgeId, FromNodeId, ToNodeId, EdgeType, Weight)
                VALUES (@G, N'e5', N'a', N'b', N't', 1.0);
                """,
                new { G = graphId },
                cancellationToken: CancellationToken.None));

        GraphSnapshot snap = await LoadAsync(connection, graphId);
        snap.Edges.Should().ContainSingle(e => e.EdgeId == "e5" && e.Properties["x"] == "y");
    }

    [Fact]
    public async Task HydrateAsync_mergeMetadata_keeps_relational_label_when_json_also_has_label()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);
        (Guid runId, Guid contextId, Guid graphId, DateTime createdUtc) = await SeedHeaderIntoAsync(connection, "mj3");

        List<GraphEdge> jsonEdges =
        [
            new()
            {
                EdgeId = "e6",
                FromNodeId = "a",
                ToNodeId = "b",
                EdgeType = "t",
                Weight = 1,
                Label = "json-wins-if-empty"
            }
        ];

        await InsertGraphHeaderAsync(
            connection,
            graphId,
            contextId,
            runId,
            createdUtc,
            EmptyGraphJsonList<GraphNode>(),
            JsonEntitySerializer.Serialize(jsonEdges),
            EmptyGraphJsonList<string>());

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO dbo.GraphSnapshotEdges (GraphSnapshotId, EdgeId, FromNodeId, ToNodeId, EdgeType, Weight)
                VALUES (@G, N'e6', N'a', N'b', N't', 1.0);
                INSERT INTO dbo.GraphSnapshotEdgeProperties (GraphSnapshotId, EdgeId, PropertySortOrder, PropertyKey, PropertyValue)
                VALUES (@G, N'e6', 0, N'$ArchLucid:EdgeLabel', N'sql-label');
                """,
                new { G = graphId },
                cancellationToken: CancellationToken.None));

        GraphSnapshot snap = await LoadAsync(connection, graphId);
        snap.Edges.Should().ContainSingle(e => e.EdgeId == "e6" && e.Label == "sql-label");
    }

    [Fact]
    public async Task HydrateAsync_mergeMetadata_keeps_relational_properties_when_json_has_props()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);
        (Guid runId, Guid contextId, Guid graphId, DateTime createdUtc) = await SeedHeaderIntoAsync(connection, "mj4");

        Dictionary<string, string> jsonProps = new(StringComparer.Ordinal) { ["from"] = "json" };

        List<GraphEdge> jsonEdges =
        [
            new()
            {
                EdgeId = "e7",
                FromNodeId = "a",
                ToNodeId = "b",
                EdgeType = "t",
                Weight = 1,
                Properties = jsonProps
            }
        ];

        await InsertGraphHeaderAsync(
            connection,
            graphId,
            contextId,
            runId,
            createdUtc,
            EmptyGraphJsonList<GraphNode>(),
            JsonEntitySerializer.Serialize(jsonEdges),
            EmptyGraphJsonList<string>());

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO dbo.GraphSnapshotEdges (GraphSnapshotId, EdgeId, FromNodeId, ToNodeId, EdgeType, Weight)
                VALUES (@G, N'e7', N'a', N'b', N't', 1.0);
                INSERT INTO dbo.GraphSnapshotEdgeProperties (GraphSnapshotId, EdgeId, PropertySortOrder, PropertyKey, PropertyValue)
                VALUES (@G, N'e7', 0, N'from', N'sql');
                """,
                new { G = graphId },
                cancellationToken: CancellationToken.None));

        GraphSnapshot snap = await LoadAsync(connection, graphId);
        snap.Edges.Should().ContainSingle(e => e.EdgeId == "e7" && e.Properties["from"] == "sql");
    }

    [Fact]
    public async Task HydrateAsync_mergeMetadata_json_missing_edge_id_skips_merge_branch()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);
        (Guid runId, Guid contextId, Guid graphId, DateTime createdUtc) = await SeedHeaderIntoAsync(connection, "mj5");

        List<GraphEdge> jsonEdges =
        [
            new()
            {
                EdgeId = "other",
                FromNodeId = "a",
                ToNodeId = "b",
                EdgeType = "t",
                Weight = 1,
                Label = "only-other"
            }
        ];

        await InsertGraphHeaderAsync(
            connection,
            graphId,
            contextId,
            runId,
            createdUtc,
            EmptyGraphJsonList<GraphNode>(),
            JsonEntitySerializer.Serialize(jsonEdges),
            EmptyGraphJsonList<string>());

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO dbo.GraphSnapshotEdges (GraphSnapshotId, EdgeId, FromNodeId, ToNodeId, EdgeType, Weight)
                VALUES (@G, N'e8', N'a', N'b', N't', 1.0);
                """,
                new { G = graphId },
                cancellationToken: CancellationToken.None));

        GraphSnapshot snap = await LoadAsync(connection, graphId);
        GraphEdge edge = snap.Edges.Should().ContainSingle(e => e.EdgeId == "e8").Subject;
        string.IsNullOrEmpty(edge.Label).Should().BeTrue();
    }

    [Fact]
    public async Task HydrateAsync_relational_nodes_without_property_rows_yield_empty_properties_dict()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);
        (Guid runId, Guid contextId, Guid graphId, DateTime createdUtc) = await SeedHeaderIntoAsync(connection, "np0");

        await InsertGraphHeaderAsync(
            connection,
            graphId,
            contextId,
            runId,
            createdUtc,
            EmptyGraphJsonList<GraphNode>(),
            EmptyGraphJsonList<GraphEdge>(),
            EmptyGraphJsonList<string>());

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO dbo.GraphSnapshotNodes
                (GraphNodeRowId, GraphSnapshotId, SortOrder, NodeId, NodeType, Label, Category, SourceType, SourceId)
                VALUES (@R, @G, 0, N'n2', N't2', N'Lb', NULL, NULL, NULL);
                """,
                new { R = Guid.NewGuid(), G = graphId },
                cancellationToken: CancellationToken.None));

        GraphSnapshot snap = await LoadAsync(connection, graphId);
        GraphNode node = snap.Nodes.Should().ContainSingle(n => n.NodeId == "n2").Subject;
        node.Properties.Should().BeEmpty();
    }

    [Fact]
    public async Task HydrateAsync_relational_nodes_with_property_rows_merge_into_node()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);
        (Guid runId, Guid contextId, Guid graphId, DateTime createdUtc) = await SeedHeaderIntoAsync(connection, "np1");

        await InsertGraphHeaderAsync(
            connection,
            graphId,
            contextId,
            runId,
            createdUtc,
            EmptyGraphJsonList<GraphNode>(),
            EmptyGraphJsonList<GraphEdge>(),
            EmptyGraphJsonList<string>());

        Guid rowId = Guid.NewGuid();

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO dbo.GraphSnapshotNodes
                (GraphNodeRowId, GraphSnapshotId, SortOrder, NodeId, NodeType, Label, Category, SourceType, SourceId)
                VALUES (@R, @G, 0, N'n3', N't3', N'Lb', NULL, NULL, NULL);
                INSERT INTO dbo.GraphSnapshotNodeProperties (GraphNodeRowId, PropertySortOrder, PropertyKey, PropertyValue)
                VALUES (@R, 0, N'a', N'b');
                """,
                new { R = rowId, G = graphId },
                cancellationToken: CancellationToken.None));

        GraphSnapshot snap = await LoadAsync(connection, graphId);
        snap.Nodes.Should().ContainSingle(n => n.NodeId == "n3" && n.Properties["a"] == "b");
    }

    [Fact]
    public async Task HydrateAsync_edge_properties_present_disables_json_merge_even_when_json_rich()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);
        (Guid runId, Guid contextId, Guid graphId, DateTime createdUtc) =
            await SeedHeaderIntoAsync(connection, "nomerge");

        Dictionary<string, string> jsonProps = new(StringComparer.Ordinal) { ["only"] = "json" };

        List<GraphEdge> jsonEdges =
        [
            new()
            {
                EdgeId = "e9",
                FromNodeId = "a",
                ToNodeId = "b",
                EdgeType = "t",
                Weight = 1,
                Label = "jsonLabel",
                Properties = jsonProps
            }
        ];

        await InsertGraphHeaderAsync(
            connection,
            graphId,
            contextId,
            runId,
            createdUtc,
            EmptyGraphJsonList<GraphNode>(),
            JsonEntitySerializer.Serialize(jsonEdges),
            EmptyGraphJsonList<string>());

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO dbo.GraphSnapshotEdges (GraphSnapshotId, EdgeId, FromNodeId, ToNodeId, EdgeType, Weight)
                VALUES (@G, N'e9', N'a', N'b', N't', 1.0);
                INSERT INTO dbo.GraphSnapshotEdgeProperties (GraphSnapshotId, EdgeId, PropertySortOrder, PropertyKey, PropertyValue)
                VALUES (@G, N'e9', 0, N'only', N'sql');
                """,
                new { G = graphId },
                cancellationToken: CancellationToken.None));

        GraphSnapshot snap = await LoadAsync(connection, graphId);
        GraphEdge edge = snap.Edges.Should().ContainSingle(e => e.EdgeId == "e9").Subject;
        edge.Label.Should().BeNullOrEmpty();
        edge.Properties.Should().ContainKey("only").WhoseValue.Should().Be("sql");
    }

    [Fact]
    public async Task HydrateAsync_two_edges_one_merge_one_full_relational()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);
        (Guid runId, Guid contextId, Guid graphId, DateTime createdUtc) = await SeedHeaderIntoAsync(connection, "two");

        List<GraphEdge> jsonEdges =
        [
            new()
            {
                EdgeId = "ea",
                FromNodeId = "1",
                ToNodeId = "2",
                EdgeType = "t",
                Weight = 1,
                Label = "ja"
            },
            new()
            {
                EdgeId = "eb",
                FromNodeId = "2",
                ToNodeId = "3",
                EdgeType = "t",
                Weight = 2,
                Label = "ignored-for-eb"
            }
        ];

        await InsertGraphHeaderAsync(
            connection,
            graphId,
            contextId,
            runId,
            createdUtc,
            EmptyGraphJsonList<GraphNode>(),
            JsonEntitySerializer.Serialize(jsonEdges),
            EmptyGraphJsonList<string>());

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO dbo.GraphSnapshotEdges (GraphSnapshotId, EdgeId, FromNodeId, ToNodeId, EdgeType, Weight)
                VALUES
                (@G, N'ea', N'1', N'2', N't', 1.0),
                (@G, N'eb', N'2', N'3', N't', 2.0);
                INSERT INTO dbo.GraphSnapshotEdgeProperties (GraphSnapshotId, EdgeId, PropertySortOrder, PropertyKey, PropertyValue)
                VALUES (@G, N'eb', 0, N'$ArchLucid:EdgeLabel', N'sql-b');
                """,
                new { G = graphId },
                cancellationToken: CancellationToken.None));

        GraphSnapshot snap = await LoadAsync(connection, graphId);
        snap.Edges.Should().HaveCount(2);
        snap.Edges.Should().Contain(e => e.EdgeId == "ea" && e.Label == "ja");
        snap.Edges.Should().Contain(e => e.EdgeId == "eb" && e.Label == "sql-b");
    }

    [Fact]
    public async Task
        HydrateAsync_relational_warnings_with_empty_nodes_and_edges_uses_row_warnings_json_fallback_false()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);
        (Guid runId, Guid contextId, Guid graphId, DateTime createdUtc) = await SeedHeaderIntoAsync(connection, "wrn");

        await InsertGraphHeaderAsync(
            connection,
            graphId,
            contextId,
            runId,
            createdUtc,
            EmptyGraphJsonList<GraphNode>(),
            EmptyGraphJsonList<GraphEdge>(),
            JsonEntitySerializer.Serialize(new List<string> { "ignored-when-relational" }));

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO dbo.GraphSnapshotWarnings (GraphSnapshotId, SortOrder, WarningText)
                VALUES (@G, 0, N'rel-w');
                """,
                new { G = graphId },
                cancellationToken: CancellationToken.None));

        GraphSnapshot snap = await LoadAsync(connection, graphId);
        snap.Warnings.Should().Equal("rel-w");
    }

    [Fact]
    public async Task HydrateAsync_relational_nodes_and_edges_without_merge_second_edge_has_no_json()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);
        (Guid runId, Guid contextId, Guid graphId, DateTime createdUtc) =
            await SeedHeaderIntoAsync(connection, "split");

        List<GraphEdge> jsonEdges =
        [
            new()
            {
                EdgeId = "only-one",
                FromNodeId = "1",
                ToNodeId = "2",
                EdgeType = "t",
                Weight = 1,
                Label = "L1"
            }
        ];

        await InsertGraphHeaderAsync(
            connection,
            graphId,
            contextId,
            runId,
            createdUtc,
            EmptyGraphJsonList<GraphNode>(),
            JsonEntitySerializer.Serialize(jsonEdges),
            EmptyGraphJsonList<string>());

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO dbo.GraphSnapshotNodes
                (GraphNodeRowId, GraphSnapshotId, SortOrder, NodeId, NodeType, Label, Category, SourceType, SourceId)
                VALUES (NEWID(), @G, 0, N'1', N't', N'n', NULL, NULL, NULL);
                INSERT INTO dbo.GraphSnapshotEdges (GraphSnapshotId, EdgeId, FromNodeId, ToNodeId, EdgeType, Weight)
                VALUES
                (@G, N'only-one', N'1', N'2', N't', 1.0),
                (@G, N'no-json', N'2', N'3', N't', 2.0);
                """,
                new { G = graphId },
                cancellationToken: CancellationToken.None));

        GraphSnapshot snap = await LoadAsync(connection, graphId);
        snap.Edges.Should().Contain(e => e.EdgeId == "only-one" && e.Label == "L1");
        snap.Edges.Should().Contain(e => e.EdgeId == "no-json" && string.IsNullOrEmpty(e.Label));
    }
}
