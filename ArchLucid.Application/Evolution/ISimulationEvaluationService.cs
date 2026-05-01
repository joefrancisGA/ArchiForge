namespace ArchLucid.Application.Evolution;

/// <summary>
///     Scores simulation/analysis artifacts using manifest diff, optional determinism results, and deterministic
///     heuristics.
/// </summary>
public interface ISimulationEvaluationService
{
    Task<SimulationEvaluationResult> EvaluateAsync(
        SimulationEvaluationRequest request,
        CancellationToken cancellationToken = default);
}
