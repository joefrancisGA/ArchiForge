namespace ArchiForge.Decisioning.Manifest.Sections;

public class ManifestIssue
{
    public string IssueType { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Severity { get; set; } = null!;
    public List<string> SupportingFindingIds { get; set; } = [];
}

