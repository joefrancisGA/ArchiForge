using System.Data;
using System.Diagnostics.CodeAnalysis;

using ArchiForge.Core.Scoping;
using ArchiForge.Persistence.Connections;
using ArchiForge.Provenance;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Provenance;

/// <summary>
/// SQL Server-backed implementation of <see cref="IProvenanceSnapshotRepository"/>.
/// Persists and retrieves <see cref="DecisionProvenanceSnapshot"/> records from the
/// <c>dbo.ProvenanceSnapshots</c> table.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class SqlProvenanceSnapshotRepository(ISqlConnectionFactory connectionFactory)
    : IProvenanceSnapshotRepository
{
    public async Task SaveAsync(
        DecisionProvenanceSnapshot snapshot,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

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

        await using SqlConnection owned = await connectionFactory.CreateOpenConnectionAsync(ct);
        await owned.ExecuteAsync(new CommandDefinition(sql, snapshot, cancellationToken: ct));
    }

    public async Task<DecisionProvenanceSnapshot?> GetByRunIdAsync(ScopeContext scope, Guid runId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(scope);

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

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
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
