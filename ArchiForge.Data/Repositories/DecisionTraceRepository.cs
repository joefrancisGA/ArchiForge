using System.Data;
using System.Text.Json;

using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Data.Infrastructure;

using Dapper;

namespace ArchiForge.Data.Repositories;

public sealed class DecisionTraceRepository(IDbConnectionFactory connectionFactory) : IDecisionTraceRepository
{
    public async Task CreateManyAsync(IEnumerable<DecisionTrace> traces, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(traces);

        const string sql = """
            INSERT INTO DecisionTraces
            (
                TraceId,
                RunId,
                EventType,
                EventDescription,
                EventJson,
                CreatedUtc
            )
            VALUES
            (
                @TraceId,
                @RunId,
                @EventType,
                @EventDescription,
                @EventJson,
                @CreatedUtc
            );
            """;

        using IDbConnection connection = connectionFactory.CreateConnection();

        var rows = traces.Select(t => new
        {
            t.TraceId,
            t.RunId,
            t.EventType,
            t.EventDescription,
            EventJson = JsonSerializer.Serialize(t, ContractJson.Default),
            t.CreatedUtc
        });

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            rows,
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<DecisionTrace>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EventJson
            FROM DecisionTraces
            WHERE RunId = @RunId
            ORDER BY CreatedUtc
            LIMIT 2000;
            """;

        using IDbConnection connection = connectionFactory.CreateConnection();

        IEnumerable<string> rows = await connection.QueryAsync<string>(new CommandDefinition(
            sql,
            new
            {
                RunId = runId
            },
            cancellationToken: cancellationToken));

        List<DecisionTrace> traces = new List<DecisionTrace>();
        foreach (string json in rows)
        {
            DecisionTrace? trace;
            try
            {
                trace = JsonSerializer.Deserialize<DecisionTrace>(json, ContractJson.Default);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to deserialize a DecisionTrace for run '{runId}'. " +
                    "The stored JSON may be corrupt or written by an incompatible schema version.", ex);
            }

            if (trace is null)
            {
                throw new InvalidOperationException(
                    $"A DecisionTrace row for run '{runId}' deserialized to null. " +
                    "The stored JSON may be empty or corrupt.");
            }

            traces.Add(trace);
        }

        return traces;
    }
}
