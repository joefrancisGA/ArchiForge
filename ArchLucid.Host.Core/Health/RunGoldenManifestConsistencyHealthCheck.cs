using System.Data.Common;
using System.Globalization;

using ArchLucid.Host.Core.Configuration;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Data.Infrastructure;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ArchLucid.Host.Core.Health;

/// <summary>
/// Detects authority rows where <c>GoldenManifestId</c> is set but no matching <c>dbo.GoldenManifests</c> row exists for the same run/manifest pair.
/// </summary>
public sealed class RunGoldenManifestConsistencyHealthCheck(
    IDbConnectionFactory connectionFactory,
    IOptions<ArchLucidOptions> archLucidOptions) : IHealthCheck
{
    private const string SqlText = """
                                   SELECT COUNT_BIG(1)
                                   FROM dbo.Runs r
                                   WHERE r.ArchivedUtc IS NULL
                                     AND r.GoldenManifestId IS NOT NULL
                                     AND NOT EXISTS (
                                         SELECT 1
                                         FROM dbo.GoldenManifests m
                                         WHERE m.ManifestId = r.GoldenManifestId
                                           AND m.RunId = r.RunId);
                                   """;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (ArchLucidOptions.EffectiveIsInMemory(archLucidOptions.Value.StorageProvider))

            return HealthCheckResult.Healthy(
                "Run/manifest consistency check skipped: InMemory storage.");

        try
        {
            DbConnection connection = (DbConnection)connectionFactory.CreateConnection();
            await using DbConnection _ = connection;
            await connection.OpenAsync(cancellationToken);

            await using DbCommand command = connection.CreateCommand();
            command.CommandText = SqlText;
            object? scalar = await command.ExecuteScalarAsync(cancellationToken);
            long mismatchCount = scalar is long l ? l : Convert.ToInt64(scalar ?? 0L, CultureInfo.InvariantCulture);

            if (mismatchCount > 0)

                return HealthCheckResult.Degraded(
                    $"Detected {mismatchCount} non-archived run(s) with GoldenManifestId not matching dbo.GoldenManifests.");

            return HealthCheckResult.Healthy("Run golden manifest references are consistent for sampled invariant.");
        }
        catch (SqlException ex) when (SqlTransientDetector.IsTransient(ex))
        {
            return HealthCheckResult.Degraded("Consistency query hit a transient SQL error.", ex);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("Consistency query failed.", ex);
        }
    }
}
