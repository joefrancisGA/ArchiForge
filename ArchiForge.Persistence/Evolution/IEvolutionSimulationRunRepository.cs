using ArchiForge.Contracts.Evolution;

namespace ArchiForge.Persistence.Evolution;

/// <summary>Persistence for 60R shadow simulation outcomes.</summary>
public interface IEvolutionSimulationRunRepository
{
    Task InsertAsync(EvolutionSimulationRunRecord record, CancellationToken cancellationToken);

    Task<IReadOnlyList<EvolutionSimulationRunRecord>> ListByCandidateAsync(
        Guid candidateChangeSetId,
        CancellationToken cancellationToken);
}
