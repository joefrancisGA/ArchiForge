using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.DecisionTraces;
using ArchiForge.Persistence.Data.Infrastructure;

using Dapper;

namespace ArchiForge.Persistence.Data.Repositories;

[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class DecisionTraceRepository(IDbConnectionFactory connectionFactory) : ICoordinatorDecisionTraceRepository
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

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = traces.Select(t =>
        {
            RunEventTracePayload run = t.RequireRunEvent();

            return new
            {
                run.TraceId,
                run.RunId,
                run.EventType,
                run.EventDescription,
                EventJson = JsonSerializer.Serialize(run, ContractJson.Default),
                run.CreatedUtc
            };
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
        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        string sql = $"""
            SELECT EventJson
            FROM DecisionTraces
            WHERE RunId = @RunId
            ORDER BY CreatedUtc
            {SqlPagingSyntax.FirstRowsOnly(2000)};
            """;

        IEnumerable<string> rows = await connection.QueryAsync<string>(new CommandDefinition(
            sql,
            new
            {
                RunId = runId
            },
            cancellationToken: cancellationToken));

        List<DecisionTrace> traces = [];
        foreach (string json in rows)
        {
            RunEventTracePayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<RunEventTracePayload>(json, ContractJson.Default);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to deserialize coordinator trace payload for run '{runId}'. " +
                    "The stored JSON may be corrupt or written by an incompatible schema version.", ex);
            }

            if (payload is null)

                throw new InvalidOperationException(
                    $"A coordinator trace row for run '{runId}' deserialized to null. " +
                    "The stored JSON may be empty or corrupt.");


            traces.Add(RunEventTrace.From(payload));
        }

        return traces;
    }
}
