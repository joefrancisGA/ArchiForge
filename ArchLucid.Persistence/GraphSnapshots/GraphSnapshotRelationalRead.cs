using System.Data;

using ArchLucid.KnowledgeGraph.Models;
using ArchLucid.Persistence.RelationalRead;
using ArchLucid.Persistence.Repositories;
using ArchLucid.Persistence.Serialization;

using Dapper;

namespace ArchLucid.Persistence.GraphSnapshots;

/// <summary>Loads graph snapshot nodes, warnings, and indexed edges from relational tables.</summary>
/// <remarks>
///     When <c>dbo.GraphSnapshotEdges</c> has rows, matching <c>EdgesJson</c> entries are merged per <c>EdgeId</c> for
///     label and properties that are absent relationally (legacy enrichment until all edge metadata is backfilled
///     relationally). Relational values win when present. <c>EdgesJson</c> is taken from the optional merge row when
///     non-empty; otherwise it is read from <c>dbo.GraphSnapshots</c> so header-only callers still merge correctly when
///     edge property rows exist only for some edges.
/// </remarks>
internal static class GraphSnapshotRelationalRead
{
    /// <summary>Identity row for a graph snapshot header (no JSON columns — avoids loading large NVARCHAR(MAX) payloads).</summary>
    internal sealed record GraphSnapshotHeaderRow(
        Guid GraphSnapshotId,
        Guid ContextSnapshotId,
        Guid RunId,
        DateTime CreatedUtc);

    /// <summary>Hydrates from a full storage row (integration tests and callers that already materialized JSON columns).</summary>
    public static Task<GraphSnapshot> HydrateAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        GraphSnapshotStorageRow row,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(row);
        GraphSnapshotHeaderRow header = new(row.GraphSnapshotId, row.ContextSnapshotId, row.RunId, row.CreatedUtc);

