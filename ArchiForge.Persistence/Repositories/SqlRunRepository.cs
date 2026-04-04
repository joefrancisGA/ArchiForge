using System.Data;
using System.Diagnostics.CodeAnalysis;

using ArchiForge.Core.Scoping;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.Interfaces;
using ArchiForge.Persistence.Models;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Repositories;

/// <summary>
/// SQL Server-backed implementation of <see cref="IRunRepository"/>.
/// Persists and retrieves <see cref="RunRecord"/> rows from the <c>dbo.Runs</c> table.
/// All read operations are scoped to the caller's tenant, workspace, and project.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class SqlRunRepository(
    ISqlConnectionFactory connectionFactory,
    IAuthorityRunListConnectionFactory authorityRunListConnectionFactory) : IRunRepository
{
    public async Task SaveAsync(
        RunRecord run,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(run);

        const string sql = """
            INSERT INTO dbo.Runs
            (
                RunId, TenantId, WorkspaceId, ScopeProjectId, ProjectId, Description, CreatedUtc,
                ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId,
                GoldenManifestId, DecisionTraceId, ArtifactBundleId, ArchivedUtc
            )
            OUTPUT inserted.RowVersionStamp
            VALUES
            (
                @RunId, @TenantId, @WorkspaceId, @ScopeProjectId, @ProjectId, @Description, @CreatedUtc,
                @ContextSnapshotId, @GraphSnapshotId, @FindingsSnapshotId,
                @GoldenManifestId, @DecisionTraceId, @ArtifactBundleId, @ArchivedUtc
            );
            """;

        if (connection is not null)
        {
            byte[] stamp = await connection.QuerySingleAsync<byte[]>(
                new CommandDefinition(sql, run, transaction, cancellationToken: ct));
            run.RowVersion = stamp;

            return;
        }

        await using SqlConnection owned = await connectionFactory.CreateOpenConnectionAsync(ct);
        byte[] ownedStamp = await owned.QuerySingleAsync<byte[]>(new CommandDefinition(sql, run, cancellationToken: ct));
        run.RowVersion = ownedStamp;
    }

    public async Task<RunRecord?> GetByIdAsync(ScopeContext scope, Guid runId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(scope);

        const string sql = """
            SELECT
                RunId, TenantId, WorkspaceId, ScopeProjectId, ProjectId, Description, CreatedUtc,
                ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId,
                GoldenManifestId, DecisionTraceId, ArtifactBundleId, ArchivedUtc,
                RowVersionStamp AS RowVersion
            FROM dbo.Runs
            WHERE RunId = @RunId
              AND TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ScopeProjectId = @ScopeProjectId
              AND ArchivedUtc IS NULL;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<RunRecord>(
            new CommandDefinition(
                sql,
                new
                {
                    RunId = runId,
                    scope.TenantId,
                    scope.WorkspaceId,
                    ScopeProjectId = scope.ProjectId
                },
                cancellationToken: ct));
    }

    public async Task<IReadOnlyList<RunRecord>> ListByProjectAsync(
        ScopeContext scope,
        string projectId,
        int take,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(scope);

        const string sql = """
            SELECT TOP (@Take)
                RunId, TenantId, WorkspaceId, ScopeProjectId, ProjectId, Description, CreatedUtc,
                ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId,
                GoldenManifestId, DecisionTraceId, ArtifactBundleId, ArchivedUtc
            FROM dbo.Runs
            WHERE ProjectId = @ProjectSlug
              AND TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ScopeProjectId = @ScopeProjectId
              AND ArchivedUtc IS NULL
            ORDER BY CreatedUtc DESC;
            """;

        await using SqlConnection connection = await authorityRunListConnectionFactory.CreateOpenConnectionAsync(ct);
        IEnumerable<RunRecord> rows = await connection.QueryAsync<RunRecord>(
            new CommandDefinition(
                sql,
                new
                {
                    ProjectSlug = projectId,
                    scope.TenantId,
                    scope.WorkspaceId,
                    ScopeProjectId = scope.ProjectId,
                    Take = Math.Clamp(take <= 0 ? 20 : take, 1, 200)
                },
                cancellationToken: ct));

        return rows.ToList();
    }

    public async Task UpdateAsync(
        RunRecord run,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(run);

        const string sql = """
            UPDATE dbo.Runs
            SET
                TenantId = @TenantId,
                WorkspaceId = @WorkspaceId,
                ScopeProjectId = @ScopeProjectId,
                ProjectId = @ProjectId,
                Description = @Description,
                ContextSnapshotId = @ContextSnapshotId,
                GraphSnapshotId = @GraphSnapshotId,
                FindingsSnapshotId = @FindingsSnapshotId,
                GoldenManifestId = @GoldenManifestId,
                DecisionTraceId = @DecisionTraceId,
                ArtifactBundleId = @ArtifactBundleId,
                ArchivedUtc = @ArchivedUtc
            OUTPUT inserted.RowVersionStamp
            WHERE RunId = @RunId
              AND (@RowVersion IS NULL OR RowVersionStamp = @RowVersion);
            """;

        if (connection is not null)
        {
            await ApplyUpdateAsync(connection, transaction, run, sql, ct);

            return;
        }

        await using SqlConnection owned = await connectionFactory.CreateOpenConnectionAsync(ct);
        await ApplyUpdateAsync(owned, null, run, sql, ct);
    }

    private static async Task ApplyUpdateAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        RunRecord run,
        string sql,
        CancellationToken ct)
    {
        byte[]? newStamp = await connection.QuerySingleOrDefaultAsync<byte[]>(
            new CommandDefinition(
                sql,
                new
                {
                    run.RunId,
                    run.TenantId,
                    run.WorkspaceId,
                    ScopeProjectId = run.ScopeProjectId,
                    run.ProjectId,
                    run.Description,
                    run.ContextSnapshotId,
                    run.GraphSnapshotId,
                    run.FindingsSnapshotId,
                    run.GoldenManifestId,
                    run.DecisionTraceId,
                    run.ArtifactBundleId,
                    run.ArchivedUtc,
                    RowVersion = run.RowVersion
                },
                transaction: transaction,
                cancellationToken: ct));

        if (newStamp is null)
        {
            if (run.RowVersion is not null)
            {
                throw new RunConcurrencyConflictException(run.RunId);
            }

            throw new InvalidOperationException($"Run '{run.RunId:D}' was not found for update.");
        }

        run.RowVersion = newStamp;
    }

    /// <inheritdoc />
    public async Task<int> ArchiveRunsCreatedBeforeAsync(DateTimeOffset cutoffUtc, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.Runs
            SET ArchivedUtc = SYSUTCDATETIME()
            WHERE ArchivedUtc IS NULL AND CreatedUtc < @Cutoff;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        return await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Cutoff = cutoffUtc.UtcDateTime }, cancellationToken: ct));
    }
}
