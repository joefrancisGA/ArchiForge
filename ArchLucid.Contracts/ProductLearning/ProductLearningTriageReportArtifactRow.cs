namespace ArchiForge.Contracts.ProductLearning;

/// <summary>One artifact facet row in a triage/export report (counts only — no raw comments).</summary>
public sealed class ProductLearningTriageReportArtifactRow
{
    public string ArtifactLabel { get; init; } = string.Empty;
    public int Trusted { get; init; }
    public int Revised { get; init; }
    public int Rejected { get; init; }
    public int FollowUp { get; init; }
    public int Runs { get; init; }

    /// <summary>Optional short hint when the pipeline surfaced a repeat theme (not full text).</summary>
    public string? ThemeHint { get; init; }
}
