namespace ArchiForge.Api.Models.Evolution;

/// <summary>POST simulate: candidate plus simulation runs with evaluation scores.</summary>
public sealed class EvolutionSimulateResponse
{
    public required EvolutionCandidateChangeSetResponse Candidate { get; init; }

    public IReadOnlyList<EvolutionSimulationRunWithEvaluationResponse> SimulationRuns { get; init; } = [];
}
