namespace ArchiForge.Decisioning.Analysis;

public class PolicyCoverageResult
{
    public int PolicyNodeCount { get; set; }
    public int PolicyApplicabilityEdgeCount { get; set; }
    public List<string> Policies { get; set; } = [];
    public List<string> UncoveredResources { get; set; } = [];
}
