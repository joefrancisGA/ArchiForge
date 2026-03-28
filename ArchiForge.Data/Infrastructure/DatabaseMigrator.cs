using System.Reflection;

using DbUp;
using DbUp.Engine;

namespace ArchiForge.Data.Infrastructure;

/// <summary>
/// Runs SQL Server database migrations using DbUp.
/// </summary>
/// <remarks>
/// Embedded scripts are limited to resources whose name contains <c>.Migrations.</c> and end with <c>.sql</c>,
/// so ad-hoc SQL (e.g. consolidated reference scripts) is never applied by DbUp. Ordering is lexicographic on
/// the full resource name; use <c>NNN_Name.sql</c> filenames under <c>Migrations/</c> (see unit tests).
/// </remarks>
public static class DatabaseMigrator
{
    /// <summary>
    /// Applies all embedded migration scripts to the SQL Server database.
    /// </summary>
    public static bool Run(string connectionString)
    {
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
}
