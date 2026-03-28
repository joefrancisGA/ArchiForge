using Microsoft.Data.SqlClient;

namespace ArchiForge.Api.Tests;

/// <summary>
/// Creates and tears down per-factory databases on the configured SQL Server instance (integration tests).
/// </summary>
internal static class SqlServerTestDatabaseHelper
{
    public static void EnsureDatabaseExists(string connectionString)
    {
        SqlConnectionStringBuilder builder = new(connectionString);
        string databaseName = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new InvalidOperationException("Connection string must specify Initial Catalog.");

        builder.InitialCatalog = "master";
        using SqlConnection master = new(builder.ConnectionString);
        master.Open();
        using SqlCommand cmd = master.CreateCommand();
        cmd.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = @name)
BEGIN
    DECLARE @sql nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@name);
    EXEC sp_executesql @sql;
END";
        cmd.Parameters.AddWithValue("@name", databaseName);
        cmd.ExecuteNonQuery();
    }

    public static void DropDatabaseIfExists(string connectionString)
    {
        SqlConnectionStringBuilder builder = new(connectionString);
        string databaseName = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(databaseName))
            return;

        builder.InitialCatalog = "master";
        using SqlConnection master = new(builder.ConnectionString);
        master.Open();
        using SqlCommand cmd = master.CreateCommand();
        cmd.CommandText = @"
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = @name)
BEGIN
    DECLARE @q sysname = @name;
    DECLARE @alter nvarchar(max) = N'ALTER DATABASE ' + QUOTENAME(@q) + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE';
    EXEC sp_executesql @alter;
    DECLARE @drop nvarchar(max) = N'DROP DATABASE ' + QUOTENAME(@q);
    EXEC sp_executesql @drop;
END";
        cmd.Parameters.AddWithValue("@name", databaseName);
        cmd.ExecuteNonQuery();
    }
}
