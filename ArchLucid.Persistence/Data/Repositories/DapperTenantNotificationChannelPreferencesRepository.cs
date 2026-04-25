using System.Diagnostics.CodeAnalysis;

using ArchLucid.Contracts.Notifications;
using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Data.Repositories;

/// <inheritdoc cref="ITenantNotificationChannelPreferencesRepository" />
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; greenfield exercised via ArchLucid.sql / DbUp.")]
public sealed class DapperTenantNotificationChannelPreferencesRepository(ISqlConnectionFactory connectionFactory)
    : ITenantNotificationChannelPreferencesRepository
{
    private readonly ISqlConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    /// <inheritdoc />
    public async Task<TenantNotificationChannelPreferencesResponse?> GetByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT
                               TenantId,
                               SchemaVersion,
                               EmailCustomerNotificationsEnabled,
                               TeamsCustomerNotificationsEnabled,
                               OutboundWebhookCustomerNotificationsEnabled,
                               UpdatedUtc
                           FROM dbo.TenantNotificationChannelPreferences
                           WHERE TenantId = @TenantId;
                           """;

        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        TenantNotificationChannelPreferencesRow? row =
            await connection.QueryFirstOrDefaultAsync<TenantNotificationChannelPreferencesRow>(
                new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: cancellationToken));

        if (row is null)
            return null;


        return new TenantNotificationChannelPreferencesResponse
        {
            SchemaVersion = row.SchemaVersion,
            TenantId = row.TenantId,
            IsConfigured = true,
            EmailCustomerNotificationsEnabled = row.EmailCustomerNotificationsEnabled,
            TeamsCustomerNotificationsEnabled = row.TeamsCustomerNotificationsEnabled,
            OutboundWebhookCustomerNotificationsEnabled = row.OutboundWebhookCustomerNotificationsEnabled,
            UpdatedUtc = new DateTimeOffset(row.UpdatedUtc, TimeSpan.Zero)
        };
    }

    /// <inheritdoc />
    public async Task<TenantNotificationChannelPreferencesResponse?> UpsertAsync(
        Guid tenantId,
        bool emailCustomerNotificationsEnabled,
        bool teamsCustomerNotificationsEnabled,
        bool outboundWebhookCustomerNotificationsEnabled,
        CancellationToken cancellationToken)
    {
        const string tenantExistsSql = """
                                       SELECT COUNT(1)
                                       FROM dbo.Tenants
                                       WHERE Id = @TenantId;
                                       """;

        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        int tenantCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                tenantExistsSql,
                new { TenantId = tenantId },
                cancellationToken: cancellationToken));

        if (tenantCount == 0)
            return null;


        const string mergeSql = """
                                MERGE dbo.TenantNotificationChannelPreferences AS t
                                USING (
                                    SELECT
                                        @TenantId AS TenantId,
                                        @Email AS EmailCustomerNotificationsEnabled,
                                        @Teams AS TeamsCustomerNotificationsEnabled,
                                        @Webhook AS OutboundWebhookCustomerNotificationsEnabled
                                ) AS s
                                ON t.TenantId = s.TenantId
                                WHEN MATCHED THEN UPDATE SET
                                    EmailCustomerNotificationsEnabled = s.EmailCustomerNotificationsEnabled,
                                    TeamsCustomerNotificationsEnabled = s.TeamsCustomerNotificationsEnabled,
                                    OutboundWebhookCustomerNotificationsEnabled = s.OutboundWebhookCustomerNotificationsEnabled,
                                    UpdatedUtc = SYSUTCDATETIME()
                                WHEN NOT MATCHED THEN INSERT (
                                    TenantId,
                                    SchemaVersion,
                                    EmailCustomerNotificationsEnabled,
                                    TeamsCustomerNotificationsEnabled,
                                    OutboundWebhookCustomerNotificationsEnabled,
                                    UpdatedUtc
                                )
                                VALUES (
                                    s.TenantId,
                                    1,
                                    s.EmailCustomerNotificationsEnabled,
                                    s.TeamsCustomerNotificationsEnabled,
                                    s.OutboundWebhookCustomerNotificationsEnabled,
                                    SYSUTCDATETIME()
                                );
                                """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                mergeSql,
                new
                {
                    TenantId = tenantId,
                    Email = emailCustomerNotificationsEnabled,
                    Teams = teamsCustomerNotificationsEnabled,
                    Webhook = outboundWebhookCustomerNotificationsEnabled
                },
                cancellationToken: cancellationToken));

        return await GetByTenantAsync(tenantId, cancellationToken);
    }

    private sealed class TenantNotificationChannelPreferencesRow
    {
        public Guid TenantId
        {
            get;
            init;
        }

        public int SchemaVersion
        {
            get;
            init;
        }

        public bool EmailCustomerNotificationsEnabled
        {
            get;
            init;
        }

        public bool TeamsCustomerNotificationsEnabled
        {
            get;
            init;
        }

        public bool OutboundWebhookCustomerNotificationsEnabled
        {
            get;
            init;
        }

        public DateTime UpdatedUtc
        {
            get;
            init;
        }
    }
}
