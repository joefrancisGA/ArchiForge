using System.Data;

using ArchLucid.Core.Billing;
using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Billing;

public sealed class SqlBillingLedger(
    ISqlConnectionFactory connectionFactory,
    IRlsSessionContextApplicator rlsSessionContextApplicator) : IBillingLedger
{
    private readonly ISqlConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    private readonly IRlsSessionContextApplicator _rlsSessionContextApplicator =
        rlsSessionContextApplicator ?? throw new ArgumentNullException(nameof(rlsSessionContextApplicator));

    public async Task<bool> TenantHasActiveSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await _rlsSessionContextApplicator.ApplyAsync(connection, cancellationToken);

        const string sql = """
                           SELECT CAST(1 AS bit)
                           FROM dbo.BillingSubscriptions
                           WHERE TenantId = @TenantId AND Status = N'Active';
                           """;

        bool? row = await connection.ExecuteScalarAsync<bool?>(
            new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: cancellationToken));

        return row == true;
    }

    public async Task UpsertPendingCheckoutAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string provider,
        string providerSessionId,
        string tierCode,
        int seats,
        int workspaces,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await _rlsSessionContextApplicator.ApplyAsync(connection, cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                "dbo.sp_Billing_UpsertPending",
                new
                {
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId,
                    Provider = provider,
                    ProviderSubscriptionId = providerSessionId,
                    Tier = tierCode,
                    SeatsPurchased = seats,
                    WorkspacesPurchased = workspaces
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<bool> TryInsertWebhookEventAsync(
        string dedupeKey,
        string provider,
        string eventType,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO dbo.BillingWebhookEvents (EventId, Provider, EventType, PayloadJson, ReceivedUtc, ProcessedUtc, ResultStatus)
                    VALUES (@EventId, @Provider, @EventType, @PayloadJson, SYSUTCDATETIME(), NULL, N'Received');
                    """,
                    new { EventId = dedupeKey, Provider = provider, EventType = eventType, PayloadJson = payloadJson },
                    cancellationToken: cancellationToken));

            return true;
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            return false;
        }
    }

    public async Task MarkWebhookProcessedAsync(string dedupeKey, string resultStatus,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE dbo.BillingWebhookEvents
                SET ProcessedUtc = SYSUTCDATETIME(), ResultStatus = @ResultStatus
                WHERE EventId = @EventId;
                """,
                new { EventId = dedupeKey, ResultStatus = resultStatus },
                cancellationToken: cancellationToken));
    }

    public async Task<string?> GetWebhookEventResultStatusAsync(string dedupeKey, CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<string?>(
            new CommandDefinition(
                """
                SELECT ResultStatus
                FROM dbo.BillingWebhookEvents
                WHERE EventId = @EventId;
                """,
                new { EventId = dedupeKey },
                cancellationToken: cancellationToken));
    }

    public async Task ActivateSubscriptionAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string provider,
        string providerSubscriptionId,
        string tierCode,
        int seats,
        int workspaces,
        string? rawWebhookJson,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                "dbo.sp_Billing_Activate",
                new
                {
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId,
                    Provider = provider,
                    ProviderSubscriptionId = providerSubscriptionId,
                    Tier = tierCode,
                    SeatsPurchased = seats,
                    WorkspacesPurchased = workspaces,
                    RawWebhookJson = rawWebhookJson
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task SuspendSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                "dbo.sp_Billing_Suspend",
                new { TenantId = tenantId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task ReinstateSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                "dbo.sp_Billing_Reinstate",
                new { TenantId = tenantId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task CancelSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                "dbo.sp_Billing_Cancel",
                new { TenantId = tenantId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task ChangePlanAsync(Guid tenantId, string tierCode, string? rawWebhookJson,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await _rlsSessionContextApplicator.ApplyAsync(connection, cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                "dbo.sp_Billing_ChangePlan",
                new { TenantId = tenantId, Tier = tierCode, RawWebhookJson = rawWebhookJson },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task ChangeQuantityAsync(Guid tenantId, int seatsPurchased, string? rawWebhookJson,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await _rlsSessionContextApplicator.ApplyAsync(connection, cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                "dbo.sp_Billing_ChangeQuantity",
                new { TenantId = tenantId, SeatsPurchased = seatsPurchased, RawWebhookJson = rawWebhookJson },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<BillingSubscriptionStateHistoryEntry>> GetSubscriptionStateHistoryAsync(
        Guid tenantId,
        int maxRows,
        CancellationToken cancellationToken = default)
    {
        if (maxRows <= 0 || maxRows > 500)
            throw new ArgumentOutOfRangeException(nameof(maxRows));


        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await _rlsSessionContextApplicator.ApplyAsync(connection, cancellationToken);

        const string sql = """
                           SELECT TOP (@MaxRows)
                               HistoryId,
                               TenantId,
                               WorkspaceId,
                               ProjectId,
                               RecordedUtc,
                               ChangeKind,
                               PrevStatus,
                               NewStatus,
                               PrevTier,
                               NewTier,
                               PrevSeatsPurchased,
                               NewSeatsPurchased,
                               PrevWorkspacesPurchased,
                               NewWorkspacesPurchased,
                               PrevProvider,
                               NewProvider,
                               PrevProviderSubscriptionId,
                               NewProviderSubscriptionId
                           FROM dbo.BillingSubscriptionStateHistory
                           WHERE TenantId = @TenantId
                           ORDER BY RecordedUtc DESC;
                           """;

        IEnumerable<BillingSubscriptionStateHistoryEntry> rows =
            await connection.QueryAsync<BillingSubscriptionStateHistoryEntry>(
                new CommandDefinition(sql, new { TenantId = tenantId, MaxRows = maxRows },
                    cancellationToken: cancellationToken));

        return [.. rows];
    }
}
