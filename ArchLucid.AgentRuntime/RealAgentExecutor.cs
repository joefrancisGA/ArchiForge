using System.Diagnostics;

using ArchLucid.AgentSimulator.Services;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Polly;
using Polly.Timeout;

namespace ArchLucid.AgentRuntime;

/// <summary>
/// Production <see cref="IAgentExecutor"/>: resolves <see cref="IAgentHandler"/> by <see cref="AgentTypeKeys.ResolveDispatchKey"/>,
/// runs independent tasks concurrently, and returns <see cref="AgentResult"/> rows in stable dispatch-key order.
/// </summary>
/// <remarks>
/// Handlers share the same <see cref="AgentEvidencePackage"/> and do not consume each other&apos;s outputs in prompts;
/// <see cref="AmbientScopeContext"/> is pushed for the batch so scoped services (e.g. LLM accounting) resolve tenant scope on thread-pool continuations.
/// On any failure, linked cancellation is signaled so in-flight completions can abort promptly.
/// </remarks>
public sealed class RealAgentExecutor : IAgentExecutor
{
    private readonly IReadOnlyDictionary<string, IAgentHandler> _handlers;
    private readonly ILogger<RealAgentExecutor> _logger;
    private readonly IOptionsMonitor<AgentPromptCatalogOptions> _promptCatalog;
    private readonly IScopeContextProvider _scopeContextProvider;
    private readonly IAgentHandlerConcurrencyGate _concurrencyGate;
    private readonly ResiliencePipeline<AgentResult> _handlerTimeoutPipeline;

    /// <summary>Builds a lookup of handlers keyed by <see cref="IAgentHandler.AgentTypeKey"/> (duplicates throw).</summary>
    public RealAgentExecutor(
        IEnumerable<IAgentHandler> handlers,
        ILogger<RealAgentExecutor> logger,
        IOptionsMonitor<AgentPromptCatalogOptions> promptCatalog,
        IScopeContextProvider scopeContextProvider,
        IAgentHandlerConcurrencyGate concurrencyGate,
        IOptions<AgentExecutionResilienceOptions> resilienceOptions)
    {
        ArgumentNullException.ThrowIfNull(handlers);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _promptCatalog = promptCatalog ?? throw new ArgumentNullException(nameof(promptCatalog));
        _scopeContextProvider = scopeContextProvider ?? throw new ArgumentNullException(nameof(scopeContextProvider));
        _concurrencyGate = concurrencyGate ?? throw new ArgumentNullException(nameof(concurrencyGate));
        ArgumentNullException.ThrowIfNull(resilienceOptions);

        AgentExecutionResilienceOptions ro = resilienceOptions.Value;
        ro.Normalize();
        int timeoutSeconds = ro.PerHandlerTimeoutSeconds;

        _handlerTimeoutPipeline = timeoutSeconds <= 0
            ? ResiliencePipeline<AgentResult>.Empty
            : new ResiliencePipelineBuilder<AgentResult>()
                .AddTimeout(TimeSpan.FromSeconds(timeoutSeconds))
                .Build();

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

        AgentTask[] orderedTasks = tasks
            .OrderBy(AgentTypeKeys.ResolveDispatchKey, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (orderedTasks.Length == 0)
        {
            return [];
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            string types = string.Join(
                ',',
                orderedTasks.Select(AgentTypeKeys.ResolveDispatchKey));

            _logger.LogInformation(
                "Agent execution batch starting: RunId={RunId}, TaskCount={TaskCount}, AgentTypeKeys={AgentTypeKeys}",
                runId,
                orderedTasks.Length,
                types);
        }

        ScopeContext batchScope = _scopeContextProvider.GetCurrentScope();

        using (AmbientScopeContext.Push(batchScope))
        using (CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            Task<AgentResult>[] work = orderedTasks
                .Select(task => ExecuteSingleAsync(runId, request, evidence, task, linked.Token))
                .ToArray();

            try
            {
                AgentResult[] finished = await Task.WhenAll(work).ConfigureAwait(false);

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        "Agent execution batch completed: RunId={RunId}, ResultCount={ResultCount}",
                        runId,
                        finished.Length);
                }

                return finished;
            }
            catch
            {
                await linked.CancelAsync();
                throw;
            }
        }
    }

    private async Task<AgentResult> ExecuteSingleAsync(
        string runId,
        ArchitectureRequest request,
        AgentEvidencePackage evidence,
        AgentTask task,
        CancellationToken cancellationToken)
    {
        string dispatchKey = AgentTypeKeys.ResolveDispatchKey(task);

        if (!_handlers.TryGetValue(dispatchKey, out IAgentHandler? handler))
        {
            throw new InvalidOperationException(
                $"No handler is registered for agent type key '{dispatchKey}'.");
        }

        Stopwatch sw = Stopwatch.StartNew();

        AgentResult result;

        using (Activity? activity = ArchLucidInstrumentation.AgentHandler.StartActivity(
                   "archiforge.agent.handle"))
        {
            activity?.SetTag("archiforge.run_id", runId);
            activity?.SetTag("archiforge.task_id", task.TaskId);
            activity?.SetTag("archiforge.agent.type", dispatchKey);
            activity?.SetTag("archiforge.agent.type_enum", task.AgentType.ToString());

            string promptVersion = ResolvePromptVersion(dispatchKey);
            activity?.SetTag("archiforge.agent.prompt_version", promptVersion);

            try
            {
                result = await _concurrencyGate.ExecuteAsync(
                    async ct =>
                        await _handlerTimeoutPipeline.ExecuteAsync(
                            async (_, innerCt) => await handler.ExecuteAsync(
                                runId,
                                request,
                                evidence,
                                task,
                                innerCt),
                            ct),
                    cancellationToken);

                ArchLucidInstrumentation.AgentHandlerInvocationsTotal.Add(
                    1,
                    new KeyValuePair<string, object?>("agent_type_key", dispatchKey),
                    new KeyValuePair<string, object?>("outcome", "success"));
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.AddException(ex);

                ArchLucidInstrumentation.AgentHandlerInvocationsTotal.Add(
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

        return result;
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
