using System.Text.Json;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Data.Infrastructure;
using Dapper;

namespace ArchiForge.Data.Repositories;

public sealed class AgentExecutionTraceRepository : IAgentExecutionTraceRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AgentExecutionTraceRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task CreateAsync(
        AgentExecutionTrace trace,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO AgentExecutionTraces
            (
                TraceId,
                RunId,
                TaskId,
                AgentType,
                ParseSucceeded,
                ErrorMessage,
                TraceJson,
                CreatedUtc
            )
            VALUES
            (
                @TraceId,
                @RunId,
                @TaskId,
                @AgentType,
                @ParseSucceeded,
                @ErrorMessage,
                @TraceJson,
                @CreatedUtc
            );
            """;

        var json = JsonSerializer.Serialize(trace, ContractJson.Default);

        using var connection = _connectionFactory.CreateConnection();

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                trace.TraceId,
                trace.RunId,
                trace.TaskId,
                AgentType = trace.AgentType.ToString(),
                trace.ParseSucceeded,
                trace.ErrorMessage,
                TraceJson = json,
                trace.CreatedUtc
            },
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<AgentExecutionTrace>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TraceJson
            FROM AgentExecutionTraces
            WHERE RunId = @RunId
            ORDER BY CreatedUtc;
            """;

        using var connection = _connectionFactory.CreateConnection();

        var rows = await connection.QueryAsync<string>(new CommandDefinition(
            sql,
            new { RunId = runId },
            cancellationToken: cancellationToken));

        return rows
            .Select(json => JsonSerializer.Deserialize<AgentExecutionTrace>(json, ContractJson.Default))
            .Where(x => x is not null)
            .Cast<AgentExecutionTrace>()
            .ToList();
    }

    public async Task<IReadOnlyList<AgentExecutionTrace>> GetByTaskIdAsync(
        string taskId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TraceJson
            FROM AgentExecutionTraces
            WHERE TaskId = @TaskId
            ORDER BY CreatedUtc;
            """;

        using var connection = _connectionFactory.CreateConnection();

        var rows = await connection.QueryAsync<string>(new CommandDefinition(
            sql,
            new { TaskId = taskId },
            cancellationToken: cancellationToken));

        return rows
            .Select(json => JsonSerializer.Deserialize<AgentExecutionTrace>(json, ContractJson.Default))
            .Where(x => x is not null)
            .Cast<AgentExecutionTrace>()
            .ToList();
    }
}
