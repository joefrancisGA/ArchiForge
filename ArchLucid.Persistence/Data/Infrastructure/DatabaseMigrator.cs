using System.Reflection;

using DbUp;
using DbUp.Engine;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Data.Infrastructure;

/// <summary>
///     Runs SQL Server database migrations using DbUp.
/// </summary>
/// <remarks>
///     Embedded scripts are limited to resources whose name contains <c>.Migrations.</c> and end with <c>.sql</c>,
///     so ad-hoc SQL (e.g. consolidated reference scripts) is never applied by DbUp. Ordering is lexicographic on
///     the full resource name; use <c>NNN_Name.sql</c> filenames under <c>Migrations/</c> (see unit tests).
///     <para>
///         <see cref="Run" /> / <see cref="RunExcludingTrailingScripts" /> take a process-wide mutex keyed by the
///         connection string so
///         parallel <c>dotnet test</c> processes (e.g. Persistence + Api integration) do not run greenfield / DbUp against
///         the same
///         catalog concurrently — that can duplicate FK hardening from <c>025_FindingsSnapshotRelational.sql</c> and
///         similar scripts.
///     </para>
/// </remarks>
public static class DatabaseMigrator
{
    private static readonly TimeSpan MigrationRunMutexWait = TimeSpan.FromMinutes(10);

    /// <summary>
    ///     Applies all embedded migration scripts to the SQL Server database.
    /// </summary>
    /// <exception cref="InvalidOperationException">When DbUp reports a failed upgrade (inner exception has provider details).</exception>
    public static void Run(string connectionString)
    {
        string secured = SqlConnectionStringSecurity.EnsureSqlClientEncryptMandatory(connectionString);
        using (MigrationCatalogMutexScope.Acquire(secured, MigrationRunMutexWait))
        {
            GreenfieldBaselineMigrationRunner.TryApplyBaselineAndStampThrough050(secured);
            RunWithScriptFilter(secured, static _ => true);
            TryEnableReadCommittedSnapshotIfNeeded(secured);
        }
    }

    /// <summary>
    ///     Applies embedded migrations in order, excluding the last <paramref name="trailingScriptCountToSkip" /> scripts
    ///     (upgrade-from-N-1 CI path).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">When skip count is not between 1 and script count - 1.</exception>
    /// <exception cref="InvalidOperationException">When DbUp reports a failed upgrade (inner exception has provider details).</exception>
    public static void RunExcludingTrailingScripts(string connectionString, int trailingScriptCountToSkip)
    {
        IReadOnlyList<string> ordered = GetOrderedMigrationResourceNames();

        if (trailingScriptCountToSkip <= 0 || trailingScriptCountToSkip >= ordered.Count)

            throw new ArgumentOutOfRangeException(
                nameof(trailingScriptCountToSkip),
                "Must be at least 1 and less than the total migration script count.");


        string secured = SqlConnectionStringSecurity.EnsureSqlClientEncryptMandatory(connectionString);
        using (MigrationCatalogMutexScope.Acquire(secured, MigrationRunMutexWait))
        {
            GreenfieldBaselineMigrationRunner.TryApplyBaselineAndStampThrough050(secured);

            HashSet<string> allowed = ordered.Take(ordered.Count - trailingScriptCountToSkip)
                .ToHashSet(StringComparer.Ordinal);
            RunWithScriptFilter(secured, allowed.Contains);
            TryEnableReadCommittedSnapshotIfNeeded(secured);
        }
    }

    /// <summary>Ordered embedded migration resource names (same order DbUp uses).</summary>
    /// <remarks>
    ///     Excludes <c>Migrations/Baseline/</c>; baseline is applied via <see cref="GreenfieldBaselineMigrationRunner" />
    ///     on empty catalogs.
    /// </remarks>
    public static IReadOnlyList<string> GetOrderedMigrationResourceNames()
    {
        return GreenfieldBaselineMigrationRunner.GetOrderedIncrementalMigrationResourceNames();
    }

    private static void RunWithScriptFilter(string connectionString, Func<string, bool> includeScript)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        List<SqlScript> scripts = assembly.GetManifestResourceNames()
            .Where(static n =>
                n.Contains(".Migrations.", StringComparison.OrdinalIgnoreCase) &&
                n.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) &&
                !n.Contains(".Migrations.Baseline.", StringComparison.OrdinalIgnoreCase))
            .Where(includeScript)
            .OrderBy(static n => n, StringComparer.OrdinalIgnoreCase)
            .Select(name => new SqlScript(name, ReadEmbeddedScript(assembly, name)))
            .ToList();

        // Per-script transactions: a single transaction across all embedded scripts breaks SQL Server when later
        // migrations include security-policy / RLS DDL and other statements that do not compose in one long transaction.
        // Migration 091 (RCSI) is a no-op here; see TryEnableReadCommittedSnapshotIfNeeded (ALTER DATABASE cannot run in DbUp's transaction).
        UpgradeEngine upgrader = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScripts(scripts)
            .WithTransactionPerScript()
            .LogToConsole()
            .Build();

        DatabaseUpgradeResult result = upgrader.PerformUpgrade();

        if (result.Successful)
            return;

        string detail = result.Error?.Message ?? "(no exception on DatabaseUpgradeResult)";

        Console.Error.WriteLine("DbUp migration failed: " + detail);

        throw new InvalidOperationException(
            "Database migration failed. See inner exception for the SQL Server error. DbUp message: " + detail,
            result.Error);
    }

    /// <summary>
    ///     Enables READ_COMMITTED_SNAPSHOT outside DbUp: <c>ALTER DATABASE</c> is rejected inside DbUp's per-script
    ///     transaction.
    /// </summary>
    private static void TryEnableReadCommittedSnapshotIfNeeded(string connectionString)
    {
        // Caller passes a string that already has Encrypt=Mandatory; normalize defensively (public migrator enforces it).
        string secured = SqlConnectionStringSecurity.EnsureSqlClientEncryptMandatory(connectionString);
        using SqlConnection connection = new(secured);
        connection.Open();

        const string sql = """
                           IF NOT EXISTS (
                               SELECT 1
                               FROM sys.databases
                               WHERE database_id = DB_ID()
                                 AND is_read_committed_snapshot_on = 1)
                           BEGIN
                               ALTER DATABASE CURRENT SET READ_COMMITTED_SNAPSHOT ON;
                           END
                           """;

        using SqlCommand command = new(sql, connection);
        command.ExecuteNonQuery();
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
