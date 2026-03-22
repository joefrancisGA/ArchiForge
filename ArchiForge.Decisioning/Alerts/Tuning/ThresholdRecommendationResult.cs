namespace ArchiForge.Decisioning.Alerts.Tuning;

public class ThresholdRecommendationResult
{
    public DateTime EvaluatedUtc { get; set; } = DateTime.UtcNow;

    public string RuleKind { get; set; } = null!;
    public string TunedMetricType { get; set; } = null!;

    public ThresholdCandidateEvaluation? RecommendedCandidate { get; set; }

    public List<string> SummaryNotes { get; set; } = [];
    public List<ThresholdCandidateEvaluation> Candidates { get; set; } = [];
}
