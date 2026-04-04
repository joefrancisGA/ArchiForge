using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Requests;

namespace ArchiForge.AgentSimulator.Services;

public sealed class DeterministicAgentSimulator : IAgentExecutor
{
    public Task<IReadOnlyList<AgentResult>> ExecuteAsync(
        string runId,
        ArchitectureRequest request,
        AgentEvidencePackage evidence,
        IReadOnlyCollection<AgentTask> tasks,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(evidence);
        ArgumentNullException.ThrowIfNull(tasks);

        List<AgentResult> results = [];

        foreach (AgentTask task in tasks.OrderBy(t => AgentTypeKeys.ResolveDispatchKey(t), StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.Equals(task.RunId, runId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Task '{task.TaskId}' belongs to run '{task.RunId}', not '{runId}'. " +
                    "Tasks from a different run must not be executed together.");
            }

            string key = AgentTypeKeys.ResolveDispatchKey(task);
            AgentResult result = CreateResultForKey(runId, task.TaskId, request, key);
            results.Add(result);
        }

        return Task.FromResult<IReadOnlyList<AgentResult>>(results);
    }

    private static AgentResult CreateResultForKey(
        string runId,
        string taskId,
        ArchitectureRequest request,
        string agentTypeKey)
    {
        if (string.Equals(agentTypeKey, AgentTypeKeys.Topology, StringComparison.OrdinalIgnoreCase))
        {
            return FakeScenarioFactory.CreateTopologyResult(runId, taskId, request);
        }

        if (string.Equals(agentTypeKey, AgentTypeKeys.Cost, StringComparison.OrdinalIgnoreCase))
        {
            return FakeScenarioFactory.CreateCostResult(runId, taskId, request);
        }

        if (string.Equals(agentTypeKey, AgentTypeKeys.Compliance, StringComparison.OrdinalIgnoreCase))
        {
            return FakeScenarioFactory.CreateComplianceResult(runId, taskId, request);
        }

        if (string.Equals(agentTypeKey, AgentTypeKeys.Critic, StringComparison.OrdinalIgnoreCase))
        {
            return FakeScenarioFactory.CreateCriticResult(runId, taskId, request);
        }

        throw new InvalidOperationException($"Unsupported agent type key: {agentTypeKey}");
    }
}
