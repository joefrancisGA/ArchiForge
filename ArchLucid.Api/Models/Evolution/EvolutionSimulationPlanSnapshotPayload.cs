namespace ArchiForge.Api.Models.Evolution;

/// <summary>Typed 59R plan snapshot carried on a candidate (same JSON shape as <c>EvolutionPlanSnapshotDocument</c>).</summary>
public sealed class EvolutionSimulationPlanSnapshotPayload
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
