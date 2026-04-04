using System.Diagnostics;

using ArchiForge.AgentSimulator.Services;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Requests;
using ArchiForge.Core.Configuration;
using ArchiForge.Core.Diagnostics;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchiForge.AgentRuntime;

/// <summary>
/// Production <see cref="IAgentExecutor"/>: resolves <see cref="IAgentHandler"/> by <see cref="AgentTypeKeys.ResolveDispatchKey"/> and runs tasks sequentially.
/// </summary>
public sealed class RealAgentExecutor : IAgentExecutor
{
    private readonly IReadOnlyDictionary<string, IAgentHandler> _handlers;
    private readonly ILogger<RealAgentExecutor> _logger;
    private readonly IOptionsMonitor<AgentPromptCatalogOptions> _promptCatalog;

    /// <summary>Builds a lookup of handlers keyed by <see cref="IAgentHandler.AgentTypeKey"/> (duplicates throw).</summary>
    public RealAgentExecutor(
        IEnumerable<IAgentHandler> handlers,
        ILogger<RealAgentExecutor> logger,
        IOptionsMonitor<AgentPromptCatalogOptions> promptCatalog)
    {
        ArgumentNullException.ThrowIfNull(handlers);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _promptCatalog = promptCatalog ?? throw new ArgumentNullException(nameof(promptCatalog));

        List<IAgentHandler> list = handlers.ToList();
        string[] duplicateKeys = list
            .GroupBy(h => h.AgentTypeKey, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();

        if (duplicateKeys.Length > 0)
        {
            throw new ArgumentException(
                $"Duplicate IAgentHandler registrations for keys: {string.Join(", ", duplicateKeys)}",
                nameof(handlers));
        }

        _handlers = list.ToDictionary(h => h.AgentTypeKey, StringComparer.OrdinalIgnoreCase);
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
                tasks
                    .Select(t => AgentTypeKeys.ResolveDispatchKey(t))
                    .OrderBy(k => k, StringComparer.OrdinalIgnoreCase));
            _logger.LogInformation(
                "Agent execution batch starting: RunId={RunId}, TaskCount={TaskCount}, AgentTypeKeys={AgentTypeKeys}",
                runId,
                tasks.Count,
                types);
        }

        IOrderedEnumerable<AgentTask> ordered = tasks.OrderBy(
            t => AgentTypeKeys.ResolveDispatchKey(t),
            StringComparer.OrdinalIgnoreCase);

        foreach (AgentTask task in ordered)
        {
            string dispatchKey = AgentTypeKeys.ResolveDispatchKey(task);

            if (!_handlers.TryGetValue(dispatchKey, out IAgentHandler? handler))
            {
                throw new InvalidOperationException(
                    $"No handler is registered for agent type key '{dispatchKey}'.");
            }

            Stopwatch sw = Stopwatch.StartNew();

            AgentResult result;

            using (Activity? activity = ArchiForgeInstrumentation.AgentHandler.StartActivity(
                       "archiforge.agent.handle",
                       ActivityKind.Internal))
            {
                activity?.SetTag("archiforge.run_id", runId);
                activity?.SetTag("archiforge.task_id", task.TaskId);
                activity?.SetTag("archiforge.agent.type", dispatchKey);
                activity?.SetTag("archiforge.agent.type_enum", task.AgentType.ToString());

                string promptVersion = ResolvePromptVersion(dispatchKey);
                activity?.SetTag("archiforge.agent.prompt_version", promptVersion);

                try
                {
                    result = await handler.ExecuteAsync(
                        runId,
                        request,
                        evidence,
                        task,
                        cancellationToken);

                    ArchiForgeInstrumentation.AgentHandlerInvocationsTotal.Add(
                        1,
                        new KeyValuePair<string, object?>("agent_type_key", dispatchKey),
                        new KeyValuePair<string, object?>("outcome", "success"));
                }
                catch (Exception ex)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    activity?.AddException(ex);

                    ArchiForgeInstrumentation.AgentHandlerInvocationsTotal.Add(
                        1,
                        new KeyValuePair<string, object?>("agent_type_key", dispatchKey),
                        new KeyValuePair<string, object?>("outcome", "error"));

                    throw;
                }

                activity?.SetTag("archiforge.agent.confidence", result.Confidence);
                activity?.SetTag("archiforge.agent.findings_count", result.Findings.Count);
                activity?.SetTag("archiforge.agent.claims_count", result.Claims.Count);
            }

            sw.Stop();

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Agent task finished: RunId={RunId}, TaskId={TaskId}, AgentTypeKey={AgentTypeKey}, DurationMs={DurationMs}",
                    runId,
                    task.TaskId,
                    dispatchKey,
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

    private string ResolvePromptVersion(string agentTypeKey)
    {
        AgentPromptCatalogOptions current = _promptCatalog.CurrentValue;

        if (current.Versions.TryGetValue(agentTypeKey, out string? v) && !string.IsNullOrWhiteSpace(v))
        {
            return v.Trim();
        }

        return "default";
    }
}
