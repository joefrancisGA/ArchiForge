namespace ArchiForge.Decisioning.Findings.Payloads;

public class RequirementFindingPayload
{
    public string RequirementText { get; set; } = null!;
    public string RequirementName { get; set; } = null!;
    public bool IsMandatory { get; set; }
}

