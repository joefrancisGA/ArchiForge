using ArchiForge.Contracts.Decisions;

namespace ArchiForge.Data.Repositories;

public interface IDecisionNodeRepository
{
    Task CreateManyAsync(
        IReadOnlyCollection<DecisionNode> decisions,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DecisionNode>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default);
}

