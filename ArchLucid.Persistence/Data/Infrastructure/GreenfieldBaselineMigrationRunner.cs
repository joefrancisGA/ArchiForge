using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Data.Infrastructure;

/// <summary>
/// On a brand-new SQL catalog, applies <c>Migrations/Baseline/000_Baseline_2026_04_17.sql</c> (union of
/// forward migrations 001–050) and stamps <c>dbo.SchemaVersions</c> so DbUp continues at 051+.
/// Existing catalogs that already ran <c>001_InitialSchema</c> are left on the incremental path.
/// </summary>
public static partial class GreenfieldBaselineMigrationRunner
{
    private const string BaselineResourceSubstring = ".Migrations.Baseline.";

    /// <summary>Returns embedded resource names for root <c>Migrations/NNN_*.sql</c> only (excludes Baseline folder).</summary>
    public static IReadOnlyList<string> GetOrderedIncrementalMigrationResourceNames()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        return assembly.GetManifestResourceNames()
            .Where(static n =>
                n.Contains(".Migrations.", StringComparison.OrdinalIgnoreCase) &&
                n.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) &&
                !n.Contains(BaselineResourceSubstring, StringComparison.OrdinalIgnoreCase))
            .OrderBy(static n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// When <c>dbo.SchemaVersions</c> does not yet record <c>001_InitialSchema</c>, applies baseline SQL and stamps
    /// 001–050 so DbUp continues at 051+. No-op once that journal row exists.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Shared CI catalogs (or a prior failed run) can leave <c>dbo.SchemaVersions</c> empty while <c>001_InitialSchema</c>
    /// objects already exist. Replaying 001 would raise "already an object named …". In that case we only stamp
    /// 001–050 so DbUp continues without re-executing DDL — but only when <c>dbo.AuditEvents</c> already exists (migration
    /// <c>035</c>). Otherwise we replay <c>035</c>–<c>050</c> first so later DbUp scripts (e.g. <c>060</c> indexes on
    /// <c>dbo.AuditEvents</c>) do not run against a missing table.
    /// </para>
    /// <para>
    /// A catalog can also have <b>non-empty</b> <c>dbo.SchemaVersions</c> (e.g. 051+ applied) <b>without</b> a
    /// <c>001_InitialSchema</c> row while physical <c>001</c> tables still exist (manual repair, forked CI, or journal
    /// drift). The legacy gate treated that as "no baseline" and skipped this runner entirely, letting DbUp re-run
    /// <c>001</c> and collide. We always enter here when <c>001</c> is missing from the journal, then branch on whether
    /// tenant tables already exist.
    /// </para>
    /// </remarks>
    public static void TryApplyBaselineAndStampThrough050(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is required.", nameof(connectionString));

        using SqlConnection connection = new(connectionString);
        connection.Open();

        if (SchemaVersionsJournalRecordsInitialSchema001(connection))
            return;

        Assembly assembly = Assembly.GetExecutingAssembly();

        if (TenantCoreTablesFromInitialMigrationExist(connection))
        {
            if (!DboAuditEventsTableExists(connection))
            {
                // Partial catalog: stamp-only would record 001–050 as applied without creating 035 targets (AuditEvents, …).
                ExecuteIncrementalMigrationScriptsInInclusiveRange(connection, assembly, 35, 50);
            }

            EnsureSchemaVersionsTable(connection, null);
            StampIncrementalScriptsThrough050(connection, null);

            return;
        }

        // Replay 001–050 from embedded incremental scripts (same SQL as `Migrations/Baseline/000_Baseline_2026_04_17.sql`,
        // which is kept for human review / optional tooling). We execute **per migration file** so `GO` lines inside
        // block comments in a concatenated mega-file cannot be mistaken for batch separators.
        ExecuteIncrementalMigrationScriptsInInclusiveRange(connection, assembly, 1, 50);

        EnsureSchemaVersionsTable(connection, null);
        StampIncrementalScriptsThrough050(connection, null);
    }

    /// <summary>Runs embedded incremental migrations whose script number is in <paramref name="minInclusive"/>–<paramref name="maxInclusive"/> (inclusive).</summary>
    private static void ExecuteIncrementalMigrationScriptsInInclusiveRange(
        SqlConnection connection,
        Assembly assembly,
        int minInclusive,
        int maxInclusive)
    {
        foreach (string resourceName in GetOrderedIncrementalMigrationResourceNames())
        {
            Match match = MigrationNumberRegex().Match(resourceName);
            if (!match.Success)
                continue;

            int n = int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            if (n < minInclusive || n > maxInclusive)
                continue;

            string sql = ReadEmbeddedScript(assembly, resourceName);
            IReadOnlyList<string> batches = SplitGoBatches(sql);

            foreach (string batch in batches)
            {
                if (string.IsNullOrWhiteSpace(batch))
                    continue;

                using SqlCommand batchCommand = new(batch, connection);
                batchCommand.CommandTimeout = 0;
                batchCommand.ExecuteNonQuery();
            }
        }
    }

    /// <summary>True when <c>dbo.AuditEvents</c> exists (created in <c>035_AuditProvenanceConversationTables</c>).</summary>
    private static bool DboAuditEventsTableExists(SqlConnection connection)
    {
        const string sql = """
            SELECT CASE WHEN OBJECT_ID(N'dbo.AuditEvents', N'U') IS NOT NULL THEN 1 ELSE 0 END;
            """;

        using SqlCommand command = new(sql, connection);
        object? scalar = command.ExecuteScalar();

        if (scalar is null || scalar is DBNull)
            return false;

        if (scalar is bool asBool)
            return asBool;

        return Convert.ToInt32(scalar, CultureInfo.InvariantCulture) != 0;
    }

    /// <summary>
    /// True when <c>001_InitialSchema</c> already created its primary tenant table — journal may be missing, empty, or
    /// inconsistent on a reused test catalog.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>001_InitialSchema.sql</c> uses <c>CREATE TABLE ArchitectureRequests</c> without a schema prefix, so the object
    /// lands in the <b>connection default schema</b> (<c>SCHEMA_NAME()</c>), often <c>dbo</c> but not guaranteed. Probing
    /// only <c>dbo.ArchitectureRequests</c> or only <c>sys.objects</c> with <c>type = N'U'</c> can miss real catalogs and
    /// replay <c>001</c>, producing &quot;already an object named …&quot;.
    /// </para>
    /// <para>
    /// Any non-system object named <c>ArchitectureRequests</c> in the caller default schema (table, view, synonym, …)
    /// blocks the same unqualified <c>CREATE</c> and is treated as &quot;tenant core already present&quot;.
    /// </para>
    /// </remarks>
    private static bool TenantCoreTablesFromInitialMigrationExist(SqlConnection connection)
    {
        const string sql = """
            SELECT CASE
                WHEN OBJECT_ID(N'dbo.ArchitectureRequests', N'U') IS NOT NULL THEN 1
                WHEN OBJECT_ID(QUOTENAME(SCHEMA_NAME()) + N'.ArchitectureRequests', N'U') IS NOT NULL THEN 1
                WHEN EXISTS (
                    SELECT 1
                    FROM sys.objects AS o
                    INNER JOIN sys.schemas AS s ON o.schema_id = s.schema_id
                    WHERE o.name = N'ArchitectureRequests'
                      AND s.name = SCHEMA_NAME()
                      AND o.is_ms_shipped = 0
                ) THEN 1
                ELSE 0
            END;
            """;

        using SqlCommand command = new(sql, connection);
        object? scalar = command.ExecuteScalar();

        if (scalar is null || scalar is DBNull)
            return false;

        if (scalar is bool asBool)
            return asBool;

        return Convert.ToInt32(scalar, CultureInfo.InvariantCulture) != 0;
    }

    /// <summary>True when <c>dbo.SchemaVersions</c> exists and records <c>001_InitialSchema</c> (DbUp script name).</summary>
    private static bool SchemaVersionsJournalRecordsInitialSchema001(SqlConnection connection)
    {
        const string sql = """
            IF OBJECT_ID(N'dbo.SchemaVersions', N'U') IS NULL
                SELECT CAST(0 AS bit);
            ELSE IF EXISTS (
                SELECT 1
                FROM dbo.SchemaVersions
                WHERE ScriptName LIKE N'%001_InitialSchema%')
                SELECT CAST(1 AS bit);
            ELSE
                SELECT CAST(0 AS bit);
            """;

        using SqlCommand command = new(sql, connection);
        object? scalar = command.ExecuteScalar();
        if (scalar is null || scalar is DBNull)
            return false;

        return Convert.ToBoolean(scalar, CultureInfo.InvariantCulture);
    }

    private static void EnsureSchemaVersionsTable(SqlConnection connection, SqlTransaction? tx)
    {
        const string ddl = """
IF OBJECT_ID(N'dbo.SchemaVersions', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SchemaVersions] (
        [Id] [int] IDENTITY(1,1) NOT NULL CONSTRAINT [PK_SchemaVersions_Id] PRIMARY KEY CLUSTERED,
        [ScriptName] [nvarchar](255) NOT NULL,
        [Applied] [datetime] NOT NULL
    );
END
""";

        using SqlCommand command = new(ddl, connection, tx);
        command.ExecuteNonQuery();
    }

    private static void StampIncrementalScriptsThrough050(SqlConnection connection, SqlTransaction? tx)
    {
        IReadOnlyList<string> incremental = GetOrderedIncrementalMigrationResourceNames();
        Regex numberRegex = MigrationNumberRegex();

        foreach (string resourceName in incremental)
        {
            Match match = numberRegex.Match(resourceName);
            if (!match.Success)
                continue;

            int n = int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            if (n < 1 || n > 50)
                continue;

            using SqlCommand stamp = new(
                """
                IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE ScriptName = @ScriptName)
                    INSERT INTO dbo.SchemaVersions (ScriptName, Applied) VALUES (@ScriptName, SYSUTCDATETIME());
                """,
                connection,
                tx);
            stamp.Parameters.AddWithValue("@ScriptName", resourceName);
            stamp.ExecuteNonQuery();
        }
    }

    [GeneratedRegex(@"\.Migrations\.(\d{3})_", RegexOptions.CultureInvariant)]
    private static partial Regex MigrationNumberRegex();

    private static string ReadEmbeddedScript(Assembly assembly, string name)
    {
        using Stream? stream = assembly.GetManifestResourceStream(name);
        if (stream is null)
            throw new InvalidOperationException($"Missing embedded migration script '{name}'.");

        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    private static IReadOnlyList<string> SplitGoBatches(string script)
    {
        string[] lines = script.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        List<string> batches = [];
        List<string> current = [];

        foreach (string line in lines)
        {
            if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
            {
                batches.Add(string.Join(Environment.NewLine, current));
                current.Clear();
            }
            else
            {
                current.Add(line);
            }
        }

        if (current.Count > 0)
            batches.Add(string.Join(Environment.NewLine, current));

        return batches;
    }
}
