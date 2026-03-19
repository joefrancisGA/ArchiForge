namespace ArchiForge.Decisioning.Models;

public class RequirementCoverageItem
{
    public string RequirementName { get; set; } = default!;
    public string RequirementText { get; set; } = default!;
    public bool IsMandatory { get; set; }
    public string CoverageStatus { get; set; } = default!;
    public List<string> SupportingFindingIds { get; set; } = [];
}

