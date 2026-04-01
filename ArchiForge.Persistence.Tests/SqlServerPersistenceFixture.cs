using ArchiForge.Data.Infrastructure;
using ArchiForge.Persistence.Sql;
using ArchiForge.TestSupport;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Tests;

/// <summary>
/// Resolves a SQL Server connection (environment variable or Windows LocalDB), ensures the test catalog exists,
/// applies embedded <see cref="DatabaseMigrator"/> scripts (core Data-layer tables) and the <c>ArchiForge.sql</c>
/// schema bootstrap (Persistence-layer tables such as AuditEvents, ConversationThreads, ProvenanceSnapshots).
/// </summary>
/// <remarks>
/// No Docker/Testcontainers dependency in this fixture. When <see cref="EnvironmentConnectionStringVariable"/> is unset and LocalDB
/// is unavailable, <see cref="IsSqlServerAvailable"/> is false and SQL integration tests should skip
/// (see <c>Xunit.SkippableFact</c>). You can still filter with <c>dotnet test --filter "Category!=SqlServerContainer"</c>.
/// </remarks>
public sealed class SqlServerPersistenceFixture : IAsyncLifetime
{
    /// <summary>Database name used when the connection string omits Initial Catalog.</summary>
    public const string DefaultTestDatabaseName = "ArchiForgePersistenceTests";

    /// <summary>Full SQL Server connection string; when set, this is the only source tried and failures fail the fixture.</summary>
    public const string EnvironmentConnectionStringVariable = TestDatabaseEnvironment.PersistenceSqlEnvironmentVariable;

    /// <summary>Message passed to Xunit.SkippableFact <c>Skip</c> when no SQL Server could be reached without an explicit env connection string.</summary>
    public const string SqlServerUnavailableSkipReason =
        "No SQL Server for persistence tests (install LocalDB on Windows or set " + EnvironmentConnectionStringVariable
        + "). Or run: dotnet test --filter \"Category!=SqlServerContainer\".";

    /// <summary>True after a successful connection and schema migration.</summary>
    public bool IsSqlServerAvailable { get; private set; }

    /// <summary>Connection string after <see cref="InitializeAsync"/> when <see cref="IsSqlServerAvailable"/> is true.</summary>
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        string? fromEnv = Environment.GetEnvironmentVariable(EnvironmentConnectionStringVariable);

        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            await InitializeFromExplicitConnectionStringOrThrowAsync(
                SqlServerIntegrationTestConnections.NormalizePersistenceConnectionString(fromEnv.Trim(), DefaultTestDatabaseName));

            return;
        }

        if (await TryInitializeFromLocalDbAsync())
            return;

        IsSqlServerAvailable = false;
        ConnectionString = string.Empty;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task InitializeFromExplicitConnectionStringOrThrowAsync(string connectionString)
    {
        try
        {
            await SqlServerTestCatalogCommands.EnsureCatalogExistsAsync(connectionString, CancellationToken.None);

            if (!DatabaseMigrator.Run(connectionString))
            {
                throw new InvalidOperationException(
                    "DbUp failed against SQL Server; see test output for script errors.");
            }

            await RunSchemaBootstrapAsync(connectionString);

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

            if (!DatabaseMigrator.Run(connectionString))
                return false;

            await RunSchemaBootstrapAsync(connectionString);

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
    /// Applies the <c>ArchiForge.sql</c> schema bootstrap script which creates Persistence-layer
    /// tables (AuditEvents, ConversationThreads, ProvenanceSnapshots, etc.) not covered by DbUp migrations.
    /// All statements use <c>IF NOT EXISTS</c> so the script is idempotent.
    /// </summary>
    private static async Task RunSchemaBootstrapAsync(string connectionString)
    {
        string assemblyDir = Path.GetDirectoryName(typeof(SqlServerPersistenceFixture).Assembly.Location)!;
        string scriptPath = Path.Combine(assemblyDir, "Scripts", "ArchiForge.sql");

        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException(
                "ArchiForge.sql schema bootstrap script not found. Ensure the test project copies it to the output directory.",
                scriptPath);
        }

        SqlSchemaBootstrapper bootstrapper = new(
            new TestSqlConnectionFactory(connectionString),
            scriptPath);

        await bootstrapper.EnsureSchemaAsync(CancellationToken.None);
    }
}
