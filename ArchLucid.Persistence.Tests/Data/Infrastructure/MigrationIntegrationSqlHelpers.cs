using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Data.Infrastructure;

/// <summary>
///     Shared helpers for migration-related SQL integration tests (batch splitting, rollback script execution).
/// </summary>
internal static class MigrationIntegrationSqlHelpers
{
    /// <summary>
    ///     Walks up from <see cref="AppContext.BaseDirectory" /> until <c>ArchLucid.Persistence/Migrations/Rollback</c>
    ///     exists (same layout as local builds and CI checkout).
    /// </summary>
    internal static string ResolvePersistenceRollbackDirectory()
    {
        string start = AppContext.BaseDirectory;

        for (string? dir = start; dir is not null; dir = Directory.GetParent(dir)?.FullName)
        {
            string candidate = Path.Combine(dir, "ArchLucid.Persistence", "Migrations", "Rollback");

            if (Directory.Exists(candidate))
                return candidate;
        }

        throw new InvalidOperationException(
            "Could not locate ArchLucid.Persistence/Migrations/Rollback relative to " + start + ".");
    }

/// <summary>
///     Same <c>GO</c> line splitting semantics as greenfield baseline replay (split on lines equal to <c>GO</c>).
/// </summary>
    internal static IReadOnlyList<string> SplitGoBatches(string script)
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

    internal static async Task ExecuteGoBatchesAsync(string connectionString, string sql,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken);

        foreach (string batch in SplitGoBatches(sql))
        {
            if (string.IsNullOrWhiteSpace(batch))
                continue;

            await using SqlCommand command = new(batch, connection);
            command.CommandTimeout = 0;

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
