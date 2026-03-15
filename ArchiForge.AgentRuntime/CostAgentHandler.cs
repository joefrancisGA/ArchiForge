using ArchiForge.AgentSimulator.Services;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Requests;

namespace ArchiForge.AgentRuntime;

public sealed class CostAgentHandler : IAgentHandler
{
    public AgentType AgentType => AgentType.Cost;

    public Task<AgentResult> ExecuteAsync(
        string runId,
        ArchitectureRequest request,
        AgentTask task,
        CancellationToken cancellationToken = default)
    {
        var result = FakeScenarioFactory.CreateCostResult(
            runId,
            task.TaskId,
            request);

        return Task.FromResult(result);
    }
}
