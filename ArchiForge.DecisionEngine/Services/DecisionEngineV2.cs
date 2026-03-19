using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Decisions;
using ArchiForge.Contracts.Requests;

namespace ArchiForge.DecisionEngine.Services;

/// <summary>
/// Decision Engine v2: weighted argument resolution (deterministic, v1 scoring model).
/// </summary>
public sealed class DecisionEngineV2 : IDecisionEngineV2
{
    public Task<IReadOnlyList<DecisionNode>> ResolveAsync(
        string runId,
        ArchitectureRequest request,
        AgentEvidencePackage evidence,
        IReadOnlyCollection<AgentTask> tasks,
        IReadOnlyCollection<AgentResult> results,
        IReadOnlyCollection<AgentEvaluation> evaluations,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(evidence);
        ArgumentNullException.ThrowIfNull(tasks);
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(evaluations);

        var decisions = new List<DecisionNode>();

        var topologyTask = tasks.FirstOrDefault(t => t.AgentType == AgentType.Topology);
        var topologyResult = results.FirstOrDefault(r => r.AgentType == AgentType.Topology);

        if (topologyTask is null || topologyResult is null)
        {
            return Task.FromResult<IReadOnlyList<DecisionNode>>(decisions);
        }

        decisions.Add(BuildTopologyAcceptanceDecision(runId, topologyTask, topologyResult, evaluations));
        decisions.Add(BuildSecurityControlsDecision(runId, topologyTask, evaluations));
        decisions.Add(BuildComplexityDecision(runId, topologyTask, evaluations));

        return Task.FromResult<IReadOnlyList<DecisionNode>>(decisions);
    }

    private static DecisionNode BuildTopologyAcceptanceDecision(
        string runId,
        AgentTask topologyTask,
        AgentResult topologyResult,
        IReadOnlyCollection<AgentEvaluation> evaluations)
    {
        var relevant = evaluations
            .Where(e => e.TargetAgentTaskId == topologyTask.TaskId)
            .ToList();

        var baseConfidence = topologyResult.Confidence;
        var support = relevant
            .Where(e => e.EvaluationType.Equals("support", StringComparison.OrdinalIgnoreCase) ||
                        e.EvaluationType.Equals("strengthen", StringComparison.OrdinalIgnoreCase))
            .Sum(e => Math.Max(0, e.ConfidenceDelta));

        var opposition = relevant
            .Where(e => e.EvaluationType.Equals("oppose", StringComparison.OrdinalIgnoreCase) ||
                        e.EvaluationType.Equals("caution", StringComparison.OrdinalIgnoreCase))
            .Sum(e => Math.Abs(e.ConfidenceDelta));

        var accept = new DecisionOption
        {
            Description = "Accept topology proposal",
            BaseConfidence = baseConfidence,
            SupportScore = support,
            OppositionScore = opposition,
            EvidenceRefs = relevant.SelectMany(e => e.EvidenceRefs).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        };

        var reject = new DecisionOption
        {
            Description = "Reject topology proposal",
            BaseConfidence = 0.10,
            SupportScore = opposition,
            OppositionScore = support
        };

        var selected = accept.FinalScore >= reject.FinalScore ? accept : reject;

        return new DecisionNode
        {
            RunId = runId,
            Topic = "TopologyAcceptance",
            Options = [accept, reject],
            SelectedOptionId = selected.OptionId,
            Confidence = selected.FinalScore,
            Rationale = selected == accept
                ? "Topology proposal retained after applying support and opposition signals."
                : "Topology proposal rejected due to accumulated opposition signals.",
            SupportingEvaluationIds = relevant
                .Where(e => e.EvaluationType.Equals("support", StringComparison.OrdinalIgnoreCase) ||
                            e.EvaluationType.Equals("strengthen", StringComparison.OrdinalIgnoreCase))
                .Select(e => e.EvaluationId)
                .ToList(),
            OpposingEvaluationIds = relevant
                .Where(e => e.EvaluationType.Equals("oppose", StringComparison.OrdinalIgnoreCase) ||
                            e.EvaluationType.Equals("caution", StringComparison.OrdinalIgnoreCase))
                .Select(e => e.EvaluationId)
                .ToList(),
            CreatedUtc = DateTime.UtcNow
        };
    }

