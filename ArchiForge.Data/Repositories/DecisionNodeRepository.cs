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
        ArgumentNullException.ThrowIfNull(decisions);

        if (decisions.Count == 0)
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
        using var transaction = connection.BeginTransaction();

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
                transaction: transaction,
                cancellationToken: cancellationToken));
        }

        transaction.Commit();
    }

    public async Task<IReadOnlyList<DecisionNode>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT DecisionJson
            FROM DecisionNodes
            WHERE RunId = @RunId
            ORDER BY CreatedUtc
            LIMIT 1000;
            """;

        using var connection = connectionFactory.CreateConnection();

        var rows = await connection.QueryAsync<string>(new CommandDefinition(
            sql,
            new { RunId = runId },
            cancellationToken: cancellationToken));

        var nodes = new List<DecisionNode>();
        foreach (var json in rows)
        {
            DecisionNode? node;
            try
            {
                node = JsonSerializer.Deserialize<DecisionNode>(json, ContractJson.Default);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to deserialize a DecisionNode for run '{runId}'. " +
                    "The stored JSON may be corrupt or written by an incompatible schema version.", ex);
            }

            if (node is null)
            {
                throw new InvalidOperationException(
                    $"A DecisionNode row for run '{runId}' deserialized to null. " +
                    "The stored JSON may be empty or corrupt.");
            }

            nodes.Add(node);
        }

        return nodes;
    }
}
