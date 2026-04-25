namespace ArchLucid.TestSupport;

/// <summary>
///     Environment variable names for integration test SQL connectivity. Prefer these over hard-coded connection strings.
/// </summary>
public static class TestDatabaseEnvironment
{
    /// <summary>
    ///     Name of the environment variable whose value is the full ADO.NET connection string for
    ///     <strong>ArchLucid.Persistence.Tests</strong>
    ///     (includes <c>Initial Catalog</c>). When set, LocalDB fallback is skipped.
    /// </summary>
    public const string PersistenceSqlEnvironmentVariable = "ARCHLUCID_SQL_TEST";

    /// <summary>
    ///     Name of the environment variable for optional <strong>ArchLucid.Api.Tests</strong> SQL host (server + auth).
    ///     <c>Initial Catalog</c> is replaced per API test factory instance. When unset, the value of
    ///     <see cref="PersistenceSqlEnvironmentVariable" /> is reused (same server, ephemeral database name per factory).
    /// </summary>
    public const string ApiIntegrationSqlEnvironmentVariable = "ARCHLUCID_API_TEST_SQL";
}
