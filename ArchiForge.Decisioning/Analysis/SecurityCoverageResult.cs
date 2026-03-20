namespace ArchiForge.Decisioning.Analysis;

public class SecurityCoverageResult
{
    public int SecurityNodeCount { get; set; }

    public int ProtectedResourceCount { get; set; }

    public int UnprotectedResourceCount { get; set; }

    public List<string> ProtectedResources { get; set; } = [];

    public List<string> UnprotectedResources { get; set; } = [];
}
