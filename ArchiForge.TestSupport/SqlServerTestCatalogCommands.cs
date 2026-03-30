using Microsoft.Data.SqlClient;

namespace ArchiForge.TestSupport;

/// <summary>
/// Creates or drops SQL Server catalogs used by integration tests (sync; safe for <c>WebApplicationFactory</c> constructors).
/// </summary>
public static class SqlServerTestCatalogCommands
{
    private const string EnsureDatabaseSql = """
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = @name)
BEGIN
    DECLARE @sql nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@name);
    EXEC sp_executesql @sql;
END
""";

    private const string DropDatabaseSql = """
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = @name)
BEGIN
    DECLARE @q sysname = @name;
    DECLARE @alter nvarchar(max) = N'ALTER DATABASE ' + QUOTENAME(@q) + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE';
    EXEC sp_executesql @alter;
    DECLARE @drop nvarchar(max) = N'DROP DATABASE ' + QUOTENAME(@q);
    EXEC sp_executesql @drop;
END
""";

    /// <summary>Ensures the database in <paramref name="connectionString"/> exists (connects via <c>master</c>).</summary>
    public static void EnsureCatalogExists(string connectionString)
    {
        SqlConnectionStringBuilder builder = new(connectionString);
        string databaseName = builder.InitialCatalog;

        if (string.IsNullOrWhiteSpace(databaseName))
            throw new InvalidOperationException("Connection string must specify Initial Catalog.");

        builder.InitialCatalog = "master";

        using SqlConnection master = new(builder.ConnectionString);
        master.Open();

        using SqlCommand cmd = master.CreateCommand();
        cmd.CommandText = EnsureDatabaseSql;
        cmd.Parameters.AddWithValue("@name", databaseName);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Async variant for persistence fixtures (same semantics as sync path).</summary>
    public static async Task EnsureCatalogExistsAsync(string connectionString, CancellationToken cancellationToken)
    {
        SqlConnectionStringBuilder target = new(connectionString);

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
            // Catalog may exist or server disallows CREATE (e.g. restricted role); caller continues to migrate/connect.
        }
    }

    /// <summary>Drops the database if it exists (best-effort).</summary>
    public static void DropCatalogIfExists(string connectionString)
    {
        SqlConnectionStringBuilder builder = new(connectionString);
        string databaseName = builder.InitialCatalog;

        if (string.IsNullOrWhiteSpace(databaseName))
            return;

        builder.InitialCatalog = "master";

        using SqlConnection master = new(builder.ConnectionString);
        master.Open();

        using SqlCommand cmd = master.CreateCommand();
        cmd.CommandText = DropDatabaseSql;
        cmd.Parameters.AddWithValue("@name", databaseName);
        cmd.ExecuteNonQuery();
    }
}
