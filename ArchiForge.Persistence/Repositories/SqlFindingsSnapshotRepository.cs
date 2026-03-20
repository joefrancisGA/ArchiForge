using System.Data;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.Serialization;
using Dapper;

namespace ArchiForge.Persistence.Repositories;

public sealed class SqlFindingsSnapshotRepository : IFindingsSnapshotRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SqlFindingsSnapshotRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task SaveAsync(
        FindingsSnapshot snapshot,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        const string sql = """
            INSERT INTO dbo.FindingsSnapshots
            (
                FindingsSnapshotId, RunId, ContextSnapshotId, GraphSnapshotId, CreatedUtc, FindingsJson
            )
            VALUES
            (
                @FindingsSnapshotId, @RunId, @ContextSnapshotId, @GraphSnapshotId, @CreatedUtc, @FindingsJson
            );
            """;

        var args = new
        {
            snapshot.FindingsSnapshotId,
            snapshot.RunId,
            snapshot.ContextSnapshotId,
            snapshot.GraphSnapshotId,
            snapshot.CreatedUtc,
            FindingsJson = JsonEntitySerializer.Serialize(snapshot)
        };

        if (connection is not null)
        {
            await connection.ExecuteAsync(new CommandDefinition(sql, args, transaction, cancellationToken: ct));
            return;
        }

        await using var owned = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await owned.ExecuteAsync(new CommandDefinition(sql, args, cancellationToken: ct));
    }

    public async Task<FindingsSnapshot?> GetByIdAsync(Guid findingsSnapshotId, CancellationToken ct)
    {
        const string sql = """
            SELECT FindingsJson
            FROM dbo.FindingsSnapshots
            WHERE FindingsSnapshotId = @FindingsSnapshotId;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var json = await connection.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition(sql, new { FindingsSnapshotId = findingsSnapshotId }, cancellationToken: ct));

        return json is null ? null : JsonEntitySerializer.Deserialize<FindingsSnapshot>(json);
    }
}
