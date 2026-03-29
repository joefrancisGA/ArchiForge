using ArchiForge.AgentSimulator.Services;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Requests;

namespace ArchiForge.AgentRuntime;

/// <summary>
/// Production <see cref="IAgentExecutor"/>: resolves <see cref="IAgentHandler"/> per <see cref="AgentTask.AgentType"/> and runs tasks sequentially.
/// </summary>
public sealed class RealAgentExecutor : IAgentExecutor
{
    private readonly IReadOnlyDictionary<AgentType, IAgentHandler> _handlers;

    /// <summary>Builds a lookup of handlers keyed by <see cref="IAgentHandler.AgentType"/> (duplicate types throw at construction).</summary>
    /// <param name="handlers">All registered agent handlers.</param>
    public RealAgentExecutor(IEnumerable<IAgentHandler> handlers)
    {
        _handlers = handlers.ToDictionary(h => h.AgentType);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AgentResult>> ExecuteAsync(
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

        foreach (AgentTask task in tasks.OrderBy(t => t.AgentType))
        {
            if (!_handlers.TryGetValue(task.AgentType, out IAgentHandler? handler))
            {
                throw new InvalidOperationException(
                    $"No handler is registered for agent type '{task.AgentType}'.");
            }

            AgentResult result = await handler.ExecuteAsync(
                runId,
                request,
                evidence,
                task,
                cancellationToken);

            results.Add(result);
        }

        return results;
    }
}
