using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Requests;

namespace ArchiForge.AgentRuntime;

public interface IAgentHandler
{
    AgentType AgentType { get; }

    Task<AgentResult> ExecuteAsync(
        string runId,
        ArchitectureRequest request,
        AgentTask task,
        CancellationToken cancellationToken = default);
}
