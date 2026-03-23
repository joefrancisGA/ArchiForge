using System.Text.Json;

using ArchiForge.Decisioning.Advisory.Models;
using ArchiForge.Decisioning.Advisory.Workflow;

namespace ArchiForge.Persistence.Advisory;

/// <inheritdoc cref="IRecommendationWorkflowService" />
public sealed class RecommendationWorkflowService(IRecommendationRepository repository) : IRecommendationWorkflowService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <inheritdoc />
    public async Task PersistPlanAsync(
        ImprovementPlan plan,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        foreach (var recommendation in plan.Recommendations)
        {
            var existing = await repository.GetByIdAsync(recommendation.RecommendationId, ct).ConfigureAwait(false);

            var record = new RecommendationRecord
            {
                RecommendationId = recommendation.RecommendationId,
                TenantId = tenantId,
                WorkspaceId = workspaceId,
                ProjectId = projectId,
                RunId = plan.RunId,
                ComparedToRunId = plan.ComparedToRunId,
                Title = recommendation.Title,
                Category = recommendation.Category,
                Rationale = recommendation.Rationale,
                SuggestedAction = recommendation.SuggestedAction,
                Urgency = recommendation.Urgency,
                ExpectedImpact = recommendation.ExpectedImpact,
                PriorityScore = recommendation.PriorityScore,
                Status = RecommendationStatus.Proposed,
                CreatedUtc = plan.GeneratedUtc,
                LastUpdatedUtc = plan.GeneratedUtc,
                SupportingFindingIdsJson = JsonSerializer.Serialize(recommendation.SupportingFindingIds, JsonOptions),
                SupportingDecisionIdsJson = JsonSerializer.Serialize(recommendation.SupportingDecisionIds, JsonOptions),
                SupportingArtifactIdsJson = JsonSerializer.Serialize(recommendation.SupportingArtifactIds, JsonOptions),
            };

            if (existing is not null && !string.Equals(existing.Status, RecommendationStatus.Proposed, StringComparison.Ordinal))
            {
                record.Status = existing.Status;
                record.CreatedUtc = existing.CreatedUtc;
                record.ReviewedByUserId = existing.ReviewedByUserId;
                record.ReviewedByUserName = existing.ReviewedByUserName;
                record.ReviewComment = existing.ReviewComment;
                record.ResolutionRationale = existing.ResolutionRationale;
                record.LastUpdatedUtc = DateTime.UtcNow;
            }

            await repository.UpsertAsync(record, ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<RecommendationRecord?> ApplyActionAsync(
        Guid recommendationId,
        string userId,
        string userName,
        RecommendationActionRequest request,
        CancellationToken ct)
    {
        var recommendation = await repository.GetByIdAsync(recommendationId, ct).ConfigureAwait(false);
        if (recommendation is null)
            return null;

        recommendation.Status = request.Action switch
        {
            RecommendationActionType.Accept => RecommendationStatus.Accepted,
            RecommendationActionType.Reject => RecommendationStatus.Rejected,
            RecommendationActionType.Defer => RecommendationStatus.Deferred,
            RecommendationActionType.MarkImplemented => RecommendationStatus.Implemented,
            _ => recommendation.Status
        };

        recommendation.ReviewedByUserId = userId;
        recommendation.ReviewedByUserName = userName;
        recommendation.ReviewComment = request.Comment;
        recommendation.ResolutionRationale = request.Rationale;
        recommendation.LastUpdatedUtc = DateTime.UtcNow;

        await repository.UpsertAsync(recommendation, ct).ConfigureAwait(false);
        return recommendation;
    }
}
