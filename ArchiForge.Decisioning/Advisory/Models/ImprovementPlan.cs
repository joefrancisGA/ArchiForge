namespace ArchiForge.Decisioning.Advisory.Models;

public class ImprovementPlan
{
    public Guid RunId { get; set; }
    public Guid? ComparedToRunId { get; set; }

    public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;

    public List<ImprovementRecommendation> Recommendations { get; set; } = [];
    public List<string> SummaryNotes { get; set; } = [];

    /// <summary>Merged <c>advisoryDefaults</c> from effective policy packs (optional keys for advisory/digest tooling).</summary>
    public Dictionary<string, string> PolicyPackAdvisoryDefaults { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}
