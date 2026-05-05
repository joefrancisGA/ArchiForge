using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Decisions;

using EvalTypes = ArchLucid.Contracts.Decisions.EvaluationTypes;

namespace ArchLucid.Decisioning.Merge;

internal sealed class TopologyAcceptanceDecisionStrategy : IDecisionStrategy
{
    public DecisionNode Build(DecisionStrategyParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(parameters.TopologyTask);
        ArgumentNullException.ThrowIfNull(parameters.TopologyResult);

        string runId = parameters.RunId;
        AgentTask topologyTask = parameters.TopologyTask;
        AgentResult topologyResult = parameters.TopologyResult;
        IReadOnlyCollection<AgentEvaluation> evaluations = parameters.Evaluations;
        TimeProvider clock = parameters.Clock;

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
            EvidenceRefs = relevant.SelectMany(e => e.EvidenceRefs).Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        };

        DecisionOption reject = new() { Description = "Reject topology proposal", BaseConfidence = 0.10, SupportScore = opposition, OppositionScore = support };

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
            CreatedUtc = clock.GetUtcNow().UtcDateTime
        };
    }
}
