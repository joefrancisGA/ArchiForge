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
///     Exercises <see cref="GraphSnapshotRelationalRead.HydrateAsync" /> when only
///     <c>dbo.GraphSnapshotWarnings</c> has rows (no relational nodes/edges): early return after
///     <c>LoadStringColumnRelationalAsync</c> with <c>ORDER BY SortOrder</c>.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
[Trait("Suite", "Core")]
public sealed class GraphSnapshotRelationalReadOrderedWarningsNoEdgesDirectSqlIntegrationTests(
    SqlServerPersistenceFixture fixture)
{
    [SkippableFact]
    public async Task HydrateAsync_returns_warnings_in_SortOrder_when_no_nodes_and_no_edges()
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
        DateTime createdUtc = new(2026, 4, 16, 12, 0, 0, DateTimeKind.Utc);

        await AuthorityRunChainTestSeed.SeedRunAndContextOnlyAsync(
            connection,
            tenantId,
            workspaceId,
            scopeProjectId,
            runId,
            contextId,
            "proj-graph-warnings-order",
            CancellationToken.None);

        string emptyNodes = JsonEntitySerializer.Serialize(new List<GraphNode>());
        string emptyEdges = JsonEntitySerializer.Serialize(new List<GraphEdge>());
        List<string> jsonWarnings = ["json-should-lose"];

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
                    EdgesJson = emptyEdges,
                    WarningsJson = JsonEntitySerializer.Serialize(jsonWarnings)
                },
                cancellationToken: CancellationToken.None));

        const string insertWarning = """
                                     INSERT INTO dbo.GraphSnapshotWarnings (GraphSnapshotId, SortOrder, WarningText)
                                     VALUES (@GraphSnapshotId, @SortOrder, @WarningText);
                                     """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertWarning,
                new { GraphSnapshotId = graphId, SortOrder = 1, WarningText = "second-row-should-appear-last" },
                cancellationToken: CancellationToken.None));

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertWarning,
                new { GraphSnapshotId = graphId, SortOrder = 0, WarningText = "first-row-should-appear-first" },
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

        snapshot.Nodes.Should().BeEmpty();
        snapshot.Edges.Should().BeEmpty();
        snapshot.Warnings.Should().Equal("first-row-should-appear-first", "second-row-should-appear-last");
    }
}
