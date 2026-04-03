using System.Collections.Concurrent;

using ArchiForge.Contracts.Evolution;

namespace ArchiForge.Persistence.Evolution;

/// <summary>In-memory 60R simulation rows for StorageProvider=InMemory.</summary>
public sealed class InMemoryEvolutionSimulationRunRepository : IEvolutionSimulationRunRepository
{
    private readonly ConcurrentDictionary<Guid, EvolutionSimulationRunRecord> _byId = new();

    public Task InsertAsync(EvolutionSimulationRunRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_byId.TryAdd(record.SimulationRunId, record))
        {
            throw new InvalidOperationException($"Simulation run '{record.SimulationRunId}' already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<EvolutionSimulationRunRecord>> ListByCandidateAsync(
        Guid candidateChangeSetId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        List<EvolutionSimulationRunRecord> list = _byId.Values
            .Where(r => r.CandidateChangeSetId == candidateChangeSetId)
            .OrderBy(static r => r.BaselineArchitectureRunId, StringComparer.Ordinal)
            .ThenBy(static r => r.CompletedUtc)
            .ToList();

        return Task.FromResult<IReadOnlyList<EvolutionSimulationRunRecord>>(list);
    }
}
