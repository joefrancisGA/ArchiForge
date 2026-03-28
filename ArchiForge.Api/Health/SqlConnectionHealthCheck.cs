using System.Data.Common;

using ArchiForge.Data.Infrastructure;
using ArchiForge.Persistence.Connections;

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
        catch (SqlException ex) when (SqlTransientDetector.IsTransient(ex))
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
}
