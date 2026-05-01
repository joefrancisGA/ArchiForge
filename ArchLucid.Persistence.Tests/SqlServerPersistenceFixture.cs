using ArchLucid.Persistence.Data.Infrastructure;
using ArchLucid.Persistence.Sql;
using ArchLucid.TestSupport;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests;

/// <summary>
///     Resolves a SQL Server connection (environment variable or Windows LocalDB), ensures the test catalog exists,
///     applies embedded <see cref="DatabaseMigrator" /> scripts (core Data-layer tables) and a minimal SQL supplement
///     for persistence-only tables (AuditEvents, ConversationThreads, ProvenanceSnapshots) that DbUp omits.
/// </summary>
/// <remarks>
///     No Docker/Testcontainers dependency in this fixture. When <see cref="EnvironmentConnectionStringVariable" /> is
///     unset and LocalDB
///     is unavailable, <see cref="IsSqlServerAvailable" /> is false and SQL integration tests should skip
///     (see <see cref="Xunit.Skip" />.<c>IfNot</c> with <c>[SkippableFact]</c> from Xunit.SkippableFact). You can still filter with
///     <c>dotnet test --filter "Category!=SqlServerContainer"</c>.
/// </remarks>
public sealed class SqlServerPersistenceFixture : IAsyncLifetime
{
    /// <summary>Database name used when the connection string omits Initial Catalog.</summary>
    public const string DefaultTestDatabaseName = "ArchLucidPersistenceTests";

    /// <summary>Full SQL Server connection string; when set, this is the only source tried and failures fail the fixture.</summary>
    public const string EnvironmentConnectionStringVariable = TestDatabaseEnvironment.PersistenceSqlEnvironmentVariable;

    /// <summary>
    ///     Message passed to <see cref="Xunit.Skip" />.<c>IfNot</c> when no SQL Server could be reached without an explicit env
    ///     connection string.
    /// </summary>
    public const string SqlServerUnavailableSkipReason =
        "No SQL Server for persistence tests (install LocalDB on Windows or set " + EnvironmentConnectionStringVariable
        + "). Or run: dotnet test --filter \"Category!=SqlServerContainer\".";

    /// <summary>True after a successful connection and schema migration.</summary>
    public bool IsSqlServerAvailable
    {
        get;
        private set;
    }

    /// <summary>Connection string after <see cref="InitializeAsync" /> when <see cref="IsSqlServerAvailable" /> is true.</summary>
    public string ConnectionString
    {
        get;
        private set;
    } = string.Empty;

    public async Task InitializeAsync()
    {
        string? fromEnv = Environment.GetEnvironmentVariable(EnvironmentConnectionStringVariable);

        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            await InitializeFromExplicitConnectionStringOrThrowAsync(
                SqlServerIntegrationTestConnections.NormalizePersistenceConnectionString(fromEnv.Trim(),
                    DefaultTestDatabaseName));

            return;
        }

        if (await TryInitializeFromLocalDbAsync())
            return;

        IsSqlServerAvailable = false;
        ConnectionString = string.Empty;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private async Task InitializeFromExplicitConnectionStringOrThrowAsync(string connectionString)
    {
        try
        {
            await SqlServerTestCatalogCommands.EnsureCatalogExistsAsync(connectionString, CancellationToken.None);

            DatabaseMigrator.Run(connectionString);

            await RunPersistenceContractSupplementAsync(connectionString);

            ConnectionString = connectionString;
            IsSqlServerAvailable = true;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                "SQL persistence tests require a reachable server when " + EnvironmentConnectionStringVariable
                                                                         + " is set. See inner exception.",
                ex);
        }
    }

    private async Task<bool> TryInitializeFromLocalDbAsync()
    {
        try
        {
            SqlConnectionStringBuilder localDb = new()
            {
                DataSource = "(localdb)\\mssqllocaldb",
                InitialCatalog = DefaultTestDatabaseName,
                IntegratedSecurity = true,
                TrustServerCertificate = true
            };

            string connectionString = localDb.ConnectionString;

            await SqlServerTestCatalogCommands.EnsureCatalogExistsAsync(connectionString, CancellationToken.None);

            DatabaseMigrator.Run(connectionString);

            await RunPersistenceContractSupplementAsync(connectionString);

            ConnectionString = connectionString;
            IsSqlServerAvailable = true;

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    ///     Applies <c>PersistenceContractSupplement.sql</c>: tables DbUp does not create, without the
    ///     FK-hardening blocks from full <c>ArchLucid.sql</c> (those require seeded authority-chain rows).
    /// </summary>
    private static async Task RunPersistenceContractSupplementAsync(string connectionString)
    {
        string assemblyDir = Path.GetDirectoryName(typeof(SqlServerPersistenceFixture).Assembly.Location)!;
        string scriptPath = Path.Combine(assemblyDir, "Scripts", "PersistenceContractSupplement.sql");

        if (!File.Exists(scriptPath))

            throw new FileNotFoundException(
                "PersistenceContractSupplement.sql not found. Ensure the test project copies Scripts to the output directory.",
                scriptPath);


        SqlSchemaBootstrapper bootstrapper = new(
            new TestSqlConnectionFactory(connectionString),
            scriptPath);

        await bootstrapper.EnsureSchemaAsync(CancellationToken.None);
    }
}
