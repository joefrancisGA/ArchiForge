using ArchiForge.Decisioning.Advisory.Delivery;
using ArchiForge.Persistence.Connections;
using Dapper;

namespace ArchiForge.Persistence.Advisory;

public sealed class DapperDigestDeliveryAttemptRepository(ISqlConnectionFactory connectionFactory)
    : IDigestDeliveryAttemptRepository
{
    public async Task CreateAsync(DigestDeliveryAttempt attempt, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.DigestDeliveryAttempts
            (
                AttemptId, DigestId, SubscriptionId,
                TenantId, WorkspaceId, ProjectId,
                AttemptedUtc, Status, ErrorMessage,
                ChannelType, Destination
            )
            VALUES
            (
                @AttemptId, @DigestId, @SubscriptionId,
                @TenantId, @WorkspaceId, @ProjectId,
                @AttemptedUtc, @Status, @ErrorMessage,
                @ChannelType, @Destination
            );
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, attempt, cancellationToken: ct));
    }

    public async Task UpdateAsync(DigestDeliveryAttempt attempt, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.DigestDeliveryAttempts
            SET
                Status = @Status,
                ErrorMessage = @ErrorMessage
            WHERE AttemptId = @AttemptId;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, attempt, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<DigestDeliveryAttempt>> ListByDigestAsync(
        Guid digestId,
        CancellationToken ct)
    {
        const string sql = """
            SELECT
                AttemptId, DigestId, SubscriptionId,
                TenantId, WorkspaceId, ProjectId,
                AttemptedUtc, Status, ErrorMessage,
                ChannelType, Destination
            FROM dbo.DigestDeliveryAttempts
            WHERE DigestId = @DigestId
            ORDER BY AttemptedUtc DESC;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        var result = await connection.QueryAsync<DigestDeliveryAttempt>(
            new CommandDefinition(sql, new { DigestId = digestId }, cancellationToken: ct));

        return result.ToList();
    }

    public async Task<IReadOnlyList<DigestDeliveryAttempt>> ListBySubscriptionAsync(
        Guid subscriptionId,
        int take,
        CancellationToken ct)
    {
        const string sql = """
            SELECT TOP (@Take)
                AttemptId, DigestId, SubscriptionId,
                TenantId, WorkspaceId, ProjectId,
                AttemptedUtc, Status, ErrorMessage,
                ChannelType, Destination
            FROM dbo.DigestDeliveryAttempts
            WHERE SubscriptionId = @SubscriptionId
            ORDER BY AttemptedUtc DESC;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        var result = await connection.QueryAsync<DigestDeliveryAttempt>(
            new CommandDefinition(
                sql,
                new { SubscriptionId = subscriptionId, Take = take },
                cancellationToken: ct));

        return result.ToList();
    }
}
