using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Data.Repositories;

public interface IDecisionTraceRepository
{
    Task CreateManyAsync(IEnumerable<DecisionTrace> traces, CancellationToken cancellationToken = default);
}