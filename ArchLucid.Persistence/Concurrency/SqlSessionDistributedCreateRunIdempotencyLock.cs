using System.Data;
using System.Security.Cryptography;
using System.Text;

using ArchLucid.Core.Concurrency;
using ArchLucid.Persistence.Connections;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Concurrency;

/// <summary>
/// SQL Server session application lock for cross-replica create-run idempotency (<c>sp_getapplock</c> / <c>sp_releaseapplock</c>).
/// </summary>
/// <remarks>
/// <c>sp_getapplock</c> return codes: 0 or 1 = granted, -1 = timeout, -2 = cancelled, -3 = deadlock victim.
/// Resource name is limited to 255 NVARCHAR characters; longer keys are hashed to a fixed hex string.
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
            int code = scalar is int i ? i : Convert.ToInt32(scalar, System.Globalization.CultureInfo.InvariantCulture);

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

    private static string NormalizeResourceName(string lockResourceName)
    {
        if (lockResourceName.Length <= 255)
            return lockResourceName;

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(lockResourceName));

        return Convert.ToHexString(hash);
    }

    private sealed class SessionLockScope(SqlConnection connection, string resourceName) : IAsyncDisposable
    {
        private readonly SqlConnection _connection = connection;

        private readonly string _resourceName = resourceName;

        public async ValueTask DisposeAsync()
        {
            try
            {
                await using SqlCommand release = _connection.CreateCommand();
                release.CommandText =
                    """
                    DECLARE @result int;
                    EXEC @result = sp_releaseapplock @Resource = @resource, @LockOwner = 'Session';
                    SELECT @result;
                    """;
                SqlParameter pResource = release.Parameters.Add("@resource", SqlDbType.NVarChar, 255);
                pResource.Value = _resourceName;

                await release.ExecuteNonQueryAsync();
            }
            catch
            {
                // Best-effort release; connection is closing anyway.
            }
            finally
            {
                await _connection.DisposeAsync();
            }
        }
    }
}
