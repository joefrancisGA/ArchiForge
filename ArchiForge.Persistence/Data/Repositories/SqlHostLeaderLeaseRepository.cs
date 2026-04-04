using System.Data;
using System.Diagnostics.CodeAnalysis;

using ArchiForge.Persistence.Data.Infrastructure;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Data.Repositories;

[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; integration-tested separately.")]
public sealed class SqlHostLeaderLeaseRepository(IDbConnectionFactory connectionFactory) : IHostLeaderLeaseRepository
{
    private sealed record LeaseRow(string HolderInstanceId, DateTime LeaseExpiresUtc);

    /// <inheritdoc />
    public async Task<bool> TryAcquireOrRenewAsync(
        string leaseName,
        string instanceId,
        int leaseDurationSeconds,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);

        if (leaseDurationSeconds < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(leaseDurationSeconds));
        }

        const int maxAttempts = 4;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            bool result = await TryAcquireOrRenewOnceAsync(
                leaseName,
                instanceId,
                leaseDurationSeconds,
                cancellationToken);

            if (result || attempt == maxAttempts - 1)
            {
                return result;
            }
        }

        return false;
    }

    private async Task<bool> TryAcquireOrRenewOnceAsync(
        string leaseName,
        string instanceId,
        int leaseDurationSeconds,
        CancellationToken cancellationToken)
    {
        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using IDbTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        try
        {
            const string selectSql = """
                SELECT HolderInstanceId, LeaseExpiresUtc
                FROM dbo.HostLeaderLeases WITH (UPDLOCK, ROWLOCK)
                WHERE LeaseName = @LeaseName
                """;

            LeaseRow? row = await connection.QuerySingleOrDefaultAsync<LeaseRow>(
                new CommandDefinition(
                    selectSql,
                    new { LeaseName = leaseName },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            DateTime nowUtc = DateTime.UtcNow;
            DateTime newExpiryUtc = nowUtc.AddSeconds(leaseDurationSeconds);

            if (row is null)
            {
                const string insertSql = """
                    INSERT INTO dbo.HostLeaderLeases (LeaseName, HolderInstanceId, LeaseExpiresUtc)
                    VALUES (@LeaseName, @HolderInstanceId, @LeaseExpiresUtc)
                    """;

                try
                {
                    await connection.ExecuteAsync(
                        new CommandDefinition(
                            insertSql,
                            new
                            {
                                LeaseName = leaseName,
                                HolderInstanceId = instanceId,
                                LeaseExpiresUtc = newExpiryUtc
                            },
                            transaction: transaction,
                            cancellationToken: cancellationToken));

                    transaction.Commit();

                    return true;
                }
                catch (SqlException ex) when (ex.Number == 2627)
                {
                    transaction.Rollback();

                    return false;
                }
            }

            if (row.LeaseExpiresUtc < nowUtc
                || string.Equals(row.HolderInstanceId, instanceId, StringComparison.Ordinal))
            {
                const string updateSql = """
                    UPDATE dbo.HostLeaderLeases
                    SET HolderInstanceId = @HolderInstanceId,
                        LeaseExpiresUtc = @LeaseExpiresUtc
                    WHERE LeaseName = @LeaseName
                    """;

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        updateSql,
                        new
                        {
                            LeaseName = leaseName,
                            HolderInstanceId = instanceId,
                            LeaseExpiresUtc = newExpiryUtc
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                transaction.Commit();

                return true;
            }

            transaction.Rollback();

            return false;
        }
        catch
        {
            try
            {
                transaction.Rollback();
            }
            catch (InvalidOperationException)
            {
            }

            throw;
        }
    }

    /// <inheritdoc />
    public async Task TryReleaseAsync(string leaseName, string instanceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(leaseName) || string.IsNullOrWhiteSpace(instanceId))
        {
            return;
        }

        const string sql = """
            DELETE FROM dbo.HostLeaderLeases
            WHERE LeaseName = @LeaseName
              AND HolderInstanceId = @HolderInstanceId
            """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { LeaseName = leaseName, HolderInstanceId = instanceId },
                cancellationToken: cancellationToken));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HostLeaderLeaseSnapshot>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT LeaseName, HolderInstanceId, LeaseExpiresUtc
            FROM dbo.HostLeaderLeases
            ORDER BY LeaseName ASC;
            """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        IEnumerable<HostLeaderLeaseSnapshot> rows = await connection.QueryAsync<HostLeaderLeaseSnapshot>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return rows.ToList();
    }
}
