using System.Diagnostics.CodeAnalysis;

using ArchiForge.Contracts.Evolution;
using ArchiForge.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Evolution;

/// <summary>Dapper access to <c>EvolutionSimulationRuns</c>.</summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class DapperEvolutionSimulationRunRepository(ISqlConnectionFactory connectionFactory)
    : IEvolutionSimulationRunRepository
{
    public async Task InsertAsync(EvolutionSimulationRunRecord record, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.EvolutionSimulationRuns
            (
                SimulationRunId,
                CandidateChangeSetId,
                BaselineArchitectureRunId,
                EvaluationMode,
                OutcomeJson,
                WarningsJson,
                CompletedUtc,
                IsShadowOnly
            )
            VALUES
            (
                @SimulationRunId,
                @CandidateChangeSetId,
                @BaselineArchitectureRunId,
                @EvaluationMode,
                @OutcomeJson,
                @WarningsJson,
                @CompletedUtc,
                @IsShadowOnly
            );
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    record.SimulationRunId,
                    record.CandidateChangeSetId,
                    record.BaselineArchitectureRunId,
                    record.EvaluationMode,
                    record.OutcomeJson,
                    record.WarningsJson,
                    record.CompletedUtc,
                    record.IsShadowOnly,
                },
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<EvolutionSimulationRunRecord>> ListByCandidateAsync(
        Guid candidateChangeSetId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                SimulationRunId,
                CandidateChangeSetId,
                BaselineArchitectureRunId,
                EvaluationMode,
                OutcomeJson,
                WarningsJson,
                CompletedUtc,
                IsShadowOnly
            FROM dbo.EvolutionSimulationRuns
            WHERE CandidateChangeSetId = @CandidateChangeSetId
            ORDER BY BaselineArchitectureRunId ASC, CompletedUtc ASC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        IEnumerable<EvolutionSimulationRunRecord> rows = await connection.QueryAsync<EvolutionSimulationRunRecord>(
            new CommandDefinition(
                sql,
                new { CandidateChangeSetId = candidateChangeSetId },
                cancellationToken: cancellationToken));

        return rows.ToList();
    }
}
