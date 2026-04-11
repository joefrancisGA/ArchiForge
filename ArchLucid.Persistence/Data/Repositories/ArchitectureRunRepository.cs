using System.Data;
using System.Diagnostics.CodeAnalysis;

using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Persistence.Data.Infrastructure;

using Dapper;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>
/// Dapper-backed persistence for <see cref="ArchitectureRun"/> entities.
/// </summary>
[Obsolete("RunsAuthorityConvergence write-freeze 2026-09-30: migrate to dbo.Runs. See docs/adr/0012.", error: false)]
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class ArchitectureRunRepository(IDbConnectionFactory connectionFactory) : IArchitectureRunRepository
{
    public async Task CreateAsync(
        ArchitectureRun run,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(run);

        const string sql = """
            INSERT INTO ArchitectureRuns
            (
                RunId,
                RequestId,
                Status,
                CreatedUtc,
                CompletedUtc,
                CurrentManifestVersion,
                ContextSnapshotId,
                GraphSnapshotId,
                ArtifactBundleId
            )
            VALUES
            (
                @RunId,
                @RequestId,
                @Status,
                @CreatedUtc,
                @CompletedUtc,
                @CurrentManifestVersion,
                @ContextSnapshotId,
                @GraphSnapshotId,
                @ArtifactBundleId
            );
            """;

        (IDbConnection conn, bool ownsConnection) =
            await ExternalDbConnection.ResolveAsync(connectionFactory, connection, cancellationToken);

        try
        {
            await conn.ExecuteAsync(new CommandDefinition(
                sql,
                new
                {
                    run.RunId,
                    run.RequestId,
                    Status = run.Status.ToString(),
                    run.CreatedUtc,
                    run.CompletedUtc,
                    run.CurrentManifestVersion,
                    run.ContextSnapshotId,
                    run.GraphSnapshotId,
                    run.ArtifactBundleId
                },
                transaction: transaction,
                cancellationToken: cancellationToken));
        }
        finally
        {
            ExternalDbConnection.DisposeIfOwned(conn, ownsConnection);
        }
    }

    public async Task<ArchitectureRun?> GetByIdAsync(string runId, CancellationToken cancellationToken = default)
    {
        const string runSql = """
            SELECT
                RunId,
                RequestId,
                Status,
                CreatedUtc,
                CompletedUtc,
                CurrentManifestVersion,
                ContextSnapshotId,
                GraphSnapshotId,
                ArtifactBundleId
            FROM ArchitectureRuns
            WHERE RunId = @RunId;
            """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        string taskSql = $"""
            SELECT TaskId
            FROM AgentTasks
            WHERE RunId = @RunId
            ORDER BY CreatedUtc
            {SqlPagingSyntax.FirstRowsOnly(500)};
            """;

        ArchitectureRunRow? row = await connection.QuerySingleOrDefaultAsync<ArchitectureRunRow>(new CommandDefinition(
            runSql,
            new { RunId = runId },
            cancellationToken: cancellationToken));

        if (row is null)
            return null;

        if (!Enum.TryParse(row.Status, true, out ArchitectureRunStatus status))
            throw new InvalidOperationException(
                $"Unrecognised ArchitectureRunStatus '{row.Status}' for run '{row.RunId}'. " +
                "The database row may have been written by a newer version of the application.");

        IEnumerable<string> taskIds = await connection.QueryAsync<string>(new CommandDefinition(
            taskSql,
            new { RunId = runId },
            cancellationToken: cancellationToken));

        return new ArchitectureRun
        {
            RunId = row.RunId,
            RequestId = row.RequestId,
            Status = status,
            CreatedUtc = row.CreatedUtc,
            CompletedUtc = row.CompletedUtc,
            CurrentManifestVersion = row.CurrentManifestVersion,
            ContextSnapshotId = row.ContextSnapshotId,
            GraphSnapshotId = row.GraphSnapshotId,
            ArtifactBundleId = row.ArtifactBundleId,
            TaskIds = [.. taskIds]
        };
    }

    /// <inheritdoc />
    public async Task ApplyDeferredAuthoritySnapshotsAsync(
        string runId,
        string? contextSnapshotId,
        Guid? graphSnapshotId,
        Guid? artifactBundleId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE ArchitectureRuns
            SET
                ContextSnapshotId = @ContextSnapshotId,
                GraphSnapshotId = @GraphSnapshotId,
                ArtifactBundleId = @ArtifactBundleId,
                Status = @Status
            WHERE RunId = @RunId
              AND Status = @ExpectedStatus;
            """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        int rowsAffected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                RunId = runId,
                ContextSnapshotId = contextSnapshotId,
                GraphSnapshotId = graphSnapshotId,
                ArtifactBundleId = artifactBundleId,
                Status = ArchitectureRunStatus.TasksGenerated.ToString(),
                ExpectedStatus = ArchitectureRunStatus.Created.ToString(),
            },
            cancellationToken: cancellationToken));

        if (rowsAffected == 0)
        {
            throw new InvalidOperationException(
                $"Run '{runId}' could not apply deferred authority snapshots: expected status '{ArchitectureRunStatus.Created}'.");
        }
    }

    public async Task UpdateStatusAsync(
        string runId,
        ArchitectureRunStatus status,
        string? currentManifestVersion = null,
        DateTime? completedUtc = null,
        CancellationToken cancellationToken = default,
        ArchitectureRunStatus? expectedStatus = null,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        // COALESCE preserves the existing manifest version when the caller passes null,
        // preventing a stale null snapshot from overwriting a version already written
        // by a concurrent flow.
        string sql = expectedStatus.HasValue
            ? """
              UPDATE ArchitectureRuns
              SET
                  Status = @Status,
                  CurrentManifestVersion = COALESCE(@CurrentManifestVersion, CurrentManifestVersion),
                  CompletedUtc = @CompletedUtc
              WHERE RunId = @RunId
                AND Status = @ExpectedStatus;
              """
            : """
              UPDATE ArchitectureRuns
              SET
                  Status = @Status,
                  CurrentManifestVersion = COALESCE(@CurrentManifestVersion, CurrentManifestVersion),
                  CompletedUtc = @CompletedUtc
              WHERE RunId = @RunId;
              """;

        (IDbConnection conn, bool ownsConnection) =
            await ExternalDbConnection.ResolveAsync(connectionFactory, connection, cancellationToken);

        try
        {
            int rowsAffected = await conn.ExecuteAsync(new CommandDefinition(
                sql,
                new
                {
                    RunId = runId,
                    Status = status.ToString(),
                    CurrentManifestVersion = currentManifestVersion,
                    CompletedUtc = completedUtc,
                    ExpectedStatus = expectedStatus?.ToString()
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            if (expectedStatus.HasValue && rowsAffected == 0)
            {
                throw new InvalidOperationException(
                    $"Run '{runId}' could not be transitioned to '{status}': " +
                    $"expected status '{expectedStatus}' but the run has already been moved by a concurrent operation.");
            }
        }
        finally
        {
            ExternalDbConnection.DisposeIfOwned(conn, ownsConnection);
        }
    }

    private sealed class ArchitectureRunRow
    {
        public string RunId { get; init; } = string.Empty;
        public string RequestId { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public DateTime CreatedUtc { get; init; }
        public DateTime? CompletedUtc { get; init; }
        public string? CurrentManifestVersion { get; init; }
        public string? ContextSnapshotId { get; init; }
        public Guid? GraphSnapshotId { get; init; }
        public Guid? ArtifactBundleId { get; init; }
    }

    public async Task<IReadOnlyList<ArchitectureRunListItem>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        string sql = $"""
            SELECT
                r.RunId,
                r.RequestId,
                r.Status,
                r.CreatedUtc,
                r.CompletedUtc,
                r.CurrentManifestVersion,
                req.SystemName
            FROM ArchitectureRuns r
            INNER JOIN ArchitectureRequests req
                ON r.RequestId = req.RequestId
            ORDER BY r.CreatedUtc DESC, r.RunId DESC
            {SqlPagingSyntax.FirstRowsOnly(200)};
            """;

        IEnumerable<ArchitectureRunListItemRow> rows = await connection.QueryAsync<ArchitectureRunListItemRow>(new CommandDefinition(
            sql,
            cancellationToken: cancellationToken));

        List<ArchitectureRunListItem> items = [];
        foreach (ArchitectureRunListItemRow row in rows)
        {
            if (!Enum.TryParse<ArchitectureRunStatus>(row.Status, true, out _))
                throw new InvalidOperationException(
                    $"Unrecognised ArchitectureRunStatus '{row.Status}' for run '{row.RunId}'. " +
                    "The database row may have been written by a newer version of the application.");

            items.Add(new ArchitectureRunListItem
            {
                RunId = row.RunId,
                RequestId = row.RequestId,
                Status = row.Status,
                CreatedUtc = row.CreatedUtc,
                CompletedUtc = row.CompletedUtc,
                CurrentManifestVersion = row.CurrentManifestVersion,
                SystemName = row.SystemName
            });
        }
        return items;
    }

    private sealed class ArchitectureRunListItemRow
    {
        public string RunId { get; init; } = string.Empty;
        public string RequestId { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public DateTime CreatedUtc { get; init; }
        public DateTime? CompletedUtc { get; init; }
        public string? CurrentManifestVersion { get; init; }
        public string SystemName { get; init; } = string.Empty;
    }
}
