using ArchiForge.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Sql;

public sealed class SqlSchemaBootstrapper(
    ISqlConnectionFactory connectionFactory,
    string scriptPath)
    : ISchemaBootstrapper
{
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
        List<string> batches = new List<string>();
        List<string> current = new List<string>();

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
