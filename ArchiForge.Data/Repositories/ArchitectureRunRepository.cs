using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Data.Infrastructure;
using Dapper;

namespace ArchiForge.Data.Repositories;

public sealed class ArchitectureRunRepository : IArchitectureRunRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ArchitectureRunRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

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
                ContextSnapshotId
            )
            VALUES
            (
                @RunId,
                @RequestId,
                @Status,
                @CreatedUtc,
                @CompletedUtc,
                @CurrentManifestVersion,
                @ContextSnapshotId
            );
            """;

        using var connection = _connectionFactory.CreateConnection();

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
                run.ContextSnapshotId
            },
            cancellationToken: cancellationToken));
    }

    public async Task<ArchitectureRun?> GetByIdAsync(string runId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                RunId,
                RequestId,
                Status,
                CreatedUtc,
                CompletedUtc,
                CurrentManifestVersion,
                ContextSnapshotId
            FROM ArchitectureRuns
            WHERE RunId = @RunId;
            """;

        using var connection = _connectionFactory.CreateConnection();

        var row = await connection.QuerySingleOrDefaultAsync<ArchitectureRunRow>(new CommandDefinition(
            sql,
            new { RunId = runId },
            cancellationToken: cancellationToken));

        if (row is null)
            return null;

        return new ArchitectureRun
        {
            RunId = row.RunId,
            RequestId = row.RequestId,
            Status = Enum.TryParse<ArchitectureRunStatus>(row.Status, true, out var status)
                ? status
                : ArchitectureRunStatus.Created,
            CreatedUtc = row.CreatedUtc,
            CompletedUtc = row.CompletedUtc,
            CurrentManifestVersion = row.CurrentManifestVersion,
            ContextSnapshotId = row.ContextSnapshotId,
            TaskIds = []
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

        using var connection = _connectionFactory.CreateConnection();

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
        public string RunId { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedUtc { get; set; }
        public DateTime? CompletedUtc { get; set; }
        public string? CurrentManifestVersion { get; set; }
        public string? ContextSnapshotId { get; set; }
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

        using var connection = _connectionFactory.CreateConnection();

        var rows = await connection.QueryAsync<ArchitectureRunListItemRow>(new CommandDefinition(
            sql,
            cancellationToken: cancellationToken));

        return rows
            .Select(row => new ArchitectureRunListItem
            {
                RunId = row.RunId,
                RequestId = row.RequestId,
                Status = row.Status,
                CreatedUtc = row.CreatedUtc,
                CompletedUtc = row.CompletedUtc,
                CurrentManifestVersion = row.CurrentManifestVersion,
                SystemName = row.SystemName
            })
            .ToList();
    }

    private sealed class ArchitectureRunListItemRow
    {
        public string RunId { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedUtc { get; set; }
        public DateTime? CompletedUtc { get; set; }
        public string? CurrentManifestVersion { get; set; }
        public string SystemName { get; set; } = string.Empty;
    }
}