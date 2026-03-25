using System.Reflection;

using DbUp;
using DbUp.Engine;

namespace ArchiForge.Data.Infrastructure;

/// <summary>
/// Runs SQL Server database migrations using DbUp.
/// Skips execution when the connection string points to SQLite (e.g. in-memory tests).
/// </summary>
public static class DatabaseMigrator
{
    public static bool Run(string connectionString)
    {
        if (IsSqliteConnection(connectionString))
            return true;

        UpgradeEngine? upgrader = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                Assembly.GetExecutingAssembly(),
                s => s.EndsWith(".sql") && s.Contains("Migrations", StringComparison.OrdinalIgnoreCase))
            .WithTransaction()
            .LogToConsole()
            .Build();

        DatabaseUpgradeResult? result = upgrader.PerformUpgrade();
        return result.Successful;
    }

    private static bool IsSqliteConnection(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return false;

        ReadOnlySpan<char> cs = connectionString.AsSpan().Trim();
        return cs.StartsWith("Data Source=file:", StringComparison.OrdinalIgnoreCase) ||
               cs.StartsWith("Data Source=:memory:", StringComparison.OrdinalIgnoreCase);
    }
}
