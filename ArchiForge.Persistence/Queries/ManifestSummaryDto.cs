namespace ArchiForge.Persistence.Queries;

public class ManifestSummaryDto
{
    public Guid ManifestId { get; set; }
    public Guid RunId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public string ManifestHash { get; set; } = null!;
    public string RuleSetId { get; set; } = null!;
    public string RuleSetVersion { get; set; } = null!;
    public int DecisionCount { get; set; }
    public int WarningCount { get; set; }
    public int UnresolvedIssueCount { get; set; }
    public string Status { get; set; } = null!;
}
