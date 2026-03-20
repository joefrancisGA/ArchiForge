using System.Data;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.Interfaces;
using ArchiForge.Persistence.Models;
using Dapper;

namespace ArchiForge.Persistence.Repositories;

public sealed class SqlRunRepository : IRunRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SqlRunRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task SaveAsync(
        RunRecord run,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        const string sql = """
            INSERT INTO dbo.Runs
            (
                RunId, ProjectId, Description, CreatedUtc,
                ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId,
                GoldenManifestId, DecisionTraceId, ArtifactBundleId
            )
            VALUES
            (
                @RunId, @ProjectId, @Description, @CreatedUtc,
                @ContextSnapshotId, @GraphSnapshotId, @FindingsSnapshotId,
                @GoldenManifestId, @DecisionTraceId, @ArtifactBundleId
            );
            """;

        if (connection is not null)
        {
            await connection.ExecuteAsync(new CommandDefinition(sql, run, transaction, cancellationToken: ct));
            return;
        }

        await using var owned = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await owned.ExecuteAsync(new CommandDefinition(sql, run, cancellationToken: ct));
    }

    public async Task<RunRecord?> GetByIdAsync(Guid runId, CancellationToken ct)
    {
        const string sql = """
            SELECT
                RunId, ProjectId, Description, CreatedUtc,
                ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId,
                GoldenManifestId, DecisionTraceId, ArtifactBundleId
            FROM dbo.Runs
            WHERE RunId = @RunId;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<RunRecord>(
            new CommandDefinition(sql, new { RunId = runId }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<RunRecord>> ListByProjectAsync(string projectId, int take, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP (@Take)
                RunId, ProjectId, Description, CreatedUtc,
                ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId,
                GoldenManifestId, DecisionTraceId, ArtifactBundleId
            FROM dbo.Runs
            WHERE ProjectId = @ProjectId
            ORDER BY CreatedUtc DESC;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var rows = await connection.QueryAsync<RunRecord>(
            new CommandDefinition(
                sql,
                new { ProjectId = projectId, Take = take <= 0 ? 20 : take },
                cancellationToken: ct));

        return rows.ToList();
    }

    public async Task UpdateAsync(
        RunRecord run,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        const string sql = """
            UPDATE dbo.Runs
            SET
                ProjectId = @ProjectId,
                Description = @Description,
                ContextSnapshotId = @ContextSnapshotId,
                GraphSnapshotId = @GraphSnapshotId,
                FindingsSnapshotId = @FindingsSnapshotId,
                GoldenManifestId = @GoldenManifestId,
                DecisionTraceId = @DecisionTraceId,
                ArtifactBundleId = @ArtifactBundleId
            WHERE RunId = @RunId;
            """;

        if (connection is not null)
        {
            await connection.ExecuteAsync(new CommandDefinition(sql, run, transaction, cancellationToken: ct));
            return;
        }

        await using var owned = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await owned.ExecuteAsync(new CommandDefinition(sql, run, cancellationToken: ct));
    }
}
