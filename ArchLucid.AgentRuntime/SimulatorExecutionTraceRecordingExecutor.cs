using System.Text.Json;

using ArchLucid.AgentRuntime.Prompts;
using ArchLucid.AgentSimulator.Services;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Requests;

namespace ArchLucid.AgentRuntime;

/// <summary>
/// Wraps <see cref="DeterministicAgentSimulator"/> and persists one <see cref="AgentExecutionTrace"/> per
/// synthetic <see cref="AgentResult"/> so Simulator-mode runs expose the same trace surface as
/// <see cref="RealAgentExecutor"/> (audit API, analysis reports, exports).
/// </summary>
public sealed class SimulatorExecutionTraceRecordingExecutor(
    DeterministicAgentSimulator innerSimulator,
    IAgentExecutionTraceRecorder traceRecorder) : IAgentExecutor
{
    private static readonly JsonSerializerOptions TraceJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    /// <inheritdoc />
    public async Task<IReadOnlyList<AgentResult>> ExecuteAsync(
        string runId,
        ArchitectureRequest request,
        AgentEvidencePackage evidence,
        IReadOnlyCollection<AgentTask> tasks,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AgentResult> results = await innerSimulator.ExecuteAsync(
            runId,
            request,
            evidence,
            tasks,
            cancellationToken);

        ILookup<string, AgentTask> tasksById = tasks.ToLookup(t => t.TaskId);

        foreach (AgentResult result in results)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string resultJson = JsonSerializer.Serialize(result, TraceJsonOptions);
            AgentTask? task = tasksById[result.TaskId].FirstOrDefault();

            string userPrompt = task is not null
                ? $"Simulator task: AgentType={task.AgentType}; Objective: {task.Objective}"
                : $"Simulator task TaskId={result.TaskId} (task row not found in batch).";

            const string simulatorSystem =
                "ArchiForge AgentExecution:Mode=Simulator. Deterministic fake AgentResult (no LLM). " +
                "Traces are persisted for API parity with RealAgentExecutor.";

            AgentPromptReproMetadata promptRepro = new(
                "simulator-deterministic",
                "1.0.0",
                AgentPromptCanonicalHasher.Sha256HexUtf8Normalized(simulatorSystem),
                ReleaseLabel: null);

            await traceRecorder.RecordAsync(
                runId,
                result.TaskId,
                result.AgentType,
                simulatorSystem,
                userPrompt,
                resultJson,
                resultJson,
                parseSucceeded: true,
                errorMessage: null,
                promptRepro,
                cancellationToken);
        }

        return results;
    }
}
