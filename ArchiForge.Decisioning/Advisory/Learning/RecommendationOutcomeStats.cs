namespace ArchiForge.Decisioning.Advisory.Learning;

public class RecommendationOutcomeStats
{
    public string Key { get; set; } = null!;

    public int ProposedCount { get; set; }
    public int AcceptedCount { get; set; }
    public int RejectedCount { get; set; }
    public int DeferredCount { get; set; }
    public int ImplementedCount { get; set; }

    public double AcceptanceRate =>
        ProposedCount == 0 ? 0 : (double)AcceptedCount / ProposedCount;

    public double RejectionRate =>
        ProposedCount == 0 ? 0 : (double)RejectedCount / ProposedCount;

    public double DeferredRate =>
        ProposedCount == 0 ? 0 : (double)DeferredCount / ProposedCount;

    public double ImplementationRate =>
        ProposedCount == 0 ? 0 : (double)ImplementedCount / ProposedCount;
}
