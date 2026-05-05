using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Decisions;

using EvalTypes = ArchLucid.Contracts.Decisions.EvaluationTypes;

namespace ArchLucid.Decisioning.Merge;

internal sealed class ComplexityDecisionStrategy : IDecisionStrategy
{
    public DecisionNode Build(DecisionStrategyParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(parameters.Tasks);

        string runId = parameters.RunId;
        IReadOnlyCollection<AgentTask> tasks = parameters.Tasks;
        IReadOnlyCollection<AgentEvaluation> evaluations = parameters.Evaluations;
        TimeProvider clock = parameters.Clock;

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
            SupportScore = relevant.Where(e =>
                    e.EvaluationType.Equals(EvalTypes.Support, StringComparison.OrdinalIgnoreCase))
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
            CreatedUtc = clock.GetUtcNow().UtcDateTime
        };
    }
}
