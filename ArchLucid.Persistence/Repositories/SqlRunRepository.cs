using System.Data;
using System.Diagnostics.CodeAnalysis;

using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Repositories;

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
                GoldenManifestId, DecisionTraceId, ArtifactBundleId, ArchivedUtc,
                ArchitectureRequestId, LegacyRunStatus, CompletedUtc, CurrentManifestVersion, OtelTraceId
            )
            OUTPUT inserted.RowVersionStamp
            VALUES
            (
                @RunId, @TenantId, @WorkspaceId, @ScopeProjectId, @ProjectId, @Description, @CreatedUtc,
                @ContextSnapshotId, @GraphSnapshotId, @FindingsSnapshotId,
                @GoldenManifestId, @DecisionTraceId, @ArtifactBundleId, @ArchivedUtc,
                @ArchitectureRequestId, @LegacyRunStatus, @CompletedUtc, @CurrentManifestVersion, @OtelTraceId
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
                ArchitectureRequestId, LegacyRunStatus, CompletedUtc, CurrentManifestVersion, OtelTraceId,
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

        // NOLOCK: dashboard-grade list on hot-write table; tolerates replica-style staleness (see ListRecentInScopeAsync).
        const string sql = """
            SELECT TOP (@Take)
                RunId, TenantId, WorkspaceId, ScopeProjectId, ProjectId, Description, CreatedUtc,
                ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId,
                GoldenManifestId, DecisionTraceId, ArtifactBundleId, ArchivedUtc,
                ArchitectureRequestId, LegacyRunStatus, CompletedUtc, CurrentManifestVersion, OtelTraceId
            FROM dbo.Runs WITH (NOLOCK)
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

    public async Task<(IReadOnlyList<RunRecord> Items, int TotalCount)> ListByProjectPagedAsync(
        ScopeContext scope,
        string projectId,
        int skip,
        int take,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(scope);

        int safeTake = Math.Clamp(take <= 0 ? 20 : take, 1, 200);
        int safeSkip = Math.Max(skip, 0);

        // NOLOCK: paged list + count for dashboards; same staleness trade-off as ListRecentInScopeAsync.
        const string countSql = """
            SELECT COUNT(1)
            FROM dbo.Runs WITH (NOLOCK)
            WHERE ProjectId = @ProjectSlug
              AND TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ScopeProjectId = @ScopeProjectId
              AND ArchivedUtc IS NULL;
            """;

        const string pageSql = """
            SELECT
                RunId, TenantId, WorkspaceId, ScopeProjectId, ProjectId, Description, CreatedUtc,
                ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId,
                GoldenManifestId, DecisionTraceId, ArtifactBundleId, ArchivedUtc,
                ArchitectureRequestId, LegacyRunStatus, CompletedUtc, CurrentManifestVersion, OtelTraceId
            FROM dbo.Runs WITH (NOLOCK)
            WHERE ProjectId = @ProjectSlug
              AND TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ScopeProjectId = @ScopeProjectId
              AND ArchivedUtc IS NULL
            ORDER BY CreatedUtc DESC
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
            """;

        object scopeParams = new
        {
            ProjectSlug = projectId,
            scope.TenantId,
            scope.WorkspaceId,
            ScopeProjectId = scope.ProjectId
        };

        object pageParams = new
        {
            ProjectSlug = projectId,
            scope.TenantId,
            scope.WorkspaceId,
            ScopeProjectId = scope.ProjectId,
            Skip = safeSkip,
            Take = safeTake
        };

        await using SqlConnection connection = await authorityRunListConnectionFactory.CreateOpenConnectionAsync(ct);
        int total = await connection.QuerySingleAsync<int>(new CommandDefinition(countSql, scopeParams, cancellationToken: ct));

        IEnumerable<RunRecord> rows = await connection.QueryAsync<RunRecord>(
            new CommandDefinition(pageSql, pageParams, cancellationToken: ct));

        return (rows.ToList(), total);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RunRecord>> ListRecentInScopeAsync(ScopeContext scope, int take, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(scope);

        // NOLOCK: dashboard / picker list; same tolerance as read-replica staleness (see LOAD_TEST_BASELINE.md). Avoids S-lock blocking behind writers on dbo.Runs.
        const string sql = """
            SELECT TOP (@Take)
                RunId, TenantId, WorkspaceId, ScopeProjectId, ProjectId, Description, CreatedUtc,
                ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId,
                GoldenManifestId, DecisionTraceId, ArtifactBundleId, ArchivedUtc,
                ArchitectureRequestId, LegacyRunStatus, CompletedUtc, CurrentManifestVersion, OtelTraceId
            FROM dbo.Runs WITH (NOLOCK)
            WHERE TenantId = @TenantId
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
                    scope.TenantId,
                    scope.WorkspaceId,
                    ScopeProjectId = scope.ProjectId,
                    Take = Math.Clamp(take <= 0 ? 200 : take, 1, 200)
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
                ArchivedUtc = @ArchivedUtc,
                ArchitectureRequestId = @ArchitectureRequestId,
                LegacyRunStatus = @LegacyRunStatus,
                CompletedUtc = @CompletedUtc,
                CurrentManifestVersion = @CurrentManifestVersion
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
                    run.ArchitectureRequestId,
                    run.LegacyRunStatus,
                    run.CompletedUtc,
                    run.CurrentManifestVersion,
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
    public async Task<RunArchiveBatchResult> ArchiveRunsCreatedBeforeAsync(DateTimeOffset cutoffUtc, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.Runs
            SET ArchivedUtc = SYSUTCDATETIME()
            OUTPUT inserted.RunId, inserted.TenantId, inserted.WorkspaceId, inserted.ScopeProjectId
            WHERE ArchivedUtc IS NULL AND CreatedUtc < @Cutoff;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        List<ArchivedRunScopeRow> rows = (await connection.QueryAsync<ArchivedRunScopeRow>(
                new CommandDefinition(sql, new { Cutoff = cutoffUtc.UtcDateTime }, cancellationToken: ct)))
            .ToList();

        return new RunArchiveBatchResult
        {
            UpdatedCount = rows.Count,
            ArchivedRuns = rows
        };
    }
}
