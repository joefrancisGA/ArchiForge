namespace ArchiForge.Api.Models.Learning;

public sealed class LearningPlanStepResponse
{
    public int Ordinal { get; init; }

    public string ActionType { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string? AcceptanceCriteria { get; init; }
}
