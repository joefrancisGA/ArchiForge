using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Decisions;
using ArchiForge.Contracts.Requests;

using EvalTypes = ArchiForge.Contracts.Decisions.EvaluationTypes;

namespace ArchiForge.DecisionEngine.Services;

/// <summary>
/// Decision Engine v2: weighted argument resolution (deterministic, v1 scoring model).
/// </summary>
public sealed class DecisionEngineV2 : IDecisionEngineV2
{
    public Task<IReadOnlyList<DecisionNode>> ResolveAsync(
        string runId,
        ArchitectureRequest request,
        IReadOnlyCollection<AgentTask> tasks,
        IReadOnlyCollection<AgentResult> results,
        IReadOnlyCollection<AgentEvaluation> evaluations,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(tasks);
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(evaluations);
        cancellationToken.ThrowIfCancellationRequested();

        List<DecisionNode> decisions = new();

        AgentTask? topologyTask = tasks.FirstOrDefault(t => t.AgentType == AgentType.Topology);
        AgentResult? topologyResult = results.FirstOrDefault(r => r.AgentType == AgentType.Topology);

        if (topologyTask is null || topologyResult is null)
        {
            return Task.FromResult<IReadOnlyList<DecisionNode>>(decisions);
        }

        decisions.Add(BuildTopologyAcceptanceDecision(runId, topologyTask, topologyResult, evaluations));
        decisions.Add(BuildSecurityControlsDecision(runId, tasks, evaluations));
        decisions.Add(BuildComplexityDecision(runId, tasks, evaluations));

        return Task.FromResult<IReadOnlyList<DecisionNode>>(decisions);
    }

