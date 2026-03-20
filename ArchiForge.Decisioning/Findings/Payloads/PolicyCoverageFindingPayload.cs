namespace ArchiForge.Decisioning.Findings.Payloads;

public class PolicyCoverageFindingPayload
{
    public int PolicyNodeCount { get; set; }

    public int PolicyApplicabilityEdgeCount { get; set; }

    public List<string> UncoveredResources { get; set; } = [];
}
