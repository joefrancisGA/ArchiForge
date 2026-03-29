using System.Data;

using ArchiForge.KnowledgeGraph.Models;
using ArchiForge.Persistence.RelationalRead;
using ArchiForge.Persistence.Repositories;

using Dapper;

namespace ArchiForge.Persistence.GraphSnapshots;

/// <summary>Loads graph snapshot nodes, warnings, and indexed edges from relational tables.</summary>
internal static class GraphSnapshotRelationalRead
{
    public static async Task<GraphSnapshot> HydrateAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        GraphSnapshotStorageRow row,
        CancellationToken ct)
    {
        Guid graphSnapshotId = row.GraphSnapshotId;

        int nodesCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.GraphSnapshotNodes WHERE GraphSnapshotId = @GraphSnapshotId",
            new
            {
                GraphSnapshotId = graphSnapshotId,
            },
            ct);

        int warningsCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.GraphSnapshotWarnings WHERE GraphSnapshotId = @GraphSnapshotId",
            new
            {
                GraphSnapshotId = graphSnapshotId,
            },
            ct);

        int edgesCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.GraphSnapshotEdges WHERE GraphSnapshotId = @GraphSnapshotId",
            new
            {
                GraphSnapshotId = graphSnapshotId,
            },
            ct);

        int edgePropsCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.GraphSnapshotEdgeProperties WHERE GraphSnapshotId = @GraphSnapshotId",
            new
            {
                GraphSnapshotId = graphSnapshotId,
            },
            ct);

        List<GraphNode>? nodesOverride = null;

        if (nodesCount > 0)
            nodesOverride = await LoadNodesRelationalAsync(connection, transaction, graphSnapshotId, ct);

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
                ct);
        }

        List<GraphEdge>? edgesOverride = null;

        if (edgesCount <= 0)
            return GraphSnapshotStorageMapper.ToSnapshot(row, nodesOverride, edgesOverride, warningsOverride);
        
        bool mergeEdgeMetadataFromJson = edgePropsCount == 0;
        edgesOverride = await LoadEdgesRelationalAsync(connection, transaction, row, mergeEdgeMetadataFromJson, ct);

        return GraphSnapshotStorageMapper.ToSnapshot(row, nodesOverride, edgesOverride, warningsOverride);
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
                cancellationToken: ct));

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
                cancellationToken: ct))).ToList();

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
                cancellationToken: ct))).ToList();

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
                    row.GraphSnapshotId,
                },
                transaction,
                cancellationToken: ct))).ToList();

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
                    row.GraphSnapshotId,
                },
                transaction,
                cancellationToken: ct))).ToList();

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
            List<GraphEdge> jsonEdges = GraphSnapshotJsonFallback.DeserializeEdgesForMerge(row.EdgesJson);
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
