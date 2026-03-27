using System.Reflection;

using DbUp;
using DbUp.Engine;

using Microsoft.Data.Sqlite;

namespace ArchiForge.Data.Infrastructure;

/// <summary>
/// Runs SQL Server database migrations using DbUp.
/// Skips execution when the connection string targets SQLite (integration tests or file-backed SQLite).
/// </summary>
/// <remarks>
/// Embedded scripts are limited to resources whose name contains <c>.Migrations.</c> and end with <c>.sql</c>,
/// so ad-hoc SQL (e.g. consolidated reference scripts) is never applied by DbUp. Ordering is lexicographic on
/// the full resource name; use <c>NNN_Name.sql</c> filenames under <c>Migrations/</c> (see unit tests).
/// </remarks>
public static class DatabaseMigrator
{
    /// <summary>
    /// Applies all embedded migration scripts to the SQL Server database. No-op (returns <c>true</c>) for SQLite.
    /// </summary>
    public static bool Run(string connectionString)
    {
        if (IsSqliteConnection(connectionString))
        {
            return true;
        }

        UpgradeEngine upgrader = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                Assembly.GetExecutingAssembly(),
                static scriptName =>
                    scriptName.Contains(".Migrations.", StringComparison.OrdinalIgnoreCase) &&
                    scriptName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .WithTransaction()
            .LogToConsole()
            .Build();

        DatabaseUpgradeResult result = upgrader.PerformUpgrade();
        return result.Successful;
    }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="connectionString"/> is intended for SQLite, not SQL Server.
    /// </summary>
    public static bool IsSqliteConnection(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        if (connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            SqliteConnectionStringBuilder builder = new(connectionString);
            string ds = builder.DataSource.Trim();

            if (string.IsNullOrEmpty(ds))
            {
                return false;
            }

            if (ds.Contains(":memory:", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (ds.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (ds.EndsWith(".db", StringComparison.OrdinalIgnoreCase) ||
                ds.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
