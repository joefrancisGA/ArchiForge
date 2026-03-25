using ArchiForge.Decisioning.Advisory.Delivery;
using ArchiForge.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Advisory;

/// <summary>Dapper implementation of <see cref="IDigestDeliveryAttemptRepository"/> over <c>dbo.DigestDeliveryAttempts</c>.</summary>
/// <param name="connectionFactory">SQL connection factory (scoped in DI).</param>
public sealed class DapperDigestDeliveryAttemptRepository(ISqlConnectionFactory connectionFactory)
    : IDigestDeliveryAttemptRepository
{
    /// <inheritdoc />
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

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, attempt, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task UpdateAsync(DigestDeliveryAttempt attempt, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.DigestDeliveryAttempts
            SET
                Status = @Status,
                ErrorMessage = @ErrorMessage
            WHERE AttemptId = @AttemptId;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, attempt, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<DigestDeliveryAttempt>> ListByDigestAsync(
        Guid digestId,
        CancellationToken ct)
    {
        const string sql = """
            SELECT TOP 200
                AttemptId, DigestId, SubscriptionId,
                TenantId, WorkspaceId, ProjectId,
                AttemptedUtc, Status, ErrorMessage,
                ChannelType, Destination
            FROM dbo.DigestDeliveryAttempts
            WHERE DigestId = @DigestId
            ORDER BY AttemptedUtc DESC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        IEnumerable<DigestDeliveryAttempt> result = await connection.QueryAsync<DigestDeliveryAttempt>(
            new CommandDefinition(sql, new
            {
                DigestId = digestId
            }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DigestDeliveryAttempt>> ListBySubscriptionAsync(
        Guid subscriptionId,
        int take,
        CancellationToken ct)
    {
        take = Math.Clamp(take, 1, 200);
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

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        IEnumerable<DigestDeliveryAttempt> result = await connection.QueryAsync<DigestDeliveryAttempt>(
            new CommandDefinition(
                sql,
                new
                {
                    SubscriptionId = subscriptionId,
                    Take = take
                },
                cancellationToken: ct));

        return result.ToList();
    }
}
