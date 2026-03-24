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

        using var connection = connectionFactory.CreateConnection();

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
                ReviewedUtc = @ReviewedUtc
            WHERE ApprovalRequestId = @ApprovalRequestId;
            """;

        using var connection = connectionFactory.CreateConnection();

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
                ReviewedUtc
            FROM GovernanceApprovalRequests
            WHERE ApprovalRequestId = @ApprovalRequestId;
            """;

        using var connection = connectionFactory.CreateConnection();

        return await connection.QuerySingleOrDefaultAsync<GovernanceApprovalRequest>(new CommandDefinition(
            sql,
            new { ApprovalRequestId = approvalRequestId },
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<GovernanceApprovalRequest>> GetByRunIdAsync(
        string runId,
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
            WHERE RunId = @RunId
            ORDER BY RequestedUtc DESC
            LIMIT 200;
            """;

        using var connection = connectionFactory.CreateConnection();

        var rows = await connection.QueryAsync<GovernanceApprovalRequest>(new CommandDefinition(
            sql,
            new { RunId = runId },
            cancellationToken: cancellationToken));

        return [.. rows];
    }
}
