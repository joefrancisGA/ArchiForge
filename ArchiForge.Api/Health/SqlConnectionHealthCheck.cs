using System.Data.Common;

using ArchiForge.Data.Infrastructure;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArchiForge.Api.Health;

/// <summary>
/// Probes the database via <see cref="IDbConnectionFactory"/>. Reports <see cref="HealthStatus.Degraded"/>
/// for transient SQL errors (timeouts, transport failures) so load balancers do not immediately evict
/// the instance, and <see cref="HealthStatus.Unhealthy"/> for permanent failures.
/// </summary>
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
        catch (SqlException ex) when (IsTransient(ex))
        {
            return HealthCheckResult.Degraded("Database connection timed out or hit a transient error.", ex);
        }
        catch (TimeoutException ex)
        {
            return HealthCheckResult.Degraded("Database connection timed out.", ex);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connection failed.", ex);
        }
    }

    /// <summary>
    /// SQL Server error numbers that indicate a transient / retriable condition.
    /// -2 = timeout; 40613 = Azure SQL DB unavailable; 40197 = service error; 49918–49920 = throttling.
    /// </summary>
    private static bool IsTransient(SqlException ex)
    {
        return ex.Number is -2 or 40613 or 40197 or 49918 or 49919 or 49920;
    }
}
