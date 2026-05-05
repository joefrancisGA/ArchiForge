using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Decisions;

using EvalTypes = ArchLucid.Contracts.Decisions.EvaluationTypes;

namespace ArchLucid.Decisioning.Merge;

internal sealed class SecurityControlsDecisionStrategy : IDecisionStrategy
{
    public DecisionNode Build(DecisionStrategyParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(parameters.Tasks);

        string runId = parameters.RunId;
        IReadOnlyCollection<AgentTask> tasks = parameters.Tasks;
        IReadOnlyCollection<AgentEvaluation> evaluations = parameters.Evaluations;
        TimeProvider clock = parameters.Clock;

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

        List<string> controls = [];

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
            SupportScore = relevant.Where(e =>
                    e.EvaluationType.Equals(EvalTypes.Strengthen, StringComparison.OrdinalIgnoreCase))
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
            CreatedUtc = clock.GetUtcNow().UtcDateTime
        };
    }
}
