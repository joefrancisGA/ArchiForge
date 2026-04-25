using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Data.Infrastructure;

/// <summary>
///     On a brand-new SQL catalog, applies <c>Migrations/Baseline/000_Baseline_2026_04_17.sql</c> (union of
///     forward migrations 001–050) and stamps <c>dbo.SchemaVersions</c> so DbUp continues at 051+.
///     Existing catalogs that already ran <c>001_InitialSchema</c> are left on the incremental path.
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
    ///     When <c>dbo.SchemaVersions</c> does not yet record <c>001_InitialSchema</c>, applies baseline SQL and stamps
    ///     001–050 so DbUp continues at 051+. No-op once that journal row exists.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Shared CI catalogs (or a prior failed run) can leave <c>dbo.SchemaVersions</c> empty while
    ///         <c>001_InitialSchema</c>
    ///         objects already exist. Replaying 001 would raise "already an object named …". In that case we only stamp
    ///         001–050 so DbUp continues without re-executing DDL — but only when <c>dbo.AuditEvents</c> already exists
    ///         (migration
    ///         <c>035</c>). Otherwise we replay incremental scripts through <c>050</c> first: from <c>035</c> when
    ///         <c>dbo.Runs</c>
    ///         exists, or from <c>017</c> when it does not (035–050 assume <c>dbo.Runs</c> from
    ///         <c>017_GraphSnapshots_ParentTables</c>)
    ///         so later DbUp scripts (e.g. <c>060</c> indexes on <c>dbo.AuditEvents</c>) do not run against a missing table.
    ///     </para>
    ///     <para>
    ///         A catalog can also have <b>non-empty</b> <c>dbo.SchemaVersions</c> (e.g. 051+ applied) <b>without</b> a
    ///         <c>001_InitialSchema</c> row while physical <c>001</c> tables still exist (manual repair, forked CI, or journal
    ///         drift). The legacy gate treated that as "no baseline" and skipped this runner entirely, letting DbUp re-run
    ///         <c>001</c> and collide. We always enter here when <c>001</c> is missing from the journal, then branch on
    ///         whether
    ///         tenant tables already exist.
    ///     </para>
    ///     <para>
    ///         If pre-flight detection still misses, replaying <c>001</c> can raise a duplicate-object error for
    ///         <c>ArchitectureRequests</c> (or <c>017_GovernanceWorkflow</c> for <c>GovernanceApprovalRequests</c> and related
    ///         tables);
    ///         that case is caught and repaired with the same stamp / optional <c>017</c>–<c>050</c>
    ///         or <c>035</c>–<c>050</c> replay (depending on <c>dbo.Runs</c>) as the tenant-exists branch.
    ///         Embedded names sort lexicographically, so <c>017_GovernanceWorkflow</c> runs before
    ///         <c>017_GraphSnapshots_ParentTables</c>;
    ///         when governance tables already exist, that file is skipped during replay so the graph parent script can still
    ///         run.
    ///     </para>
    /// </remarks>
    public static void TryApplyBaselineAndStampThrough050(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is required.", nameof(connectionString));

        // Mandatory TLS: CodeQL cs/insecure-sql-connection; see SqlConnectionStringSecurity.
        string secured = SqlConnectionStringSecurity.EnsureSqlClientEncryptMandatory(connectionString);
        using SqlConnection connection = new(secured);
        connection.Open();

        if (SchemaVersionsJournalRecordsInitialSchema001(connection))
            return;

        Assembly assembly = Assembly.GetExecutingAssembly();

        if (TenantCoreTablesFromInitialMigrationExist(connection) || GovernanceWorkflow017TablesExist(connection))
        {
            StampThrough050OrReplay035IfAuditMissingThenStamp(connection, assembly);

            return;
        }

        // Replay 001–050 from embedded incremental scripts (same SQL as `Migrations/Baseline/000_Baseline_2026_04_17.sql`,
        // which is kept for human review / optional tooling). We execute **per migration file** so `GO` lines inside
        // block comments in a concatenated mega-file cannot be mistaken for batch separators.
        try
        {
            ExecuteIncrementalMigrationScriptsInInclusiveRange(connection, assembly, 1, 50);
        }
        catch (SqlException ex) when (IsKnownDuplicateInitialMigrationTable(ex) ||
                                      IsKnownDuplicateBaselineConstraintName(ex))
        {
            StampThrough050OrReplay035IfAuditMissingThenStamp(connection, assembly);

            return;
        }

        EnsureSchemaVersionsTable(connection, null);
        StampIncrementalScriptsThrough050(connection, null);
    }

    /// <summary>
    ///     Stamps 001–050 into <c>dbo.SchemaVersions</c> and replays incremental scripts when <c>dbo.AuditEvents</c> is
    ///     missing
    ///     (partial catalog repair). Replays from <b>017</b> when <c>dbo.Runs</c> is absent — migrations in the 035–050 range
    ///     (e.g. <c>039_RowVersion</c>, <c>036_Rls</c>) assume <c>dbo.Runs</c> exists (created in
    ///     <c>017_GraphSnapshots_ParentTables</c>).
    /// </summary>
    private static void StampThrough050OrReplay035IfAuditMissingThenStamp(SqlConnection connection, Assembly assembly)
    {
        if (!DboAuditEventsTableExists(connection))
        {
            // Partial catalog: stamp-only would record 001–050 as applied without creating 035 targets (AuditEvents, …).
            // Never start at 035 if dbo.Runs is missing — 039+ ALTER dbo.Runs and 036 binds RLS on dbo.Runs.
            int minInclusive = DboRunsTableExists(connection) ? 35 : 17;

            try
            {
                ExecuteIncrementalMigrationScriptsInInclusiveRange(connection, assembly, minInclusive, 50);
            }
            catch (SqlException ex) when (IsKnownDuplicateInitialMigrationTable(ex) ||
                                          IsKnownDuplicateBaselineConstraintName(ex))
            {
                // Objects from 017–050 (including 027 FK hardening) may already exist while SchemaVersions is empty;
                // fall through to stamp so DbUp does not re-execute the same DDL.
            }
        }

        EnsureSchemaVersionsTable(connection, null);
        StampIncrementalScriptsThrough050(connection, null);
    }

    /// <summary>
    ///     SQL Server duplicate-object on <c>CREATE TABLE</c> for tables introduced in <c>001</c> or
    ///     <c>017_GovernanceWorkflow</c>
    ///     (error 2714 / "already an object named …") — repaired like the tenant-present path.
    /// </summary>
    internal static bool IsKnownDuplicateInitialMigrationTable(SqlException ex)
    {
        if (ex is null)
            return false;

        return IsKnownDuplicateInitialMigrationTable(ex.Message, ex.Number);
    }

    /// <summary>
    ///     Drift repair: replaying <c>027_ArtifactBundleRelational.sql</c> / <c>025_FindingsSnapshotRelational.sql</c> /
    ///     related FK
    ///     hardening on catalogs where the FK already exists (journal drift, shared CI DB, parallel <c>dotnet test</c> before
    ///     the
    ///     <see cref="DatabaseMigrator" /> catalog mutex, or tooling that applied <c>ArchLucid.sql</c> fragments) can raise
    ///     duplicate
    ///     constraint name even when historical migration <c>001–028</c> must not be edited — treat like other baseline
    ///     duplicate-object cases.
    /// </summary>
    internal static bool IsKnownDuplicateBaselineConstraintName(SqlException ex)
    {
        if (ex is null)
            return false;

        return IsKnownDuplicateBaselineConstraintName(ex.Message);
    }

    /// <summary>Test seam for <see cref="IsKnownDuplicateBaselineConstraintName(SqlException)" />.</summary>
    internal static bool IsKnownDuplicateBaselineConstraintName(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        if (!IsKnownDuplicateBaselineConstraintMessage(message))
            return false;

        if (message.Contains("already an object named", StringComparison.OrdinalIgnoreCase))
            return true;

        return message.Contains("Could not create constraint", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Duplicate FK names from <c>027</c>-style hardening, <c>025_FindingsSnapshotRelational</c>, and
    ///     <c>026_GoldenManifestPhase1Relational</c>
    ///     when journals drift or two processes race before the <see cref="DatabaseMigrator" /> catalog mutex lands.
    /// </summary>
    private static bool IsKnownDuplicateBaselineConstraintMessage(string message)
    {
        if (message.Contains("FK_ArtifactBundles_GoldenManifests_ManifestId", StringComparison.OrdinalIgnoreCase)
            || message.Contains("FK_ArtifactBundles_Runs_RunId", StringComparison.OrdinalIgnoreCase))

            return true;


        if (message.Contains("FK_FindingsSnapshots_", StringComparison.OrdinalIgnoreCase))
            return true;

        if (message.Contains("FK_GoldenManifests_FindingsSnapshots_FindingsSnapshotId",
                StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    /// <summary>
    ///     Test seam: same predicate as <see cref="IsKnownDuplicateInitialMigrationTable(SqlException)" /> without
    ///     constructing <see cref="SqlException" />.
    /// </summary>
    internal static bool IsKnownDuplicateInitialMigrationTable(string message, int errorNumber)
    {
        if (string.IsNullOrEmpty(message))
            return false;

        if (!ContainsAnyKnownBaselineDuplicateTableName(message))
            return false;

        if (message.Contains("already an object named", StringComparison.OrdinalIgnoreCase))
            return true;

        // SQL Server: "There is already an object named '…' in the database."
        return errorNumber == 2714;
    }

    private static bool ContainsAnyKnownBaselineDuplicateTableName(string message)
    {
        return message.Contains("ArchitectureRequests", StringComparison.OrdinalIgnoreCase)
               || message.Contains("GovernanceApprovalRequests", StringComparison.OrdinalIgnoreCase)
               || message.Contains("GovernancePromotionRecords", StringComparison.OrdinalIgnoreCase)
               || message.Contains("GovernanceEnvironmentActivations", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Runs embedded incremental migrations whose script number is in <paramref name="minInclusive" />–
    ///     <paramref name="maxInclusive" /> (inclusive).
    /// </summary>
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

            int n = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            if (n < minInclusive || n > maxInclusive)
                continue;

            if (ShouldSkipEmbeddedMigrationResourceAlreadyApplied(connection, resourceName))
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

    /// <summary>True when <c>dbo.Runs</c> exists (created in <c>017_GraphSnapshots_ParentTables</c>).</summary>
    private static bool DboRunsTableExists(SqlConnection connection)
    {
        const string sql = """
                           SELECT CASE WHEN OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL THEN 1 ELSE 0 END;
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
    ///     True when <c>017_GovernanceWorkflow.sql</c> objects already exist (unqualified <c>CREATE TABLE</c>; same drift
    ///     cases as <c>001</c>).
    ///     Any of the three workflow tables blocks a full replay of that script.
    /// </summary>
    /// <remarks>
    ///     CI catalogs can place objects outside <c>dbo</c> or the session default schema; probing only <c>dbo</c> +
    ///     <c>SCHEMA_NAME()</c>
    ///     misses them and replays <c>017_GovernanceWorkflow</c>, producing duplicate <c>GovernanceApprovalRequests</c>.
    /// </remarks>
    private static bool GovernanceWorkflow017TablesExist(SqlConnection connection)
    {
        const string sql = """
                           SELECT CASE WHEN EXISTS (
                               SELECT 1
                               FROM sys.tables AS t
                               WHERE t.name IN (
                                   N'GovernanceApprovalRequests',
                                   N'GovernancePromotionRecords',
                                   N'GovernanceEnvironmentActivations')
                                 AND t.is_ms_shipped = 0
                           ) THEN 1 ELSE 0 END;
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
    ///     Two <c>017_*.sql</c> files sort lexicographically: <c>017_GovernanceWorkflow</c> before
    ///     <c>017_GraphSnapshots_ParentTables</c>.
    ///     The workflow script is not idempotent; skip it when its tables already exist so replay can still apply graph
    ///     parents.
    /// </summary>
    private static bool ShouldSkipEmbeddedMigrationResourceAlreadyApplied(SqlConnection connection, string resourceName)
    {
        if (!resourceName.Contains("017_GovernanceWorkflow", StringComparison.OrdinalIgnoreCase))
            return false;

        return GovernanceWorkflow017TablesExist(connection);
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
    ///     True when <c>001_InitialSchema</c> already created its primary tenant table — journal may be missing, empty, or
    ///     inconsistent on a reused test catalog.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <c>001_InitialSchema.sql</c> uses <c>CREATE TABLE ArchitectureRequests</c> without a schema prefix, so the
    ///         object
    ///         lands in the <b>connection default schema</b> (<c>SCHEMA_NAME()</c>), often <c>dbo</c> but not guaranteed.
    ///         Probing
    ///         only <c>dbo.ArchitectureRequests</c> or only <c>sys.objects</c> with <c>type = N'U'</c> can miss real catalogs
    ///         and
    ///         replay <c>001</c>, producing &quot;already an object named …&quot;.
    ///     </para>
    ///     <para>
    ///         Any non-system object named <c>ArchitectureRequests</c> in the caller default schema (table, view, synonym, …)
    ///         blocks the same unqualified <c>CREATE</c> and is treated as &quot;tenant core already present&quot;.
    ///     </para>
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
                               WHEN EXISTS (
                                   SELECT 1
                                   FROM sys.tables AS t
                                   WHERE t.name = N'ArchitectureRequests'
                                     AND t.is_ms_shipped = 0
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

            int n = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
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

            if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
            {
                batches.Add(string.Join(Environment.NewLine, current));
                current.Clear();
            }
            else

                current.Add(line);


        if (current.Count > 0)
            batches.Add(string.Join(Environment.NewLine, current));

        return batches;
    }
}
