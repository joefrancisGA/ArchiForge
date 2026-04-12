using System.Data;
using System.Diagnostics.CodeAnalysis;

using ArchLucid.Contracts.Governance;
using ArchLucid.Decisioning.Governance.PolicyPacks;
using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Governance;

/// <summary>
/// Dapper access to <c>dbo.PolicyPackChangeLog</c> (INSERT and scoped reads only).
/// </summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class DapperPolicyPackChangeLogRepository(
    ISqlConnectionFactory connectionFactory,
    IGovernanceResolutionReadConnectionFactory governanceResolutionReadConnectionFactory)
    : IPolicyPackChangeLogRepository
{
    /// <inheritdoc />
    public async Task AppendAsync(
        PolicyPackChangeLogEntry entry,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.ChangeType);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.ChangedBy);

        DateTime changedUtc = entry.ChangedUtc == default ? DateTime.UtcNow : entry.ChangedUtc;

        const string sql = """
            INSERT INTO dbo.PolicyPackChangeLog
            (
                PolicyPackId, TenantId, WorkspaceId, ProjectId,
                ChangeType, ChangedBy, ChangedUtc,
                PreviousValue, NewValue, SummaryText
            )
            VALUES
            (
                @PolicyPackId, @TenantId, @WorkspaceId, @ProjectId,
                @ChangeType, @ChangedBy, @ChangedUtc,
                @PreviousValue, @NewValue, @SummaryText
            );
            """;

        object param = new
        {
            entry.PolicyPackId,
            entry.TenantId,
            entry.WorkspaceId,
            entry.ProjectId,
            entry.ChangeType,
            entry.ChangedBy,
            ChangedUtc = changedUtc,
            entry.PreviousValue,
            entry.NewValue,
            entry.SummaryText,
        };

        (SqlConnection conn, bool ownsConnection) =
            await SqlExternalConnection.ResolveAsync(connectionFactory, connection, cancellationToken);

        try
        {
            await conn.ExecuteAsync(
                new CommandDefinition(sql, param, transaction: transaction, cancellationToken: cancellationToken));
        }
        finally
        {
            SqlExternalConnection.DisposeIfOwned(conn, ownsConnection);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PolicyPackChangeLogEntry>> GetByPolicyPackIdAsync(
        Guid policyPackId,
        int maxRows = 50,
        CancellationToken cancellationToken = default)
    {
        if (maxRows <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRows));
        }

        const string sql = """
            SELECT TOP (@MaxRows)
                ChangeLogId, PolicyPackId, TenantId, WorkspaceId, ProjectId,
                ChangeType, ChangedBy, ChangedUtc,
                PreviousValue, NewValue, SummaryText
            FROM dbo.PolicyPackChangeLog
            WHERE PolicyPackId = @PolicyPackId
            ORDER BY ChangedUtc DESC;
            """;

        await using SqlConnection connection =
            await governanceResolutionReadConnectionFactory.CreateOpenConnectionAsync(cancellationToken);

        IEnumerable<PolicyPackChangeLogEntry> rows = await connection.QueryAsync<PolicyPackChangeLogEntry>(
            new CommandDefinition(
                sql,
                new { PolicyPackId = policyPackId, MaxRows = maxRows },
                cancellationToken: cancellationToken));

        return rows.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PolicyPackChangeLogEntry>> GetByTenantAsync(
        Guid tenantId,
        int maxRows = 100,
        CancellationToken cancellationToken = default)
    {
        if (maxRows <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRows));
        }

        const string sql = """
            SELECT TOP (@MaxRows)
                ChangeLogId, PolicyPackId, TenantId, WorkspaceId, ProjectId,
                ChangeType, ChangedBy, ChangedUtc,
                PreviousValue, NewValue, SummaryText
            FROM dbo.PolicyPackChangeLog
            WHERE TenantId = @TenantId
            ORDER BY ChangedUtc DESC;
            """;

        await using SqlConnection connection =
            await governanceResolutionReadConnectionFactory.CreateOpenConnectionAsync(cancellationToken);

        IEnumerable<PolicyPackChangeLogEntry> rows = await connection.QueryAsync<PolicyPackChangeLogEntry>(
            new CommandDefinition(
                sql,
                new { TenantId = tenantId, MaxRows = maxRows },
                cancellationToken: cancellationToken));

        return rows.ToList();
    }
}
