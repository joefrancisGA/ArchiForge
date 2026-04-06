using ArchiForge.Contracts.Evolution;
using ArchiForge.Contracts.ProductLearning;

namespace ArchiForge.Api.Services.Evolution;

/// <summary>60R orchestration: candidates from 59R plans and read-only shadow evaluation (no system mutation).</summary>
public interface IEvolutionSimulationService
{
    Task<EvolutionCandidateChangeSetRecord> CreateCandidateFromImprovementPlanAsync(
        Guid planId,
        ProductLearningScope scope,
        string? createdByUserId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EvolutionSimulationRunRecord>> RunShadowEvaluationAsync(
        Guid candidateChangeSetId,
        ProductLearningScope scope,
        CancellationToken cancellationToken);

    /// <summary>
    /// Clears prior simulation rows for the candidate, re-runs linked baselines, persists outcomes with embedded
    /// evaluation scores (60R-v2 JSON envelope).
    /// </summary>
    Task<IReadOnlyList<EvolutionSimulationRunRecord>> SimulateCandidateWithEvaluationAsync(
        Guid candidateChangeSetId,
        ProductLearningScope scope,
        CancellationToken cancellationToken);
}
