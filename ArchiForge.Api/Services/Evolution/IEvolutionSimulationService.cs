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
}
