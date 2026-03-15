using ArchiForge.AgentSimulator.Services;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Requests;

namespace ArchiForge.AgentRuntime;

public sealed class CriticAgentHandler : IAgentHandler
{
    public AgentType AgentType => AgentType.Critic;

    public Task<AgentResult> ExecuteAsync(
        string runId,
        ArchitectureRequest request,
        AgentTask task,
        CancellationToken cancellationToken = default)
    {
        var result = FakeScenarioFactory.CreateCriticResult(
            runId,
            task.TaskId,
            request);

        return Task.FromResult(result);
    }
}
