namespace ArchiForge.Decisioning.Findings.Payloads;

public class ComplianceFindingPayload
{
    public string RulePackId { get; set; } = default!;

    public string RulePackVersion { get; set; } = default!;

    public string RuleId { get; set; } = default!;

    public string ControlId { get; set; } = default!;

    public string ControlName { get; set; } = default!;

    public string AppliesToCategory { get; set; } = default!;

    public List<string> AffectedResources { get; set; } = [];
}
