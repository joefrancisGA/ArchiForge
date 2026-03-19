namespace ArchiForge.Decisioning.Manifest.Sections;

public class TopologySection
{
    public List<string> SelectedPatterns { get; set; } = [];
    public List<string> Resources { get; set; } = [];
    public List<string> Gaps { get; set; } = [];
}

