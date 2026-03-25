using System.Data.Common;

using ArchiForge.Data.Infrastructure;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArchiForge.Api.Health;

public sealed class SqlConnectionHealthCheck(IDbConnectionFactory connectionFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            DbConnection connection = (DbConnection)connectionFactory.CreateConnection();
            await using DbConnection _ = connection;
            await connection.OpenAsync(cancellationToken);
            return HealthCheckResult.Healthy("Database connection successful.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connection failed.", ex);
        }
    }
}
