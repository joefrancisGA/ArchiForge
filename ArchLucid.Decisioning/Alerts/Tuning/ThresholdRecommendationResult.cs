namespace ArchiForge.Decisioning.Alerts.Tuning;

/// <summary>Output of <see cref="IThresholdRecommendationService.RecommendAsync"/> with ranked candidates.</summary>
public class ThresholdRecommendationResult
{
    /// <summary>When scoring completed.</summary>
    public DateTime EvaluatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Echo of request rule kind.</summary>
    public string RuleKind { get; set; } = null!;

    /// <summary>Echo of tuned metric id.</summary>
    public string TunedMetricType { get; set; } = null!;

    /// <summary>Best-scoring candidate after heuristic ranking; null if no candidates evaluated.</summary>
    public ThresholdCandidateEvaluation? RecommendedCandidate { get; set; }

    /// <summary>Overall notes (e.g. empty candidate list).</summary>
    public List<string> SummaryNotes { get; set; } = [];

    /// <summary>One entry per simulated threshold with simulation + score.</summary>
    public List<ThresholdCandidateEvaluation> Candidates { get; set; } = [];
}
