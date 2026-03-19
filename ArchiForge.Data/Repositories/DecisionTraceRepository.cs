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

        using var connection = connectionFactory.CreateConnection();

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
            ORDER BY CreatedUtc;
            """;

        using var connection = connectionFactory.CreateConnection();

        var rows = await connection.QueryAsync<string>(new CommandDefinition(
            sql,
            new { RunId = runId },
            cancellationToken: cancellationToken));

        return rows
            .Select(json => JsonSerializer.Deserialize<DecisionTrace>(json, ContractJson.Default))
            .Where(x => x is not null)
            .Cast<DecisionTrace>()
            .ToList();
    }
}