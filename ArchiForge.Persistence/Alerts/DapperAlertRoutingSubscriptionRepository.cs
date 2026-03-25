using ArchiForge.Decisioning.Alerts.Delivery;
using ArchiForge.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Alerts;

/// <summary>Dapper implementation of <see cref="IAlertRoutingSubscriptionRepository"/> over <c>dbo.AlertRoutingSubscriptions</c>.</summary>
/// <param name="connectionFactory">SQL connection factory (scoped in DI).</param>
public sealed class DapperAlertRoutingSubscriptionRepository(ISqlConnectionFactory connectionFactory)
    : IAlertRoutingSubscriptionRepository
{
    /// <inheritdoc />
    public async Task CreateAsync(AlertRoutingSubscription subscription, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.AlertRoutingSubscriptions
            (
                RoutingSubscriptionId, TenantId, WorkspaceId, ProjectId,
                Name, ChannelType, Destination, MinimumSeverity, IsEnabled,
                CreatedUtc, LastDeliveredUtc, MetadataJson
            )
            VALUES
            (
                @RoutingSubscriptionId, @TenantId, @WorkspaceId, @ProjectId,
                @Name, @ChannelType, @Destination, @MinimumSeverity, @IsEnabled,
                @CreatedUtc, @LastDeliveredUtc, @MetadataJson
            );
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, subscription, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task UpdateAsync(AlertRoutingSubscription subscription, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.AlertRoutingSubscriptions
            SET
                Name = @Name,
                ChannelType = @ChannelType,
                Destination = @Destination,
                MinimumSeverity = @MinimumSeverity,
                IsEnabled = @IsEnabled,
                LastDeliveredUtc = @LastDeliveredUtc,
                MetadataJson = @MetadataJson
            WHERE RoutingSubscriptionId = @RoutingSubscriptionId;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, subscription, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<AlertRoutingSubscription?> GetByIdAsync(Guid routingSubscriptionId, CancellationToken ct)
    {
        const string sql = """
            SELECT *
            FROM dbo.AlertRoutingSubscriptions
            WHERE RoutingSubscriptionId = @RoutingSubscriptionId;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        return await connection.QueryFirstOrDefaultAsync<AlertRoutingSubscription>(
            new CommandDefinition(sql, new
            {
                RoutingSubscriptionId = routingSubscriptionId
            }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<AlertRoutingSubscription>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        const string sql = """
            SELECT TOP 200 *
            FROM dbo.AlertRoutingSubscriptions
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
            ORDER BY CreatedUtc DESC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        IEnumerable<AlertRoutingSubscription> rows = await connection.QueryAsync<AlertRoutingSubscription>(
            new CommandDefinition(
                sql,
                new
                {
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId
                },
                cancellationToken: ct));
        return rows.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AlertRoutingSubscription>> ListEnabledByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        const string sql = """
            SELECT TOP 200 *
            FROM dbo.AlertRoutingSubscriptions
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
              AND IsEnabled = 1
            ORDER BY CreatedUtc DESC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        IEnumerable<AlertRoutingSubscription> rows = await connection.QueryAsync<AlertRoutingSubscription>(
            new CommandDefinition(
                sql,
                new
                {
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId
                },
                cancellationToken: ct));
        return rows.ToList();
    }
}