    private static DecisionNode BuildSecurityControlsDecision(
        string runId,
        AgentTask topologyTask,
        IReadOnlyCollection<AgentEvaluation> evaluations)
    {
        var relevant = evaluations
            .Where(e => e.TargetAgentTaskId == topologyTask.TaskId)
            .ToList();

        var promotePrivateEndpoints = relevant.Any(e =>
            e.EvaluationType.Equals("strengthen", StringComparison.OrdinalIgnoreCase) &&
            e.Rationale.Contains("private", StringComparison.OrdinalIgnoreCase));

        var promoteManagedIdentity = relevant.Any(e =>
            e.EvaluationType.Equals("strengthen", StringComparison.OrdinalIgnoreCase) &&
            e.Rationale.Contains("managed identity", StringComparison.OrdinalIgnoreCase));

        var controls = new List<string>();

        if (promotePrivateEndpoints)
            controls.Add("Private Endpoints");

        if (promoteManagedIdentity)
            controls.Add("Managed Identity");

        var promote = new DecisionOption
        {
            Description = controls.Count == 0
                ? "No control promotion"
                : $"Promote controls: {string.Join(", ", controls)}",
            BaseConfidence = controls.Count == 0 ? 0.30 : 0.80,
            SupportScore = relevant.Where(e => e.EvaluationType.Equals("strengthen", StringComparison.OrdinalIgnoreCase))
                .Sum(e => Math.Max(0, e.ConfidenceDelta)),
            OppositionScore = 0
        };

        return new DecisionNode
        {
            RunId = runId,
            Topic = "SecurityControlPromotion",
            Options = [promote],
            SelectedOptionId = promote.OptionId,
            Confidence = promote.FinalScore,
            Rationale = promote.Description,
            SupportingEvaluationIds = relevant
                .Where(e => e.EvaluationType.Equals("strengthen", StringComparison.OrdinalIgnoreCase))
                .Select(e => e.EvaluationId)
                .ToList(),
            CreatedUtc = DateTime.UtcNow
        };
    }

    private static DecisionNode BuildComplexityDecision(
        string runId,
        AgentTask topologyTask,
        IReadOnlyCollection<AgentEvaluation> evaluations)
    {
        var relevant = evaluations
            .Where(e => e.TargetAgentTaskId == topologyTask.TaskId)
            .ToList();

        var cautions = relevant
            .Where(e => e.EvaluationType.Equals("caution", StringComparison.OrdinalIgnoreCase) ||
                        e.EvaluationType.Equals("oppose", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var keep = new DecisionOption
        {
            Description = "Keep current solution complexity",
            BaseConfidence = 0.60,
            SupportScore = relevant.Where(e => e.EvaluationType.Equals("support", StringComparison.OrdinalIgnoreCase))
                .Sum(e => Math.Max(0, e.ConfidenceDelta)),
            OppositionScore = cautions.Sum(e => Math.Abs(e.ConfidenceDelta))
        };

        var reduce = new DecisionOption
        {
            Description = "Reduce complexity / consider MVP trimming",
            BaseConfidence = cautions.Count > 0 ? 0.65 : 0.20,
            SupportScore = cautions.Sum(e => Math.Abs(e.ConfidenceDelta)),
            OppositionScore = 0
        };

        var selected = keep.FinalScore >= reduce.FinalScore ? keep : reduce;

        return new DecisionNode
        {
            RunId = runId,
            Topic = "ComplexityDisposition",
            Options = [keep, reduce],
            SelectedOptionId = selected.OptionId,
            Confidence = selected.FinalScore,
            Rationale = selected == keep
                ? "Complexity retained because opposition did not outweigh the base design confidence."
                : "Complexity reduction recommended due to caution and opposition signals.",
            SupportingEvaluationIds = relevant
                .Where(e => e.EvaluationType.Equals("support", StringComparison.OrdinalIgnoreCase))
                .Select(e => e.EvaluationId)
                .ToList(),
            OpposingEvaluationIds = cautions.Select(e => e.EvaluationId).ToList(),
            CreatedUtc = DateTime.UtcNow
        };
    }
}

