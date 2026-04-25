using System.Data;
using System.Diagnostics.CodeAnalysis;

using ArchLucid.KnowledgeGraph.Interfaces;
using ArchLucid.KnowledgeGraph.Models;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.GraphSnapshots;
using ArchLucid.Persistence.RelationalRead;
using ArchLucid.Persistence.Serialization;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Repositories;

/// <summary>
///     SQL Server-backed implementation of <see cref="IGraphSnapshotRepository" />.
///     Dual-writes legacy JSON on <c>dbo.GraphSnapshots</c> plus relational children; reads prefer child rows per
///     collection.
///     <c>dbo.GraphSnapshotEdges</c> remains authoritative for
///     <see cref="IGraphSnapshotRepository.ListIndexedEdgesAsync" /> (same query and ordering).
/// </summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class SqlGraphSnapshotRepository(ISqlConnectionFactory connectionFactory) : IGraphSnapshotRepository
{
    public async Task SaveAsync(
        GraphSnapshot snapshot,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (connection is not null)
        {
            await SaveCoreAsync(snapshot, connection, transaction, ct);
            return;
        }

        await using SqlConnection owned = await connectionFactory.CreateOpenConnectionAsync(ct);
        await using SqlTransaction tx = owned.BeginTransaction();

        try
        {
            await SaveCoreAsync(snapshot, owned, tx, ct);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<GraphSnapshot?> GetByIdAsync(Guid graphSnapshotId, CancellationToken ct)
    {
        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        return await GetByIdAsync(graphSnapshotId, connection, null, ct);
    }

    public async Task<GraphSnapshot?> GetLatestByContextSnapshotIdAsync(Guid contextSnapshotId, CancellationToken ct)
    {
        const string sql = """
                           SELECT TOP 1
                               GraphSnapshotId, ContextSnapshotId, RunId, CreatedUtc,
                               NodesJson, EdgesJson, WarningsJson
                           FROM dbo.GraphSnapshots
                           WHERE ContextSnapshotId = @ContextSnapshotId
                           ORDER BY CreatedUtc DESC;
                           """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        GraphSnapshotStorageRow? row = await connection.QuerySingleOrDefaultAsync<GraphSnapshotStorageRow>(
            new CommandDefinition(
                sql,
                new { ContextSnapshotId = contextSnapshotId },
                cancellationToken: ct));

        if (row is null)
            return null;

        return await GraphSnapshotRelationalRead.HydrateAsync(connection, null, row, ct);
    }

    public async Task<IReadOnlyList<GraphSnapshotIndexedEdge>> ListIndexedEdgesAsync(Guid graphSnapshotId,
        CancellationToken ct)
    {
        const string sql = """
                           SELECT EdgeId, FromNodeId, ToNodeId, EdgeType, Weight
                           FROM dbo.GraphSnapshotEdges
                           WHERE GraphSnapshotId = @GraphSnapshotId
                           ORDER BY EdgeId;
                           """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        IEnumerable<IndexedEdgeRow> rows = await connection.QueryAsync<IndexedEdgeRow>(
            new CommandDefinition(
                sql,
                new { GraphSnapshotId = graphSnapshotId },
                cancellationToken: ct));

        return rows
            .Select(r => new GraphSnapshotIndexedEdge(r.EdgeId, r.FromNodeId, r.ToNodeId, r.EdgeType, r.Weight))
            .ToList();
    }

    private static async Task SaveCoreAsync(
        GraphSnapshot snapshot,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        const string headerSql = """
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

        object headerArgs = new
        {
            snapshot.GraphSnapshotId,
            snapshot.ContextSnapshotId,
            snapshot.RunId,
            snapshot.CreatedUtc,
            NodesJson = JsonEntitySerializer.Serialize(snapshot.Nodes),
            EdgesJson = JsonEntitySerializer.Serialize(snapshot.Edges),
            WarningsJson = JsonEntitySerializer.Serialize(snapshot.Warnings)
        };

        await connection.ExecuteAsync(new CommandDefinition(headerSql, headerArgs, transaction, cancellationToken: ct))
            ;

        await InsertNodesAndPropertiesAsync(snapshot, connection, transaction, ct);
        await InsertWarningsAsync(snapshot, connection, transaction, ct);
        await InsertIndexedEdgesAsync(connection, transaction, snapshot, ct);
        await InsertEdgePropertiesAsync(snapshot, connection, transaction, ct);
    }

    private static async Task InsertNodesAndPropertiesAsync(
        GraphSnapshot snapshot,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        const string insertNodeSql = """
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

        const string insertPropertySql = """
                                         INSERT INTO dbo.GraphSnapshotNodeProperties
                                         (GraphNodeRowId, PropertySortOrder, PropertyKey, PropertyValue)
                                         VALUES (@GraphNodeRowId, @PropertySortOrder, @PropertyKey, @PropertyValue);
                                         """;

        for (int i = 0; i < snapshot.Nodes.Count; i++)
        {
            GraphNode node = snapshot.Nodes[i];
            Guid rowId = Guid.NewGuid();

            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertNodeSql,
                    new
                    {
                        GraphNodeRowId = rowId,
                        snapshot.GraphSnapshotId,
                        SortOrder = i,
                        node.NodeId,
                        node.NodeType,
                        node.Label,
                        node.Category,
                        node.SourceType,
                        node.SourceId
                    },
                    transaction,
                    cancellationToken: ct));

            List<KeyValuePair<string, string>> orderedProps = node.Properties
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .ToList();

            for (int p = 0; p < orderedProps.Count; p++)
            {
                KeyValuePair<string, string> kv = orderedProps[p];

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        insertPropertySql,
                        new
                        {
                            GraphNodeRowId = rowId,
                            PropertySortOrder = p,
                            PropertyKey = kv.Key,
                            PropertyValue = kv.Value
                        },
                        transaction,
                        cancellationToken: ct));
            }
        }
    }

    private static async Task InsertWarningsAsync(
        GraphSnapshot snapshot,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        const string insertWarningSql = """
                                        INSERT INTO dbo.GraphSnapshotWarnings (GraphSnapshotId, SortOrder, WarningText)
                                        VALUES (@GraphSnapshotId, @SortOrder, @WarningText);
                                        """;

        for (int w = 0; w < snapshot.Warnings.Count; w++)

            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertWarningSql,
                    new { snapshot.GraphSnapshotId, SortOrder = w, WarningText = snapshot.Warnings[w] },
                    transaction,
                    cancellationToken: ct));
    }

    private static async Task InsertIndexedEdgesAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        GraphSnapshot snapshot,
        CancellationToken ct)
    {
        IReadOnlyList<GraphSnapshotEdgeRow> rows = GraphSnapshotEdgeIndexer.BuildRows(snapshot);

        if (rows.Count == 0)
            return;

        const string edgeSql = """
                               INSERT INTO dbo.GraphSnapshotEdges (GraphSnapshotId, EdgeId, FromNodeId, ToNodeId, EdgeType, Weight)
                               VALUES (@GraphSnapshotId, @EdgeId, @FromNodeId, @ToNodeId, @EdgeType, @Weight);
                               """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                edgeSql,
                rows.Select(r => new
                {
                    r.GraphSnapshotId,
                    r.EdgeId,
                    r.FromNodeId,
                    r.ToNodeId,
                    r.EdgeType,
                    r.Weight
                }),
                transaction,
                cancellationToken: ct));
    }

    private static async Task InsertEdgePropertiesAsync(
        GraphSnapshot snapshot,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        const string insertEdgePropSql = """
                                         INSERT INTO dbo.GraphSnapshotEdgeProperties
                                         (GraphSnapshotId, EdgeId, PropertySortOrder, PropertyKey, PropertyValue)
                                         VALUES (@GraphSnapshotId, @EdgeId, @PropertySortOrder, @PropertyKey, @PropertyValue);
                                         """;

        foreach (GraphEdge edge in snapshot.Edges)
        {
            int sort = 0;

            if (!string.IsNullOrEmpty(edge.Label))

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        insertEdgePropSql,
                        new
                        {
                            snapshot.GraphSnapshotId,
                            edge.EdgeId,
                            PropertySortOrder = sort++,
                            PropertyKey = GraphSnapshotEdgeRelationalConstants.StoredLabelPropertyKey,
                            PropertyValue = edge.Label
                        },
                        transaction,
                        cancellationToken: ct));


            List<KeyValuePair<string, string>> orderedProps = edge.Properties
                .Where(kv => !string.Equals(kv.Key, GraphSnapshotEdgeRelationalConstants.StoredLabelPropertyKey,
                    StringComparison.Ordinal))
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .ToList();

            foreach (KeyValuePair<string, string> kv in orderedProps)

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        insertEdgePropSql,
                        new
                        {
                            snapshot.GraphSnapshotId,
                            edge.EdgeId,
                            PropertySortOrder = sort++,
                            PropertyKey = kv.Key,
                            PropertyValue = kv.Value
                        },
                        transaction,
                        cancellationToken: ct));
        }
    }

    /// <inheritdoc cref="GetByIdAsync(System.Guid,System.Threading.CancellationToken)" />
    public async Task<GraphSnapshot?> GetByIdAsync(
        Guid graphSnapshotId,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        const string sql = """
                           SELECT
                               GraphSnapshotId, ContextSnapshotId, RunId, CreatedUtc,
                               NodesJson, EdgesJson, WarningsJson
                           FROM dbo.GraphSnapshots
                           WHERE GraphSnapshotId = @GraphSnapshotId;
                           """;

        GraphSnapshotStorageRow? row = await connection.QuerySingleOrDefaultAsync<GraphSnapshotStorageRow>(
            new CommandDefinition(
                sql,
                new { GraphSnapshotId = graphSnapshotId },
                transaction,
                cancellationToken: ct));

        if (row is null)
            return null;

        return await GraphSnapshotRelationalRead.HydrateAsync(connection, transaction, row, ct);
    }

    /// <summary>
    ///     Inserts relational graph slices that are still empty while JSON columns contain data (idempotent per slice).
    /// </summary>
    internal static async Task BackfillRelationalSlicesAsync(
        GraphSnapshot snapshot,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(connection);

        Guid graphSnapshotId = snapshot.GraphSnapshotId;

        int nodesCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.GraphSnapshotNodes WHERE GraphSnapshotId = @GraphSnapshotId",
            new { GraphSnapshotId = graphSnapshotId },
            ct);

        int warningsCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.GraphSnapshotWarnings WHERE GraphSnapshotId = @GraphSnapshotId",
            new { GraphSnapshotId = graphSnapshotId },
            ct);

        int edgesCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.GraphSnapshotEdges WHERE GraphSnapshotId = @GraphSnapshotId",
            new { GraphSnapshotId = graphSnapshotId },
            ct);

        int edgePropsCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.GraphSnapshotEdgeProperties WHERE GraphSnapshotId = @GraphSnapshotId",
            new { GraphSnapshotId = graphSnapshotId },
            ct);

        if (nodesCount == 0 && snapshot.Nodes.Count > 0)
            await InsertNodesAndPropertiesAsync(snapshot, connection, transaction, ct);

        if (warningsCount == 0 && snapshot.Warnings.Count > 0)
            await InsertWarningsAsync(snapshot, connection, transaction, ct);

        if (edgesCount == 0 && snapshot.Edges.Count > 0)
        {
            await InsertIndexedEdgesAsync(connection, transaction, snapshot, ct);
            await InsertEdgePropertiesAsync(snapshot, connection, transaction, ct);
        }
        else if (edgesCount > 0 && edgePropsCount == 0 && snapshot.Edges.Count > 0)
            await InsertEdgePropertiesAsync(snapshot, connection, transaction, ct);
    }

    private sealed class IndexedEdgeRow
    {
        public string EdgeId
        {
            get;
            init;
        } = null!;

        public string FromNodeId
        {
            get;
            init;
        } = null!;

        public string ToNodeId
        {
            get;
            init;
        } = null!;

        public string EdgeType
        {
            get;
            init;
        } = null!;

        public double Weight
        {
            get;
            init;
        }
    }
}
