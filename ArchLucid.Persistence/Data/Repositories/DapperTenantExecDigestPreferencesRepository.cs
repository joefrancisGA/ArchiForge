using System.Diagnostics.CodeAnalysis;

using ArchLucid.Contracts.Notifications;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Models;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Data.Repositories;

/// <inheritdoc cref="ITenantExecDigestPreferencesRepository" />
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; exercised via ArchLucid.sql / DbUp.")]
public sealed class DapperTenantExecDigestPreferencesRepository(ISqlConnectionFactory connectionFactory)
    : ITenantExecDigestPreferencesRepository
{
    private readonly ISqlConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    /// <inheritdoc />
    public async Task<ExecDigestPreferencesResponse?> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                TenantId,
                SchemaVersion,
                EmailEnabled,
                RecipientEmails,
                IanaTimeZoneId,
                DayOfWeek,
                HourOfDay,
                UpdatedUtc
            FROM dbo.TenantExecDigestPreferences
            WHERE TenantId = @TenantId;
            """;

        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        TenantExecDigestPreferencesRow? row = await connection.QueryFirstOrDefaultAsync<TenantExecDigestPreferencesRow>(
            new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: cancellationToken));

        if (row is null)
            return null;

        return ExecDigestPreferencesMapper.ToResponse(row);
    }

    /// <inheritdoc />
    public async Task<ExecDigestPreferencesResponse?> UpsertAsync(
        Guid tenantId,
        bool emailEnabled,
        IReadOnlyList<string> recipientEmails,
        string ianaTimeZoneId,
        int dayOfWeek,
        int hourOfDay,
        CancellationToken cancellationToken)
    {
        const string tenantExistsSql = """
            SELECT COUNT(1)
            FROM dbo.Tenants
            WHERE Id = @TenantId;
            """;

        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        int tenantCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(tenantExistsSql, new { TenantId = tenantId }, cancellationToken: cancellationToken));

        if (tenantCount == 0)
            return null;

        string tz = string.IsNullOrWhiteSpace(ianaTimeZoneId) ? "UTC" : ianaTimeZoneId.Trim();
        string emails = ExecDigestPreferencesMapper.SerializeEmails(recipientEmails);

        const string mergeSql = """
            MERGE dbo.TenantExecDigestPreferences AS t
            USING (
                SELECT
                    @TenantId AS TenantId,
                    @EmailEnabled AS EmailEnabled,
                    @RecipientEmails AS RecipientEmails,
                    @Tz AS IanaTimeZoneId,
                    @Dow AS DayOfWeek,
                    @Hour AS HourOfDay
            ) AS s
            ON t.TenantId = s.TenantId
            WHEN MATCHED THEN UPDATE SET
                EmailEnabled = s.EmailEnabled,
                RecipientEmails = NULLIF(LTRIM(RTRIM(s.RecipientEmails)), N''),
                IanaTimeZoneId = s.IanaTimeZoneId,
                DayOfWeek = s.DayOfWeek,
                HourOfDay = s.HourOfDay,
                UpdatedUtc = SYSUTCDATETIME()
            WHEN NOT MATCHED THEN INSERT (
                TenantId,
                SchemaVersion,
                EmailEnabled,
                RecipientEmails,
                IanaTimeZoneId,
                DayOfWeek,
                HourOfDay,
                UpdatedUtc
            )
            VALUES (
                s.TenantId,
                1,
                s.EmailEnabled,
                NULLIF(LTRIM(RTRIM(s.RecipientEmails)), N''),
                s.IanaTimeZoneId,
                s.DayOfWeek,
                s.HourOfDay,
                SYSUTCDATETIME()
            );
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                mergeSql,
                new
                {
                    TenantId = tenantId,
                    EmailEnabled = emailEnabled,
                    RecipientEmails = emails,
                    Tz = tz,
                    Dow = (byte)dayOfWeek,
                    Hour = (byte)hourOfDay,
                },
                cancellationToken: cancellationToken));

        return await GetByTenantAsync(tenantId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> ListEmailEnabledTenantIdsAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TenantId
            FROM dbo.TenantExecDigestPreferences WITH (NOLOCK)
            WHERE EmailEnabled = 1;
            """;

        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        IEnumerable<Guid> rows = await connection.QueryAsync<Guid>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return rows.ToList();
    }

    /// <inheritdoc />
    public async Task<bool> TryDisableEmailAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.TenantExecDigestPreferences
            SET EmailEnabled = 0,
                UpdatedUtc = SYSUTCDATETIME()
            WHERE TenantId = @TenantId
              AND EmailEnabled = 1;
            """;

        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        int n = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: cancellationToken));

        return n > 0;
    }
}
