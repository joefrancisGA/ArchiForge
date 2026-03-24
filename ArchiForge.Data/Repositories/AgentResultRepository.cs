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
        // Delete-then-insert by (RunId, TaskId) so that a duplicate submission from a
        // retrying agent replaces the previous row rather than violating a unique constraint.
        const string deleteSql = "DELETE FROM AgentResults WHERE RunId = @RunId AND TaskId = @TaskId;";

        const string insertSql = """
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
        var parameters = new
        {
            result.ResultId,
            result.TaskId,
            result.RunId,
            AgentType = result.AgentType.ToString(),
            result.Confidence,
            ResultJson = json,
            result.CreatedUtc
        };

        using var connection = connectionFactory.CreateConnection();
        connection.Open();

        using var tx = connection.BeginTransaction();

        await connection.ExecuteAsync(new CommandDefinition(
            deleteSql,
            new { result.RunId, result.TaskId },
            transaction: tx,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            insertSql,
            parameters,
            transaction: tx,
            cancellationToken: cancellationToken));

        tx.Commit();
    }

    public async Task CreateManyAsync(IReadOnlyList<AgentResult> results, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(results);

        if (results.Count == 0)
            return;

        var distinctRunIds = results.Select(r => r.RunId).Distinct().ToList();
        if (distinctRunIds.Count > 1)
        {
            throw new ArgumentException(
                $"All results in a batch must belong to the same run. " +
                $"Found distinct RunIds: {string.Join(", ", distinctRunIds)}.",
                nameof(results));
        }

        // Delete all existing results for this run before bulk-inserting so that a retry
        // of ExecuteRunAsync (inside a TransactionScope) does not produce duplicate rows.
        const string deleteSql = "DELETE FROM AgentResults WHERE RunId = @RunId;";

        const string insertSql = """
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
        connection.Open();

        using var tx = connection.BeginTransaction();

        await connection.ExecuteAsync(new CommandDefinition(
            deleteSql,
            new { results[0].RunId },
            transaction: tx,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(insertSql, args, transaction: tx, cancellationToken: cancellationToken));

        tx.Commit();
    }

    public async Task<IReadOnlyList<AgentResult>> GetByRunIdAsync(string runId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT ResultJson
            FROM AgentResults
            WHERE RunId = @RunId
            ORDER BY CreatedUtc
            LIMIT 1000;
            """;

        using var connection = connectionFactory.CreateConnection();

        var rows = await connection.QueryAsync<string>(new CommandDefinition(
            sql,
            new
            {
                RunId = runId
            },
            cancellationToken: cancellationToken));

        var results = new List<AgentResult>();
        foreach (var json in rows)
        {
            AgentResult? result;
            try
            {
                result = JsonSerializer.Deserialize<AgentResult>(json, ContractJson.Default);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to deserialize an AgentResult for run '{runId}'. " +
                    "The stored JSON may be corrupt or written by an incompatible schema version.", ex);
            }

            if (result is null)
            {
                throw new InvalidOperationException(
                    $"An AgentResult row for run '{runId}' deserialized to null. " +
                    "The stored JSON may be empty or corrupt.");
            }

            results.Add(result);
        }

        return results;
    }
}
