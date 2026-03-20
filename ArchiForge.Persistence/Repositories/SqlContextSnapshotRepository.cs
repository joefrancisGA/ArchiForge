using System.Data;
using ArchiForge.ContextIngestion.Interfaces;
using ArchiForge.ContextIngestion.Models;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.Serialization;
using Dapper;

namespace ArchiForge.Persistence.Repositories;

public sealed class SqlContextSnapshotRepository : IContextSnapshotRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SqlContextSnapshotRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ContextSnapshot?> GetLatestAsync(string projectId, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP 1
                SnapshotId,
                RunId,
                ProjectId,
                CreatedUtc,
                CanonicalObjectsJson,
                DeltaSummary,
                WarningsJson,
                ErrorsJson,
                SourceHashesJson
            FROM dbo.ContextSnapshots
            WHERE ProjectId = @ProjectId
            ORDER BY CreatedUtc DESC;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var row = await connection.QuerySingleOrDefaultAsync<ContextSnapshotRow>(
            new CommandDefinition(sql, new { ProjectId = projectId }, cancellationToken: ct));

        return row is null ? null : Map(row);
    }

    public async Task<ContextSnapshot?> GetByIdAsync(Guid snapshotId, CancellationToken ct)
    {
        const string sql = """
            SELECT
                SnapshotId,
                RunId,
                ProjectId,
                CreatedUtc,
                CanonicalObjectsJson,
                DeltaSummary,
                WarningsJson,
                ErrorsJson,
                SourceHashesJson
            FROM dbo.ContextSnapshots
            WHERE SnapshotId = @SnapshotId;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var row = await connection.QuerySingleOrDefaultAsync<ContextSnapshotRow>(
            new CommandDefinition(sql, new { SnapshotId = snapshotId }, cancellationToken: ct));

        return row is null ? null : Map(row);
    }

    public async Task SaveAsync(
        ContextSnapshot snapshot,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        const string sql = """
            INSERT INTO dbo.ContextSnapshots
            (
                SnapshotId, RunId, ProjectId, CreatedUtc,
                CanonicalObjectsJson, DeltaSummary, WarningsJson, ErrorsJson, SourceHashesJson
            )
            VALUES
            (
                @SnapshotId, @RunId, @ProjectId, @CreatedUtc,
                @CanonicalObjectsJson, @DeltaSummary, @WarningsJson, @ErrorsJson, @SourceHashesJson
            );
            """;

        var args = new
        {
            snapshot.SnapshotId,
            snapshot.RunId,
            snapshot.ProjectId,
            snapshot.CreatedUtc,
            CanonicalObjectsJson = JsonEntitySerializer.Serialize(snapshot.CanonicalObjects),
            snapshot.DeltaSummary,
            WarningsJson = JsonEntitySerializer.Serialize(snapshot.Warnings),
            ErrorsJson = JsonEntitySerializer.Serialize(snapshot.Errors),
            SourceHashesJson = JsonEntitySerializer.Serialize(snapshot.SourceHashes)
        };

        if (connection is not null)
        {
            await connection.ExecuteAsync(new CommandDefinition(sql, args, transaction, cancellationToken: ct));
            return;
        }

        await using var owned = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await owned.ExecuteAsync(new CommandDefinition(sql, args, cancellationToken: ct));
    }

    private static ContextSnapshot Map(ContextSnapshotRow row)
    {
        return new ContextSnapshot
        {
            SnapshotId = row.SnapshotId,
            RunId = row.RunId,
            ProjectId = row.ProjectId,
            CreatedUtc = row.CreatedUtc,
            CanonicalObjects = JsonEntitySerializer.Deserialize<List<CanonicalObject>>(row.CanonicalObjectsJson),
            DeltaSummary = row.DeltaSummary,
            Warnings = JsonEntitySerializer.Deserialize<List<string>>(row.WarningsJson),
            Errors = JsonEntitySerializer.Deserialize<List<string>>(row.ErrorsJson),
            SourceHashes = JsonEntitySerializer.Deserialize<Dictionary<string, string>>(row.SourceHashesJson)
        };
    }

    private sealed class ContextSnapshotRow
    {
        public Guid SnapshotId { get; set; }
        public Guid RunId { get; set; }
        public string ProjectId { get; set; } = default!;
        public DateTime CreatedUtc { get; set; }
        public string CanonicalObjectsJson { get; set; } = default!;
        public string? DeltaSummary { get; set; }
        public string WarningsJson { get; set; } = default!;
        public string ErrorsJson { get; set; } = default!;
        public string SourceHashesJson { get; set; } = default!;
    }
}
