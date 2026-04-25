using System.Data;
using System.Diagnostics.CodeAnalysis;

using ArchLucid.Contracts.Agents;
using ArchLucid.Persistence.Data.Infrastructure;

using Dapper;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>Dapper append-only writes to <c>dbo.AgentOutputEvaluationResults</c>.</summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class AgentOutputEvaluationResultRepository(IDbConnectionFactory connectionFactory)
    : IAgentOutputEvaluationResultRepository
{
    public async Task AppendAsync(AgentOutputEvaluationResultInsert row, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(row);

        const string sql = """
                           INSERT INTO AgentOutputEvaluationResults
                           (
                               RunId,
                               TraceId,
                               CaseId,
                               AgentType,
                               OverallScore,
                               StructuralMatch,
                               SemanticMatch,
                               MissingKeysJson,
                               CreatedUtc
                           )
                           VALUES
                           (
                               @RunId,
                               @TraceId,
                               @CaseId,
                               @AgentType,
                               @OverallScore,
                               @StructuralMatch,
                               @SemanticMatch,
                               @MissingKeysJson,
                               @CreatedUtc
                           );
                           """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    row.RunId,
                    row.TraceId,
                    row.CaseId,
                    AgentType = row.AgentType.ToString(),
                    row.OverallScore,
                    row.StructuralMatch,
                    row.SemanticMatch,
                    row.MissingKeysJson,
                    row.CreatedUtc
                },
                cancellationToken: cancellationToken));
    }
}
