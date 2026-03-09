using ArchiForge.Contracts.Agents;

namespace ArchiForge.Data.Repositories;

public interface IAgentResultRepository
{
    Task CreateAsync(AgentResult result, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgentResult>> GetByRunIdAsync(string runId, CancellationToken cancellationToken = default);
}