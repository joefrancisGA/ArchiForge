using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Data.Infrastructure;

using Dapper;

namespace ArchiForge.Data.Repositories;

public sealed class ArchitectureRunRepository(IDbConnectionFactory connectionFactory) : IArchitectureRunRepository
{
    public async Task CreateAsync(ArchitectureRun run, CancellationToken cancellationToken = default)
    {
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

        using var connection = connectionFactory.CreateConnection();

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
            ORDER BY CreatedUtc;
            """;

        using var connection = connectionFactory.CreateConnection();

        var row = await connection.QuerySingleOrDefaultAsync<ArchitectureRunRow>(new CommandDefinition(
            runSql,
            new { RunId = runId },
            cancellationToken: cancellationToken));

        if (row is null)
            return null;

        if (!Enum.TryParse<ArchitectureRunStatus>(row.Status, true, out var status))
            throw new InvalidOperationException(
                $"Unrecognised ArchitectureRunStatus '{row.Status}' for run '{row.RunId}'. " +
                "The database row may have been written by a newer version of the application.");

        var taskIds = await connection.QueryAsync<string>(new CommandDefinition(
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
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE ArchitectureRuns
            SET
                Status = @Status,
                CurrentManifestVersion = @CurrentManifestVersion,
                CompletedUtc = @CompletedUtc
            WHERE RunId = @RunId;
            """;

        using var connection = connectionFactory.CreateConnection();

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                RunId = runId,
                Status = status.ToString(),
                CurrentManifestVersion = currentManifestVersion,
                CompletedUtc = completedUtc
            },
            cancellationToken: cancellationToken));
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
            ORDER BY r.CreatedUtc DESC, r.RunId DESC;
            """;

        using var connection = connectionFactory.CreateConnection();

        var rows = await connection.QueryAsync<ArchitectureRunListItemRow>(new CommandDefinition(
            sql,
            cancellationToken: cancellationToken));

        var items = new List<ArchitectureRunListItem>();
        foreach (var row in rows)
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
