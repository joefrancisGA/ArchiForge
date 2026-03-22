namespace ArchiForge.Decisioning.Advisory.Learning;

public class AdaptiveScoringInput
{
    public string Category { get; set; } = null!;
    public string Urgency { get; set; } = null!;
    public string? SignalType { get; set; }

    public int BasePriorityScore { get; set; }
}
