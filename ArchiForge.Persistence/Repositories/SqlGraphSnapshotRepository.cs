using System.Data;

using ArchiForge.KnowledgeGraph.Interfaces;
using ArchiForge.KnowledgeGraph.Models;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.Serialization;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Repositories;

/// <summary>
/// SQL Server-backed implementation of <see cref="IGraphSnapshotRepository"/>.
/// Persists and retrieves <see cref="GraphSnapshot"/> rows from the <c>dbo.GraphSnapshots</c> table,
/// serializing node, edge, and warning collections to JSON columns.
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

        const string sql = """
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

        object args = new
        {
            snapshot.GraphSnapshotId,
            snapshot.ContextSnapshotId,
            snapshot.RunId,
            snapshot.CreatedUtc,
            NodesJson = JsonEntitySerializer.Serialize(snapshot.Nodes),
            EdgesJson = JsonEntitySerializer.Serialize(snapshot.Edges),
            WarningsJson = JsonEntitySerializer.Serialize(snapshot.Warnings)
        };

        if (connection is not null)
        {
            await connection.ExecuteAsync(new CommandDefinition(sql, args, transaction, cancellationToken: ct)).ConfigureAwait(false);
            await InsertIndexedEdgesAsync(connection, transaction, snapshot, ct).ConfigureAwait(false);
            return;
        }

        await using SqlConnection owned = await connectionFactory.CreateOpenConnectionAsync(ct).ConfigureAwait(false);
        await owned.ExecuteAsync(new CommandDefinition(sql, args, cancellationToken: ct)).ConfigureAwait(false);
        await InsertIndexedEdgesAsync(owned, transaction: null, snapshot, ct).ConfigureAwait(false);
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

        await connection.ExecuteAsync(new CommandDefinition(
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
            cancellationToken: ct)).ConfigureAwait(false);
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
        GraphSnapshotRow? row = await connection.QuerySingleOrDefaultAsync<GraphSnapshotRow>(
            new CommandDefinition(sql, new
            {
                GraphSnapshotId = graphSnapshotId
            }, cancellationToken: ct)).ConfigureAwait(false);

        if (row is null)
            return null;

        try
        {
            return new GraphSnapshot
            {
                GraphSnapshotId = row.GraphSnapshotId,
                ContextSnapshotId = row.ContextSnapshotId,
                RunId = row.RunId,
                CreatedUtc = row.CreatedUtc,
                Nodes = JsonEntitySerializer.Deserialize<List<GraphNode>>(row.NodesJson),
                Edges = JsonEntitySerializer.Deserialize<List<GraphEdge>>(row.EdgesJson),
                Warnings = JsonEntitySerializer.Deserialize<List<string>>(row.WarningsJson)
            };
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize GraphSnapshot '{row.GraphSnapshotId}'. " +
                "The stored JSON may be corrupt or from an incompatible schema version.", ex);
        }
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
        GraphSnapshotRow? row = await connection.QuerySingleOrDefaultAsync<GraphSnapshotRow>(
            new CommandDefinition(sql, new
            {
                ContextSnapshotId = contextSnapshotId
            }, cancellationToken: ct)).ConfigureAwait(false);

        if (row is null)
            return null;

        try
        {
            return new GraphSnapshot
            {
                GraphSnapshotId = row.GraphSnapshotId,
                ContextSnapshotId = row.ContextSnapshotId,
                RunId = row.RunId,
                CreatedUtc = row.CreatedUtc,
                Nodes = JsonEntitySerializer.Deserialize<List<GraphNode>>(row.NodesJson),
                Edges = JsonEntitySerializer.Deserialize<List<GraphEdge>>(row.EdgesJson),
                Warnings = JsonEntitySerializer.Deserialize<List<string>>(row.WarningsJson)
            };
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize GraphSnapshot '{row.GraphSnapshotId}'. " +
                "The stored JSON may be corrupt or from an incompatible schema version.", ex);
        }
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
            new CommandDefinition(sql, new
            {
                GraphSnapshotId = graphSnapshotId
            }, cancellationToken: ct)).ConfigureAwait(false);

        return rows
            .Select(r => new GraphSnapshotIndexedEdge(r.EdgeId, r.FromNodeId, r.ToNodeId, r.EdgeType, r.Weight))
            .ToList();
    }

    private sealed class IndexedEdgeRow
    {
        public string EdgeId { get; init; } = null!;
        public string FromNodeId { get; init; } = null!;
        public string ToNodeId { get; init; } = null!;
        public string EdgeType { get; init; } = null!;
        public double Weight { get; init; }
    }

    private sealed class GraphSnapshotRow
    {
        public Guid GraphSnapshotId
        {
            get; init;
        }
        public Guid ContextSnapshotId
        {
            get; init;
        }
        public Guid RunId
        {
            get; init;
        }
        public DateTime CreatedUtc
        {
            get; init;
        }
        public string NodesJson { get; init; } = null!;
        public string EdgesJson { get; init; } = null!;
        public string WarningsJson { get; init; } = null!;
    }
}
