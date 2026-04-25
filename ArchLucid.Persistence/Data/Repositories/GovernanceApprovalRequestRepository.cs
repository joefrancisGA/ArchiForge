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
                item.SlaBreachNotifiedUtc
            },
            cancellationToken: cancellationToken));
    }

    /// <inheritdoc />
    public async Task<bool> TryTransitionFromReviewableAsync(
        string approvalRequestId,
        string newStatus,
        string reviewedBy,
        string? reviewComment,
        DateTime reviewedUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approvalRequestId);
        ArgumentException.ThrowIfNullOrWhiteSpace(newStatus);
        ArgumentException.ThrowIfNullOrWhiteSpace(reviewedBy);

        // Serializable + UPDLOCK: concurrent HTTP approvers each open their own connection; taking the row lock
        // before UPDATE serializes them on this key so only one transition from Draft/Submitted can commit.
        const string lockReviewableRowSql = """
                                            SELECT 1
                                            FROM dbo.GovernanceApprovalRequests WITH (UPDLOCK, ROWLOCK)
                                            WHERE ApprovalRequestId = @ApprovalRequestId
                                              AND (Status = @Draft OR Status = @Submitted);
                                            """;

        const string updateSql = """
                                 UPDATE dbo.GovernanceApprovalRequests
                                 SET
                                     Status = @NewStatus,
                                     ReviewedBy = @ReviewedBy,
                                     ReviewComment = @ReviewComment,
                                     ReviewedUtc = @ReviewedUtc
                                 WHERE ApprovalRequestId = @ApprovalRequestId
                                   AND (Status = @Draft OR Status = @Submitted);
                                 """;

        object transitionParams = new
        {
            ApprovalRequestId = approvalRequestId,
            NewStatus = newStatus,
            ReviewedBy = reviewedBy,
            ReviewComment = reviewComment,
            ReviewedUtc = reviewedUtc,
            GovernanceApprovalStatus.Draft,
            GovernanceApprovalStatus.Submitted
        };

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using IDbTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            int? lockHeld = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    lockReviewableRowSql,
                    new
                    {
                        ApprovalRequestId = approvalRequestId,
                        GovernanceApprovalStatus.Draft,
                        GovernanceApprovalStatus.Submitted
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            if (lockHeld is null)
            {
                transaction.Commit();
                return false;
            }

            int affected = await connection.ExecuteAsync(
                new CommandDefinition(updateSql, transitionParams, transaction, cancellationToken: cancellationToken));

            transaction.Commit();
            return affected == 1;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
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
                item.SlaBreachNotifiedUtc
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

        IEnumerable<GovernanceApprovalRequest> rows = await connection.QueryAsync<GovernanceApprovalRequest>(
            new CommandDefinition(
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
            throw new ArgumentOutOfRangeException(nameof(maxRows));


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

        IEnumerable<GovernanceApprovalRequest> rows = await connection.QueryAsync<GovernanceApprovalRequest>(
            new CommandDefinition(
                sql,
                new { MaxRows = maxRows, GovernanceApprovalStatus.Draft, GovernanceApprovalStatus.Submitted },
                cancellationToken: cancellationToken));

        return [.. rows];
    }

    public async Task<IReadOnlyList<GovernanceApprovalRequest>> GetRecentDecisionsAsync(
        int maxRows = 50,
        CancellationToken cancellationToken = default)
    {
        if (maxRows <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxRows));


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

        IEnumerable<GovernanceApprovalRequest> rows = await connection.QueryAsync<GovernanceApprovalRequest>(
            new CommandDefinition(
                sql,
                new
                {
                    MaxRows = maxRows,
                    GovernanceApprovalStatus.Approved,
                    GovernanceApprovalStatus.Rejected,
                    GovernanceApprovalStatus.Promoted
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

        IEnumerable<GovernanceApprovalRequest> rows = await connection.QueryAsync<GovernanceApprovalRequest>(
            new CommandDefinition(
                sql,
                new { UtcNow = utcNow, GovernanceApprovalStatus.Draft, GovernanceApprovalStatus.Submitted },
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
