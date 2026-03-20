namespace ArchiForge.Decisioning.Analysis;

public class TopologyCoverageResult
{
    public bool HasNetwork { get; set; }

    public bool HasCompute { get; set; }

    public bool HasStorage { get; set; }

    public bool HasData { get; set; }

    public List<string> PresentCategories { get; set; } = [];

    public List<string> MissingCategories { get; set; } = [];

    public int TopologyNodeCount { get; set; }
}
