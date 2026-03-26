using ArchiForge.Decisioning.Advisory.Workflow;
using ArchiForge.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Advisory;

/// <inheritdoc cref="IRecommendationRepository" />
/// <remarks>Uses a single <c>MERGE</c> statement keyed on <see cref="RecommendationRecord.RecommendationId"/>.</remarks>
public sealed class DapperRecommendationRepository(ISqlConnectionFactory connectionFactory) : IRecommendationRepository
{
    /// <inheritdoc />
    public async Task UpsertAsync(RecommendationRecord recommendation, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(recommendation);

        const string sql = """
            MERGE dbo.RecommendationRecords AS target
            USING (SELECT @RecommendationId AS RecommendationId) AS source
            ON target.RecommendationId = source.RecommendationId
            WHEN MATCHED THEN
                UPDATE SET
                    TenantId = @TenantId,
                    WorkspaceId = @WorkspaceId,
                    ProjectId = @ProjectId,
                    RunId = @RunId,
                    ComparedToRunId = @ComparedToRunId,
                    Title = @Title,
                    Category = @Category,
                    Rationale = @Rationale,
                    SuggestedAction = @SuggestedAction,
                    Urgency = @Urgency,
                    ExpectedImpact = @ExpectedImpact,
                    PriorityScore = @PriorityScore,
                    Status = @Status,
                    LastUpdatedUtc = @LastUpdatedUtc,
                    ReviewedByUserId = @ReviewedByUserId,
                    ReviewedByUserName = @ReviewedByUserName,
                    ReviewComment = @ReviewComment,
                    ResolutionRationale = @ResolutionRationale,
                    SupportingFindingIdsJson = @SupportingFindingIdsJson,
                    SupportingDecisionIdsJson = @SupportingDecisionIdsJson,
                    SupportingArtifactIdsJson = @SupportingArtifactIdsJson
            WHEN NOT MATCHED THEN
                INSERT
                (
                    RecommendationId,
                    TenantId, WorkspaceId, ProjectId,
                    RunId, ComparedToRunId,
                    Title, Category, Rationale, SuggestedAction, Urgency, ExpectedImpact,
                    PriorityScore, Status, CreatedUtc, LastUpdatedUtc,
                    ReviewedByUserId, ReviewedByUserName, ReviewComment, ResolutionRationale,
                    SupportingFindingIdsJson, SupportingDecisionIdsJson, SupportingArtifactIdsJson
                )
                VALUES
                (
                    @RecommendationId,
                    @TenantId, @WorkspaceId, @ProjectId,
                    @RunId, @ComparedToRunId,
                    @Title, @Category, @Rationale, @SuggestedAction, @Urgency, @ExpectedImpact,
                    @PriorityScore, @Status, @CreatedUtc, @LastUpdatedUtc,
                    @ReviewedByUserId, @ReviewedByUserName, @ReviewComment, @ResolutionRationale,
                    @SupportingFindingIdsJson, @SupportingDecisionIdsJson, @SupportingArtifactIdsJson
                );
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct).ConfigureAwait(false);
        await connection.ExecuteAsync(new CommandDefinition(sql, recommendation, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<RecommendationRecord?> GetByIdAsync(Guid recommendationId, CancellationToken ct)
    {
        const string sql = """
            SELECT RecommendationId,
                   TenantId, WorkspaceId, ProjectId,
                   RunId, ComparedToRunId,
                   Title, Category, Rationale, SuggestedAction, Urgency, ExpectedImpact,
                   PriorityScore, Status, CreatedUtc, LastUpdatedUtc,
                   ReviewedByUserId, ReviewedByUserName, ReviewComment, ResolutionRationale,
                   SupportingFindingIdsJson, SupportingDecisionIdsJson, SupportingArtifactIdsJson
            FROM dbo.RecommendationRecords
            WHERE RecommendationId = @RecommendationId;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct).ConfigureAwait(false);
        return await connection.QueryFirstOrDefaultAsync<RecommendationRecord>(
            new CommandDefinition(sql, new
            {
                RecommendationId = recommendationId
            }, cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecommendationRecord>> ListByRunAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid runId,
        CancellationToken ct)
    {
        const string sql = """
            SELECT TOP 500 RecommendationId,
                   TenantId, WorkspaceId, ProjectId,
                   RunId, ComparedToRunId,
                   Title, Category, Rationale, SuggestedAction, Urgency, ExpectedImpact,
                   PriorityScore, Status, CreatedUtc, LastUpdatedUtc,
                   ReviewedByUserId, ReviewedByUserName, ReviewComment, ResolutionRationale,
                   SupportingFindingIdsJson, SupportingDecisionIdsJson, SupportingArtifactIdsJson
            FROM dbo.RecommendationRecords
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
              AND RunId = @RunId
            ORDER BY PriorityScore DESC, CreatedUtc DESC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct).ConfigureAwait(false);
        IEnumerable<RecommendationRecord> result = await connection.QueryAsync<RecommendationRecord>(
            new CommandDefinition(
                sql,
                new
                {
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId,
                    RunId = runId
                },
                cancellationToken: ct)).ConfigureAwait(false);

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecommendationRecord>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string? status,
        int take,
        CancellationToken ct)
    {
        const string sql = """
            SELECT TOP (@Take) RecommendationId,
                   TenantId, WorkspaceId, ProjectId,
                   RunId, ComparedToRunId,
                   Title, Category, Rationale, SuggestedAction, Urgency, ExpectedImpact,
                   PriorityScore, Status, CreatedUtc, LastUpdatedUtc,
                   ReviewedByUserId, ReviewedByUserName, ReviewComment, ResolutionRationale,
                   SupportingFindingIdsJson, SupportingDecisionIdsJson, SupportingArtifactIdsJson
            FROM dbo.RecommendationRecords
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
              AND (@Status IS NULL OR Status = @Status)
            ORDER BY LastUpdatedUtc DESC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct).ConfigureAwait(false);
        IEnumerable<RecommendationRecord> result = await connection.QueryAsync<RecommendationRecord>(
            new CommandDefinition(
                sql,
                new
                {
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId,
                    Status = status,
                    Take = Math.Clamp(take <= 0 ? 50 : take, 1, 500)
                },
                cancellationToken: ct)).ConfigureAwait(false);

        return result.ToList();
    }
}
