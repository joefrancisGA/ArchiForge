using System.Diagnostics;

using ArchLucid.Contracts.Agents;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Persistence.Data.Repositories;

using Microsoft.Extensions.Logging;

namespace ArchLucid.AgentRuntime.Evaluation;

/// <summary>
/// Loads traces for a run, scores parsed JSON shape, and emits OTEL metrics (intended for post-run or batch jobs).
/// </summary>
public sealed class AgentOutputEvaluationRecorder(
    IAgentExecutionTraceRepository traceRepository,
    IAgentOutputEvaluator evaluator,
    ILogger<AgentOutputEvaluationRecorder> logger)
{
    private const double LowStructuralScoreThreshold = 0.5;

    /// <summary>
    /// Evaluates all traces with successful parses and records histogram/counter metrics.
    /// </summary>
    public async Task EvaluateAndRecordMetricsAsync(string runId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(runId);

        IReadOnlyList<AgentExecutionTrace> traces = await traceRepository.GetByRunIdAsync(runId, cancellationToken);

        foreach (AgentExecutionTrace trace in traces)
        {
            if (!trace.ParseSucceeded || string.IsNullOrEmpty(trace.ParsedResultJson))
            {
                continue;
            }

            AgentOutputEvaluationScore score = evaluator.Evaluate(trace.TraceId, trace.ParsedResultJson, trace.AgentType);
            string agentLabel = trace.AgentType.ToString();
            TagList tags = new() { { "agent_type", agentLabel } };

            if (score.IsJsonParseFailure)
            {
                ArchLucidInstrumentation.AgentOutputParseFailuresTotal.Add(1, tags);
                continue;
            }

            ArchLucidInstrumentation.AgentOutputStructuralCompletenessRatio.Record(score.StructuralCompletenessRatio, tags);

            if (score.StructuralCompletenessRatio < LowStructuralScoreThreshold)
            {
                logger.LogWarning(
                    "Agent output structural score {Score:F2} below threshold for run {RunId} trace {TraceId} agent {AgentType}; missing key count {MissingCount}.",
                    score.StructuralCompletenessRatio,
                    runId,
                    trace.TraceId,
                    agentLabel,
                    score.MissingKeys.Count);
            }
        }
    }
}
