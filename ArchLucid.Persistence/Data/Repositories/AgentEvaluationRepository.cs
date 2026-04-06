using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Decisions;
using ArchiForge.Persistence.Data.Infrastructure;

using Dapper;

namespace ArchiForge.Persistence.Data.Repositories;

/// <summary>
/// Dapper-backed persistence for <see cref="IAgentEvaluationRepository"/>; writes and reads agent evaluation records from the <c>AgentEvaluations</c> table.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class AgentEvaluationRepository(IDbConnectionFactory connectionFactory) : IAgentEvaluationRepository
{
    public async Task CreateManyAsync(
        IReadOnlyCollection<AgentEvaluation> evaluations,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evaluations);

        if (evaluations.Count == 0)
        
            return;
        

        List<string> distinctRunIds = evaluations.Select(e => e.RunId).Distinct().ToList();
        if (distinctRunIds.Count > 1)
        
            throw new ArgumentException(
                $"All evaluations in a batch must belong to the same run. " +
                $"Found distinct RunIds: {string.Join(", ", distinctRunIds)}.",
                nameof(evaluations));
        

        string runId = evaluations.First().RunId;

        // Delete all existing evaluations for this run before inserting so that a retry
        // of ExecuteRunAsync (inside a TransactionScope) does not produce duplicate rows.
        const string deleteSql = "DELETE FROM AgentEvaluations WHERE RunId = @RunId;";

        const string insertSql = """
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

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using IDbTransaction transaction = connection.BeginTransaction();

        await connection.ExecuteAsync(new CommandDefinition(
            deleteSql,
            new { RunId = runId },
            transaction: transaction,
            cancellationToken: cancellationToken));

        foreach (AgentEvaluation e in evaluations)
        {
            string payload = JsonSerializer.Serialize(e, ContractJson.Default);
            await connection.ExecuteAsync(new CommandDefinition(
                insertSql,
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
                transaction: transaction,
                cancellationToken: cancellationToken));
        }

        transaction.Commit();
    }

    public async Task<IReadOnlyList<AgentEvaluation>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        string sql = $"""
            SELECT EvaluationJson
            FROM AgentEvaluations
            WHERE RunId = @RunId
            ORDER BY CreatedUtc
            {SqlPagingSyntax.FirstRowsOnly(500)};
            """;

        IEnumerable<string> rows = await connection.QueryAsync<string>(new CommandDefinition(
            sql,
            new { RunId = runId },
            cancellationToken: cancellationToken));

        List<AgentEvaluation> evaluations = [];
        foreach (string json in rows)
        {
            AgentEvaluation? evaluation;
            try
            {
                evaluation = JsonSerializer.Deserialize<AgentEvaluation>(json, ContractJson.Default);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to deserialize an AgentEvaluation for run '{runId}'. " +
                    "The stored JSON may be corrupt or written by an incompatible schema version.", ex);
            }

            if (evaluation is null)
            
                throw new InvalidOperationException(
                    $"An AgentEvaluation row for run '{runId}' deserialized to null. " +
                    "The stored JSON may be empty or corrupt.");
            

            evaluations.Add(evaluation);
        }

        return evaluations;
    }
}
