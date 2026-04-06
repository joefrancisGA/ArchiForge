namespace ArchiForge.Api.Models.Evolution;

/// <summary>GET results: candidate, plan snapshot, and simulation outcomes with evaluation scores.</summary>
public sealed class EvolutionResultsResponse
{
    public required EvolutionCandidateChangeSetResponse Candidate { get; init; }

    public required string PlanSnapshotJson { get; init; }

    public IReadOnlyList<EvolutionSimulationRunWithEvaluationResponse> SimulationRuns { get; init; } = [];
}
