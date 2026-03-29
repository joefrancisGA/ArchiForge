using ArchiForge.AgentSimulator.Services;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Requests;

namespace ArchiForge.AgentRuntime;

/// <summary>
/// <see cref="AgentType.Cost"/> handler for dev/test: returns deterministic <see cref="AgentResult"/> from <see cref="FakeScenarioFactory"/> without calling an LLM.
/// </summary>
public sealed class CostAgentHandler : IAgentHandler
{
    public AgentType AgentType => AgentType.Cost;

    public Task<AgentResult> ExecuteAsync(
        string runId,
        ArchitectureRequest request,
        AgentEvidencePackage evidence,
        AgentTask task,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(evidence);
        ArgumentNullException.ThrowIfNull(task);
        AgentResult result = FakeScenarioFactory.CreateCostResult(
            runId,
            task.TaskId,
            request);

        return Task.FromResult(result);
    }
}
