using System.Diagnostics.CodeAnalysis;

using ArchLucid.Contracts.Integrations;
using ArchLucid.Core.Notifications.Teams;
using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Data.Repositories;

/// <inheritdoc cref="ITenantTeamsIncomingWebhookConnectionRepository" />
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; exercised via ArchLucid.sql / DbUp and integration tests.")]
public sealed class DapperTenantTeamsIncomingWebhookConnectionRepository(ISqlConnectionFactory connectionFactory)
    : ITenantTeamsIncomingWebhookConnectionRepository
{
    private readonly ISqlConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    /// <inheritdoc />
    public async Task<TeamsIncomingWebhookConnectionResponse?> GetAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                TenantId,
                KeyVaultSecretName,
                Label,
                EnabledTriggersJson,
                UpdatedUtc
            FROM dbo.TenantTeamsIncomingWebhookConnections
            WHERE TenantId = @TenantId;
            """;

        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        TeamsIncomingWebhookRow? row = await connection.QueryFirstOrDefaultAsync<TeamsIncomingWebhookRow>(
            new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: cancellationToken));

        if (row is null)
            return null;


        return ToResponse(row, isConfigured: true);
    }

    /// <inheritdoc />
    public async Task<TeamsIncomingWebhookConnectionResponse?> UpsertAsync(
        Guid tenantId,
        string keyVaultSecretName,
        string? label,
        IReadOnlyList<string>? enabledTriggers,
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


        // null EnabledTriggers means "leave existing JSON unchanged on UPDATE; use catalog default on INSERT".
        // Two-source MERGE keeps that semantic in a single round-trip without a SELECT-then-MERGE race.
        string? enabledTriggersJson = enabledTriggers is null
            ? null
            : TeamsNotificationTriggerCatalog.Serialize(enabledTriggers.Count == 0 ? [] : enabledTriggers);

        const string mergeSql = """
            MERGE dbo.TenantTeamsIncomingWebhookConnections AS t
            USING (
                SELECT
                    @TenantId AS TenantId,
                    @KeyVaultSecretName AS KeyVaultSecretName,
                    @Label AS Label,
                    @EnabledTriggersJson AS EnabledTriggersJson
            ) AS s
            ON t.TenantId = s.TenantId
            WHEN MATCHED THEN UPDATE SET
                KeyVaultSecretName = s.KeyVaultSecretName,
                Label = s.Label,
                EnabledTriggersJson = COALESCE(s.EnabledTriggersJson, t.EnabledTriggersJson),
                UpdatedUtc = SYSUTCDATETIME()
            WHEN NOT MATCHED THEN INSERT (TenantId, KeyVaultSecretName, Label, EnabledTriggersJson, UpdatedUtc)
            VALUES (s.TenantId, s.KeyVaultSecretName, s.Label, COALESCE(s.EnabledTriggersJson, @CatalogDefaultJson), SYSUTCDATETIME());
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                mergeSql,
                new
                {
                    TenantId = tenantId,
                    KeyVaultSecretName = keyVaultSecretName,
                    Label = label,
                    EnabledTriggersJson = enabledTriggersJson,
                    CatalogDefaultJson = TeamsNotificationTriggerCatalog.DefaultEnabledTriggersJson,
                },
                cancellationToken: cancellationToken));

        return await GetAsync(tenantId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        const string sql = """
            DELETE FROM dbo.TenantTeamsIncomingWebhookConnections
            WHERE TenantId = @TenantId;
            """;

        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        int affected = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: cancellationToken));

        return affected > 0;
    }

    private static TeamsIncomingWebhookConnectionResponse ToResponse(TeamsIncomingWebhookRow row, bool isConfigured) =>
        new()
        {
            TenantId = row.TenantId,
            IsConfigured = isConfigured,
            Label = row.Label,
            KeyVaultSecretName = row.KeyVaultSecretName,
            EnabledTriggers = TeamsNotificationTriggerCatalog.ParseOrDefault(row.EnabledTriggersJson),
            UpdatedUtc = new DateTimeOffset(row.UpdatedUtc, TimeSpan.Zero),
        };

    private sealed class TeamsIncomingWebhookRow
    {
        public Guid TenantId { get; init; }

        public string KeyVaultSecretName { get; init; } = "";

        public string? Label { get; init; }

        public string? EnabledTriggersJson { get; init; }

        public DateTime UpdatedUtc { get; init; }
    }
}
