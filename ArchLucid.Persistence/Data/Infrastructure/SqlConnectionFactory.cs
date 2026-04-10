using System.Data;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ArchLucid.Persistence.Data.Infrastructure;

[ExcludeFromCodeCoverage(Justification = "Requires live SQL Server connection; tested via integration tests.")]
public sealed class SqlConnectionFactory(IConfiguration configuration) : IDbConnectionFactory
{
    private readonly string _connectionString = configuration.GetConnectionString("ArchLucid")
                                                ?? throw new InvalidOperationException(
                                                    "Connection string 'ArchLucid' was not found.");

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
