using System.Diagnostics.CodeAnalysis;

using ArchLucid.Contracts.ProductLearning;
using ArchLucid.Contracts.ProductLearning.Planning;
using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Coordination.ProductLearning.Planning;

[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
internal sealed class DapperProductLearningPlanningThemeRepository(ISqlConnectionFactory connectionFactory)
{
    public async Task InsertThemeAsync(ProductLearningImprovementThemeRecord theme, CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsureTheme(theme);

        Guid themeId = theme.ThemeId == Guid.Empty ? Guid.NewGuid() : theme.ThemeId;
        DateTime createdUtc = theme.CreatedUtc == default ? DateTime.UtcNow : theme.CreatedUtc;
        string status = ProductLearningPlanningRepositoryValidation.NormalizeThemeStatus(theme.Status);

        const string sql = """
                           INSERT INTO dbo.ProductLearningImprovementThemes
                           (
                               ThemeId,
                               TenantId,
                               WorkspaceId,
                               ProjectId,
                               ThemeKey,
                               SourceAggregateKey,
                               PatternKey,
                               Title,
                               Summary,
                               AffectedArtifactTypeOrWorkflowArea,
                               SeverityBand,
                               EvidenceSignalCount,
                               DistinctRunCount,
                               AverageTrustScore,
                               DerivationRuleVersion,
                               Status,
                               CreatedUtc,
                               CreatedByUserId
                           )
                           VALUES
                           (
                               @ThemeId,
                               @TenantId,
                               @WorkspaceId,
                               @ProjectId,
                               @ThemeKey,
                               @SourceAggregateKey,
                               @PatternKey,
                               @Title,
                               @Summary,
                               @AffectedArtifactTypeOrWorkflowArea,
                               @SeverityBand,
                               @EvidenceSignalCount,
                               @DistinctRunCount,
                               @AverageTrustScore,
                               @DerivationRuleVersion,
                               @Status,
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
                    ThemeId = themeId,
                    theme.TenantId,
                    theme.WorkspaceId,
                    theme.ProjectId,
                    theme.ThemeKey,
                    theme.SourceAggregateKey,
                    theme.PatternKey,
                    theme.Title,
                    theme.Summary,
                    theme.AffectedArtifactTypeOrWorkflowArea,
                    theme.SeverityBand,
                    theme.EvidenceSignalCount,
                    theme.DistinctRunCount,
                    theme.AverageTrustScore,
                    theme.DerivationRuleVersion,
                    Status = status,
                    CreatedUtc = createdUtc,
                    theme.CreatedByUserId
                },
                cancellationToken: cancellationToken));
    }

    public async Task<ProductLearningImprovementThemeRecord?> GetThemeAsync(
        Guid themeId,
        ProductLearningScope scope,
        CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsureScope(scope);

        const string sql = """
                           SELECT
                               ThemeId,
                               TenantId,
                               WorkspaceId,
                               ProjectId,
                               ThemeKey,
                               SourceAggregateKey,
                               PatternKey,
                               Title,
                               Summary,
                               AffectedArtifactTypeOrWorkflowArea,
                               SeverityBand,
                               EvidenceSignalCount,
                               DistinctRunCount,
                               AverageTrustScore,
                               DerivationRuleVersion,
                               Status,
                               CreatedUtc,
                               CreatedByUserId
                           FROM dbo.ProductLearningImprovementThemes
                           WHERE ThemeId = @ThemeId
                             AND TenantId = @TenantId
                             AND WorkspaceId = @WorkspaceId
                             AND ProjectId = @ProjectId;
                           """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        ProductLearningImprovementThemeSqlRow? row =
            await connection.QuerySingleOrDefaultAsync<ProductLearningImprovementThemeSqlRow>(
                new CommandDefinition(
                    sql,
                    new { ThemeId = themeId, scope.TenantId, scope.WorkspaceId, scope.ProjectId },
                    cancellationToken: cancellationToken));

        return row is null ? null : MapTheme(row);
    }

    public async Task<IReadOnlyList<ProductLearningImprovementThemeRecord>> ListThemesAsync(
        ProductLearningScope scope,
        int take,
        CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsureScope(scope);
        ProductLearningPlanningRepositoryValidation.EnsureTake(take);

        const string sql = """
                           SELECT TOP (@Take)
                               ThemeId,
                               TenantId,
                               WorkspaceId,
                               ProjectId,
                               ThemeKey,
                               SourceAggregateKey,
                               PatternKey,
                               Title,
                               Summary,
                               AffectedArtifactTypeOrWorkflowArea,
                               SeverityBand,
                               EvidenceSignalCount,
                               DistinctRunCount,
                               AverageTrustScore,
                               DerivationRuleVersion,
                               Status,
                               CreatedUtc,
                               CreatedByUserId
                           FROM dbo.ProductLearningImprovementThemes
                           WHERE TenantId = @TenantId
                             AND WorkspaceId = @WorkspaceId
                             AND ProjectId = @ProjectId
                           ORDER BY CreatedUtc DESC, ThemeId ASC;
                           """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        IEnumerable<ProductLearningImprovementThemeSqlRow> rows =
            await connection.QueryAsync<ProductLearningImprovementThemeSqlRow>(
                new CommandDefinition(
                    sql,
                    new { Take = take, scope.TenantId, scope.WorkspaceId, scope.ProjectId },
                    cancellationToken: cancellationToken));

        return rows.Select(static r => MapTheme(r)).ToList();
    }

    private static ProductLearningImprovementThemeRecord MapTheme(ProductLearningImprovementThemeSqlRow row)
    {
        return new ProductLearningImprovementThemeRecord
        {
            ThemeId = row.ThemeId,
            TenantId = row.TenantId,
            WorkspaceId = row.WorkspaceId,
            ProjectId = row.ProjectId,
            ThemeKey = row.ThemeKey,
            SourceAggregateKey = row.SourceAggregateKey,
            PatternKey = row.PatternKey,
            Title = row.Title,
            Summary = row.Summary,
            AffectedArtifactTypeOrWorkflowArea = row.AffectedArtifactTypeOrWorkflowArea,
            SeverityBand = row.SeverityBand,
            EvidenceSignalCount = row.EvidenceSignalCount,
            DistinctRunCount = row.DistinctRunCount,
            AverageTrustScore = row.AverageTrustScore,
            DerivationRuleVersion = row.DerivationRuleVersion,
            Status = row.Status,
            CreatedUtc = row.CreatedUtc,
            CreatedByUserId = row.CreatedByUserId
        };
    }
}
