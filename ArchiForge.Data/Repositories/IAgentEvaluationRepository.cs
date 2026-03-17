using ArchiForge.Contracts.Decisions;

namespace ArchiForge.Data.Repositories;

public interface IAgentEvaluationRepository
{
    Task CreateManyAsync(
        IReadOnlyCollection<AgentEvaluation> evaluations,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgentEvaluation>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default);
}

