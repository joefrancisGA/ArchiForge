using Microsoft.Data.Sqlite;

namespace ArchLucid.Cli;

/// <summary>
///     Creates a file-backed SQLite catalog for CLI-side quickstart scaffolding. This is not the ArchLucid product
///     database — hosts still use <c>ArchLucid:StorageProvider</c> (<c>InMemory</c> or <c>Sql</c>/SQL Server).
/// </summary>
internal static class QuickStartSQLiteProjectRegistry
{
    internal const string DefaultRelativeDbPath = "local/archlucid-evaluation.sqlite";

    internal static void EnsureRegistered(
        string absoluteDatabasePath,
        string projectName,
        string? baseDirectory,
        bool overwriteExistingFiles,
        bool includeTerraformStubs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(absoluteDatabasePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectName);

        string? directory = Path.GetDirectoryName(absoluteDatabasePath);

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        SqliteConnectionStringBuilder builder = new()
        {
            DataSource = absoluteDatabasePath,
            // One-shot CLI provisioning: default pooling can keep file handles on Windows until GC/pool teardown.
            Pooling = false
        };

        using SqliteConnection connection = new(builder.ConnectionString);
        connection.Open();

        using (SqliteCommand create = connection.CreateCommand())
        {
            create.CommandText =
                """
                CREATE TABLE IF NOT EXISTS PROJECTS (
                    ProjectName TEXT NOT NULL PRIMARY KEY,
                    BaseDirectory TEXT,
                    OverwriteExistingFiles INTEGER NOT NULL,
                    IncludeTerraformStubs INTEGER NOT NULL
                );
                """;

            create.ExecuteNonQuery();
        }

        using SqliteCommand upsert = connection.CreateCommand();

        upsert.CommandText =
            """
            INSERT INTO PROJECTS (ProjectName, BaseDirectory, OverwriteExistingFiles, IncludeTerraformStubs)
            VALUES (@ProjectName, @BaseDirectory, @OverwriteExistingFiles, @IncludeTerraformStubs)
            ON CONFLICT(ProjectName) DO UPDATE SET
                BaseDirectory = excluded.BaseDirectory,
                OverwriteExistingFiles = excluded.OverwriteExistingFiles,
                IncludeTerraformStubs = excluded.IncludeTerraformStubs;
            """;

        upsert.Parameters.AddWithValue("@ProjectName", projectName);
        upsert.Parameters.Add("@BaseDirectory", SqliteType.Text).Value =
            baseDirectory is null ? DBNull.Value : baseDirectory;

        upsert.Parameters.AddWithValue("@OverwriteExistingFiles", overwriteExistingFiles ? 1 : 0);

        upsert.Parameters.AddWithValue("@IncludeTerraformStubs", includeTerraformStubs ? 1 : 0);

        upsert.ExecuteNonQuery();
    }
}
