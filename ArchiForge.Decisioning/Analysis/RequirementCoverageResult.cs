namespace ArchiForge.Decisioning.Analysis;

public class RequirementCoverageResult
{
    public int RequirementNodeCount { get; set; }

    public int RelatedRequirementCount { get; set; }

    public int UnrelatedRequirementCount { get; set; }

    public List<string> CoveredRequirements { get; set; } = [];

    public List<string> UncoveredRequirements { get; set; } = [];
}
