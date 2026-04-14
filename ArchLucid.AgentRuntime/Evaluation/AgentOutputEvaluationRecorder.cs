using System.Diagnostics;

using ArchLucid.Contracts.Agents;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Persistence.Data.Repositories;

using Microsoft.Extensions.Logging;

namespace ArchLucid.AgentRuntime.Evaluation;

/// <summary>
/// Loads traces for a run, scores parsed JSON shape and semantic quality, and emits OTEL metrics (intended for post-run or batch jobs).
/// </summary>
public sealed class AgentOutputEvaluationRecorder(
    IAgentExecutionTraceRepository traceRepository,
    IAgentOutputEvaluator evaluator,
    IAgentOutputSemanticEvaluator semanticEvaluator,
    ILogger<AgentOutputEvaluationRecorder> logger)
{
    private const double LowStructuralScoreThreshold = 0.5;
    private const double LowSemanticScoreThreshold = 0.5;

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

            string agentLabel = trace.AgentType.ToString();
            TagList tags = new() { { "agent_type", agentLabel } };

            AgentOutputEvaluationScore score = evaluator.Evaluate(trace.TraceId, trace.ParsedResultJson, trace.AgentType);

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

            AgentOutputSemanticScore semanticScore = semanticEvaluator.Evaluate(trace.TraceId, trace.ParsedResultJson, trace.AgentType);

            ArchLucidInstrumentation.AgentOutputSemanticScore.Record(semanticScore.OverallSemanticScore, tags);

            if (semanticScore.OverallSemanticScore < LowSemanticScoreThreshold)
            {
                logger.LogWarning(
                    "Agent output semantic score {Score:F2} below threshold for run {RunId} trace {TraceId} agent {AgentType}; empty claims {EmptyClaims}, incomplete findings {IncompleteFindings}.",
                    semanticScore.OverallSemanticScore,
                    runId,
                    trace.TraceId,
                    agentLabel,
                    semanticScore.EmptyClaimCount,
                    semanticScore.IncompleteFindingCount);
            }
        }
    }
}
