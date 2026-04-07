using ArchLucid.Contracts.Evolution;
using ArchLucid.Contracts.ProductLearning;

namespace ArchLucid.Persistence.Coordination.Evolution;

/// <summary>Persistence for 60R candidate change sets (scoped).</summary>
public interface IEvolutionCandidateChangeSetRepository
{
    Task InsertAsync(EvolutionCandidateChangeSetRecord record, CancellationToken cancellationToken);

    Task<EvolutionCandidateChangeSetRecord?> GetByIdAsync(
        Guid candidateChangeSetId,
        ProductLearningScope scope,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EvolutionCandidateChangeSetRecord>> ListAsync(
        ProductLearningScope scope,
        int take,
        CancellationToken cancellationToken);

    Task UpdateStatusAsync(
        Guid candidateChangeSetId,
        ProductLearningScope scope,
        string status,
        CancellationToken cancellationToken);
}
