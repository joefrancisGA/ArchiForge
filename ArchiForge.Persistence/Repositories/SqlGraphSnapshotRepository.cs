using System.Data;
using ArchiForge.KnowledgeGraph.Interfaces;
using ArchiForge.KnowledgeGraph.Models;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.Serialization;
using Dapper;

namespace ArchiForge.Persistence.Repositories;

public sealed class SqlGraphSnapshotRepository : IGraphSnapshotRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SqlGraphSnapshotRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task SaveAsync(
        GraphSnapshot snapshot,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
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

        var args = new
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
            await connection.ExecuteAsync(new CommandDefinition(sql, args, transaction, cancellationToken: ct));
            return;
        }

        await using var owned = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await owned.ExecuteAsync(new CommandDefinition(sql, args, cancellationToken: ct));
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

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var row = await connection.QuerySingleOrDefaultAsync<GraphSnapshotRow>(
            new CommandDefinition(sql, new { GraphSnapshotId = graphSnapshotId }, cancellationToken: ct));

        if (row is null)
            return null;

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

    private sealed class GraphSnapshotRow
    {
        public Guid GraphSnapshotId { get; set; }
        public Guid ContextSnapshotId { get; set; }
        public Guid RunId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string NodesJson { get; set; } = default!;
        public string EdgesJson { get; set; } = default!;
        public string WarningsJson { get; set; } = default!;
    }
}
