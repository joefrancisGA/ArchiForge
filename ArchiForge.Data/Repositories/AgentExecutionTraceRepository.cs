using System.Data;
using System.Text.Json;

using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Data.Infrastructure;

using Dapper;

namespace ArchiForge.Data.Repositories;

public sealed class AgentExecutionTraceRepository(IDbConnectionFactory connectionFactory)
    : IAgentExecutionTraceRepository
{
    public async Task CreateAsync(
        AgentExecutionTrace trace,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(trace);

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

        string json = JsonSerializer.Serialize(trace, ContractJson.Default);

        using IDbConnection connection = connectionFactory.CreateConnection();

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
            ORDER BY CreatedUtc
            LIMIT 500;
            """;

        using IDbConnection connection = connectionFactory.CreateConnection();

        IEnumerable<string> rows = await connection.QueryAsync<string>(new CommandDefinition(
            sql,
            new
            {
                RunId = runId
            },
            cancellationToken: cancellationToken));

        return DeserializeTraces(rows, $"run '{runId}'");
    }

    public async Task<(IReadOnlyList<AgentExecutionTrace> Traces, int TotalCount)> GetPagedByRunIdAsync(
        string runId,
        int offset,
        int limit,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TraceJson,
                   COUNT(*) OVER () AS TotalCount
            FROM AgentExecutionTraces
            WHERE RunId = @RunId
            ORDER BY CreatedUtc
            OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;
            """;

        int clampedOffset = Math.Max(0, offset);
        int clampedLimit = Math.Clamp(limit, 1, 500);

        using IDbConnection connection = connectionFactory.CreateConnection();

        IEnumerable<dynamic> rows = await connection.QueryAsync(new CommandDefinition(
            sql,
            new { RunId = runId, Offset = clampedOffset, Limit = clampedLimit },
            cancellationToken: cancellationToken));

        List<dynamic> list = rows.ToList();
        int totalCount = list.Count > 0 ? (int)list[0].TotalCount : 0;

        IReadOnlyList<AgentExecutionTrace> traces = DeserializeTraces(list.Select(row => (string)row.TraceJson), $"run '{runId}' (paged)");

        return (traces, totalCount);
    }

    public async Task<IReadOnlyList<AgentExecutionTrace>> GetByTaskIdAsync(
        string taskId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TraceJson
            FROM AgentExecutionTraces
            WHERE TaskId = @TaskId
            ORDER BY CreatedUtc
            LIMIT 500;
            """;

        using IDbConnection connection = connectionFactory.CreateConnection();

        IEnumerable<string> rows = await connection.QueryAsync<string>(new CommandDefinition(
            sql,
            new
            {
                TaskId = taskId
            },
            cancellationToken: cancellationToken));

        return DeserializeTraces(rows, $"task '{taskId}'");
    }

    private static IReadOnlyList<AgentExecutionTrace> DeserializeTraces(
        IEnumerable<string> jsonRows,
        string context)
    {
        List<AgentExecutionTrace> traces = new();
        foreach (string json in jsonRows)
        {
            AgentExecutionTrace? trace;
            try
            {
                trace = JsonSerializer.Deserialize<AgentExecutionTrace>(json, ContractJson.Default);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to deserialize an AgentExecutionTrace for {context}. " +
                    "The stored JSON may be corrupt or written by an incompatible schema version.", ex);
            }

            if (trace is null)
            {
                throw new InvalidOperationException(
                    $"An AgentExecutionTrace row for {context} deserialized to null. " +
                    "The stored JSON may be empty or corrupt.");
            }

            traces.Add(trace);
        }

        return traces;
    }
}
