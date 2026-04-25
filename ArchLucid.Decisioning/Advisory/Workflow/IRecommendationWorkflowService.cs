using ArchLucid.Decisioning.Advisory.Models;

namespace ArchLucid.Decisioning.Advisory.Workflow;

/// <summary>
///     Maps advisory <see cref="ImprovementPlan" /> output to durable <see cref="RecommendationRecord" /> rows and applies
///     operator workflow actions.
/// </summary>
/// <remarks>
///     Default implementation: <c>ArchLucid.Persistence.Advisory.RecommendationWorkflowService</c>. Invoked from HTTP
///     after plan generation
///     (<c>ArchLucid.Api.Controllers.AdvisoryController</c>) and when applying accept/reject/defer/implemented.
/// </remarks>
public interface IRecommendationWorkflowService
{
    /// <summary>
    ///     Upserts every recommendation in the plan for the given scope, preserving review state when a row already left
    ///     <see cref="RecommendationStatus.Proposed" />.
    /// </summary>
    /// <param name="plan">
    ///     Generated plan; each <see cref="ImprovementRecommendation" /> must carry a stable
    ///     <see cref="ImprovementRecommendation.RecommendationId" />.
    /// </param>
    /// <param name="tenantId">Tenant scope.</param>
    /// <param name="workspaceId">Workspace scope.</param>
    /// <param name="projectId">Project scope.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    ///     If an existing row is found and its status is no longer <c>Proposed</c>, status, reviewer fields, and original
    ///     <see cref="RecommendationRecord.CreatedUtc" /> are retained;
    ///     content fields are refreshed from the plan and <see cref="RecommendationRecord.LastUpdatedUtc" /> is set to UTC
    ///     now.
    /// </remarks>
    Task PersistPlanAsync(
        ImprovementPlan plan,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct);

    /// <summary>
    ///     Updates status and reviewer metadata from <see cref="RecommendationActionRequest" />; unknown actions leave status
    ///     unchanged.
    /// </summary>
    /// <param name="recommendationId">Primary key of the recommendation row.</param>
    /// <param name="userId">Authenticated user id (from claims).</param>
    /// <param name="userName">Display or identity name for audit trails.</param>
    /// <param name="request">
    ///     Action discriminator in <see cref="RecommendationActionRequest.Action" />; optional
    ///     comment/rationale.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated row, or <c>null</c> if no row exists for <paramref name="recommendationId" />.</returns>
    Task<RecommendationRecord?> ApplyActionAsync(
        Guid recommendationId,
        string userId,
        string userName,
        RecommendationActionRequest request,
        CancellationToken ct);
}
