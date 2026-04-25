using ArchLucid.KnowledgeGraph.Models;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.GraphSnapshots;
using ArchLucid.Persistence.Repositories;
using ArchLucid.Persistence.Serialization;
using ArchLucid.Persistence.Tests.Support;

using Dapper;

using FluentAssertions;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.GraphSnapshots;

/// <summary>
///     Branch coverage for <see cref="GraphSnapshotRelationalRead.LoadEdgesRelationalAsync" /> when
///     <c>mergeMetadataFromJson</c> is <see langword="true" /> but <c>EdgesJson</c> omits an edge id present
///     relationally — <c>jsonById.TryGetValue</c> is <see langword="false" /> for that row.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
[Trait("Suite", "Core")]
public sealed class GraphSnapshotRelationalReadJsonMergePartialEdgeDirectSqlIntegrationTests(
    SqlServerPersistenceFixture fixture)
{
    [SkippableFact]
    public async Task HydrateAsync_skips_EdgesJson_merge_for_relational_edges_missing_from_JSON_dictionary()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

        Guid tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid workspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid scopeProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        Guid runId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        DateTime createdUtc = new(2026, 4, 15, 18, 0, 0, DateTimeKind.Utc);

        await AuthorityRunChainTestSeed.SeedRunAndContextOnlyAsync(
            connection,
            tenantId,
            workspaceId,
            scopeProjectId,
            runId,
            contextId,
            "proj-graph-json-merge-partial",
            CancellationToken.None);

        List<GraphEdge> jsonEdges =
        [
            new()
            {
                EdgeId = "e-in-both",
                FromNodeId = "a",
                ToNodeId = "b",
                EdgeType = "REL",
                Weight = 3d,
                Label = "label-only-for-e-in-both",
                Properties = new Dictionary<string, string>(StringComparer.Ordinal) { ["jk"] = "jv" }
            }
        ];

        string edgesJson = JsonEntitySerializer.Serialize(jsonEdges);
        string emptyNodes = JsonEntitySerializer.Serialize(new List<GraphNode>());
        string emptyWarnings = JsonEntitySerializer.Serialize(new List<string>());

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
                    NodesJson = emptyNodes,
                    EdgesJson = edgesJson,
                    WarningsJson = emptyWarnings
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
                    EdgeId = "e-in-both",
                    FromNodeId = "a",
                    ToNodeId = "b",
                    EdgeType = "REL",
                    Weight = 3d
                },
                cancellationToken: CancellationToken.None));

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertEdge,
                new
                {
                    GraphSnapshotId = graphId,
                    EdgeId = "e-sql-only",
                    FromNodeId = "x",
                    ToNodeId = "y",
                    EdgeType = "LINK",
                    Weight = 0.5d
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

        snapshot.Edges.Should().HaveCount(2);
        GraphEdge merged = snapshot.Edges.Single(e => e.EdgeId == "e-in-both");
        merged.Label.Should().Be("label-only-for-e-in-both");
        merged.Properties.Should().ContainKey("jk").WhoseValue.Should().Be("jv");
        merged.Weight.Should().Be(3d);

        GraphEdge sqlOnly = snapshot.Edges.Single(e => e.EdgeId == "e-sql-only");
        sqlOnly.FromNodeId.Should().Be("x");
        sqlOnly.ToNodeId.Should().Be("y");
        sqlOnly.Weight.Should().Be(0.5d);
        sqlOnly.Label.Should().BeNullOrEmpty();
        sqlOnly.Properties.Should().BeEmpty();
    }
}
