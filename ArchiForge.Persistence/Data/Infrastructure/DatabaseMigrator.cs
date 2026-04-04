using System.Reflection;

using DbUp;
using DbUp.Engine;

namespace ArchiForge.Persistence.Data.Infrastructure;

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
    public static bool Run(string connectionString) =>
        RunWithScriptFilter(connectionString, static _ => true);

    /// <summary>
    /// Applies embedded migrations in order, excluding the last <paramref name="trailingScriptCountToSkip"/> scripts (upgrade-from-N-1 CI path).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">When skip count is not between 1 and script count - 1.</exception>
    public static bool RunExcludingTrailingScripts(string connectionString, int trailingScriptCountToSkip)
    {
        IReadOnlyList<string> ordered = GetOrderedMigrationResourceNames();

        if (trailingScriptCountToSkip <= 0 || trailingScriptCountToSkip >= ordered.Count)
        
            throw new ArgumentOutOfRangeException(
                nameof(trailingScriptCountToSkip),
                "Must be at least 1 and less than the total migration script count.");
        

        HashSet<string> allowed = ordered.Take(ordered.Count - trailingScriptCountToSkip).ToHashSet(StringComparer.Ordinal);
        return RunWithScriptFilter(connectionString, allowed.Contains);
    }

    /// <summary>Ordered embedded migration resource names (same order DbUp uses).</summary>
    public static IReadOnlyList<string> GetOrderedMigrationResourceNames()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        return assembly.GetManifestResourceNames()
            .Where(static n =>
                n.Contains(".Migrations.", StringComparison.OrdinalIgnoreCase) &&
                n.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(static n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool RunWithScriptFilter(string connectionString, Func<string, bool> includeScript)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        List<SqlScript> scripts = assembly.GetManifestResourceNames()
            .Where(static n =>
                n.Contains(".Migrations.", StringComparison.OrdinalIgnoreCase) &&
                n.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .Where(includeScript)
            .OrderBy(static n => n, StringComparer.OrdinalIgnoreCase)
            .Select(name => new SqlScript(name, ReadEmbeddedScript(assembly, name)))
            .ToList();

        UpgradeEngine upgrader = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScripts(scripts)
            .WithTransaction()
            .LogToConsole()
            .Build();

        DatabaseUpgradeResult result = upgrader.PerformUpgrade();
        return result.Successful;
    }

    private static string ReadEmbeddedScript(Assembly assembly, string name)
    {
        using Stream? stream = assembly.GetManifestResourceStream(name);
        if (stream is null)
            throw new InvalidOperationException($"Missing embedded migration script '{name}'.");

        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
