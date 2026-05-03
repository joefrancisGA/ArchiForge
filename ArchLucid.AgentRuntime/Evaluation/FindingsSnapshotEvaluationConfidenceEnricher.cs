using ArchLucid.AgentRuntime.Evaluation.ReferenceCases;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Decisioning.Findings;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Models;
using ArchLucid.Persistence.Data.Repositories;

using Microsoft.Extensions.Logging;

namespace ArchLucid.AgentRuntime.Evaluation;

/// <summary>
///     Best-effort enrichment of persisted findings snapshots using agent execution traces (never throws).
/// </summary>
public sealed class FindingsSnapshotEvaluationConfidenceEnricher(
    IAgentExecutionTraceRepository traceRepository,
    IAgentOutputEvaluator structuralEvaluator,
    IAgentOutputSemanticEvaluator semanticEvaluator,
    IAgentOutputQualityGate qualityGate,
    AgentOutputReferenceCaseRunEvaluator referenceCaseRunEvaluator,
    FindingConfidenceCalculator confidenceCalculator,
    ILogger<FindingsSnapshotEvaluationConfidenceEnricher> logger)
    : IFindingsSnapshotEvaluationConfidenceEnricher
{
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

    private readonly ILogger<FindingsSnapshotEvaluationConfidenceEnricher> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task TryEnrichAsync(FindingsSnapshot snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (snapshot.Findings.Count == 0)
            return;

        try
        {
            string runKey = snapshot.RunId.ToString("N");
            IReadOnlyList<AgentExecutionTrace> traces =
                await _traceRepository.GetByRunIdAsync(runKey, cancellationToken);

            Dictionary<AgentType, AgentExecutionTrace> traceByAgentType = traces
                .GroupBy(static t => t.AgentType)
                .ToDictionary(static g => g.Key, static g => g.First());

            foreach (Finding finding in snapshot.Findings)
            {
                AgentExecutionTrace? trace = ResolveTraceForFinding(finding, traces, traceByAgentType);

                bool schemaPassed = trace is not null &&
                                    AgentOutputFindingConfidenceSignals.ComputeQualityGateAccepted(
                                        trace,
                                        _structuralEvaluator,
                                        _semanticEvaluator,
                                        _qualityGate);

                bool referenceMatched = trace is not null &&
                                        _referenceCaseRunEvaluator.ComputeAnyPassingReferenceCase(trace);

                TraceCompletenessScore completeness = ExplainabilityTraceCompletenessAnalyzer.AnalyzeFinding(finding);

                FindingConfidenceCalculationResult? calculated = _confidenceCalculator.Calculate(
                    schemaPassed,
                    referenceMatched,
                    (decimal)completeness.CompletenessRatio);

                if (calculated is null)
                    continue;

                finding.EvaluationConfidenceScore = calculated.Score;
                finding.ConfidenceLevel = calculated.Level;
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
                    "Findings snapshot evaluation confidence enrichment failed for run {RunId}; snapshot saved without enrichment.",
                    snapshot.RunId.ToString("N"));
        }
    }

    private static AgentExecutionTrace? ResolveTraceForFinding(
        Finding finding,
        IReadOnlyList<AgentExecutionTrace> traces,
        IReadOnlyDictionary<AgentType, AgentExecutionTrace> traceByAgentType)
    {
        string? key = finding.AgentExecutionTraceId ?? finding.Trace.SourceAgentExecutionTraceId;

        if (!string.IsNullOrWhiteSpace(key))

            foreach (AgentExecutionTrace trace in traces)
            {
                if (TraceIdsLikelyMatch(trace.TraceId, key))
                    return trace;
            }

        if (Enum.TryParse(finding.EngineType, ignoreCase: true, out AgentType engineType) &&
            traceByAgentType.TryGetValue(engineType, out AgentExecutionTrace? byEngine))
            return byEngine;

        return null;
    }

    private static bool TraceIdsLikelyMatch(string persistedTraceId, string findingKey)
    {
        if (string.Equals(persistedTraceId, findingKey, StringComparison.OrdinalIgnoreCase))
            return true;

        int n = Math.Min(32, Math.Min(persistedTraceId.Length, findingKey.Length));

        if (n == 0)
            return false;

        return persistedTraceId.AsSpan(0, n).Equals(findingKey.AsSpan(0, n), StringComparison.OrdinalIgnoreCase);
    }
}
