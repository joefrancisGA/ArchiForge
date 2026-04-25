using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using ArchLucid.Core.Concurrency;
using ArchLucid.Persistence.Connections;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Concurrency;

/// <summary>
///     SQL Server session application lock for cross-replica create-run idempotency (<c>sp_getapplock</c> /
///     <c>sp_releaseapplock</c>).
/// </summary>
/// <remarks>
///     <c>sp_getapplock</c> return codes: 0 or 1 = granted, -1 = timeout, -2 = cancelled, -3 = deadlock victim.
///     Resource name is limited to 255 NVARCHAR characters; longer keys are hashed to a fixed hex string.
/// </remarks>
public sealed class SqlSessionDistributedCreateRunIdempotencyLock(ISqlConnectionFactory connectionFactory)
    : IDistributedCreateRunIdempotencyLock
{
    private readonly ISqlConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    /// <inheritdoc />
    public async Task<IAsyncDisposable> AcquireExclusiveSessionLockAsync(
        string lockResourceName,
        int lockTimeoutMs,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lockResourceName);

        if (lockTimeoutMs < 0)
            throw new ArgumentOutOfRangeException(nameof(lockTimeoutMs));

        string resource = NormalizeResourceName(lockResourceName);

        SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        try
        {
            await using SqlCommand cmd = connection.CreateCommand();
            // SqlClient defaults CommandTimeout to 30s. sp_getapplock may block for @LockTimeout milliseconds; if the
            // command timeout fires first, the client aborts while still waiting for the lock (k6 then reports ~30.3s
            // POST latency and create_run checks fail). Align ADO.NET timeout with the lock wait budget + headroom.
            cmd.CommandTimeout = SqlCommandTimeoutSecondsForLockWait(lockTimeoutMs);
            cmd.CommandText =
                """
                DECLARE @result int;
                EXEC @result = sp_getapplock
                    @Resource = @resource,
                    @LockMode = 'Exclusive',
                    @LockOwner = 'Session',
                    @LockTimeout = @timeoutMs;
                SELECT @result;
                """;
            SqlParameter pResource = cmd.Parameters.Add("@resource", SqlDbType.NVarChar, 255);
            pResource.Value = resource;
            cmd.Parameters.AddWithValue("@timeoutMs", lockTimeoutMs);

            object? scalar = await cmd.ExecuteScalarAsync(cancellationToken);
            int code = scalar is int i ? i : Convert.ToInt32(scalar, CultureInfo.InvariantCulture);

            if (code < 0)
                throw new TimeoutException(
                    $"sp_getapplock could not acquire exclusive lock for create-run idempotency (code={code}).");

            return new SessionLockScope(connection, resource);
        }
        catch
        {
            await connection.DisposeAsync();

            throw;
        }
    }

    /// <summary>
    ///     Maps lock wait milliseconds to <see cref="SqlCommand.CommandTimeout" /> seconds (0 = unlimited per SqlClient).
    /// </summary>
    private static int SqlCommandTimeoutSecondsForLockWait(int lockTimeoutMs)
    {
        if (lockTimeoutMs <= 0)
            return 0;

        // Ceiling to whole seconds, add buffer for scheduling. Max orchestrator lock is 600s; command timeout must exceed that.
        int seconds = (lockTimeoutMs + 999) / 1000 + 30;

        return seconds > 660 ? 660 : seconds;
    }

    private static string NormalizeResourceName(string lockResourceName)
    {
        if (lockResourceName.Length <= 255)
            return lockResourceName;

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(lockResourceName));

        return Convert.ToHexString(hash);
    }

    private sealed class SessionLockScope(SqlConnection connection, string resourceName) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                await using SqlCommand release = connection.CreateCommand();
                release.CommandText =
                    """
                    DECLARE @result int;
                    EXEC @result = sp_releaseapplock @Resource = @resource, @LockOwner = 'Session';
                    SELECT @result;
                    """;
                SqlParameter pResource = release.Parameters.Add("@resource", SqlDbType.NVarChar, 255);
                pResource.Value = resourceName;

                await release.ExecuteNonQueryAsync();
            }
            catch
            {
                // Best-effort release; connection is closing anyway.
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }
    }
}
