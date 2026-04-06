namespace ArchiForge.Api.Models.Learning;

public sealed class LearningPlansListResponse
{
    public DateTime GeneratedUtc { get; init; }

    public IReadOnlyList<LearningPlanListItemResponse> Plans { get; init; } = [];
}
