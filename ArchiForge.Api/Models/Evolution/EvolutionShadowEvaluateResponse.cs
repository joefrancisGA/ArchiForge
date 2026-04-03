namespace ArchiForge.Api.Models.Evolution;

public sealed class EvolutionShadowEvaluateResponse
{
    public IReadOnlyList<EvolutionSimulationRunResponse> SimulationRuns { get; init; } = [];
}
