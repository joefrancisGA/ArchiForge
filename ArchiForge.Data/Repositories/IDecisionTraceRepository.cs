using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Data.Repositories;

public interface IDecisionTraceRepository
{
    Task CreateManyAsync(IEnumerable<DecisionTrace> traces, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DecisionTrace>> GetByRunIdAsync(string runId, CancellationToken cancellationToken = default);
}