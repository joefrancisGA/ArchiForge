namespace ArchiForge.Decisioning.Findings.Payloads;

public class TopologyCoverageFindingPayload
{
    public int TopologyNodeCount { get; set; }

    public List<string> PresentCategories { get; set; } = [];

    public List<string> MissingCategories { get; set; } = [];
}
