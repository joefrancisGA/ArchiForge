using ArchiForge.KnowledgeGraph.Models;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.Repositories;
using ArchiForge.Persistence.Serialization;

using Dapper;

using FluentAssertions;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Tests;

/// <summary>
/// <see cref="SqlGraphSnapshotRepository"/> against SQL Server + DbUp (relational children + JSON dual-write / read fallback).
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
                    Properties = new Dictionary<string, string>(StringComparer.Ordinal) { ["env"] = "prod" },
                },
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
                    Properties = new Dictionary<string, string>(StringComparer.Ordinal) { ["protocol"] = "grpc" },
                },
            ],
            Warnings = ["w1"],
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
                    Weight = 2d,
                },
                new GraphEdge
                {
                    EdgeId = "a",
                    FromNodeId = "a",
                    ToNodeId = "b",
                    EdgeType = "Y",
                    Weight = 1d,
                },
            ],
        };

        await repository.SaveAsync(snapshot, CancellationToken.None);

        IReadOnlyList<GraphSnapshotIndexedEdge> indexed = await repository.ListIndexedEdgesAsync(snapshot.GraphSnapshotId, CancellationToken.None);
        indexed.Should().HaveCount(2);
        indexed[0].EdgeId.Should().Be("a");
        indexed[1].EdgeId.Should().Be("b");
        indexed[1].Weight.Should().Be(2d);
    }

    [SkippableFact]
    public async Task GetById_when_relational_children_absent_falls_back_to_json_for_nodes_and_warnings()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        SqlGraphSnapshotRepository repository = new(factory);

        Guid graphId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid runId = Guid.NewGuid();

        List<GraphNode> nodes =
        [
            new()
            {
                NodeId = "legacy-n",
                NodeType = "T",
                Label = "Legacy",
                Properties = [],
            },
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
                Properties = new Dictionary<string, string>(StringComparer.Ordinal) { ["k"] = "v" },
            },
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
                    WarningsJson = warningsJson,
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
                    Weight = 1d,
                },
                cancellationToken: CancellationToken.None));

        GraphSnapshot? loaded = await repository.GetByIdAsync(graphId, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded.Nodes.Should().ContainSingle(n => n.NodeId == "legacy-n");
        loaded.Warnings.Should().Equal("jw");
        loaded.Edges.Should().ContainSingle();
        loaded.Edges[0].Label.Should().Be("edge-label-from-json");
        loaded.Edges[0].Properties["k"].Should().Be("v");
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
            Warnings = ["tw"],
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
