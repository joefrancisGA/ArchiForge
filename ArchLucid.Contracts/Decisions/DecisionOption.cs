namespace ArchiForge.Contracts.Decisions;

/// <summary>
/// Represents a single candidate option within a <see cref="DecisionNode"/>.
/// Scores are accumulated from agent evaluations and used by the decision engine
/// to select the best option.
/// </summary>
public sealed class DecisionOption
{
    /// <summary>Unique identifier for this option within its <see cref="DecisionNode"/>.</summary>
    public string OptionId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Human-readable description of the architectural choice this option represents.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Initial confidence assigned to this option before evaluations are applied.
    /// Typically seeded by the proposing agent.
    /// </summary>
    public double BaseConfidence { get; set; }

    /// <summary>
    /// Cumulative score added by <see cref="EvaluationTypes.Support"/> and
    /// <see cref="EvaluationTypes.Strengthen"/> evaluations.
    /// </summary>
    public double SupportScore { get; set; }

    /// <summary>
    /// Cumulative score subtracted by <see cref="EvaluationTypes.Oppose"/> and
    /// <see cref="EvaluationTypes.Caution"/> evaluations.
    /// </summary>
    public double OppositionScore { get; set; }

    /// <summary>
    /// Computed score used to rank options: <c>BaseConfidence + SupportScore − OppositionScore</c>.
    /// Higher is better.
    /// </summary>
    public double FinalScore => BaseConfidence + SupportScore - OppositionScore;

    /// <summary>References to evidence items (policy IDs, service catalog IDs) that back this option.</summary>
    public List<string> EvidenceRefs { get; set; } = [];
}