    private static DecisionNode BuildTopologyAcceptanceDecision(
        string runId,
        AgentTask topologyTask,
        AgentResult topologyResult,
        IReadOnlyCollection<AgentEvaluation> evaluations)
    {
        List<AgentEvaluation> relevant = evaluations
            .Where(e => e.TargetAgentTaskId == topologyTask.TaskId)
            .ToList();

        double baseConfidence = topologyResult.Confidence;
        double support = relevant
            .Where(e => e.EvaluationType.Equals(EvalTypes.Support, StringComparison.OrdinalIgnoreCase) ||
                        e.EvaluationType.Equals(EvalTypes.Strengthen, StringComparison.OrdinalIgnoreCase))
            .Sum(e => Math.Max(0, e.ConfidenceDelta));

        double opposition = relevant
            .Where(e => e.EvaluationType.Equals(EvalTypes.Oppose, StringComparison.OrdinalIgnoreCase) ||
                        e.EvaluationType.Equals(EvalTypes.Caution, StringComparison.OrdinalIgnoreCase))
            .Sum(e => Math.Abs(e.ConfidenceDelta));

        DecisionOption accept = new()
        {
            Description = "Accept topology proposal",
            BaseConfidence = baseConfidence,
            SupportScore = support,
            OppositionScore = opposition,
            EvidenceRefs = relevant.SelectMany(e => e.EvidenceRefs).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        };

        DecisionOption reject = new()
        {
            Description = "Reject topology proposal",
            BaseConfidence = 0.10,
            SupportScore = opposition,
            OppositionScore = support
        };

        DecisionOption selected = accept.FinalScore >= reject.FinalScore ? accept : reject;

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
                .Where(e => e.EvaluationType.Equals(EvalTypes.Support, StringComparison.OrdinalIgnoreCase) ||
                            e.EvaluationType.Equals(EvalTypes.Strengthen, StringComparison.OrdinalIgnoreCase))
                .Select(e => e.EvaluationId)
                .ToList(),
            OpposingEvaluationIds = relevant
                .Where(e => e.EvaluationType.Equals(EvalTypes.Oppose, StringComparison.OrdinalIgnoreCase) ||
                            e.EvaluationType.Equals(EvalTypes.Caution, StringComparison.OrdinalIgnoreCase))
                .Select(e => e.EvaluationId)
                .ToList(),
            CreatedUtc = DateTime.UtcNow
        };
    }

    private static DecisionNode BuildSecurityControlsDecision(
        string runId,
        IReadOnlyCollection<AgentTask> tasks,
        IReadOnlyCollection<AgentEvaluation> evaluations)
    {
        // Include evaluations targeting any task, not just topology, so signals from
        // Compliance and Critic agents influence security control promotion.
        HashSet<string> taskIds = tasks.Select(t => t.TaskId).ToHashSet(StringComparer.Ordinal);
        List<AgentEvaluation> relevant = evaluations
            .Where(e => taskIds.Contains(e.TargetAgentTaskId))
            .ToList();

        bool promotePrivateEndpoints = relevant.Any(e =>
            e.EvaluationType.Equals(EvalTypes.Strengthen, StringComparison.OrdinalIgnoreCase) &&
            e.Rationale.Contains("private", StringComparison.OrdinalIgnoreCase));

        bool promoteManagedIdentity = relevant.Any(e =>
            e.EvaluationType.Equals(EvalTypes.Strengthen, StringComparison.OrdinalIgnoreCase) &&
            e.Rationale.Contains("managed identity", StringComparison.OrdinalIgnoreCase));

        List<string> controls = new();

        if (promotePrivateEndpoints)
            controls.Add("Private Endpoints");

        if (promoteManagedIdentity)
            controls.Add("Managed Identity");

        DecisionOption promote = new()
        {
            Description = controls.Count == 0
                ? "No control promotion"
                : $"Promote controls: {string.Join(", ", controls)}",
            BaseConfidence = controls.Count == 0 ? 0.30 : 0.80,
            SupportScore = relevant.Where(e => e.EvaluationType.Equals(EvalTypes.Strengthen, StringComparison.OrdinalIgnoreCase))
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
                .Where(e => e.EvaluationType.Equals(EvalTypes.Strengthen, StringComparison.OrdinalIgnoreCase))
                .Select(e => e.EvaluationId)
                .ToList(),
            CreatedUtc = DateTime.UtcNow
        };
    }

    private static DecisionNode BuildComplexityDecision(
        string runId,
        IReadOnlyCollection<AgentTask> tasks,
        IReadOnlyCollection<AgentEvaluation> evaluations)
    {
        // Include evaluations targeting any task so Critic/Compliance cautions contribute.
        HashSet<string> taskIds = tasks.Select(t => t.TaskId).ToHashSet(StringComparer.Ordinal);
        List<AgentEvaluation> relevant = evaluations
            .Where(e => taskIds.Contains(e.TargetAgentTaskId))
            .ToList();

        List<AgentEvaluation> cautions = relevant
            .Where(e => e.EvaluationType.Equals(EvalTypes.Caution, StringComparison.OrdinalIgnoreCase) ||
                        e.EvaluationType.Equals(EvalTypes.Oppose, StringComparison.OrdinalIgnoreCase))
            .ToList();

        DecisionOption keep = new()
        {
            Description = "Keep current solution complexity",
            BaseConfidence = 0.60,
            SupportScore = relevant.Where(e => e.EvaluationType.Equals(EvalTypes.Support, StringComparison.OrdinalIgnoreCase))
                .Sum(e => Math.Max(0, e.ConfidenceDelta)),
            OppositionScore = cautions.Sum(e => Math.Abs(e.ConfidenceDelta))
        };

        DecisionOption reduce = new()
        {
            Description = "Reduce complexity / consider MVP trimming",
            BaseConfidence = cautions.Count > 0 ? 0.65 : 0.20,
            SupportScore = cautions.Sum(e => Math.Abs(e.ConfidenceDelta)),
            OppositionScore = 0
        };

        DecisionOption selected = keep.FinalScore >= reduce.FinalScore ? keep : reduce;

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
                .Where(e => e.EvaluationType.Equals(EvalTypes.Support, StringComparison.OrdinalIgnoreCase))
                .Select(e => e.EvaluationId)
                .ToList(),
            OpposingEvaluationIds = cautions.Select(e => e.EvaluationId).ToList(),
            CreatedUtc = DateTime.UtcNow
        };
    }
}
