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

        Assembly assembly = Assembly.GetExecutingAssembly();
        string resourceName = "ArchiForge.Data.SQL.ArchiForge.Sqlite.sql";
        using Stream stream = assembly.GetManifestResourceStream(resourceName)
                              ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
        using StreamReader reader = new(stream);
        string schema = reader.ReadToEnd();

        using SqliteConnection connection = new(connectionString);
        connection.Open();
        using SqliteCommand cmd = connection.CreateCommand();
        cmd.CommandText = schema;
        cmd.ExecuteNonQuery();
    }
}
