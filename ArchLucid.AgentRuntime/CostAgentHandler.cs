using ArchLucid.AgentSimulator.Services;
using ArchLucid.Contracts.Abstractions.Agents;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;

namespace ArchLucid.AgentRuntime;

/// <summary>
///     <see cref="Contracts.Common.AgentType.Cost" /> handler for dev/test: returns deterministic
///     <see cref="AgentResult" /> from <see cref="FakeScenarioFactory" /> without calling an LLM.
/// </summary>
public sealed class CostAgentHandler : IAgentHandler
{
    public AgentType AgentType => AgentType.Cost;

    /// <inheritdoc />
    public string AgentTypeKey => AgentTypeKeys.Cost;

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
