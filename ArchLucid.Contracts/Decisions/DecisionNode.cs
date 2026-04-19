namespace ArchLucid.Contracts.Decisions;

/// <summary>
/// Represents a single architectural decision point produced by the decision engine
/// during a run. Each node captures the topic debated, the options evaluated, the
/// selected outcome, and the agent evaluations that influenced it.
/// </summary>
public sealed class DecisionNode
{
    /// <summary>Unique identifier for this decision node.</summary>
    public string DecisionId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>The architecture run this decision belongs to.</summary>
    public string RunId { get; set; } = string.Empty;

    /// <summary>Short label describing the architectural question being decided (e.g., <c>TopologyAcceptance</c>).</summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>All options that were evaluated for this decision point.</summary>
    public IReadOnlyList<DecisionOption> Options { get; set; } = [];

    /// <summary>
    /// The <see cref="DecisionOption.OptionId"/> of the selected option,
    /// or <see langword="null"/> when no option reached the confidence threshold.
    /// </summary>
    public string? SelectedOptionId
    {
        get; set;
    }

    /// <summary>Free-text rationale explaining why the selected option was chosen.</summary>
    public string Rationale { get; set; } = string.Empty;

    /// <summary>
    /// Aggregate confidence score for the selected option, in the range [0, 1].
    /// Computed from <see cref="DecisionOption.FinalScore"/> after evaluation weighting.
    /// </summary>
    public double Confidence
    {
        get; set;
    }

    /// <summary>Identifiers of <see cref="AgentEvaluation"/> records that supported the selected option.</summary>
    public IReadOnlyList<string> SupportingEvaluationIds { get; set; } = [];

    /// <summary>Identifiers of <see cref="AgentEvaluation"/> records that opposed the selected option.</summary>
    public IReadOnlyList<string> OpposingEvaluationIds { get; set; } = [];

    /// <summary>UTC timestamp when this decision node was recorded.</summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

