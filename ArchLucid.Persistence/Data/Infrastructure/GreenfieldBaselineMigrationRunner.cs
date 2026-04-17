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

    /// <summary>Resource name for the greenfield baseline script, or <c>null</c> if not embedded.</summary>
    public static string? GetBaselineMigrationResourceName()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        return assembly.GetManifestResourceNames()
            .FirstOrDefault(static n =>
                n.Contains(BaselineResourceSubstring, StringComparison.OrdinalIgnoreCase) &&
                n.EndsWith(".sql", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// When the catalog has no <c>001_InitialSchema</c> journal row, applies baseline SQL and stamps 001–050 in
    /// <c>dbo.SchemaVersions</c>. No-op when incremental history is already present.
    /// </summary>
    public static void TryApplyBaselineAndStampThrough050(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is required.", nameof(connectionString));

        if (!ShouldApplyBaseline(connectionString))
            return;

        string? baselineName = GetBaselineMigrationResourceName();
        if (baselineName is null)
            throw new InvalidOperationException("Greenfield baseline script is not embedded (expected Migrations/Baseline/*.sql).");

        Assembly assembly = Assembly.GetExecutingAssembly();
        string baselineSql = ReadEmbeddedScript(assembly, baselineName);
        IReadOnlyList<string> batches = SplitGoBatches(baselineSql);

        using SqlConnection connection = new(connectionString);
        connection.Open();

        using SqlTransaction tx = connection.BeginTransaction();
        try
        {
            foreach (string batch in batches)
            {
                if (string.IsNullOrWhiteSpace(batch))
                    continue;

                using SqlCommand batchCommand = new(batch, connection, tx);
                batchCommand.CommandTimeout = 0;
                batchCommand.ExecuteNonQuery();
            }

            EnsureSchemaVersionsTable(connection, tx);
            StampIncrementalScriptsThrough050(connection, tx);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private static bool ShouldApplyBaseline(string connectionString)
    {
        using SqlConnection connection = new(connectionString);
        connection.Open();

        using SqlCommand command = new(
            """
            IF OBJECT_ID(N'dbo.SchemaVersions', N'U') IS NULL
                SELECT CAST(1 AS bit) AS ShouldBaseline;
            ELSE IF EXISTS (
                SELECT 1 FROM dbo.SchemaVersions
                WHERE ScriptName LIKE N'%001_InitialSchema%')
                SELECT CAST(0 AS bit) AS ShouldBaseline;
            ELSE IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions)
                SELECT CAST(1 AS bit) AS ShouldBaseline;
            ELSE
                SELECT CAST(0 AS bit) AS ShouldBaseline;
            """,
            connection);

        object? scalar = command.ExecuteScalar();
        if (scalar is null || scalar is DBNull)
            return false;

        return Convert.ToBoolean(scalar, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void EnsureSchemaVersionsTable(SqlConnection connection, SqlTransaction tx)
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

    private static void StampIncrementalScriptsThrough050(SqlConnection connection, SqlTransaction tx)
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
