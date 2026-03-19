using System.Data;
using System.Reflection;
using Microsoft.Data.Sqlite;

namespace ArchiForge.Data.Infrastructure;

/// <summary>
/// Creates SQLite connections, typically for in-memory testing.
/// Ensures schema exists on first connection.
/// </summary>
public sealed class SqliteConnectionFactory(string connectionString) : IDbConnectionFactory
{
    private static readonly Lock SchemaLock = new();
    private static readonly HashSet<string> InitializedDatabases = [];

    public IDbConnection CreateConnection()
    {
        EnsureSchema();
        return new SqliteConnection(connectionString);
    }

    private void EnsureSchema()
    {
        lock (SchemaLock)
        {
            if (!InitializedDatabases.Add(connectionString))
                return;
        }

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "ArchiForge.Data.SQL.ArchiForge.Sqlite.sql";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        var schema = reader.ReadToEnd();

        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = schema;
        cmd.ExecuteNonQuery();
    }
}
