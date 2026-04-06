namespace ArchiForge.Api.Services.Evolution;

/// <summary>Serializable snapshot of a 59R plan for audit/explainability on a 60R candidate.</summary>
public sealed class EvolutionPlanSnapshotDocument
{
    public Guid PlanId { get; init; }

    public Guid ThemeId { get; init; }

    public required string Title { get; init; }

    public required string Summary { get; init; }

    public int PriorityScore { get; init; }

    public string? PriorityExplanation { get; init; }

    public required string Status { get; init; }

    public int ActionStepCount { get; init; }

    public IReadOnlyList<string> LinkedArchitectureRunIds { get; init; } = [];
}
