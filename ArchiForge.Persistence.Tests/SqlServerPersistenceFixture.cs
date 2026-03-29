using ArchiForge.Data.Infrastructure;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Tests;

/// <summary>
/// Resolves a SQL Server connection (environment variable or Windows LocalDB), ensures the test catalog exists,
/// and applies embedded <see cref="DatabaseMigrator"/> scripts (same path as API startup on SQL Server).
/// </summary>
/// <remarks>
/// No Docker/Testcontainers dependency. When <see cref="EnvironmentConnectionStringVariable"/> is unset and LocalDB
/// is unavailable, <see cref="IsSqlServerAvailable"/> is false and SQL integration tests should skip
/// (see <c>Xunit.SkippableFact</c>). You can still filter with <c>dotnet test --filter "Category!=SqlServerContainer"</c>.
/// </remarks>
public sealed class SqlServerPersistenceFixture : IAsyncLifetime
{
    /// <summary>Database name used when the connection string omits Initial Catalog.</summary>
    public const string DefaultTestDatabaseName = "ArchiForgePersistenceTests";

    /// <summary>Full SQL Server connection string; when set, this is the only source tried and failures fail the fixture.</summary>
    public const string EnvironmentConnectionStringVariable = "ARCHIFORGE_SQL_TEST";

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
            await InitializeFromExplicitConnectionStringOrThrowAsync(NormalizeConnectionString(fromEnv.Trim()));
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
            await TryEnsureDatabaseExistsAsync(connectionString, CancellationToken.None);

            if (!DatabaseMigrator.Run(connectionString))
            {
                throw new InvalidOperationException(
                    "DbUp failed against SQL Server; see test output for script errors.");
            }

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

            await TryEnsureDatabaseExistsAsync(connectionString, CancellationToken.None);

            if (!DatabaseMigrator.Run(connectionString))
                return false;

            ConnectionString = connectionString;
            IsSqlServerAvailable = true;

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string NormalizeConnectionString(string raw)
    {
        SqlConnectionStringBuilder builder = new(raw)
        {
            TrustServerCertificate = true
        };

        if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
            builder.InitialCatalog = DefaultTestDatabaseName;

        return builder.ConnectionString;
    }

    /// <summary>
    /// Creates the target catalog on the instance when missing. Skips safely when the login cannot create databases
    /// (e.g. Azure SQL); migrations then fail with a clear error if the catalog does not exist.
    /// </summary>
    private static async Task TryEnsureDatabaseExistsAsync(string targetConnectionString, CancellationToken cancellationToken)
    {
        SqlConnectionStringBuilder target = new(targetConnectionString);

        if (string.IsNullOrWhiteSpace(target.InitialCatalog))
            throw new InvalidOperationException("Connection string must specify Initial Catalog after normalization.");

        string databaseName = target.InitialCatalog;
        target.InitialCatalog = "master";

        await using SqlConnection connection = new(target.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using SqlCommand command = new(
            """
            DECLARE @name sysname = @db;
            IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = @name)
            BEGIN
              DECLARE @sql nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@name);
              EXEC sys.sp_executesql @sql;
            END
            """,
            connection);

        SqlParameter dbParameter = command.Parameters.Add("@db", System.Data.SqlDbType.NVarChar, 128);
        dbParameter.Value = databaseName;

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException)
        {
            // Caller may still connect if the catalog was created out-of-band (Azure / restricted roles).
        }
    }
}
