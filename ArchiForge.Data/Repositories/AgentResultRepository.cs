using System.Text.Json;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Data.Infrastructure;
using Dapper;

namespace ArchiForge.Data.Repositories;

public sealed class AgentResultRepository(IDbConnectionFactory connectionFactory) : IAgentResultRepository
{
    public async Task CreateAsync(AgentResult result, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO AgentResults
            (
                ResultId,
                TaskId,
                RunId,
                AgentType,
                Confidence,
                ResultJson,
                CreatedUtc
            )
            VALUES
            (
                @ResultId,
                @TaskId,
                @RunId,
                @AgentType,
                @Confidence,
                @ResultJson,
                @CreatedUtc
            );
            """;

        var json = JsonSerializer.Serialize(result, ContractJson.Default);

        using var connection = connectionFactory.CreateConnection();

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                result.ResultId,
                result.TaskId,
                result.RunId,
                AgentType = result.AgentType.ToString(),
                result.Confidence,
                ResultJson = json,
                result.CreatedUtc
            },
            cancellationToken: cancellationToken));
    }

    public async Task CreateManyAsync(IReadOnlyList<AgentResult> results, CancellationToken cancellationToken = default)
    {
        if (results.Count == 0)
            return;

        const string sql = """
            INSERT INTO AgentResults
            (
                ResultId,
                TaskId,
                RunId,
                AgentType,
                Confidence,
                ResultJson,
                CreatedUtc
            )
            VALUES
            (
                @ResultId,
                @TaskId,
                @RunId,
                @AgentType,
                @Confidence,
                @ResultJson,
                @CreatedUtc
            );
            """;

        var args = results.Select(result => new
        {
            result.ResultId,
            result.TaskId,
            result.RunId,
            AgentType = result.AgentType.ToString(),
            result.Confidence,
            ResultJson = JsonSerializer.Serialize(result, ContractJson.Default),
            result.CreatedUtc
        });

        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, args, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<AgentResult>> GetByRunIdAsync(string runId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT ResultJson
            FROM AgentResults
            WHERE RunId = @RunId
            ORDER BY CreatedUtc;
            """;

        using var connection = connectionFactory.CreateConnection();

        var rows = await connection.QueryAsync<string>(new CommandDefinition(
            sql,
            new { RunId = runId },
            cancellationToken: cancellationToken));

        return [.. rows.Select(json => JsonSerializer.Deserialize<AgentResult>(json, ContractJson.Default)).Where(x => x is not null).Cast<AgentResult>()];
    }
}