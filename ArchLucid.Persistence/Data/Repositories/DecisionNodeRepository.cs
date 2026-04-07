using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Decisions;
using ArchLucid.Persistence.Data.Infrastructure;

using Dapper;

namespace ArchLucid.Persistence.Data.Repositories;

[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class DecisionNodeRepository(IDbConnectionFactory connectionFactory) : IDecisionNodeRepository
{
    public async Task CreateManyAsync(
        IReadOnlyCollection<DecisionNode> decisions,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(decisions);

        if (decisions.Count == 0)
            return;

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

        (IDbConnection conn, bool ownsConnection) =
            await ExternalDbConnection.ResolveAsync(connectionFactory, connection, cancellationToken);

        try
        {
            if (transaction is not null)
            {
                await InsertDecisionNodesAsync(conn, transaction, decisions, sql, cancellationToken);
            }
            else
            {
                using IDbTransaction tx = conn.BeginTransaction();

                await InsertDecisionNodesAsync(conn, tx, decisions, sql, cancellationToken);

                tx.Commit();
            }
        }
        finally
        {
            ExternalDbConnection.DisposeIfOwned(conn, ownsConnection);
        }
    }

    private static async Task InsertDecisionNodesAsync(
        IDbConnection conn,
        IDbTransaction tx,
        IReadOnlyCollection<DecisionNode> decisions,
        string sql,
        CancellationToken cancellationToken)
    {
        foreach (DecisionNode decision in decisions)
        {
            string payload = JsonSerializer.Serialize(decision, ContractJson.Default);
            await conn.ExecuteAsync(new CommandDefinition(
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
                transaction: tx,
                cancellationToken: cancellationToken));
        }
    }

    public async Task<IReadOnlyList<DecisionNode>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        string sql = $"""
            SELECT DecisionJson
            FROM DecisionNodes
            WHERE RunId = @RunId
            ORDER BY CreatedUtc
            {SqlPagingSyntax.FirstRowsOnly(1000)};
            """;

        IEnumerable<string> rows = await connection.QueryAsync<string>(new CommandDefinition(
            sql,
            new { RunId = runId },
            cancellationToken: cancellationToken));

        List<DecisionNode> nodes = [];
        foreach (string json in rows)
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
            
                throw new InvalidOperationException(
                    $"A DecisionNode row for run '{runId}' deserialized to null. " +
                    "The stored JSON may be empty or corrupt.");
            

            nodes.Add(node);
        }

        return nodes;
    }
}
