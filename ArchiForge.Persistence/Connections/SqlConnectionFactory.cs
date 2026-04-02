using System.Diagnostics.CodeAnalysis;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Connections;

[ExcludeFromCodeCoverage(Justification = "Requires live SQL Server connection; tested via integration tests.")]
public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken ct)
    {
        SqlConnection connection = new(_connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
