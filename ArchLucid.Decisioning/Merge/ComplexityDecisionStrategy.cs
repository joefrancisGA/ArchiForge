using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Decisions;

using EvalTypes = ArchLucid.Contracts.Decisions.EvaluationTypes;

namespace ArchLucid.Decisioning.Merge;

internal sealed class ComplexityDecisionStrategy : IDecisionStrategy
{
    /// <inheritdoc cref="IDecisionStrategy.Build(DecisionStrategyParameters)" />
    /// <remarks>
    ///     Produces a <c>ComplexityDisposition</c> node comparing “keep” vs “reduce” using evaluation deltas aggregated per
    ///     <see cref="DecisionOption.FinalScore" />. Requires <see cref="DecisionStrategyParameters.Tasks" />.
    /// </remarks>
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

        // BaseConfidence 0.60: deliberate “status quo” prior — retaining the current design is assumed moderately sound until
        // critics accumulate measurable delta. Chosen above 0.5 so modest support bumps help “keep” without requiring unanimity.
        DecisionOption keep = new()
        {
            Description = "Keep current solution complexity",
            BaseConfidence = 0.60,
            // SupportScore: only Support evaluations contribute; each adds max(0, ConfidenceDelta). With FinalScore = Base + Support - Opposition,
            // positive deltas raise the keep score; negative Support deltas are ignored so they cannot artificially prop “keep”.
            SupportScore = relevant.Where(e =>
                    e.EvaluationType.Equals(EvalTypes.Support, StringComparison.OrdinalIgnoreCase))
                .Sum(e => Math.Max(0, e.ConfidenceDelta)),
            // OppositionScore: Caution + Oppose feed Opposition with |ConfidenceDelta| so pushback subtracts from FinalScore linearly in magnitude.
            OppositionScore = cautions.Sum(e => Math.Abs(e.ConfidenceDelta))
        };

        // BaseConfidence 0.65 when cautions exist: intentional asymmetry — once any caution/opposition is present, “reduce” starts
        // slightly above “keep” (0.65 vs 0.60) so MVP trimming is preferred unless Support deltas decisively outweigh the gap.
        // BaseConfidence 0.20 when no cautions: “reduce” should almost never win without critic signals (low prior avoids noise-driven churn).
        DecisionOption reduce = new()
        {
            Description = "Reduce complexity / consider MVP trimming",
            BaseConfidence = cautions.Count > 0 ? 0.65 : 0.20,
            // SupportScore: here the strategy treats caution/opposition magnitude as evidence FOR reducing complexity (additive to “reduce”).
            // OppositionScore stays 0, so FinalScore = Base + sum(|delta|) for critics — a simple mirror of the penalty applied to “keep”.
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