        return HydrateAsync(connection, transaction, header, jsonRowForMerge: row, ct);
    }

    /// <summary>
    ///     Hydrates using relational slices; when relational edges exist, <c>EdgesJson</c> is merged per <c>EdgeId</c>
    ///     for missing relational label/properties. Non-empty <paramref name="jsonRowForMerge" />.<c>EdgesJson</c>
    ///     avoids an extra query; when omitted or empty, <c>EdgesJson</c> is read from <c>dbo.GraphSnapshots</c>.
    /// </summary>
    public static async Task<GraphSnapshot> HydrateAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        GraphSnapshotHeaderRow header,
        GraphSnapshotStorageRow? jsonRowForMerge,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(header);

        Guid graphSnapshotId = header.GraphSnapshotId;

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

        List<GraphNode>? nodesOverride = null;

        if (nodesCount > 0)
            nodesOverride = await LoadNodesRelationalAsync(connection, transaction, graphSnapshotId, ct);

        List<string>? warningsOverride = null;

        if (warningsCount > 0)

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


        List<GraphEdge>? edgesOverride = null;

        GraphSnapshotStorageRow syntheticHeader = new()
        {
            GraphSnapshotId = header.GraphSnapshotId,
            ContextSnapshotId = header.ContextSnapshotId,
            RunId = header.RunId,
            CreatedUtc = header.CreatedUtc,
            NodesJson = "[]",
            EdgesJson = "[]",
            WarningsJson = "[]"
        };

        if (edgesCount <= 0)
            return GraphSnapshotStorageMapper.ToSnapshot(syntheticHeader, nodesOverride, edgesOverride, warningsOverride);

        edgesOverride = await LoadEdgesRelationalAsync(
            connection,
            transaction,
            graphSnapshotId,
            jsonRowForMerge,
            ct);

        return GraphSnapshotStorageMapper.ToSnapshot(syntheticHeader, nodesOverride, edgesOverride, warningsOverride);
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
                new { GraphSnapshotId = graphSnapshotId },
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
                new { GraphSnapshotId = graphSnapshotId },
                transaction,
                cancellationToken: ct))).ToList();

        if (nodeRows.Count == 0)
            return [];

        const string propsSql = """
                                SELECT p.GraphNodeRowId, p.PropertySortOrder, p.PropertyKey, p.PropertyValue
                                FROM dbo.GraphSnapshotNodeProperties AS p
                                WHERE EXISTS (
                                    SELECT 1
                                    FROM dbo.GraphSnapshotNodes AS n
                                    WHERE n.GraphNodeRowId = p.GraphNodeRowId
                                      AND n.GraphSnapshotId = @GraphSnapshotId)
                                ORDER BY p.GraphNodeRowId, p.PropertySortOrder;
                                """;

        List<NodePropertyRow> propertyRows = (await connection.QueryAsync<NodePropertyRow>(
            new CommandDefinition(
                propsSql,
                new { GraphSnapshotId = graphSnapshotId },
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
                    Properties = props
                });
        }

        return result;
    }

    private static async Task<List<GraphEdge>> LoadEdgesRelationalAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        Guid graphSnapshotId,
        GraphSnapshotStorageRow? jsonRowForMerge,
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
                new { GraphSnapshotId = graphSnapshotId },
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
                new { GraphSnapshotId = graphSnapshotId },
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

        string edgesJson;

        if (jsonRowForMerge is not null && !string.IsNullOrWhiteSpace(jsonRowForMerge.EdgesJson))
            edgesJson = jsonRowForMerge.EdgesJson;
        else
        {
            const string edgesJsonSql =
                "SELECT EdgesJson FROM dbo.GraphSnapshots WHERE GraphSnapshotId = @GraphSnapshotId";

            string? loaded = await connection.QuerySingleOrDefaultAsync<string>(
                new CommandDefinition(
                    edgesJsonSql,
                    new { GraphSnapshotId = graphSnapshotId },
                    transaction,
                    cancellationToken: ct));

            edgesJson = string.IsNullOrWhiteSpace(loaded) ? "[]" : loaded;
        }

        List<GraphEdge> jsonEdges = JsonEntitySerializer.Deserialize<List<GraphEdge>>(edgesJson);
        Dictionary<string, GraphEdge> jsonById = jsonEdges.ToDictionary(e => e.EdgeId, StringComparer.Ordinal);

        List<GraphEdge> result = [];
        foreach (GraphEdgeTableRow er in edgeRows)
        {
            string? label = null;
            Dictionary<string, string> props = new(StringComparer.Ordinal);

            if (propsByEdge.TryGetValue(er.EdgeId, out List<EdgePropertyRow>? rowsForEdge))

                foreach (EdgePropertyRow pr in rowsForEdge.OrderBy(x => x.PropertySortOrder))

                    if (string.Equals(pr.PropertyKey, GraphSnapshotEdgeRelationalConstants.StoredLabelPropertyKey,
                            StringComparison.Ordinal))
                        label = pr.PropertyValue;
                    else
                        props[pr.PropertyKey] = pr.PropertyValue;


            GraphEdge edge = new()
            {
                EdgeId = er.EdgeId,
                FromNodeId = er.FromNodeId,
                ToNodeId = er.ToNodeId,
                EdgeType = er.EdgeType,
                Weight = er.Weight,
                Label = label,
                Properties = props
            };

            if (jsonById.TryGetValue(er.EdgeId, out GraphEdge? fromJson))
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
            get;
            init;
        }

        public int SortOrder
        {
            get;
            init;
        }

        public string NodeId
        {
            get;
            init;
        } = null!;

        public string NodeType
        {
            get;
            init;
        } = null!;

        public string Label
        {
            get;
            init;
        } = null!;

        public string? Category
        {
            get;
            init;
        }

        public string? SourceType
        {
            get;
            init;
        }

        public string? SourceId
        {
            get;
            init;
        }
    }

    private sealed class NodePropertyRow
    {
        public Guid GraphNodeRowId
        {
            get;
            init;
        }

        public int PropertySortOrder
        {
            get;
            init;
        }

        public string PropertyKey
        {
            get;
            init;
        } = null!;

        public string PropertyValue
        {
            get;
            init;
        } = null!;
    }

    private sealed class GraphEdgeTableRow
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

    private sealed class EdgePropertyRow
    {
        public string EdgeId
        {
            get;
            init;
        } = null!;

        public int PropertySortOrder
        {
            get;
            init;
        }

        public string PropertyKey
        {
            get;
            init;
        } = null!;

        public string PropertyValue
        {
            get;
            init;
        } = null!;
    }
}
