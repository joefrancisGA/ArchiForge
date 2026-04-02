using ArchiForge.Api.Models.Learning;
using ArchiForge.Contracts.ProductLearning;

namespace ArchiForge.Api.Services.Learning;

/// <summary>Scoped read model for 59R learning themes and improvement plans.</summary>
public interface ILearningPlanningReadService
{
    Task<LearningThemesListResponse> GetThemesAsync(
        ProductLearningScope scope,
        int maxThemes,
        CancellationToken cancellationToken);

    Task<LearningPlansListResponse> GetPlansAsync(
        ProductLearningScope scope,
        int maxPlans,
        CancellationToken cancellationToken);

    Task<LearningPlanDetailResponse?> GetPlanByIdAsync(
        Guid planId,
        ProductLearningScope scope,
        CancellationToken cancellationToken);

    Task<LearningSummaryResponse> GetSummaryAsync(
        ProductLearningScope scope,
        int maxThemes,
        int maxPlans,
        CancellationToken cancellationToken);
}
