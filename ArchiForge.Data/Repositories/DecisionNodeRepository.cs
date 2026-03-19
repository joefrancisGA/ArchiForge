using System.Text.Json;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Decisions;
using ArchiForge.Data.Infrastructure;
using Dapper;

namespace ArchiForge.Data.Repositories;

public sealed class DecisionNodeRepository(IDbConnectionFactory connectionFactory) : IDecisionNodeRepository
{
    public async Task CreateManyAsync(
        IReadOnlyCollection<DecisionNode> decisions,
        CancellationToken cancellationToken = default)
    {
        if (decisions is null || decisions.Count == 0)
        {
            return;
        }

        const string sql = """
            INSERT INTO DecisionNodes
            (
                DecisionId,
                RunId,
                Topic,
                SelectedOptionId,
                Confidence,
                Rationale,
                DecisionJson,
                CreatedUtc
            )
            VALUES
            (
                @DecisionId,
                @RunId,
                @Topic,
                @SelectedOptionId,
                @Confidence,
                @Rationale,
                @DecisionJson,
                @CreatedUtc
            );
            """;

        using var connection = connectionFactory.CreateConnection();

        foreach (var decision in decisions)
        {
            var payload = JsonSerializer.Serialize(decision, ContractJson.Default);
            await connection.ExecuteAsync(new CommandDefinition(
                sql,
                new
                {
                    decision.DecisionId,
                    decision.RunId,
                    decision.Topic,
                    decision.SelectedOptionId,
                    decision.Confidence,
                    decision.Rationale,
                    DecisionJson = payload,
                    decision.CreatedUtc
                },
                cancellationToken: cancellationToken));
        }
    }

    public async Task<IReadOnlyList<DecisionNode>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT DecisionJson
            FROM DecisionNodes
            WHERE RunId = @RunId
            ORDER BY CreatedUtc;
            """;

        using var connection = connectionFactory.CreateConnection();

        var rows = await connection.QueryAsync<string>(new CommandDefinition(
            sql,
            new { RunId = runId },
            cancellationToken: cancellationToken));

        return rows
            .Select(json => JsonSerializer.Deserialize<DecisionNode>(json, ContractJson.Default))
            .Where(x => x is not null)
            .Cast<DecisionNode>()
            .ToList();
    }
}

