using ArchLucid.AgentRuntime.Evaluation.ReferenceCases;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Findings;
using ArchLucid.Decisioning.Findings;
using ArchLucid.Decisioning.Findings.Factories;
using ArchLucid.Decisioning.Models;
using ArchLucid.Persistence.Data.Repositories;

using Microsoft.Extensions.Logging;

namespace ArchLucid.AgentRuntime.Evaluation;

/// <summary>
///     After trace evaluation metrics run, stamps each <see cref="ArchitectureFinding" /> with deterministic evaluation
///     confidence (never throws to callers).
/// </summary>
public sealed class AgentArchitectureFindingConfidenceEnricher(
    IAgentResultRepository agentResultRepository,
    IAgentExecutionTraceRepository traceRepository,
    IAgentOutputEvaluator structuralEvaluator,
    IAgentOutputSemanticEvaluator semanticEvaluator,
    IAgentOutputQualityGate qualityGate,
    AgentOutputReferenceCaseRunEvaluator referenceCaseRunEvaluator,
    FindingConfidenceCalculator confidenceCalculator,
    ILogger<AgentArchitectureFindingConfidenceEnricher> logger) : IAgentArchitectureFindingConfidenceEnricher
{
    private readonly IAgentResultRepository _agentResultRepository =
        agentResultRepository ?? throw new ArgumentNullException(nameof(agentResultRepository));

    private readonly IAgentExecutionTraceRepository _traceRepository =
        traceRepository ?? throw new ArgumentNullException(nameof(traceRepository));

    private readonly IAgentOutputEvaluator _structuralEvaluator =
        structuralEvaluator ?? throw new ArgumentNullException(nameof(structuralEvaluator));

    private readonly IAgentOutputSemanticEvaluator _semanticEvaluator =
        semanticEvaluator ?? throw new ArgumentNullException(nameof(semanticEvaluator));

    private readonly IAgentOutputQualityGate _qualityGate =
        qualityGate ?? throw new ArgumentNullException(nameof(qualityGate));

    private readonly AgentOutputReferenceCaseRunEvaluator _referenceCaseRunEvaluator =
        referenceCaseRunEvaluator ?? throw new ArgumentNullException(nameof(referenceCaseRunEvaluator));

    private readonly FindingConfidenceCalculator _confidenceCalculator =
        confidenceCalculator ?? throw new ArgumentNullException(nameof(confidenceCalculator));

    private readonly ILogger<AgentArchitectureFindingConfidenceEnricher> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task TryEnrichRunAsync(string runId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(runId))
            return;

        try
        {
            IReadOnlyList<AgentResult> results =
                await _agentResultRepository.GetByRunIdAsync(runId.Trim(), cancellationToken);

            if (results.Count == 0)
                return;

            IReadOnlyList<AgentExecutionTrace> traces =
                await _traceRepository.GetByRunIdAsync(runId.Trim(), cancellationToken);

            Dictionary<AgentType, AgentExecutionTrace> traceByAgentType = traces
                .GroupBy(static t => t.AgentType)
                .ToDictionary(static g => g.Key, static g => g.First(), comparer: null);

            foreach (AgentResult result in results)
            {
                traceByAgentType.TryGetValue(result.AgentType, out AgentExecutionTrace? traceForAgent);

                bool schemaPassed = traceForAgent is not null &&
                                    AgentOutputFindingConfidenceSignals.ComputeQualityGateAccepted(
                                        traceForAgent,
                                        _structuralEvaluator,
                                        _semanticEvaluator,
                                        _qualityGate);

                bool referenceMatched = traceForAgent is not null &&
                                        _referenceCaseRunEvaluator.ComputeAnyPassingReferenceCase(traceForAgent);

                bool touched = false;

                foreach (ArchitectureFinding finding in result.Findings)
                {
                    Finding shaped = FindingFactory.CreateFromAgentArchitectureFinding(finding, result, traceForAgent);

                    TraceCompletenessScore completeness = ExplainabilityTraceCompletenessAnalyzer.AnalyzeFinding(shaped);

                    FindingConfidenceCalculationResult? calculated = _confidenceCalculator.Calculate(
                        schemaPassed,
                        referenceMatched,
                        (decimal)completeness.CompletenessRatio);

                    if (calculated is null)
                        continue;

                    finding.EvaluationConfidenceScore = calculated.Score;
                    finding.ConfidenceLevel = calculated.Level;
                    touched = true;
                }

                if (touched)
                    await _agentResultRepository.CreateAsync(result, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Warning))

                _logger.LogWarning(
                    ex,
                    "Finding evaluation confidence enrichment failed for run {RunId}; continuing without enriched scores.",
                    runId.Trim());
        }
    }
}
