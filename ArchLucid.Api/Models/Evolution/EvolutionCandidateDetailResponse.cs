namespace ArchiForge.Api.Models.Evolution;

/// <summary>Candidate with embedded plan snapshot and simulation history.</summary>
public sealed class EvolutionCandidateDetailResponse
{
    public required EvolutionCandidateChangeSetResponse Candidate { get; init; }

    public required string PlanSnapshotJson { get; init; }

    public IReadOnlyList<EvolutionSimulationRunResponse> SimulationRuns { get; init; } = [];
}
