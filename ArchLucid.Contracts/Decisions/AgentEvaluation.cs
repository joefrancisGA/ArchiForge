namespace ArchiForge.Contracts.Decisions;

/// <summary>
/// A lightweight, deterministic evaluation that supports or opposes a specific decision topic/option.
/// </summary>
public sealed class AgentEvaluation
{
    public string EvaluationId { get; set; } = Guid.NewGuid().ToString("N");
    public string RunId { get; set; } = string.Empty;

    /// <summary>The agent task this evaluation targets.</summary>
    public string TargetAgentTaskId { get; set; } = string.Empty;

    /// <summary>
    /// Evaluation stance. Use values from <see cref="EvaluationTypes"/>:
    /// <c>support</c>, <c>strengthen</c>, <c>oppose</c>, or <c>caution</c>.
    /// </summary>
    public string EvaluationType { get; set; } = string.Empty;

    /// <summary>Signed delta. Oppose/caution should be negative or will be treated as absolute opposition.</summary>
    public double ConfidenceDelta { get; set; }
    public string Rationale { get; set; } = string.Empty;
    public List<string> EvidenceRefs { get; set; } = [];
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

