using ArchiForge.AgentSimulator.Services;
using System.Diagnostics;

using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Requests;

using Microsoft.Extensions.Logging;

namespace ArchiForge.AgentRuntime;

/// <summary>
/// Production <see cref="IAgentExecutor"/>: resolves <see cref="IAgentHandler"/> per <see cref="AgentTask.AgentType"/> and runs tasks sequentially.
/// </summary>
public sealed class RealAgentExecutor : IAgentExecutor
{
    private readonly IReadOnlyDictionary<AgentType, IAgentHandler> _handlers;
    private readonly ILogger<RealAgentExecutor> _logger;

    /// <summary>Builds a lookup of handlers keyed by <see cref="IAgentHandler.AgentType"/> (duplicate types throw at construction).</summary>
    /// <param name="handlers">All registered agent handlers.</param>
    public RealAgentExecutor(IEnumerable<IAgentHandler> handlers, ILogger<RealAgentExecutor> logger)
    {
        _handlers = handlers.ToDictionary(h => h.AgentType);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        if (_logger.IsEnabled(LogLevel.Information))
        {
            string types = string.Join(
                ',',
                tasks.OrderBy(t => t.AgentType).Select(t => t.AgentType.ToString()));
            _logger.LogInformation(
                "Agent execution batch starting: RunId={RunId}, TaskCount={TaskCount}, AgentTypes={AgentTypes}",
                runId,
                tasks.Count,
                types);
        }

        foreach (AgentTask task in tasks.OrderBy(t => t.AgentType))
        {
            if (!_handlers.TryGetValue(task.AgentType, out IAgentHandler? handler))
            {
                throw new InvalidOperationException(
                    $"No handler is registered for agent type '{task.AgentType}'.");
            }

            Stopwatch sw = Stopwatch.StartNew();

            AgentResult result = await handler.ExecuteAsync(
                runId,
                request,
                evidence,
                task,
                cancellationToken);

            sw.Stop();

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Agent task finished: RunId={RunId}, TaskId={TaskId}, AgentType={AgentType}, DurationMs={DurationMs}",
                    runId,
                    task.TaskId,
                    task.AgentType,
                    sw.ElapsedMilliseconds);
            }

            results.Add(result);
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Agent execution batch completed: RunId={RunId}, ResultCount={ResultCount}",
                runId,
                results.Count);
        }

        return results;
    }
}
