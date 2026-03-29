using System.Data;

using ArchiForge.KnowledgeGraph.Interfaces;
using ArchiForge.KnowledgeGraph.Models;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.GraphSnapshots;
using ArchiForge.Persistence.Serialization;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Repositories;

/// <summary>
/// SQL Server-backed implementation of <see cref="IGraphSnapshotRepository"/>.
/// Dual-writes legacy JSON on <c>dbo.GraphSnapshots</c> plus relational children; reads prefer child rows per collection.
/// <c>dbo.GraphSnapshotEdges</c> remains authoritative for <see cref="IGraphSnapshotRepository.ListIndexedEdgesAsync"/> (same query and ordering).
/// </summary>
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
            await SaveCoreAsync(snapshot, connection, transaction, ct).ConfigureAwait(false);
            return;
        }

        await using SqlConnection owned = await connectionFactory.CreateOpenConnectionAsync(ct).ConfigureAwait(false);
        using SqlTransaction tx = owned.BeginTransaction();

        try
        {
            await SaveCoreAsync(snapshot, owned, tx, ct).ConfigureAwait(false);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
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
            WarningsJson = JsonEntitySerializer.Serialize(snapshot.Warnings),
        };

        await connection.ExecuteAsync(new CommandDefinition(headerSql, headerArgs, transaction, cancellationToken: ct))
            .ConfigureAwait(false);

        await InsertNodesAndPropertiesAsync(snapshot, connection, transaction, ct).ConfigureAwait(false);
        await InsertWarningsAsync(snapshot, connection, transaction, ct).ConfigureAwait(false);
        await InsertIndexedEdgesAsync(connection, transaction, snapshot, ct).ConfigureAwait(false);
        await InsertEdgePropertiesAsync(snapshot, connection, transaction, ct).ConfigureAwait(false);
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
                        GraphSnapshotId = snapshot.GraphSnapshotId,
                        SortOrder = i,
                        node.NodeId,
                        node.NodeType,
                        Label = node.Label ?? string.Empty,
                        node.Category,
                        node.SourceType,
                        node.SourceId,
                    },
                    transaction,
                    cancellationToken: ct)).ConfigureAwait(false);

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
                            PropertyValue = kv.Value,
                        },
                        transaction,
                        cancellationToken: ct)).ConfigureAwait(false);
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
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertWarningSql,
                    new
                    {
                        GraphSnapshotId = snapshot.GraphSnapshotId,
                        SortOrder = w,
                        WarningText = snapshot.Warnings[w],
                    },
                    transaction,
                    cancellationToken: ct)).ConfigureAwait(false);
        }
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
                rows.Select(
                    r => new
                    {
                        r.GraphSnapshotId,
                        r.EdgeId,
                        r.FromNodeId,
                        r.ToNodeId,
                        r.EdgeType,
                        r.Weight,
                    }),
                transaction,
                cancellationToken: ct)).ConfigureAwait(false);
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
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        insertEdgePropSql,
                        new
                        {
                            snapshot.GraphSnapshotId,
                            edge.EdgeId,
                            PropertySortOrder = sort++,
                            PropertyKey = GraphSnapshotEdgeRelationalConstants.StoredLabelPropertyKey,
                            PropertyValue = edge.Label,
                        },
                        transaction,
                        cancellationToken: ct)).ConfigureAwait(false);
            }

            List<KeyValuePair<string, string>> orderedProps = edge.Properties
                .Where(kv => !string.Equals(kv.Key, GraphSnapshotEdgeRelationalConstants.StoredLabelPropertyKey, StringComparison.Ordinal))
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .ToList();

            for (int p = 0; p < orderedProps.Count; p++)
            {
                KeyValuePair<string, string> kv = orderedProps[p];

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        insertEdgePropSql,
                        new
                        {
                            snapshot.GraphSnapshotId,
                            edge.EdgeId,
                            PropertySortOrder = sort++,
                            PropertyKey = kv.Key,
                            PropertyValue = kv.Value,
                        },
                        transaction,
                        cancellationToken: ct)).ConfigureAwait(false);
            }
        }
    }

    public async Task<GraphSnapshot?> GetByIdAsync(Guid graphSnapshotId, CancellationToken ct)
    {
        const string sql = """
            SELECT
                GraphSnapshotId, ContextSnapshotId, RunId, CreatedUtc,
                NodesJson, EdgesJson, WarningsJson
            FROM dbo.GraphSnapshots
            WHERE GraphSnapshotId = @GraphSnapshotId;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct).ConfigureAwait(false);
        GraphSnapshotStorageRow? row = await connection.QuerySingleOrDefaultAsync<GraphSnapshotStorageRow>(
            new CommandDefinition(
                sql,
                new
                {
                    GraphSnapshotId = graphSnapshotId,
                },
                cancellationToken: ct)).ConfigureAwait(false);

        if (row is null)
            return null;

        return await HydrateAsync(connection, transaction: null, row, ct).ConfigureAwait(false);
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

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct).ConfigureAwait(false);
        GraphSnapshotStorageRow? row = await connection.QuerySingleOrDefaultAsync<GraphSnapshotStorageRow>(
            new CommandDefinition(
                sql,
                new
                {
                    ContextSnapshotId = contextSnapshotId,
                },
                cancellationToken: ct)).ConfigureAwait(false);

        if (row is null)
            return null;

        return await HydrateAsync(connection, transaction: null, row, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<GraphSnapshotIndexedEdge>> ListIndexedEdgesAsync(Guid graphSnapshotId, CancellationToken ct)
    {
        const string sql = """
            SELECT EdgeId, FromNodeId, ToNodeId, EdgeType, Weight
            FROM dbo.GraphSnapshotEdges
            WHERE GraphSnapshotId = @GraphSnapshotId
            ORDER BY EdgeId;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct).ConfigureAwait(false);
        IEnumerable<IndexedEdgeRow> rows = await connection.QueryAsync<IndexedEdgeRow>(
            new CommandDefinition(
                sql,
                new
                {
                    GraphSnapshotId = graphSnapshotId,
                },
                cancellationToken: ct)).ConfigureAwait(false);

        return rows
            .Select(r => new GraphSnapshotIndexedEdge(r.EdgeId, r.FromNodeId, r.ToNodeId, r.EdgeType, r.Weight))
            .ToList();
    }

    private static async Task<GraphSnapshot> HydrateAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        GraphSnapshotStorageRow row,
        CancellationToken ct)
    {
        Guid graphSnapshotId = row.GraphSnapshotId;

        int nodesCount = await ScalarCountAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.GraphSnapshotNodes WHERE GraphSnapshotId = @GraphSnapshotId",
            new
            {
                GraphSnapshotId = graphSnapshotId,
            },
            ct).ConfigureAwait(false);

        int warningsCount = await ScalarCountAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.GraphSnapshotWarnings WHERE GraphSnapshotId = @GraphSnapshotId",
            new
            {
                GraphSnapshotId = graphSnapshotId,
            },
            ct).ConfigureAwait(false);

        int edgesCount = await ScalarCountAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.GraphSnapshotEdges WHERE GraphSnapshotId = @GraphSnapshotId",
            new
            {
                GraphSnapshotId = graphSnapshotId,
            },
            ct).ConfigureAwait(false);

        int edgePropsCount = await ScalarCountAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.GraphSnapshotEdgeProperties WHERE GraphSnapshotId = @GraphSnapshotId",
            new
            {
                GraphSnapshotId = graphSnapshotId,
            },
            ct).ConfigureAwait(false);

        List<GraphNode>? nodesOverride = null;

        if (nodesCount > 0)
        {
            nodesOverride = await LoadNodesRelationalAsync(connection, transaction, graphSnapshotId, ct).ConfigureAwait(false);
        }

        List<string>? warningsOverride = null;

        if (warningsCount > 0)
        {
            warningsOverride = await LoadStringColumnRelationalAsync(
                connection,
                transaction,
                """
                SELECT WarningText AS Item
                FROM dbo.GraphSnapshotWarnings
                WHERE GraphSnapshotId = @GraphSnapshotId
                ORDER BY SortOrder;
                """,
                graphSnapshotId,
                ct).ConfigureAwait(false);
        }

        List<GraphEdge>? edgesOverride = null;

        if (edgesCount > 0)
        {
            bool mergeEdgeMetadataFromJson = edgePropsCount == 0;
            edgesOverride = await LoadEdgesRelationalAsync(connection, transaction, row, mergeEdgeMetadataFromJson, ct)
                .ConfigureAwait(false);
        }

        return GraphSnapshotStorageMapper.ToSnapshot(row, nodesOverride, edgesOverride, warningsOverride);
    }

    private static async Task<int> ScalarCountAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        string sql,
        object param,
        CancellationToken ct)
    {
        int count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, param, transaction, cancellationToken: ct))
            .ConfigureAwait(false);
        return count;
    }

    private static async Task<List<string>> LoadStringColumnRelationalAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        string sql,
        Guid graphSnapshotId,
        CancellationToken ct)
    {
        IEnumerable<string> rows = await connection.QueryAsync<string>(
            new CommandDefinition(
                sql,
                new
                {
                    GraphSnapshotId = graphSnapshotId,
                },
                transaction,
                cancellationToken: ct)).ConfigureAwait(false);

        return rows.ToList();
    }

    private static async Task<List<GraphNode>> LoadNodesRelationalAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        Guid graphSnapshotId,
        CancellationToken ct)
    {
        const string nodesSql = """
            SELECT GraphNodeRowId, SortOrder, NodeId, NodeType, Label, Category, SourceType, SourceId
            FROM dbo.GraphSnapshotNodes
            WHERE GraphSnapshotId = @GraphSnapshotId
            ORDER BY SortOrder;
            """;

        List<GraphNodeRow> nodeRows = (await connection.QueryAsync<GraphNodeRow>(
            new CommandDefinition(
                nodesSql,
                new
                {
                    GraphSnapshotId = graphSnapshotId,
                },
                transaction,
                cancellationToken: ct)).ConfigureAwait(false)).ToList();

        if (nodeRows.Count == 0)
            return [];

        List<Guid> rowIds = nodeRows.Select(r => r.GraphNodeRowId).ToList();

        const string propsSql = """
            SELECT GraphNodeRowId, PropertySortOrder, PropertyKey, PropertyValue
            FROM dbo.GraphSnapshotNodeProperties
            WHERE GraphNodeRowId IN @RowIds
            ORDER BY GraphNodeRowId, PropertySortOrder;
            """;

        List<NodePropertyRow> propertyRows = (await connection.QueryAsync<NodePropertyRow>(
            new CommandDefinition(
                propsSql,
                new
                {
                    RowIds = rowIds,
                },
                transaction,
                cancellationToken: ct)).ConfigureAwait(false)).ToList();

        Dictionary<Guid, Dictionary<string, string>> propsByNode = new();
        foreach (NodePropertyRow pr in propertyRows)
        {
            if (!propsByNode.TryGetValue(pr.GraphNodeRowId, out Dictionary<string, string>? dict))
            {
                dict = new Dictionary<string, string>(StringComparer.Ordinal);
                propsByNode[pr.GraphNodeRowId] = dict;
            }

            dict[pr.PropertyKey] = pr.PropertyValue;
        }

        List<GraphNode> result = [];
        foreach (GraphNodeRow r in nodeRows)
        {
            propsByNode.TryGetValue(r.GraphNodeRowId, out Dictionary<string, string>? props);
            props ??= new Dictionary<string, string>(StringComparer.Ordinal);

            result.Add(
                new GraphNode
                {
                    NodeId = r.NodeId,
                    NodeType = r.NodeType,
                    Label = r.Label,
                    Category = r.Category,
                    SourceType = r.SourceType,
                    SourceId = r.SourceId,
                    Properties = props,
                });
        }

        return result;
    }

    private static async Task<List<GraphEdge>> LoadEdgesRelationalAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        GraphSnapshotStorageRow row,
        bool mergeMetadataFromJson,
        CancellationToken ct)
    {
        const string edgesSql = """
            SELECT EdgeId, FromNodeId, ToNodeId, EdgeType, Weight
            FROM dbo.GraphSnapshotEdges
            WHERE GraphSnapshotId = @GraphSnapshotId
            ORDER BY EdgeId;
            """;

        List<GraphEdgeTableRow> edgeRows = (await connection.QueryAsync<GraphEdgeTableRow>(
            new CommandDefinition(
                edgesSql,
                new
                {
                    GraphSnapshotId = row.GraphSnapshotId,
                },
                transaction,
                cancellationToken: ct)).ConfigureAwait(false)).ToList();

        if (edgeRows.Count == 0)
            return [];

        List<EdgePropertyRow> propertyRows = (await connection.QueryAsync<EdgePropertyRow>(
            new CommandDefinition(
                """
                SELECT EdgeId, PropertySortOrder, PropertyKey, PropertyValue
                FROM dbo.GraphSnapshotEdgeProperties
                WHERE GraphSnapshotId = @GraphSnapshotId
                ORDER BY EdgeId, PropertySortOrder;
                """,
                new
                {
                    GraphSnapshotId = row.GraphSnapshotId,
                },
                transaction,
                cancellationToken: ct)).ConfigureAwait(false)).ToList();

        Dictionary<string, List<EdgePropertyRow>> propsByEdge = new(StringComparer.Ordinal);
        foreach (EdgePropertyRow pr in propertyRows)
        {
            if (!propsByEdge.TryGetValue(pr.EdgeId, out List<EdgePropertyRow>? list))
            {
                list = [];
                propsByEdge[pr.EdgeId] = list;
            }

            list.Add(pr);
        }

        Dictionary<string, GraphEdge>? jsonById = null;

        if (mergeMetadataFromJson)
        {
            List<GraphEdge> jsonEdges = JsonEntitySerializer.Deserialize<List<GraphEdge>>(row.EdgesJson);
            jsonById = jsonEdges.ToDictionary(e => e.EdgeId, StringComparer.Ordinal);
        }

        List<GraphEdge> result = [];
        foreach (GraphEdgeTableRow er in edgeRows)
        {
            string? label = null;
            Dictionary<string, string> props = new(StringComparer.Ordinal);

            if (propsByEdge.TryGetValue(er.EdgeId, out List<EdgePropertyRow>? rowsForEdge))
            {
                foreach (EdgePropertyRow pr in rowsForEdge.OrderBy(x => x.PropertySortOrder))
                {
                    if (string.Equals(pr.PropertyKey, GraphSnapshotEdgeRelationalConstants.StoredLabelPropertyKey, StringComparison.Ordinal))
                    {
                        label = pr.PropertyValue;
                    }
                    else
                    {
                        props[pr.PropertyKey] = pr.PropertyValue;
                    }
                }
            }

            GraphEdge edge = new()
            {
                EdgeId = er.EdgeId,
                FromNodeId = er.FromNodeId,
                ToNodeId = er.ToNodeId,
                EdgeType = er.EdgeType,
                Weight = er.Weight,
                Label = label,
                Properties = props,
            };

            if (mergeMetadataFromJson && jsonById is not null && jsonById.TryGetValue(er.EdgeId, out GraphEdge? fromJson))
            {
                if (string.IsNullOrEmpty(edge.Label) && !string.IsNullOrEmpty(fromJson.Label))
                    edge.Label = fromJson.Label;

                if (edge.Properties.Count == 0 && fromJson.Properties.Count > 0)
                    edge.Properties = new Dictionary<string, string>(fromJson.Properties, StringComparer.Ordinal);
            }

            result.Add(edge);
        }

        return result;
    }

    private sealed class IndexedEdgeRow
    {
        public string EdgeId { get; init; } = null!;

        public string FromNodeId { get; init; } = null!;

        public string ToNodeId { get; init; } = null!;

        public string EdgeType { get; init; } = null!;

        public double Weight { get; init; }
    }

    private sealed class GraphNodeRow
    {
        public Guid GraphNodeRowId
        {
            get; init;
        }

        public int SortOrder
        {
            get; init;
        }

        public string NodeId { get; init; } = null!;

        public string NodeType { get; init; } = null!;

        public string Label { get; init; } = null!;

        public string? Category { get; init; }

        public string? SourceType { get; init; }

        public string? SourceId { get; init; }
    }

    private sealed class NodePropertyRow
    {
        public Guid GraphNodeRowId
        {
            get; init;
        }

        public int PropertySortOrder
        {
            get; init;
        }

        public string PropertyKey { get; init; } = null!;

        public string PropertyValue { get; init; } = null!;
    }

    private sealed class GraphEdgeTableRow
    {
        public string EdgeId { get; init; } = null!;

        public string FromNodeId { get; init; } = null!;

        public string ToNodeId { get; init; } = null!;

        public string EdgeType { get; init; } = null!;

        public double Weight { get; init; }
    }

    private sealed class EdgePropertyRow
    {
        public string EdgeId { get; init; } = null!;

        public int PropertySortOrder
        {
            get; init;
        }

        public string PropertyKey { get; init; } = null!;

        public string PropertyValue { get; init; } = null!;
    }
}
