namespace ArchiForge.Decisioning.Findings.Payloads;

public class SecurityCoverageFindingPayload
{
    public int SecurityNodeCount { get; set; }
    public int ProtectedResourceCount { get; set; }
    public int UnprotectedResourceCount { get; set; }
    public List<string> UnprotectedResources { get; set; } = [];
}
