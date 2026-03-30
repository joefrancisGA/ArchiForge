using Microsoft.Data.SqlClient;

namespace ArchiForge.TestSupport;

/// <summary>
/// Builds SQL Server connection strings for test assemblies. Resolution order is explicit and environment-driven.
/// </summary>
public static class SqlServerIntegrationTestConnections
{
    /// <summary>
    /// Connection string for an ephemeral API test database (per factory). Does not create the catalog — callers use
    /// <see cref="SqlServerTestCatalogCommands"/>.
    /// </summary>
    /// <remarks>
    /// <list type="number">
    /// <item><description><see cref="TestDatabaseEnvironment.ApiIntegrationSqlEnvironmentVariable"/> if set.</description></item>
    /// <item><description><see cref="TestDatabaseEnvironment.PersistenceSqlEnvironmentVariable"/> if set (same server/auth, new catalog).</description></item>
    /// <item><description>Windows: <c>localhost</c> + integrated security.</description></item>
    /// <item><description>Non-Windows: throws — set one of the environment variables (Docker/CI SQL Server).</description></item>
    /// </list>
    /// </remarks>
    public static string CreateEphemeralApiDatabaseConnectionString(string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new ArgumentException("Database name is required.", nameof(databaseName));

        string? apiHost = Environment.GetEnvironmentVariable(TestDatabaseEnvironment.ApiIntegrationSqlEnvironmentVariable);

        if (!string.IsNullOrWhiteSpace(apiHost))
            return WithDatabaseName(apiHost.Trim(), databaseName);

        string? persistence = Environment.GetEnvironmentVariable(TestDatabaseEnvironment.PersistenceSqlEnvironmentVariable);

        if (!string.IsNullOrWhiteSpace(persistence))
            return WithDatabaseName(persistence.Trim(), databaseName);

        if (OperatingSystem.IsWindows())
        {
            SqlConnectionStringBuilder windowsLocal = new()
            {
                DataSource = "localhost",
                InitialCatalog = databaseName,
                IntegratedSecurity = true,
                TrustServerCertificate = true,
                MultipleActiveResultSets = true,
            };

            return windowsLocal.ConnectionString;
        }

        throw new InvalidOperationException(
            "API integration tests require SQL Server. On Linux or macOS set environment variable "
            + TestDatabaseEnvironment.PersistenceSqlEnvironmentVariable
            + " or "
            + TestDatabaseEnvironment.ApiIntegrationSqlEnvironmentVariable
            + " to a reachable instance (see docs/BUILD.md).");
    }

    /// <summary>
    /// Normalizes a persistence test connection string: ensures <c>TrustServerCertificate</c> and default catalog when missing.
    /// </summary>
    public static string NormalizePersistenceConnectionString(string raw, string defaultCatalog)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("Connection string is required.", nameof(raw));

        SqlConnectionStringBuilder builder = new(raw.Trim())
        {
            TrustServerCertificate = true,
        };

        if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
            builder.InitialCatalog = defaultCatalog;

        return builder.ConnectionString;
    }

    private static string WithDatabaseName(string templateConnectionString, string databaseName)
    {
        SqlConnectionStringBuilder builder = new(templateConnectionString.Trim())
        {
            TrustServerCertificate = true,
            InitialCatalog = databaseName,
            MultipleActiveResultSets = true,
        };

        return builder.ConnectionString;
    }
}
