using ArchiForge.Contracts.Agents;

namespace ArchiForge.Data.Repositories;

public interface IAgentTaskRepository
{
    Task CreateManyAsync(IEnumerable<AgentTask> tasks, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgentTask>> GetByRunIdAsync(string runId, CancellationToken cancellationToken = default);
}
