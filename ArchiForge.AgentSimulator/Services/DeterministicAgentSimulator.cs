using ArchiForge.Contracts.Agents;
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

        List<AgentResult> results = new List<AgentResult>();

        foreach (AgentTask task in tasks.OrderBy(t => t.AgentType))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.Equals(task.RunId, runId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Task '{task.TaskId}' belongs to run '{task.RunId}', not '{runId}'. " +
                    "Tasks from a different run must not be executed together.");
            }

            AgentResult result = task.AgentType switch
            {
                Contracts.Common.AgentType.Topology => CreateTopologyResult(runId, task.TaskId, request),
                Contracts.Common.AgentType.Cost => CreateCostResult(runId, task.TaskId, request),
                Contracts.Common.AgentType.Compliance => CreateComplianceResult(runId, task.TaskId, request),
                Contracts.Common.AgentType.Critic => CreateCriticResult(runId, task.TaskId, request),
                _ => throw new InvalidOperationException($"Unsupported agent type: {task.AgentType}")
            };

            results.Add(result);
        }

        return Task.FromResult<IReadOnlyList<AgentResult>>(results);
    }

    private static AgentResult CreateTopologyResult(
        string runId,
        string taskId,
        ArchitectureRequest request)
    {
        return FakeScenarioFactory.CreateTopologyResult(runId, taskId, request);
    }

    private static AgentResult CreateCostResult(
        string runId,
        string taskId,
        ArchitectureRequest request)
    {
        return FakeScenarioFactory.CreateCostResult(runId, taskId, request);
    }

    private static AgentResult CreateComplianceResult(
        string runId,
        string taskId,
        ArchitectureRequest request)
    {
        return FakeScenarioFactory.CreateComplianceResult(runId, taskId, request);
    }

    private static AgentResult CreateCriticResult(
        string runId,
        string taskId,
        ArchitectureRequest request)
    {
        return FakeScenarioFactory.CreateCriticResult(runId, taskId, request);
    }
}
