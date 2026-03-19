namespace ArchiForge.Decisioning.Models;

public class ManifestIssue
{
    public string IssueType { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Severity { get; set; } = default!;
    public List<string> SupportingFindingIds { get; set; } = [];
}

