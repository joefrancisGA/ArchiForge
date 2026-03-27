using System.Data;

using ArchiForge.Contracts.Governance;
using ArchiForge.Data.Infrastructure;

using Dapper;

namespace ArchiForge.Data.Repositories;

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
                ReviewedUtc
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
                @ReviewedUtc
            );
            """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);

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
                item.ReviewedUtc
            },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
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
                ReviewedUtc = @ReviewedUtc
            WHERE ApprovalRequestId = @ApprovalRequestId;
            """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                item.ApprovalRequestId,
                item.Status,
                item.ReviewedBy,
                item.ReviewComment,
                item.ReviewedUtc
            },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
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
                ReviewedUtc
            FROM GovernanceApprovalRequests
            WHERE ApprovalRequestId = @ApprovalRequestId;
            """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        return await connection.QuerySingleOrDefaultAsync<GovernanceApprovalRequest>(new CommandDefinition(
            sql,
            new { ApprovalRequestId = approvalRequestId },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<GovernanceApprovalRequest>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);

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
                ReviewedUtc
            FROM GovernanceApprovalRequests
            WHERE RunId = @RunId
            ORDER BY RequestedUtc DESC
            {SqlPagingSyntax.FirstRowsOnly(connection, 200)};
            """;

        IEnumerable<GovernanceApprovalRequest> rows = await connection.QueryAsync<GovernanceApprovalRequest>(new CommandDefinition(
            sql,
            new { RunId = runId },
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        return [.. rows];
    }
}
