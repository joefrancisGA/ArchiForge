using System.Data;

using ArchiForge.Contracts.Governance;
using ArchiForge.Data.Infrastructure;

using Dapper;

namespace ArchiForge.Data.Repositories;

public sealed class GovernancePromotionRecordRepository(IDbConnectionFactory connectionFactory)
    : IGovernancePromotionRecordRepository
{
    public async Task CreateAsync(GovernancePromotionRecord item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        const string sql = """
            INSERT INTO GovernancePromotionRecords
            (
                PromotionRecordId,
                RunId,
                ManifestVersion,
                SourceEnvironment,
                TargetEnvironment,
                PromotedBy,
                PromotedUtc,
                ApprovalRequestId,
                Notes
            )
            VALUES
            (
                @PromotionRecordId,
                @RunId,
                @ManifestVersion,
                @SourceEnvironment,
                @TargetEnvironment,
                @PromotedBy,
                @PromotedUtc,
                @ApprovalRequestId,
                @Notes
            );
            """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                item.PromotionRecordId,
                item.RunId,
                item.ManifestVersion,
                item.SourceEnvironment,
                item.TargetEnvironment,
                item.PromotedBy,
                item.PromotedUtc,
                item.ApprovalRequestId,
                item.Notes
            },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<GovernancePromotionRecord>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        string sql = $"""
            SELECT
                PromotionRecordId,
                RunId,
                ManifestVersion,
                SourceEnvironment,
                TargetEnvironment,
                PromotedBy,
                PromotedUtc,
                ApprovalRequestId,
                Notes
            FROM GovernancePromotionRecords
            WHERE RunId = @RunId
            ORDER BY PromotedUtc DESC
            {SqlPagingSyntax.FirstRowsOnly(connection, 200)};
            """;

        IEnumerable<GovernancePromotionRecord> rows = await connection.QueryAsync<GovernancePromotionRecord>(new CommandDefinition(
            sql,
            new { RunId = runId },
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        return [.. rows];
    }
}
