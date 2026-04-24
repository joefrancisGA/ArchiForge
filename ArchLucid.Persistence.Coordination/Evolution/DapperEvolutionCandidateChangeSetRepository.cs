using System.Diagnostics.CodeAnalysis;

using ArchLucid.Contracts.Evolution;
using ArchLucid.Contracts.ProductLearning;
using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Coordination.Evolution;

/// <summary>Dapper access to <c>EvolutionCandidateChangeSets</c>.</summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class DapperEvolutionCandidateChangeSetRepository(ISqlConnectionFactory connectionFactory)
    : IEvolutionCandidateChangeSetRepository
{
    public async Task InsertAsync(EvolutionCandidateChangeSetRecord record, CancellationToken cancellationToken)
    {
        const string sql = """
                           INSERT INTO dbo.EvolutionCandidateChangeSets
                           (
                               CandidateChangeSetId,
                               TenantId,
                               WorkspaceId,
                               ProjectId,
                               SourcePlanId,
                               Status,
                               Title,
                               Summary,
                               PlanSnapshotJson,
                               DerivationRuleVersion,
                               CreatedUtc,
                               CreatedByUserId
                           )
                           VALUES
                           (
                               @CandidateChangeSetId,
                               @TenantId,
                               @WorkspaceId,
                               @ProjectId,
                               @SourcePlanId,
                               @Status,
                               @Title,
                               @Summary,
                               @PlanSnapshotJson,
                               @DerivationRuleVersion,
                               @CreatedUtc,
                               @CreatedByUserId
                           );
                           """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    record.CandidateChangeSetId,
                    record.TenantId,
                    record.WorkspaceId,
                    record.ProjectId,
                    record.SourcePlanId,
                    record.Status,
                    record.Title,
                    record.Summary,
                    record.PlanSnapshotJson,
                    record.DerivationRuleVersion,
                    record.CreatedUtc,
                    record.CreatedByUserId
                },
                cancellationToken: cancellationToken));
    }

    public async Task<EvolutionCandidateChangeSetRecord?> GetByIdAsync(
        Guid candidateChangeSetId,
        ProductLearningScope scope,
        CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT
                               CandidateChangeSetId,
                               TenantId,
                               WorkspaceId,
                               ProjectId,
                               SourcePlanId,
                               Status,
                               Title,
                               Summary,
                               PlanSnapshotJson,
                               DerivationRuleVersion,
                               CreatedUtc,
                               CreatedByUserId
                           FROM dbo.EvolutionCandidateChangeSets
                           WHERE CandidateChangeSetId = @CandidateChangeSetId
                             AND TenantId = @TenantId
                             AND WorkspaceId = @WorkspaceId
                             AND ProjectId = @ProjectId;
                           """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<EvolutionCandidateChangeSetRecord>(
            new CommandDefinition(
                sql,
                new { CandidateChangeSetId = candidateChangeSetId, scope.TenantId, scope.WorkspaceId, scope.ProjectId },
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<EvolutionCandidateChangeSetRecord>> ListAsync(
        ProductLearningScope scope,
        int take,
        CancellationToken cancellationToken)
    {
        int n = take < 1 ? 1 : Math.Min(take, 100);

        const string sql = """
                           SELECT TOP (@Take)
                               CandidateChangeSetId,
                               TenantId,
                               WorkspaceId,
                               ProjectId,
                               SourcePlanId,
                               Status,
                               Title,
                               Summary,
                               PlanSnapshotJson,
                               DerivationRuleVersion,
                               CreatedUtc,
                               CreatedByUserId
                           FROM dbo.EvolutionCandidateChangeSets
                           WHERE TenantId = @TenantId
                             AND WorkspaceId = @WorkspaceId
                             AND ProjectId = @ProjectId
                           ORDER BY CreatedUtc DESC, CandidateChangeSetId ASC;
                           """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        IEnumerable<EvolutionCandidateChangeSetRecord> rows =
            await connection.QueryAsync<EvolutionCandidateChangeSetRecord>(
                new CommandDefinition(
                    sql,
                    new { Take = n, scope.TenantId, scope.WorkspaceId, scope.ProjectId },
                    cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task UpdateStatusAsync(
        Guid candidateChangeSetId,
        ProductLearningScope scope,
        string status,
        CancellationToken cancellationToken)
    {
        const string sql = """
                           UPDATE dbo.EvolutionCandidateChangeSets
                           SET Status = @Status
                           WHERE CandidateChangeSetId = @CandidateChangeSetId
                             AND TenantId = @TenantId
                             AND WorkspaceId = @WorkspaceId
                             AND ProjectId = @ProjectId;
                           """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    Status = status,
                    CandidateChangeSetId = candidateChangeSetId,
                    scope.TenantId,
                    scope.WorkspaceId,
                    scope.ProjectId
                },
                cancellationToken: cancellationToken));
    }
}
