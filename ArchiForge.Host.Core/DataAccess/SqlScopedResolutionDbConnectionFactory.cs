using System.Data;

using ArchiForge.Persistence.Data.Infrastructure;
using ArchiForge.Persistence.Connections;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Host.Core.DataAccess;

/// <summary>
/// Bridges <see cref="IDbConnectionFactory"/> (Data-layer Dapper repos, health checks) to scoped
/// <see cref="ISqlConnectionFactory"/> (resilience + optional RLS session context) without making
/// <see cref="IDbConnectionFactory"/> itself scoped (hosted health checks resolve from the root provider).
/// </summary>
/// <remarks>
/// <see cref="CreateOpenConnectionAsync"/> opens one short DI scope only to resolve
/// <see cref="ISqlConnectionFactory"/>; the returned <see cref="SqlConnection"/> outlives that scope.
/// <see cref="CreateConnection"/> returns an unopened connection for probes that open explicitly (e.g. readiness).
/// </remarks>
public sealed class SqlScopedResolutionDbConnectionFactory(
    IServiceScopeFactory scopeFactory,
    string connectionString) : IDbConnectionFactory
{
    private readonly IServiceScopeFactory _scopeFactory =
        scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

    private readonly string _connectionString =
        connectionString ?? throw new ArgumentNullException(nameof(connectionString));

    /// <inheritdoc />
    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }

    /// <inheritdoc />
    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        SqlConnection connection;

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        ISqlConnectionFactory sql = scope.ServiceProvider.GetRequiredService<ISqlConnectionFactory>();
        connection = await sql.CreateOpenConnectionAsync(cancellationToken);

        return connection;
    }
}
