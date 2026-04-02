using System.Diagnostics.CodeAnalysis;

using ArchiForge.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Sql;

/// <summary>
/// Bootstraps the SQL schema by reading a T-SQL script file from <paramref name="scriptPath"/>,
/// splitting it on <c>GO</c> batch separators, and executing each batch against the database.
/// </summary>
public sealed class SqlSchemaBootstrapper(
    ISqlConnectionFactory connectionFactory,
    string scriptPath)
    : ISchemaBootstrapper
{
    [ExcludeFromCodeCoverage(Justification = "Reads file and executes SQL batches; requires live SQL Server. SplitGoBatches is tested separately.")]
    public async Task EnsureSchemaAsync(CancellationToken ct)
    {
        if (!File.Exists(scriptPath))
            throw new FileNotFoundException($"Schema script not found: {scriptPath}");

        string script = await File.ReadAllTextAsync(scriptPath, ct);
        IReadOnlyList<string> batches = SplitGoBatches(script);

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);

        foreach (string batch in batches)
        {
            if (!string.IsNullOrWhiteSpace(batch))
            {
                await connection.ExecuteAsync(new CommandDefinition(batch, cancellationToken: ct));
            }
        }
    }

    public IReadOnlyList<string> SplitGoBatches(string script)
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
        {
            batches.Add(string.Join(Environment.NewLine, current));
        }

        return batches;
    }
}
