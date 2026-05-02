using System.Diagnostics;

using ArchLucid.AgentRuntime.Evaluation.ReferenceCases;
using ArchLucid.Contracts.Agents;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Persistence.Data.Repositories;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.AgentRuntime.Evaluation;

/// <summary>
///     Loads traces for a run, scores parsed JSON shape and semantic quality, and emits OTEL metrics (intended for
///     post-run or batch jobs).
/// </summary>
public sealed class AgentOutputEvaluationRecorder(
    IAgentExecutionTraceRepository traceRepository,
    IAgentOutputEvaluator evaluator,
    IAgentOutputSemanticEvaluator semanticEvaluator,
    IAgentOutputQualityGate qualityGate,
    IOptions<AgentOutputQualityGateOptions> gateOptions,
    AgentOutputReferenceCaseRunEvaluator referenceCaseRunEvaluator,
    ILogger<AgentOutputEvaluationRecorder> logger)
{
    private const double LowStructuralScoreThreshold = 0.5;

    /// <summary>
    ///     Log when semantic score is critically low (product/docs threshold; quality gate uses
    ///     <see cref="AgentOutputQualityGateOptions" />).
    /// </summary>
    private const double LowSemanticScoreThreshold = 0.3;

    private readonly AgentOutputQualityGateOptions _gateOptions =
        (gateOptions ?? throw new ArgumentNullException(nameof(gateOptions))).Value;

    private readonly AgentOutputReferenceCaseRunEvaluator _referenceCaseRunEvaluator =
        referenceCaseRunEvaluator ?? throw new ArgumentNullException(nameof(referenceCaseRunEvaluator));

    /// <summary>
    ///     Evaluates all traces with successful parses and records histogram/counter metrics.
    /// </summary>
    public async Task EvaluateAndRecordMetricsAsync(string runId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(runId);

        IReadOnlyList<AgentExecutionTrace> traces = await traceRepository.GetByRunIdAsync(runId, cancellationToken);

        foreach (AgentExecutionTrace trace in traces)
        {
            if (!trace.ParseSucceeded || string.IsNullOrEmpty(trace.ParsedResultJson))
                continue;

            string agentLabel = trace.AgentType.ToString();
            TagList tags = new() { { "agent_type", agentLabel } };

            AgentOutputEvaluationScore score =
                evaluator.Evaluate(trace.TraceId, trace.ParsedResultJson, trace.AgentType);

            if (score.IsJsonParseFailure)
            {
                ArchLucidInstrumentation.AgentOutputParseFailuresTotal.Add(1, tags);
                continue;
            }

            ArchLucidInstrumentation.AgentOutputStructuralCompletenessRatio.Record(score.StructuralCompletenessRatio,
                tags);

            if (score.StructuralCompletenessRatio < LowStructuralScoreThreshold)

                logger.LogWarningAgentOutputStructuralScoreBelowThreshold(
                    score.StructuralCompletenessRatio,
                    runId,
                    trace.TraceId,
                    agentLabel,
                    score.MissingKeys.Count);

            AgentOutputSemanticScore semanticScore =
                semanticEvaluator.Evaluate(trace.TraceId, trace.ParsedResultJson, trace.AgentType);

            ArchLucidInstrumentation.AgentOutputSemanticScore.Record(semanticScore.OverallSemanticScore, tags);

            AgentOutputQualityGateOutcome gateOutcome = qualityGate.Evaluate(score, semanticScore);
            TagList gateTags = new()
            {
                { "agent_type", agentLabel }, { "outcome", gateOutcome.ToString().ToLowerInvariant() }
            };

            ArchLucidInstrumentation.AgentOutputQualityGateTotal.Add(1, gateTags);

            if (gateOutcome == AgentOutputQualityGateOutcome.Rejected)
            {
                logger.LogWarningAgentOutputQualityGateRejected(
                    runId,
                    trace.TraceId,
                    agentLabel,
                    score.StructuralCompletenessRatio,
                    semanticScore.OverallSemanticScore);

                if (_gateOptions.EnforceOnReject)
                    throw new AgentOutputQualityGateRejectedException(runId, trace.TraceId, agentLabel);
            }

            else if (gateOutcome == AgentOutputQualityGateOutcome.Warned)

                logger.LogWarningAgentOutputQualityGateWarned(
                    runId,
                    trace.TraceId,
                    agentLabel,
                    score.StructuralCompletenessRatio,
                    semanticScore.OverallSemanticScore);

            if (semanticScore.OverallSemanticScore < LowSemanticScoreThreshold)

                logger.LogWarningAgentOutputSemanticScoreBelowThreshold(
                    semanticScore.OverallSemanticScore,
                    runId,
                    trace.TraceId,
                    agentLabel,
                    semanticScore.EmptyClaimCount,
                    semanticScore.IncompleteFindingCount);

            await _referenceCaseRunEvaluator.EvaluateTraceAsync(trace, runId, cancellationToken);
        }
    }
}
