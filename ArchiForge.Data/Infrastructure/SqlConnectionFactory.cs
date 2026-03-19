using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ArchiForge.Data.Infrastructure;

public sealed class SqlConnectionFactory(IConfiguration configuration) : IDbConnectionFactory
{
    private readonly string _connectionString = configuration.GetConnectionString("ArchiForge")
                                                ?? throw new InvalidOperationException("Connection string 'ArchiForge' was not found.");

    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}