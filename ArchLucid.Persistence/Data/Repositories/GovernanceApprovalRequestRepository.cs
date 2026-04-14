using System.Data;
using System.Diagnostics.CodeAnalysis;

using ArchLucid.Contracts.Governance;
using ArchLucid.Persistence.Data.Infrastructure;

using Dapper;

namespace ArchLucid.Persistence.Data.Repositories;

[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class GovernanceApprovalRequestRepository(IDbConnectionFactory connectionFactory)
    : IGovernanceApprovalRequestRepository
{
    public async Task CreateAsync(GovernanceApprovalRequest item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        const string sql = """
            INSERT INTO GovernanceApprovalRequests
            (
                ApprovalRequestId,
                RunId,
                ManifestVersion,
                SourceEnvironment,
                TargetEnvironment,
                Status,
                RequestedBy,
                ReviewedBy,
                RequestComment,
                ReviewComment,
                RequestedUtc,
                ReviewedUtc,
                SlaDeadlineUtc,
                SlaBreachNotifiedUtc
            )
            VALUES
            (
                @ApprovalRequestId,
                @RunId,
                @ManifestVersion,
                @SourceEnvironment,
                @TargetEnvironment,
                @Status,
                @RequestedBy,
                @ReviewedBy,
                @RequestComment,
                @ReviewComment,
                @RequestedUtc,
                @ReviewedUtc,
                @SlaDeadlineUtc,
                @SlaBreachNotifiedUtc
            );
            """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                item.ApprovalRequestId,
                item.RunId,
                item.ManifestVersion,
                item.SourceEnvironment,
                item.TargetEnvironment,
                item.Status,
                item.RequestedBy,
                item.ReviewedBy,
                item.RequestComment,
                item.ReviewComment,
                item.RequestedUtc,
                item.ReviewedUtc,
                item.SlaDeadlineUtc,
                item.SlaBreachNotifiedUtc,
            },
            cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(GovernanceApprovalRequest item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        const string sql = """
            UPDATE GovernanceApprovalRequests
            SET
                Status = @Status,
                ReviewedBy = @ReviewedBy,
                ReviewComment = @ReviewComment,
                ReviewedUtc = @ReviewedUtc,
                SlaDeadlineUtc = @SlaDeadlineUtc,
                SlaBreachNotifiedUtc = @SlaBreachNotifiedUtc
            WHERE ApprovalRequestId = @ApprovalRequestId;
            """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                item.ApprovalRequestId,
                item.Status,
                item.ReviewedBy,
                item.ReviewComment,
                item.ReviewedUtc,
                item.SlaDeadlineUtc,
                item.SlaBreachNotifiedUtc,
            },
            cancellationToken: cancellationToken));
    }

    public async Task<GovernanceApprovalRequest?> GetByIdAsync(
        string approvalRequestId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                ApprovalRequestId,
                RunId,
                ManifestVersion,
                SourceEnvironment,
                TargetEnvironment,
                Status,
                RequestedBy,
                ReviewedBy,
                RequestComment,
                ReviewComment,
                RequestedUtc,
                ReviewedUtc,
                SlaDeadlineUtc,
                SlaBreachNotifiedUtc
            FROM GovernanceApprovalRequests
            WHERE ApprovalRequestId = @ApprovalRequestId;
            """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<GovernanceApprovalRequest>(new CommandDefinition(
            sql,
            new { ApprovalRequestId = approvalRequestId },
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<GovernanceApprovalRequest>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        string sql = $"""
            SELECT
                ApprovalRequestId,
                RunId,
                ManifestVersion,
                SourceEnvironment,
                TargetEnvironment,
                Status,
                RequestedBy,
                ReviewedBy,
                RequestComment,
                ReviewComment,
                RequestedUtc,
                ReviewedUtc,
                SlaDeadlineUtc,
                SlaBreachNotifiedUtc
            FROM GovernanceApprovalRequests
            WHERE RunId = @RunId
            ORDER BY RequestedUtc DESC
            {SqlPagingSyntax.FirstRowsOnly(200)};
            """;

        IEnumerable<GovernanceApprovalRequest> rows = await connection.QueryAsync<GovernanceApprovalRequest>(new CommandDefinition(
            sql,
            new { RunId = runId },
            cancellationToken: cancellationToken));

        return [.. rows];
    }

    public async Task<IReadOnlyList<GovernanceApprovalRequest>> GetPendingAsync(
        int maxRows = 50,
        CancellationToken cancellationToken = default)
    {
        if (maxRows <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRows));
        }

        const string sql = """
            SELECT TOP (@MaxRows)
                ApprovalRequestId,
                RunId,
                ManifestVersion,
                SourceEnvironment,
                TargetEnvironment,
                Status,
                RequestedBy,
                ReviewedBy,
                RequestComment,
                ReviewComment,
                RequestedUtc,
                ReviewedUtc,
                SlaDeadlineUtc,
                SlaBreachNotifiedUtc
            FROM GovernanceApprovalRequests
            WHERE Status IN (@Draft, @Submitted)
            ORDER BY RequestedUtc DESC;
            """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        IEnumerable<GovernanceApprovalRequest> rows = await connection.QueryAsync<GovernanceApprovalRequest>(new CommandDefinition(
            sql,
            new
            {
                MaxRows = maxRows,
                Draft = GovernanceApprovalStatus.Draft,
                Submitted = GovernanceApprovalStatus.Submitted,
            },
            cancellationToken: cancellationToken));

        return [.. rows];
    }

    public async Task<IReadOnlyList<GovernanceApprovalRequest>> GetRecentDecisionsAsync(
        int maxRows = 50,
        CancellationToken cancellationToken = default)
    {
        if (maxRows <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRows));
        }

        const string sql = """
            SELECT TOP (@MaxRows)
                ApprovalRequestId,
                RunId,
                ManifestVersion,
                SourceEnvironment,
                TargetEnvironment,
                Status,
                RequestedBy,
                ReviewedBy,
                RequestComment,
                ReviewComment,
                RequestedUtc,
                ReviewedUtc,
                SlaDeadlineUtc,
                SlaBreachNotifiedUtc
            FROM GovernanceApprovalRequests
            WHERE Status IN (@Approved, @Rejected, @Promoted)
              AND ReviewedUtc IS NOT NULL
            ORDER BY ReviewedUtc DESC;
            """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        IEnumerable<GovernanceApprovalRequest> rows = await connection.QueryAsync<GovernanceApprovalRequest>(new CommandDefinition(
            sql,
            new
            {
                MaxRows = maxRows,
                Approved = GovernanceApprovalStatus.Approved,
                Rejected = GovernanceApprovalStatus.Rejected,
                Promoted = GovernanceApprovalStatus.Promoted,
            },
            cancellationToken: cancellationToken));

        return [.. rows];
    }

    public async Task<IReadOnlyList<GovernanceApprovalRequest>> GetPendingSlaBreachedAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP 200
                ApprovalRequestId,
                RunId,
                ManifestVersion,
                SourceEnvironment,
                TargetEnvironment,
                Status,
                RequestedBy,
                ReviewedBy,
                RequestComment,
                ReviewComment,
                RequestedUtc,
                ReviewedUtc,
                SlaDeadlineUtc,
                SlaBreachNotifiedUtc
            FROM GovernanceApprovalRequests
            WHERE Status IN (@Draft, @Submitted)
              AND SlaDeadlineUtc IS NOT NULL
              AND SlaDeadlineUtc <= @UtcNow
              AND SlaBreachNotifiedUtc IS NULL
            ORDER BY SlaDeadlineUtc ASC;
            """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        IEnumerable<GovernanceApprovalRequest> rows = await connection.QueryAsync<GovernanceApprovalRequest>(new CommandDefinition(
            sql,
            new
            {
                UtcNow = utcNow,
                Draft = GovernanceApprovalStatus.Draft,
                Submitted = GovernanceApprovalStatus.Submitted,
            },
            cancellationToken: cancellationToken));

        return [.. rows];
    }

    public async Task PatchSlaBreachNotifiedAsync(
        string approvalRequestId,
        DateTime slaBreachNotifiedUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approvalRequestId);

        const string sql = """
            UPDATE GovernanceApprovalRequests
            SET SlaBreachNotifiedUtc = @SlaBreachNotifiedUtc
            WHERE ApprovalRequestId = @ApprovalRequestId;
            """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { ApprovalRequestId = approvalRequestId, SlaBreachNotifiedUtc = slaBreachNotifiedUtc },
            cancellationToken: cancellationToken));
    }
}
