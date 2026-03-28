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

    /// <inheritdoc />
    public bool SupportsAmbientTransactionScope => false;

    public IDbConnection CreateConnection()
    {
        EnsureSchema();
        return new SqliteConnection(connectionString);
    }

    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        EnsureSchema();
        SqliteConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    private void EnsureSchema()
    {
        lock (SchemaLock)
        {
            if (InitializedDatabases.Contains(connectionString))
                return;

            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = "ArchiForge.Data.SQL.ArchiForge.Sqlite.sql";
            using Stream stream = assembly.GetManifestResourceStream(resourceName)
                                  ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
            using StreamReader textReader = new(stream);
            string schema = textReader.ReadToEnd();

            using SqliteConnection connection = new(connectionString);
            connection.Open();

            // ExecuteNonQuery runs only the first statement in a batch; the rest are skipped (native sqlite3_prepare).
            // Batched DDL must use ExecuteReader and drain result sets so every CREATE runs. See Microsoft Learn: Batching (Microsoft.Data.Sqlite).
            ExecuteSqliteScript(connection, schema);

            InitializedDatabases.Add(connectionString);
        }
    }

    private static void ExecuteSqliteScript(SqliteConnection connection, string sql)
    {
        using SqliteCommand cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        using SqliteDataReader batchReader = cmd.ExecuteReader();

        while (true)
        {
            while (batchReader.Read())
            {
            }

            if (!batchReader.NextResult())
            {
                break;
            }
        }
    }
}
