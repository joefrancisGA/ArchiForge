namespace ArchiForge.Decisioning.Findings.Payloads;

public class PolicyApplicabilityFindingPayload
{
    public string PolicyName { get; set; } = "";

    public string? PolicyReference { get; set; }

    public int ApplicableTopologyResourceCount { get; set; }

    public List<string> ApplicableTopologyNodeIds { get; set; } = [];
}
