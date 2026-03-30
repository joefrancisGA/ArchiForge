using System.Data.Common;

using ArchiForge.Api.Configuration;
using ArchiForge.Data.Infrastructure;
using ArchiForge.Persistence.Connections;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ArchiForge.Api.Health;

/// <summary>
/// Probes the database via <see cref="IDbConnectionFactory"/> when <see cref="ArchiForgeOptions.StorageProvider"/> is Sql.
/// Skips (Healthy) for InMemory storage so readiness reflects the configured persistence mode.
/// </summary>
public sealed class SqlConnectionHealthCheck(
    IDbConnectionFactory connectionFactory,
    IOptions<ArchiForgeOptions> archiForgeOptions) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(archiForgeOptions.Value.StorageProvider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            return HealthCheckResult.Healthy(
                "Database readiness skipped: ArchiForge:StorageProvider is InMemory (no SQL persistence).");
        }

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
