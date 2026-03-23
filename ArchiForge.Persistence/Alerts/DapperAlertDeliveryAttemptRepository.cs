using ArchiForge.Decisioning.Alerts.Delivery;
using ArchiForge.Persistence.Connections;

using Dapper;

namespace ArchiForge.Persistence.Alerts;

/// <summary>Dapper implementation of <see cref="IAlertDeliveryAttemptRepository"/> over <c>dbo.AlertDeliveryAttempts</c>.</summary>
/// <param name="connectionFactory">SQL connection factory (scoped in DI).</param>
public sealed class DapperAlertDeliveryAttemptRepository(ISqlConnectionFactory connectionFactory)
    : IAlertDeliveryAttemptRepository
{
    /// <inheritdoc />
    public async Task CreateAsync(AlertDeliveryAttempt attempt, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.AlertDeliveryAttempts
            (
                AlertDeliveryAttemptId, AlertId, RoutingSubscriptionId,
                TenantId, WorkspaceId, ProjectId,
                AttemptedUtc, Status, ErrorMessage,
                ChannelType, Destination, RetryCount
            )
            VALUES
            (
                @AlertDeliveryAttemptId, @AlertId, @RoutingSubscriptionId,
                @TenantId, @WorkspaceId, @ProjectId,
                @AttemptedUtc, @Status, @ErrorMessage,
                @ChannelType, @Destination, @RetryCount
            );
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, attempt, cancellationToken: ct));
    }

    public async Task UpdateAsync(AlertDeliveryAttempt attempt, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.AlertDeliveryAttempts
            SET
                Status = @Status,
                ErrorMessage = @ErrorMessage,
                RetryCount = @RetryCount
            WHERE AlertDeliveryAttemptId = @AlertDeliveryAttemptId;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, attempt, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AlertDeliveryAttempt>> ListByAlertAsync(
        Guid alertId,
        CancellationToken ct)
    {
        const string sql = """
            SELECT *
            FROM dbo.AlertDeliveryAttempts
            WHERE AlertId = @AlertId
            ORDER BY AttemptedUtc DESC;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        var result = await connection.QueryAsync<AlertDeliveryAttempt>(
            new CommandDefinition(sql, new
            {
                AlertId = alertId
            }, cancellationToken: ct));
        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AlertDeliveryAttempt>> ListBySubscriptionAsync(
        Guid routingSubscriptionId,
        int take,
        CancellationToken ct)
    {
        const string sql = """
            SELECT TOP (@Take) *
            FROM dbo.AlertDeliveryAttempts
            WHERE RoutingSubscriptionId = @RoutingSubscriptionId
            ORDER BY AttemptedUtc DESC;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        var result = await connection.QueryAsync<AlertDeliveryAttempt>(
            new CommandDefinition(
                sql,
                new
                {
                    RoutingSubscriptionId = routingSubscriptionId,
                    Take = take
                },
                cancellationToken: ct));
        return result.ToList();
    }
}
