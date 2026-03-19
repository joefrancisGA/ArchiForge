using System.Text.Json;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Decisions;
using ArchiForge.Data.Infrastructure;
using Dapper;

namespace ArchiForge.Data.Repositories;

public sealed class AgentEvaluationRepository(IDbConnectionFactory connectionFactory) : IAgentEvaluationRepository
{
    public async Task CreateManyAsync(
        IReadOnlyCollection<AgentEvaluation> evaluations,
        CancellationToken cancellationToken = default)
    {
        if (evaluations is null || evaluations.Count == 0)
        {
            return;
        }

        const string sql = """
            INSERT INTO AgentEvaluations
            (
                EvaluationId,
                RunId,
                TargetAgentTaskId,
                EvaluationType,
                ConfidenceDelta,
                Rationale,
                EvaluationJson,
                CreatedUtc
            )
            VALUES
            (
                @EvaluationId,
                @RunId,
                @TargetAgentTaskId,
                @EvaluationType,
                @ConfidenceDelta,
                @Rationale,
                @EvaluationJson,
                @CreatedUtc
            );
            """;

        using var connection = connectionFactory.CreateConnection();

        foreach (var e in evaluations)
        {
            var payload = JsonSerializer.Serialize(e, ContractJson.Default);
            await connection.ExecuteAsync(new CommandDefinition(
                sql,
                new
                {
                    e.EvaluationId,
                    e.RunId,
                    e.TargetAgentTaskId,
                    e.EvaluationType,
                    e.ConfidenceDelta,
                    e.Rationale,
                    EvaluationJson = payload,
                    e.CreatedUtc
                },
                cancellationToken: cancellationToken));
        }
    }

    public async Task<IReadOnlyList<AgentEvaluation>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EvaluationJson
            FROM AgentEvaluations
            WHERE RunId = @RunId
            ORDER BY CreatedUtc;
            """;

        using var connection = connectionFactory.CreateConnection();

        var rows = await connection.QueryAsync<string>(new CommandDefinition(
            sql,
            new { RunId = runId },
            cancellationToken: cancellationToken));

        return rows
            .Select(json => JsonSerializer.Deserialize<AgentEvaluation>(json, ContractJson.Default))
            .Where(x => x is not null)
            .Cast<AgentEvaluation>()
            .ToList();
    }
}

