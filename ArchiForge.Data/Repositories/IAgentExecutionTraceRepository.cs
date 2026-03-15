using ArchiForge.Contracts.Agents;

namespace ArchiForge.Data.Repositories;

public interface IAgentExecutionTraceRepository
{
    Task CreateAsync(
        AgentExecutionTrace trace,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgentExecutionTrace>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgentExecutionTrace>> GetByTaskIdAsync(
        string taskId,
        CancellationToken cancellationToken = default);
}
