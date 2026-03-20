using System.Data;
using ArchiForge.Core.Scoping;
using ArchiForge.Persistence.Connections;
using ArchiForge.Provenance;
using Dapper;

namespace ArchiForge.Persistence.Provenance;

public sealed class SqlProvenanceSnapshotRepository : IProvenanceSnapshotRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SqlProvenanceSnapshotRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task SaveAsync(
        DecisionProvenanceSnapshot snapshot,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        const string sql = """
            INSERT INTO dbo.ProvenanceSnapshots (
                Id, TenantId, WorkspaceId, ProjectId, RunId, GraphJson, CreatedUtc
            )
            VALUES (
                @Id, @TenantId, @WorkspaceId, @ProjectId, @RunId, @GraphJson, @CreatedUtc
            );
            """;

        if (connection is not null)
        {
            await connection.ExecuteAsync(new CommandDefinition(sql, snapshot, transaction, cancellationToken: ct));
            return;
        }

        await using var owned = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await owned.ExecuteAsync(new CommandDefinition(sql, snapshot, cancellationToken: ct));
    }

    public async Task<DecisionProvenanceSnapshot?> GetByRunIdAsync(ScopeContext scope, Guid runId, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP 1
                Id, TenantId, WorkspaceId, ProjectId, RunId, GraphJson, CreatedUtc
            FROM dbo.ProvenanceSnapshots
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ScopeProjectId
              AND RunId = @RunId
            ORDER BY CreatedUtc DESC;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<DecisionProvenanceSnapshot>(
            new CommandDefinition(
                sql,
                new
                {
                    scope.TenantId,
                    scope.WorkspaceId,
                    ScopeProjectId = scope.ProjectId,
                    RunId = runId
                },
                cancellationToken: ct));
    }
}
