namespace ArchiForge.ArtifactSynthesis.Models;

public class UnresolvedIssueArtifactItem
{
    public string IssueType { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Severity { get; set; } = default!;
}
