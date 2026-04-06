namespace ArchiForge.ArtifactSynthesis.Models;

public class CoverageSummaryArtifactModel
{
    public int CoveredRequirementCount { get; set; }
    public int UncoveredRequirementCount { get; set; }
    public int SecurityGapCount { get; set; }
    public int ComplianceGapCount { get; set; }
    public int UnresolvedIssueCount { get; set; }
    public List<string> TopologyGaps { get; set; } = [];
}
