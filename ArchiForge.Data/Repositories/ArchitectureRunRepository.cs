using System.Data;

using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Data.Infrastructure;

using Dapper;

namespace ArchiForge.Data.Repositories;

/// <summary>
/// Dapper-backed persistence for <see cref="ArchitectureRun"/> entities.
/// </summary>
public sealed class ArchitectureRunRepository(IDbConnectionFactory connectionFactory) : IArchitectureRunRepository
{
    public async Task CreateAsync(ArchitectureRun run, CancellationToken cancellationToken = default)
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

        using IDbConnection connection = connectionFactory.CreateConnection();

        await connection.ExecuteAsync(new CommandDefinition(
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
            cancellationToken: cancellationToken));
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

        const string taskSql = """
            SELECT TaskId
            FROM AgentTasks
            WHERE RunId = @RunId
            ORDER BY CreatedUtc
            LIMIT 500;
            """;

        using IDbConnection connection = connectionFactory.CreateConnection();

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

    public async Task UpdateStatusAsync(
        string runId,
        ArchitectureRunStatus status,
        string? currentManifestVersion = null,
        DateTime? completedUtc = null,
        CancellationToken cancellationToken = default,
        ArchitectureRunStatus? expectedStatus = null)
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

        using IDbConnection connection = connectionFactory.CreateConnection();

        int rowsAffected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                RunId = runId,
                Status = status.ToString(),
                CurrentManifestVersion = currentManifestVersion,
                CompletedUtc = completedUtc,
                ExpectedStatus = expectedStatus?.ToString()
            },
            cancellationToken: cancellationToken));

        if (expectedStatus.HasValue && rowsAffected == 0)
        {
            throw new InvalidOperationException(
                $"Run '{runId}' could not be transitioned to '{status}': " +
                $"expected status '{expectedStatus}' but the run has already been moved by a concurrent operation.");
        }
    }

    private sealed class ArchitectureRunRow
    {
        public string RunId { get; init; } = string.Empty;
        public string RequestId { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public DateTime CreatedUtc
        {
            get; init;
        }
        public DateTime? CompletedUtc
        {
            get; init;
        }
        public string? CurrentManifestVersion
        {
            get; init;
        }
        public string? ContextSnapshotId
        {
            get; init;
        }
        public Guid? GraphSnapshotId
        {
            get; init;
        }
        public Guid? ArtifactBundleId
        {
            get; init;
        }
    }

    public async Task<IReadOnlyList<ArchitectureRunListItem>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
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
            LIMIT 200;
            """;

        using IDbConnection connection = connectionFactory.CreateConnection();

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
        public DateTime CreatedUtc
        {
            get; init;
        }
        public DateTime? CompletedUtc
        {
            get; init;
        }
        public string? CurrentManifestVersion
        {
            get; init;
        }
        public string SystemName { get; init; } = string.Empty;
    }
}
