using System.Data;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ArchiForge.Data.Infrastructure;

[ExcludeFromCodeCoverage(Justification = "Requires live SQL Server connection; tested via integration tests.")]
public sealed class SqlConnectionFactory(IConfiguration configuration) : IDbConnectionFactory
{
    private readonly string _connectionString = configuration.GetConnectionString("ArchiForge")
                                                ?? throw new InvalidOperationException("Connection string 'ArchiForge' was not found.");

    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }

    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        SqlConnection connection = new(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
