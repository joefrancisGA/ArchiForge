using ArchLucid.KnowledgeGraph.Models;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Repositories;
using ArchLucid.Persistence.Serialization;
using ArchLucid.Persistence.Tests.Support;

using Dapper;

using FluentAssertions;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests;

/// <summary>
///     <see cref="SqlGraphSnapshotRepository" /> against SQL Server + DbUp (relational children + JSON dual-write; reads
///     are relational-first).
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class SqlGraphSnapshotRepositorySqlIntegrationTests(SqlServerPersistenceFixture fixture)
{
    [SkippableFact]
    public async Task Save_then_GetById_round_trips_relational_collections()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        SqlGraphSnapshotRepository repository = new(factory);

        Guid graphId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid runId = Guid.NewGuid();
        DateTime created = new(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);

        GraphSnapshot snapshot = new()
        {
            GraphSnapshotId = graphId,
            ContextSnapshotId = contextId,
            RunId = runId,
            CreatedUtc = created,
            Nodes =
            [
                new GraphNode
                {
                    NodeId = "n1",
                    NodeType = "Service",
                    Label = "Api",
                    Category = "app",
                    SourceType = "code",
                    SourceId = "src",
                    Properties = new Dictionary<string, string>(StringComparer.Ordinal) { ["env"] = "prod" }
                }
            ],
            Edges =
            [
                new GraphEdge
                {
                    EdgeId = "e1",
                    FromNodeId = "n1",
                    ToNodeId = "n2",
                    EdgeType = "CALLS",
                    Label = "sync",
                    Weight = 2d,
                    Properties = new Dictionary<string, string>(StringComparer.Ordinal) { ["protocol"] = "grpc" }
                }
            ],
            Warnings = ["w1"]
        };

        await repository.SaveAsync(snapshot, CancellationToken.None);

        GraphSnapshot? loaded = await repository.GetByIdAsync(graphId, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded.GraphSnapshotId.Should().Be(graphId);
        loaded.ContextSnapshotId.Should().Be(contextId);
        loaded.RunId.Should().Be(runId);
        loaded.CreatedUtc.Should().Be(created);
        loaded.Nodes.Should().ContainSingle();
        loaded.Nodes[0].NodeId.Should().Be("n1");
        loaded.Nodes[0].Properties["env"].Should().Be("prod");
        loaded.Edges.Should().ContainSingle();
        loaded.Edges[0].EdgeId.Should().Be("e1");
        loaded.Edges[0].Label.Should().Be("sync");
        loaded.Edges[0].Properties["protocol"].Should().Be("grpc");
        loaded.Warnings.Should().Equal("w1");
    }

    [SkippableFact]
    public async Task ListIndexedEdgesAsync_preserves_order_by_EdgeId_and_core_fields()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        SqlGraphSnapshotRepository repository = new(factory);

        GraphSnapshot snapshot = new()
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

        await repository.SaveAsync(snapshot, CancellationToken.None);

        IReadOnlyList<GraphSnapshotIndexedEdge> indexed =
            await repository.ListIndexedEdgesAsync(snapshot.GraphSnapshotId, CancellationToken.None);
        indexed.Should().HaveCount(2);
        indexed[0].EdgeId.Should().Be("a");
        indexed[1].EdgeId.Should().Be("b");
        indexed[1].Weight.Should().Be(2d);
    }

    [SkippableFact]
    public async Task
        GetById_relational_edges_merge_label_and_properties_from_edges_json_when_edge_properties_table_empty()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        SqlGraphSnapshotRepository repository = new(factory);

        Guid graphId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid runId = Guid.NewGuid();

        List<GraphNode> nodes =
        [
            new() { NodeId = "legacy-n", NodeType = "T", Label = "Legacy", Properties = [] }
        ];

        List<GraphEdge> edges =
        [
            new()
            {
                EdgeId = "e-legacy",
                FromNodeId = "legacy-n",
                ToNodeId = "x",
                EdgeType = "REL",
                Label = "edge-label-from-json",
                Weight = 1d,
                Properties = new Dictionary<string, string>(StringComparer.Ordinal) { ["k"] = "v" }
            }
        ];

        string nodesJson = JsonEntitySerializer.Serialize(nodes);
        string edgesJson = JsonEntitySerializer.Serialize(edges);
        string warningsJson = JsonEntitySerializer.Serialize(new List<string> { "jw" });

        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

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
                    CreatedUtc = DateTime.UtcNow,
                    NodesJson = nodesJson,
                    EdgesJson = edgesJson,
                    WarningsJson = warningsJson
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
                    EdgeId = "e-legacy",
                    FromNodeId = "legacy-n",
                    ToNodeId = "x",
                    EdgeType = "REL",
                    Weight = 1d
                },
                cancellationToken: CancellationToken.None));

        GraphSnapshot? loaded = await repository.GetByIdAsync(graphId, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded.Nodes.Should().BeEmpty("nodes are relational-only; JSON columns are not read");
        loaded.Warnings.Should().BeEmpty("warnings are relational-only; JSON columns are not read");
        loaded.Edges.Should().ContainSingle();
        loaded.Edges[0].Label.Should().Be("edge-label-from-json");
        loaded.Edges[0].Properties["k"].Should().Be("v");
    }

    /// <summary>
    ///     Header JSON columns are legacy dual-write artifacts; <see cref="GraphSnapshotRelationalRead" /> must not
    ///     deserialize them for nodes/warnings (and must not build edges without relational edge rows).
    /// </summary>
    [SkippableFact]
    public async Task GetById_when_no_relational_children_returns_empty_collections_even_when_json_columns_populated()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        SqlGraphSnapshotRepository repository = new(factory);

        Guid graphId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid runId = Guid.NewGuid();

        List<GraphNode> nodesIfRead =
        [
            new()
            {
                NodeId = "would-be-json-node",
                NodeType = "T",
                Label = "Should not appear",
                Properties = new Dictionary<string, string>(StringComparer.Ordinal) { ["ghost"] = "1" }
            }
        ];

        List<GraphEdge> edgesIfRead =
        [
            new()
            {
                EdgeId = "would-be-json-edge",
                FromNodeId = "a",
                ToNodeId = "b",
                EdgeType = "X",
                Label = "ghost-edge",
                Weight = 1d
            }
        ];

        string nodesJson = JsonEntitySerializer.Serialize(nodesIfRead);
        string edgesJson = JsonEntitySerializer.Serialize(edgesIfRead);
        string warningsJson = JsonEntitySerializer.Serialize(new List<string> { "ghost-warning" });

        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

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
                    CreatedUtc = DateTime.UtcNow,
                    NodesJson = nodesJson,
                    EdgesJson = edgesJson,
                    WarningsJson = warningsJson
                },
                cancellationToken: CancellationToken.None));

        GraphSnapshot? loaded = await repository.GetByIdAsync(graphId, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded.Nodes.Should().BeEmpty("relational GraphSnapshotNodes has no rows — JSON must not hydrate nodes");
        loaded.Edges.Should().BeEmpty("relational GraphSnapshotEdges has no rows — JSON must not hydrate edges");
        loaded.Warnings.Should()
            .BeEmpty("relational GraphSnapshotWarnings has no rows — JSON must not hydrate warnings");
    }

    [SkippableFact]
    public async Task GetById_when_no_relational_children_and_json_columns_null_returns_empty_collections()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        SqlGraphSnapshotRepository repository = new(factory);

        Guid tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid workspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid scopeProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        Guid graphId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid runId = Guid.NewGuid();
        DateTime createdUtc = new(2026, 11, 13, 16, 0, 0, DateTimeKind.Utc);

        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);
        await AuthorityRunChainTestSeed.SeedRunAndContextOnlyAsync(
            connection,
            tenantId,
            workspaceId,
            scopeProjectId,
            runId,
            contextId,
            "proj-graph-null-json",
            CancellationToken.None);

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
                    NodesJson = (string?)null,
                    EdgesJson = (string?)null,
                    WarningsJson = (string?)null
                },
                cancellationToken: CancellationToken.None));

        GraphSnapshot? loaded = await repository.GetByIdAsync(graphId, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded.GraphSnapshotId.Should().Be(graphId);
        loaded.ContextSnapshotId.Should().Be(contextId);
        loaded.RunId.Should().Be(runId);
        loaded.CreatedUtc.Should().Be(createdUtc);
        loaded.Nodes.Should().BeEmpty();
        loaded.Edges.Should().BeEmpty();
        loaded.Warnings.Should().BeEmpty();
    }

    [SkippableFact]
    public async Task SaveAsync_with_explicit_transaction_commits_relational_rows()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        SqlGraphSnapshotRepository repository = new(factory);

        Guid graphId = Guid.NewGuid();
        GraphSnapshot snapshot = new()
        {
            GraphSnapshotId = graphId,
            ContextSnapshotId = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            CreatedUtc = DateTime.UtcNow,
            Nodes = [],
            Edges = [],
            Warnings = ["tw"]
        };

        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);
        await using SqlTransaction tx = connection.BeginTransaction();
        await repository.SaveAsync(snapshot, CancellationToken.None, connection, tx);
        tx.Commit();

        GraphSnapshot? loaded = await repository.GetByIdAsync(graphId, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded.Warnings.Should().Equal("tw");
    }
}
