using ArchLucid.Persistence.Connections;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests;

/// <summary>
///     Opens real <see cref="SqlConnection" /> instances for contract tests that need <see cref="ISqlConnectionFactory" />
///     .
/// </summary>
public sealed class TestSqlConnectionFactory(string connectionString) : ISqlConnectionFactory
{
    private readonly string _connectionString =
        connectionString ?? throw new ArgumentNullException(nameof(connectionString));

    /// <inheritdoc />
    public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken ct)
    {
        SqlConnection connection = new(_connectionString);
        await connection.OpenAsync(ct);

        return connection;
    }
}
