using ArchiForge.Decisioning.Advisory.Delivery;
using ArchiForge.Persistence.Connections;
using Dapper;

namespace ArchiForge.Persistence.Advisory;

public sealed class DapperDigestSubscriptionRepository(ISqlConnectionFactory connectionFactory)
    : IDigestSubscriptionRepository
{
    public async Task CreateAsync(DigestSubscription subscription, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.DigestSubscriptions
            (
                SubscriptionId, TenantId, WorkspaceId, ProjectId,
                Name, ChannelType, Destination, IsEnabled,
                CreatedUtc, LastDeliveredUtc, MetadataJson
            )
            VALUES
            (
                @SubscriptionId, @TenantId, @WorkspaceId, @ProjectId,
                @Name, @ChannelType, @Destination, @IsEnabled,
                @CreatedUtc, @LastDeliveredUtc, @MetadataJson
            );
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, subscription, cancellationToken: ct));
    }

    public async Task UpdateAsync(DigestSubscription subscription, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.DigestSubscriptions
            SET
                Name = @Name,
                ChannelType = @ChannelType,
                Destination = @Destination,
                IsEnabled = @IsEnabled,
                LastDeliveredUtc = @LastDeliveredUtc,
                MetadataJson = @MetadataJson
            WHERE SubscriptionId = @SubscriptionId;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, subscription, cancellationToken: ct));
    }

    public async Task<DigestSubscription?> GetByIdAsync(Guid subscriptionId, CancellationToken ct)
    {
        const string sql = """
            SELECT
                SubscriptionId, TenantId, WorkspaceId, ProjectId,
                Name, ChannelType, Destination, IsEnabled,
                CreatedUtc, LastDeliveredUtc, MetadataJson
            FROM dbo.DigestSubscriptions
            WHERE SubscriptionId = @SubscriptionId;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        return await connection.QueryFirstOrDefaultAsync<DigestSubscription>(
            new CommandDefinition(sql, new { SubscriptionId = subscriptionId }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<DigestSubscription>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        const string sql = """
            SELECT
                SubscriptionId, TenantId, WorkspaceId, ProjectId,
                Name, ChannelType, Destination, IsEnabled,
                CreatedUtc, LastDeliveredUtc, MetadataJson
            FROM dbo.DigestSubscriptions
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
            ORDER BY CreatedUtc DESC;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        var result = await connection.QueryAsync<DigestSubscription>(
            new CommandDefinition(
                sql,
                new { TenantId = tenantId, WorkspaceId = workspaceId, ProjectId = projectId },
                cancellationToken: ct));

        return result.ToList();
    }

    public async Task<IReadOnlyList<DigestSubscription>> ListEnabledByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        const string sql = """
            SELECT
                SubscriptionId, TenantId, WorkspaceId, ProjectId,
                Name, ChannelType, Destination, IsEnabled,
                CreatedUtc, LastDeliveredUtc, MetadataJson
            FROM dbo.DigestSubscriptions
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
              AND IsEnabled = 1
            ORDER BY CreatedUtc DESC;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        var result = await connection.QueryAsync<DigestSubscription>(
            new CommandDefinition(
                sql,
                new { TenantId = tenantId, WorkspaceId = workspaceId, ProjectId = projectId },
                cancellationToken: ct));

        return result.ToList();
    }
}
